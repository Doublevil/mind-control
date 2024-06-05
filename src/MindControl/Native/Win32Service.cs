using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MindControl.Results;

namespace MindControl.Native;

/// <summary>
/// Contains DllImports for Windows functions required internally by other components.
/// </summary>
public partial class Win32Service : IOperatingSystemService
{
    private SystemInfo? _systemInfo;

    /// <summary>
    /// Builds and returns a failure object representing the last Win32 error that occurred.
    /// </summary>
    private static OperatingSystemCallFailure GetLastSystemError()
    {
        int errorCode = Marshal.GetLastWin32Error();
        return new OperatingSystemCallFailure(errorCode, new Win32Exception(errorCode).Message);
    }
    
    /// <summary>
    /// Opens the process with the given identifier, in a way that allows memory manipulation.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>A result holding either the handle of the opened process, or a system failure.</returns>
    public Result<IntPtr, SystemFailure> OpenProcess(int pid)
    {
        var handle = OpenProcess(0x1F0FFF, true, pid);
        if (handle == IntPtr.Zero)
            return GetLastSystemError();

        return handle;
    }

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>A result holding either a boolean indicating if the process is 64-bits, or a system failure.</returns>
    public Result<bool, SystemFailure> IsProcess64Bits(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!IsWow64Process(process.Handle, out bool isWow64))
                return GetLastSystemError();
        
