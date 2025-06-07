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
    /// <param name="apiFunctionName">Name of the API function that failed.</param>
    /// <param name="operationName">Name of the top-level operation that failed.</param>
    private static OperatingSystemCallFailure GetLastSystemErrorAsFailure(string apiFunctionName, string operationName)
    {
        int errorCode = Marshal.GetLastWin32Error();
        return new OperatingSystemCallFailure(apiFunctionName, operationName, errorCode,
            new Win32Exception(errorCode).Message);
    }
    
    /// <summary>
    /// Opens the process with the given identifier, in a way that allows memory manipulation.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>A result holding either the handle of the opened process, or a system failure.</returns>
    public Result<IntPtr> OpenProcess(int pid)
    {
        var handle = OpenProcess(0x1F0FFF, true, pid);
        if (handle == IntPtr.Zero)
            return GetLastSystemErrorAsFailure(nameof(OpenProcess), "Process opening");

        return handle;
    }

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>A result holding either a boolean indicating if the process is 64-bit, or a system failure.</returns>
    public Result<bool> IsProcess64Bit(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!IsWow64Process(process.Handle, out bool isWow64))
                return GetLastSystemErrorAsFailure(nameof(IsWow64Process), "Process bitness check");
        
            // Process is 64-bit if we are running a 64-bit system and the process is NOT in wow64.
            return !isWow64 && IsSystem64Bit();
        }
        catch (Exception)
        {
            return new InvalidArgumentFailure(nameof(pid),
                $"The process of PID {pid} was not found. Check that the process is running.");
        }
    }

    /// <summary>
    /// Returns a value indicating if the system is 64-bit or not.
    /// </summary>
    /// <returns>A boolean indicating if the system is 64-bit or not.</returns>
    private bool IsSystem64Bit() => IntPtr.Size == 8;

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>A result holding either an array of bytes containing the data read from the process memory, or a
    /// system failure.</returns>
    public Result<byte[]> ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        
        var result = new byte[length];
        int returnValue = ReadProcessMemory(processHandle, baseAddress, result, length, out _);

        return returnValue == 0 ?
            GetLastSystemErrorAsFailure(nameof(ReadProcessMemory), "Process memory reading")
            : result;
    }
    
    /// <summary>
    /// Reads a targeted range of the memory of a specified process into the given buffer. Supports partial reads, in
    /// case the full length failed to be read but at least one byte was successfully copied into the buffer.
    /// Prefer <see cref="Win32Service.ReadProcessMemory(IntPtr,UIntPtr,ulong)"/> when you know the length of the data
    /// to read.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="buffer">Buffer to store the data read from the memory. The buffer must be large enough to store
    /// the data read.</param>
    /// <param name="offset">Offset in the buffer where the data will be stored.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>A result holding either the number of bytes actually read from memory, or a system failure.</returns>
    public Result<ulong> ReadProcessMemoryPartial(IntPtr processHandle, UIntPtr baseAddress,
        byte[] buffer, int offset, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if ((ulong)buffer.Length < (ulong)offset + length)
            return new InvalidArgumentFailure(nameof(buffer),
                "The buffer is too small to store the requested number of bytes.");
        if (UIntPtr.MaxValue.ToUInt64() - baseAddress.ToUInt64() < length)
            return new InvalidArgumentFailure(nameof(length),
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
            var initialReadError = GetLastSystemErrorAsFailure(nameof(ReadProcessMemory),
                "Partial process memory reading");
            
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

            // Determine if the process is 64-bit
            if (!IsWow64Process(processHandle, out bool isWow64))
                return GetLastSystemErrorAsFailure(nameof(IsWow64Process), "Partial process memory reading");
            bool is64Bit = !isWow64 && IsSystem64Bit();
            
            // Build the memory range that spans across everything we attempted to read
            var range = MemoryRange.FromStartAndSize(baseAddress, length);
            
            // Determine the last readable address within the range
            var lastReadableAddress = GetLastConsecutiveReadableAddressWithinRange(processHandle, is64Bit, range);
            
            // If we couldn't determine the last readable address, or it matches/exceeds the end of the range, there
            // is no point in trying to read again. Return the initial read error.
            if (lastReadableAddress == null || lastReadableAddress.Value.ToUInt64() >= range.End.ToUInt64())
                return initialReadError;
            
            // If we found a readable address within the range, we read up to that point.
            var newLength = lastReadableAddress.Value.ToUInt64() - baseAddress.ToUInt64() + 1;
            returnValue = ReadProcessMemory(processHandle, baseAddress, bufferPtr, newLength, out bytesRead);

            // If the function failed again and didn't read any byte again, return the read error.
            if (bytesRead == UIntPtr.Zero && returnValue == 0)
                return GetLastSystemErrorAsFailure(nameof(ReadProcessMemory), "Partial process memory reading");

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
    /// <param name="is64Bit">A boolean indicating if the target process is 64-bit or not.</param>
    /// <param name="range">Memory range to check for readable regions.</param>
    /// <returns>The end address of the last consecutive readable region within the range, or null if the start of the
    /// range is unreadable.</returns>
    private UIntPtr? GetLastConsecutiveReadableAddressWithinRange(IntPtr processHandle, bool is64Bit,
        MemoryRange range)
    {
        var applicationMemoryLimit = GetFullMemoryRange(is64Bit).End;
        ulong rangeEnd = Math.Min(range.End.ToUInt64(), applicationMemoryLimit.ToUInt64());
        UIntPtr currentAddress = range.Start;
        while (currentAddress.ToUInt64() <= rangeEnd)
        {
            var getRegionResult = GetRegionMetadata(processHandle, currentAddress);
            
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
    /// <param name="is64Bit">A boolean indicating if the target process is 64-bit or not.</param>
    /// <param name="targetAddress">An address in the target page.</param>
    /// <param name="newProtection">New protection value for the page.</param>
    /// <returns>A result holding either the memory protection value that was effective on the page before being
    /// changed, or a failure.</returns>
    public Result<MemoryProtection> ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bit,
        UIntPtr targetAddress, MemoryProtection newProtection)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if (targetAddress == UIntPtr.Zero)
            return new InvalidArgumentFailure(nameof(targetAddress), "The target address cannot be a zero pointer.");

        var result = VirtualProtectEx(processHandle, targetAddress, is64Bit ? 8 : 4, newProtection,
            out var previousProtection);

        return result ? previousProtection : GetLastSystemErrorAsFailure(nameof(VirtualProtectEx),
            "Memory protection overwriting");
    }

    /// <summary>
    /// Writes the given bytes into the memory of the specified process, at the target address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access.</param>
    /// <param name="targetAddress">Base address in the memory of the process to which data will be written.</param>
    /// <param name="value">Bytes to write in the process memory.</param>
    /// <returns>A result indicating either a success or a failure.</returns>
    public Result WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, Span<byte> value)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if (targetAddress == UIntPtr.Zero)
            return new InvalidArgumentFailure(nameof(targetAddress), "The target address cannot be a zero pointer.");
        
        var result = WriteProcessMemory(processHandle, targetAddress, ref value.GetPinnableReference(),
            (UIntPtr)value.Length, out _);
        
        return result ? Result.Success
            : GetLastSystemErrorAsFailure(nameof(WriteProcessMemory), "Process memory writing");
    }

    /// <summary>
    /// Allocates memory in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a failure.</returns>
    public Result<UIntPtr> AllocateMemory(IntPtr processHandle, int size,
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
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a failure.</returns>
    public Result<UIntPtr> AllocateMemory(IntPtr processHandle, UIntPtr address, int size,
        MemoryAllocationType allocationType, MemoryProtection protection)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if (size <= 0)
            return new InvalidArgumentFailure(nameof(size), "The size to allocate must be strictly positive.");
        
        var result = VirtualAllocEx(processHandle, address, (uint)size, (uint)allocationType, (uint)protection);
        return result == UIntPtr.Zero ?
            GetLastSystemErrorAsFailure(nameof(VirtualAllocEx), "Process memory allocation")
            : result;
    }

    /// <summary>
    /// Gets the address of a function in the specified module.
    /// </summary>
    /// <param name="moduleName">Name or path of the module. This module must be loaded in the current process.</param>
    /// <param name="functionName">Name of the target function in the specified module.</param>
    /// <returns>A result holding the address of the function if located, or a system failure otherwise.</returns>
    private Result<UIntPtr> GetFunctionAddress(string moduleName, string functionName)
    {
        var moduleHandle = GetModuleHandle(moduleName);
        if (moduleHandle == IntPtr.Zero)
            return GetLastSystemErrorAsFailure(nameof(GetModuleHandle), "Function address retrieval");
        
        var functionAddress = GetProcAddress(moduleHandle, functionName);
        if (functionAddress == UIntPtr.Zero)
            return GetLastSystemErrorAsFailure(nameof(GetProcAddress), "Function address retrieval");

        return functionAddress;
    }

    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>A result holding either the handle of the thread, or a system failure.</returns>
    public Result<IntPtr> CreateRemoteThread(IntPtr processHandle, UIntPtr startAddress,
        UIntPtr parameterAddress)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if (startAddress == UIntPtr.Zero)
            return new InvalidArgumentFailure(nameof(startAddress), "The start address is invalid (zero pointer).");

        var result = CreateRemoteThread(processHandle, IntPtr.Zero, 0, startAddress, parameterAddress, 0, out _);
        if (result == IntPtr.Zero)
            return GetLastSystemErrorAsFailure(nameof(CreateRemoteThread), "Remote thread creation");

        return result;
    }

    /// <summary>
    /// Waits for the specified thread to finish execution and returns its exit code.
    /// </summary>
    /// <param name="threadHandle">Handle of the target thread.</param>
    /// <param name="timeout">Maximum time to wait for the thread to finish.</param>
    /// <returns>A result holding either the exit code of the thread, or a failure.</returns>
    public Result<uint> WaitThread(IntPtr threadHandle, TimeSpan timeout)
    {
        if (threadHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(threadHandle), "The thread handle is invalid (zero pointer).");

        uint result = WaitForSingleObject(threadHandle, (uint)timeout.TotalMilliseconds);
        if (result == WaitForSingleObjectResult.Failed)
            return GetLastSystemErrorAsFailure(nameof(WaitForSingleObject), "Thread waiting");
        if (result == WaitForSingleObjectResult.Timeout)
            return new ThreadWaitTimeoutFailure();
        if (!WaitForSingleObjectResult.IsSuccessful(result))
            return new ThreadWaitTimeoutFailure();

        var exitCodeResult = GetExitCodeThread(threadHandle, out uint exitCode);
        if (!exitCodeResult)
            return GetLastSystemErrorAsFailure(nameof(GetExitCodeThread), "Thread waiting");
        return exitCode;
    }
    
    /// <summary>
    /// Frees the memory allocated in the specified process for a region or a placeholder.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="regionBaseAddress">Base address of the region or placeholder to free, as returned by the memory
    /// allocation methods.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    public Result ReleaseMemory(IntPtr processHandle, UIntPtr regionBaseAddress)
    {
        if (processHandle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(processHandle), "The process handle is invalid (zero pointer).");
        if (regionBaseAddress == UIntPtr.Zero)
            return new InvalidArgumentFailure(nameof(regionBaseAddress),
                "The region base address is invalid (zero pointer).");

        return VirtualFreeEx(processHandle, regionBaseAddress, 0, (uint)MemoryFreeType.Release)
            ? Result.Success : GetLastSystemErrorAsFailure(nameof(VirtualFreeEx), "Memory release");
    }

    /// <summary>
    /// Closes the given handle.
    /// </summary>
    /// <param name="handle">Handle to close.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    public Result CloseHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return new InvalidArgumentFailure(nameof(handle), "The handle is invalid (zero pointer).");

        return WinCloseHandle(handle) ? Result.Success
            : GetLastSystemErrorAsFailure(nameof(WinCloseHandle), "Handle closing");
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
    /// <param name="is64Bit">A boolean indicating if the target application is 64-bit or not.</param>
    public MemoryRange GetFullMemoryRange(bool is64Bit)
    {
        var systemInfo = GetSystemInfo();
        var maxAddress = is64Bit ? systemInfo.MaximumApplicationAddress
            : Math.Min(uint.MaxValue, systemInfo.MaximumApplicationAddress);
        return new MemoryRange(systemInfo.MinimumApplicationAddress, maxAddress);
    }

    /// <summary>
    /// Gets the page size of the system.
    /// </summary>
    public uint GetPageSize() => GetSystemInfo().PageSize;

    /// <summary>
    /// Gets the metadata of a memory region in the virtual address space of a process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="baseAddress">Base address of the target memory region.</param>
    /// <returns>A result holding either the metadata of the target memory region, or a system failure.</returns>
    public Result<MemoryRangeMetadata> GetRegionMetadata(IntPtr processHandle, UIntPtr baseAddress)
    {
        MemoryBasicInformation memoryBasicInformation;
        if (IsSystem64Bit())
        {
            // Use the 64-bit variant of the structure.
            var memInfo64 = new MemoryBasicInformation64();
            if (VirtualQueryEx(processHandle, baseAddress, out memInfo64,
                (UIntPtr)Marshal.SizeOf(memInfo64)) == UIntPtr.Zero)
                return GetLastSystemErrorAsFailure(nameof(VirtualQueryEx), "Memory region metadata retrieval");

            memoryBasicInformation = new MemoryBasicInformation((UIntPtr)memInfo64.BaseAddress,
                (UIntPtr)memInfo64.AllocationBase, memInfo64.AllocationProtect, (UIntPtr)memInfo64.RegionSize,
                memInfo64.State, memInfo64.Protect, memInfo64.Type);
        }
        else
        {
            // Use the 32-bit variant of the structure.
            var memInfo32 = new MemoryBasicInformation32();
            if (VirtualQueryEx(processHandle, baseAddress, out memInfo32, 
                (UIntPtr)Marshal.SizeOf(memInfo32)) == UIntPtr.Zero)
                return GetLastSystemErrorAsFailure(nameof(VirtualQueryEx), "Memory region metadata retrieval");

            memoryBasicInformation = new MemoryBasicInformation(memInfo32.BaseAddress,
                memInfo32.AllocationBase, memInfo32.AllocationProtect, memInfo32.RegionSize,
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