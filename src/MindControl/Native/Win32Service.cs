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
    /// <returns>Handle of the opened process.</returns>
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
    /// <returns>True if the process is 64-bits, false otherwise.</returns>
    public Result<bool, SystemFailure> IsProcess64Bits(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!IsWow64Process(process.Handle, out bool isWow64))
                return GetLastSystemError();
        
            bool isSystem64Bits = IntPtr.Size == 8;
        
            // Process is 64 bits if we are running a 64-bits system and the process is NOT in wow64.
            return !isWow64 && isSystem64Bits;
        }
        catch (Exception)
        {
            return new SystemFailureOnInvalidArgument(nameof(pid),
                $"The process of PID {pid} was not found. Check that the process is running.");
        }
    }

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>An array of bytes containing the data read from the memory.</returns>
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
    /// Overwrites the memory protection of the page that the given address is part of.
    /// Returns the memory protection that was effective on the page before being changed.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.
    /// The handle must have PROCESS_VM_OPERATION access.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.</param>
    /// <param name="targetAddress">An address in the target page.</param>
    /// <param name="newProtection">New protection value for the page.</param>
    /// <returns>The memory protection value that was effective on the page before being changed.</returns>
    public Result<MemoryProtection, SystemFailure> ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bits,
        UIntPtr targetAddress, MemoryProtection newProtection)
    {
        if (processHandle == IntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(processHandle),
                "The process handle is invalid (zero pointer).");
        if (targetAddress == UIntPtr.Zero)
            return new SystemFailureOnInvalidArgument(nameof(targetAddress),
                "The target address cannot be a zero pointer.");

        bool result = VirtualProtectEx(processHandle, targetAddress, (IntPtr)(is64Bits ? 8 : 4), newProtection,
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

        bool result = WriteProcessMemory(processHandle, targetAddress, value, (UIntPtr)(size ?? value.Length),
            IntPtr.Zero);

        return result ? Result<SystemFailure>.Success : GetLastSystemError();
    }

    /// <summary>
    /// Allocates memory in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A pointer to the start of the allocated memory.</returns>
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
    /// <returns>A pointer to the start of the allocated memory.</returns>
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
    /// <returns>The address of the function if located, or null otherwise.</returns>
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
    public Result<UIntPtr, SystemFailure> GetLoadLibraryFunctionAddress()
        => GetFunctionAddress("kernel32.dll", "LoadLibraryW");

    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>Handle of the thread.</returns>
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
    /// <returns>True if the thread finished execution, false if the timeout was reached. Other failures will return
    /// a failure.</returns>
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
            IsReadable = memoryBasicInformation.Protect.HasFlag(MemoryProtection.ReadOnly),
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