            // Process is 64 bits if we are running a 64-bits system and the process is NOT in wow64.
            return !isWow64 && IsSystem64Bits();
        }
        catch (Exception)
        {
            return new SystemFailureOnInvalidArgument(nameof(pid),
                $"The process of PID {pid} was not found. Check that the process is running.");
        }
    }

    /// <summary>
    /// Returns a value indicating if the system is 64 bits or not.
    /// </summary>
    /// <returns>A boolean indicating if the system is 64 bits or not.</returns>
    private bool IsSystem64Bits() => IntPtr.Size == 8;

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>A result holding either an array of bytes containing the data read from the process memory, or a
    /// system failure.</returns>
    public Result<byte[], SystemFailure> ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        
        var result = new byte[length];
        int returnValue = ReadProcessMemory(processHandle, baseAddress, result, length, out _);

        return returnValue == 0 ? GetLastSystemError() : result;
    }
    
    /// <summary>
    /// Reads a targeted range of the memory of a specified process into the given buffer. Supports partial reads, in
    /// case the full length failed to be read but at least one byte was successfully copied into the buffer.
    /// Prefer <see cref="ReadProcessMemory"/> when you know the length of the data to read.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="buffer">Buffer to store the data read from the memory. The buffer must be large enough to store
    /// the data read.</param>
    /// <param name="offset">Offset in the buffer where the data will be stored.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>A result holding either the number of bytes actually read from memory, or a system failure.</returns>
    public Result<ulong, SystemFailure> ReadProcessMemoryPartial(IntPtr processHandle, UIntPtr baseAddress,
        byte[] buffer, int offset, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if ((ulong)buffer.Length < (ulong)offset + length)
            return new SystemFailureOnInvalidArgument(nameof(buffer),
                "The buffer is too small to store the requested number of bytes.");
        if (UIntPtr.MaxValue.ToUInt64() - baseAddress.ToUInt64() < length)
            return new SystemFailureOnInvalidArgument(nameof(length),
                "The base address plus the length to read exceeds the maximum possible address.");
        
        // We need to take in account the offset, meaning we can only write to the buffer from a certain position,
        // defined as the "offset" parameter.
        // This is a problem, because the Win32 API doesn't have that. It starts writing from the beginning of whatever
        // buffer you pass in.
        // But in fact, as with any array, the Win32 API sees the buffer as a pointer. This means we can use an
        // alternative signature that uses a UIntPtr as the buffer instead of a byte array.
        // Which, in turns, means that we can call it with the address of a specific element in the buffer, and the API
        // will start writing from there.
        
        // To get the pointer to the right element in the buffer, we first have to pin the buffer in memory.
        // This ensures that the garbage collector doesn't move the buffer around while we're working with it.
        var bufferGcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            // Then we use a Marshal method to get a pointer to the element in the buffer at the given offset.
            var bufferPtr = (UIntPtr)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset).ToInt64();
            
            // Finally, we can call the Win32 API with the pointer to the right element in the buffer.
            int returnValue = ReadProcessMemory(processHandle, baseAddress, bufferPtr, length, out var bytesRead);

            // If the function is a success or read at least one byte, we return the number of bytes read.
            if (bytesRead != UIntPtr.Zero || returnValue != 0)
                return bytesRead.ToUInt64();
            var initialReadError = GetLastSystemError();
            
            // If we are here, we know that the function failed, and also didn't read anything.
            // This may mean that the whole range is unreadable, but might also mean that only part of it is.
            // Sadly, the Win32 API will fail in this way even if only one byte is unreadable.
            // The strategy now is going to be to query for memory regions in the range, and determine at which point
            // the memory that we want stops being readable. We then read up to that point.
            // We do that extended procedure only if the first read failed. This should make most cases faster.
            
            // Edge case: if we attempted to read only one byte, we won't be able to reduce the range to read, so we
            // return immediately with the first reading error.
            if (length == 1)
                return initialReadError;

            // Determine if the process is 64 bits
            if (!IsWow64Process(processHandle, out bool isWow64))
                return GetLastSystemError();
            bool is64Bits = !isWow64 && IsSystem64Bits();
            
            // Build the memory range that spans across everything we attempted to read
            var range = MemoryRange.FromStartAndSize(baseAddress, length);
            
            // Determine the last readable address within the range
            var lastReadableAddress = GetLastConsecutiveReadableAddressWithinRange(processHandle, is64Bits, range);
            
            // If we couldn't determine the last readable address, or it matches/exceeds the end of the range, there
            // is no point in trying to read again. Return the initial read error.
            if (lastReadableAddress == null || lastReadableAddress.Value.ToUInt64() >= range.End.ToUInt64())
                return initialReadError;
            
            // If we found a readable address within the range, we read up to that point.
            var newLength = lastReadableAddress.Value.ToUInt64() - baseAddress.ToUInt64();
            returnValue = ReadProcessMemory(processHandle, baseAddress, bufferPtr, newLength, out bytesRead);

            // If the function failed again and didn't read any byte again, return the read error.
            if (bytesRead == UIntPtr.Zero && returnValue == 0)
                return GetLastSystemError();

            // In other cases, we return the number of bytes read.
            return bytesRead.ToUInt64();
        }
        finally
        {
            // After using the pinned buffer, we must free it, so that the garbage collector can handle it again.
            bufferGcHandle.Free();
        }
    }

    /// <summary>
    /// Given a range, checks memory regions within that range starting from the start of the range upwards, and returns
    /// the end address of the last consecutive readable region. If the start of the range is unreadable, returns null.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.</param>
    /// <param name="range">Memory range to check for readable regions.</param>
    /// <returns>The end address of the last consecutive readable region within the range, or null if the start of the
    /// range is unreadable.</returns>
    private UIntPtr? GetLastConsecutiveReadableAddressWithinRange(IntPtr processHandle, bool is64Bits,
        MemoryRange range)
    {
        var applicationMemoryLimit = GetFullMemoryRange().End;
        ulong rangeEnd = Math.Min(range.End.ToUInt64(), applicationMemoryLimit.ToUInt64());
        UIntPtr currentAddress = range.Start;
        while (currentAddress.ToUInt64() <= rangeEnd)
        {
            var getRegionResult = GetRegionMetadata(processHandle, currentAddress, is64Bits);
            
            // If we failed to get the region metadata, stop iterating.
            if (getRegionResult.IsFailure)
                break;

            var currentRangeMetadata = getRegionResult.Value;
            
            // If the current region is not readable, stop iterating.
            if (!currentRangeMetadata.IsReadable)
                break;
            
            // Keep iterating to the next region.
            currentAddress = (UIntPtr)(currentRangeMetadata.StartAddress.ToUInt64()
                + currentRangeMetadata.Size.ToUInt64());
        }

        // If the current address is still the same as the start of the range, it means the start of the range is
        // unreadable.
        if (currentAddress == range.Start)
            return null;
        
        return currentAddress - 1;
    }

    /// <summary>
    /// Overwrites the memory protection of the page that the given address is part of.
    /// Returns the memory protection that was effective on the page before being changed.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.
    /// The handle must have PROCESS_VM_OPERATION access.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.</param>
    /// <param name="targetAddress">An address in the target page.</param>
    /// <param name="newProtection">New protection value for the page.</param>
    /// <returns>A result holding either the memory protection value that was effective on the page before being
    /// changed, or a system failure.</returns>
    public Result<MemoryProtection, SystemFailure> ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bits,
        UIntPtr targetAddress, MemoryProtection newProtection)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (targetAddress == UIntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(targetAddress),
                "The target address cannot be a zero pointer.");

        var result = VirtualProtectEx(processHandle, targetAddress, (IntPtr)(is64Bits ? 8 : 4), newProtection,
            out var previousProtection);

        return result ? previousProtection : GetLastSystemError();
    }

    /// <summary>
    /// Writes the given bytes into the memory of the specified process, at the target address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access.</param>
    /// <param name="targetAddress">Base address in the memory of the process to which data will be written.</param>
    /// <param name="value">Byte array to write in the memory. It is assumed that the entire array will be
    /// written, unless a size is specified.</param>
    /// <param name="size">Specify this value if you only want to write part of the value array in memory.
    /// This parameter is useful when using buffer byte arrays. Leave it to null to use the entire array.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    public Result<SystemFailure> WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, byte[] value, int? size = null)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (targetAddress == UIntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(targetAddress),
                "The target address cannot be a zero pointer.");
        if (size != null && size.Value > value.Length)
            return new SystemFailureOnInvalidArgument(nameof(size),
                "The size cannot exceed the length of the value array.");

        var result = WriteProcessMemory(processHandle, targetAddress, value, (UIntPtr)(size ?? value.Length),
            out _);

        return result ? Result<SystemFailure>.Success : GetLastSystemError();
    }

    /// <summary>
    /// Writes the given bytes into the memory of the specified process, at the target address. Supports partial reads,
    /// in case the full length failed to be written but at least one byte was successfully written.
    /// Prefer <see cref="IOperatingSystemService.WriteProcessMemory"/> in most cases.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access.</param>
    /// <param name="targetAddress">Base address in the memory of the process to which data will be written.</param>
    /// <param name="buffer">Byte array to write in the memory. Depending on the <paramref name="offset"/> and
    /// <paramref name="size"/> parameters, only part of the buffer may be copied into the process memory.</param>
    /// <param name="offset">Offset in the buffer where the data to write starts.</param>
    /// <param name="size">Number of bytes to write from the buffer into the process memory, starting from the
    /// <paramref name="offset"/>.</param>
    /// <returns>A result holding either the number of bytes written, or a system failure when no bytes were written.
    /// </returns>
    public Result<ulong, SystemFailure> WriteProcessMemoryPartial(IntPtr processHandle, UIntPtr targetAddress,
        byte[] buffer, int offset, int size)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (buffer.Length < offset + size)
            return new SystemFailureOnInvalidArgument(nameof(buffer),
                "The buffer is too small to write the requested number of bytes.");
        
        // We need to take in account the offset, meaning we can only copy from the buffer from a certain position,
        // defined as the "offset" parameter.
        // This is a problem, because the Win32 API doesn't have that. It starts copying from the beginning of whatever
        // buffer you pass in.
        // But in fact, as with any array, the Win32 API sees the buffer as a pointer. This means we can use an
        // alternative signature that uses a UIntPtr as the buffer instead of a byte array.
        // Which, in turns, means that we can call it with the address of a specific element in the buffer, and the API
        // will start copying from there.
        
        // To get the pointer to the right element in the buffer, we first have to pin the buffer in memory.
        // This ensures that the garbage collector doesn't move the buffer around while we're working with it.
        var bufferGcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            // Then we use a Marshal method to get a pointer to the element in the buffer at the given offset.
            var bufferPtr = (UIntPtr)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset).ToInt64();
            
            // Finally, we can call the Win32 API with the pointer to the right element in the buffer.
            bool returnValue = WriteProcessMemory(processHandle, targetAddress, bufferPtr, (UIntPtr)size,
                out var bytesWritten);

            // Only return a failure when the function failed AND didn't write anything.
            // This ensures that a non-error result is returned for a partial write.
            if (bytesWritten == UIntPtr.Zero && !returnValue)
                return GetLastSystemError();

            return bytesWritten.ToUInt64();
        }
        finally
        {
            // After using the pinned buffer, we must free it, so that the garbage collector can handle it again.
            bufferGcHandle.Free();
        }
    }

    /// <summary>
    /// Allocates memory in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a system failure.</returns>
    public Result<UIntPtr, SystemFailure> AllocateMemory(IntPtr processHandle, int size,
        MemoryAllocationType allocationType, MemoryProtection protection)
        => AllocateMemory(processHandle, UIntPtr.Zero, size, allocationType, protection);
    
    /// <summary>
    /// Allocates memory in the specified process at the specified address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="address">Address where the memory will be allocated.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a system failure.</returns>
    public Result<UIntPtr, SystemFailure> AllocateMemory(IntPtr processHandle, UIntPtr address, int size,
        MemoryAllocationType allocationType, MemoryProtection protection)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (size <= 0)
            return new SystemFailureOnInvalidArgument(nameof(size), "The size to allocate must be strictly positive.");
        
        var result = VirtualAllocEx(processHandle, address, (uint)size, (uint)allocationType, (uint)protection);
        return result == UIntPtr.Zero ? GetLastSystemError() : result;
    }

    /// <summary>
    /// Gets the address of a function in the specified module.
    /// </summary>
    /// <param name="moduleName">Name or path of the module. This module must be loaded in the current process.</param>
    /// <param name="functionName">Name of the target function in the specified module.</param>
    /// <returns>A result holding the address of the function if located, or a system failure otherwise.</returns>
    private Result<UIntPtr, SystemFailure> GetFunctionAddress(string moduleName, string functionName)
    {
        var moduleHandle = GetModuleHandle(moduleName);
        if (moduleHandle == IntPtr.Zero)
            return GetLastSystemError();
        
        var functionAddress = GetProcAddress(moduleHandle, functionName);
        if (functionAddress == UIntPtr.Zero)
            return GetLastSystemError();

        return functionAddress;
    }

    /// <summary>
    /// Gets the address of the function used to load a library in the current process.
    /// </summary>
    /// <returns>A result holding either the address of the function, or a system failure.</returns>
    public Result<UIntPtr, SystemFailure> GetLoadLibraryFunctionAddress()
        => GetFunctionAddress("kernel32.dll", "LoadLibraryW");

    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>A result holding either the handle of the thread, or a system failure.</returns>
    public Result<IntPtr, SystemFailure> CreateRemoteThread(IntPtr processHandle, UIntPtr startAddress,
        UIntPtr parameterAddress)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (startAddress == UIntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(startAddress),
                "The start address is invalid (zero pointer).");

        var result = CreateRemoteThread(processHandle, IntPtr.Zero, 0, startAddress, parameterAddress, 0, out _);
        if (result == IntPtr.Zero)
            return GetLastSystemError();

        return result;
    }

    /// <summary>
    /// Waits for the specified thread to finish execution.
    /// </summary>
    /// <param name="threadHandle">Handle of the target thread.</param>
    /// <param name="timeout">Maximum time to wait for the thread to finish.</param>
    /// <returns>A result holding either a boolean indicating if the thread returned (True) or timed out (False), or a
    /// system failure for other error cases.</returns>
    public Result<bool, SystemFailure> WaitThread(IntPtr threadHandle, TimeSpan timeout)
    {
        if (threadHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(threadHandle),
                "The thread handle is invalid (zero pointer).");

        uint result = WaitForSingleObject(threadHandle, (uint)timeout.TotalMilliseconds);
        if (WaitForSingleObjectResult.IsSuccessful(result))
            return true;
        if (result == WaitForSingleObjectResult.Timeout)
            return false;

        return GetLastSystemError();
    }
    
    /// <summary>
    /// Frees the memory allocated in the specified process for a region or a placeholder.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="regionBaseAddress">Base address of the region or placeholder to free, as returned by the memory
    /// allocation methods.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    public Result<SystemFailure> ReleaseMemory(IntPtr processHandle, UIntPtr regionBaseAddress)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (regionBaseAddress == UIntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(regionBaseAddress),
                "The region base address is invalid (zero pointer).");

        return VirtualFreeEx(processHandle, regionBaseAddress, 0, (uint)MemoryFreeType.Release)
            ? Result<SystemFailure>.Success : GetLastSystemError();
    }

    /// <summary>
    /// Closes the given handle.
    /// </summary>
    /// <param name="handle">Handle to close.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    public Result<SystemFailure> CloseHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(handle), "The handle is invalid (zero pointer).");

        return WinCloseHandle(handle) ? Result<SystemFailure>.Success : GetLastSystemError();
    }

    /// <summary>
    /// Gets information about the current system.
    /// </summary>
    private SystemInfo GetSystemInfo()
    {
        if (_systemInfo != null)
            return _systemInfo.Value;

        GetSystemInfo(out var info);
        _systemInfo = info;
        
        return info;
    }
    
    /// <summary>
    /// Gets the range of memory addressable by applications in the current system.
    /// </summary>
    public MemoryRange GetFullMemoryRange()
    {
        var systemInfo = GetSystemInfo();
        return new MemoryRange(systemInfo.MinimumApplicationAddress, systemInfo.MaximumApplicationAddress);
    }

    /// <summary>
    /// Gets the allocation granularity (minimal allocation size) of the system.
    /// </summary>
    public uint GetAllocationGranularity() => GetSystemInfo().AllocationGranularity;

    /// <summary>
    /// Gets the page size of the system.
    /// </summary>
    public uint GetPageSize() => GetSystemInfo().PageSize;

    /// <summary>
    /// Gets the metadata of a memory region in the virtual address space of a process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="baseAddress">Base address of the target memory region.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.</param>
    /// <returns>A result holding either the metadata of the target memory region, or a system failure.</returns>
    public Result<MemoryRangeMetadata, SystemFailure> GetRegionMetadata(IntPtr processHandle, UIntPtr baseAddress,
        bool is64Bits)
    {
        MemoryBasicInformation memoryBasicInformation;
        if (is64Bits)
        {
            // Use the 64-bit variant of the structure.
            var memInfo64 = new MemoryBasicInformation64();
            if (VirtualQueryEx(processHandle, baseAddress, out memInfo64,
                    (UIntPtr)Marshal.SizeOf(memInfo64)) == UIntPtr.Zero)
                return GetLastSystemError();

            memoryBasicInformation = new MemoryBasicInformation((UIntPtr)memInfo64.BaseAddress,
                (UIntPtr)memInfo64.AllocationBase, memInfo64.AllocationProtect, (UIntPtr)memInfo64.RegionSize,
                memInfo64.State, memInfo64.Protect, memInfo64.Type);
        }
        else
        {
            // Use the 32-bits variant of the structure.
            var memInfo32 = new MemoryBasicInformation32();
            if (VirtualQueryEx(processHandle, baseAddress, out memInfo32,
                    (UIntPtr)Marshal.SizeOf(memInfo32)) == UIntPtr.Zero)
                return GetLastSystemError();

            memoryBasicInformation = new MemoryBasicInformation((UIntPtr)memInfo32.BaseAddress,
                (UIntPtr)memInfo32.AllocationBase, memInfo32.AllocationProtect, (UIntPtr)memInfo32.RegionSize,
                memInfo32.State, memInfo32.Protect, memInfo32.Type);
        }
        
        // In the end, we have a bitness-agnostic structure that we can use to build the metadata.
        return new MemoryRangeMetadata
        {
            StartAddress = memoryBasicInformation.BaseAddress,
            Size = memoryBasicInformation.RegionSize,
            IsCommitted = memoryBasicInformation.State == MemoryState.Commit,
            IsFree = memoryBasicInformation.State == MemoryState.Free,
            IsProtected = memoryBasicInformation.Protect.HasFlag(MemoryProtection.PageGuard)
                          || memoryBasicInformation.Protect.HasFlag(MemoryProtection.NoAccess),
            IsMapped = memoryBasicInformation.Type == PageType.Mapped,
            IsReadable = memoryBasicInformation.Protect.HasFlag(MemoryProtection.ReadOnly) 
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ReadWrite)
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteRead)
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteReadWrite),
            IsWritable = memoryBasicInformation.Protect.HasFlag(MemoryProtection.ReadWrite)
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.WriteCopy)
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteReadWrite)
                         || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteWriteCopy),
            IsExecutable = memoryBasicInformation.Protect.HasFlag(MemoryProtection.Execute)
                           || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteRead)
                           || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteReadWrite)
                           || memoryBasicInformation.Protect.HasFlag(MemoryProtection.ExecuteWriteCopy)
        };
    }
}