using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MindControl.Native;

/// <summary>
/// Contains DllImports for Windows functions required internally by other components.
/// </summary>
public partial class Win32Service : IOperatingSystemService
{
    private SystemInfo? _systemInfo;

    /// <summary>
    /// Opens the process with the given identifier, in a way that allows memory manipulation.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>Handle of the opened process.</returns>
    public IntPtr OpenProcess(int pid)
    {
        var handle = OpenProcess(0x1F0FFF, true, pid);
        if (handle == IntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return handle;
    }

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>True if the process is 64-bits, false otherwise.</returns>
    public bool IsProcess64Bits(int pid)
    {
        var process = Process.GetProcessById(pid);
        if (process == null)
            throw new ArgumentException($"Process {pid} was not found.");
        
        if (!IsWow64Process(process.Handle, out bool isWow64))
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
        process.Dispose();
        
        bool isSystem64Bits = IntPtr.Size == 8;
        
        // Process is 64 bits if we are running a 64-bits system and the process is NOT in wow64.
        return !isWow64 && isSystem64Bits;
    }

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>An array of bytes containing the data read from the memory.</returns>
    public byte[]? ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        
        var result = new byte[length];
        int returnValue = ReadProcessMemory(processHandle, baseAddress, result, length, out _);

        if (returnValue == 0)
        {
            int errorCode = Marshal.GetLastWin32Error();
            
            // ERROR_PARTIAL_COPY (299): Generic error that is raised when the address isn't valid for whatever reason.
            // This error is quite generic and does not really allow users to identify what's wrong.
            // In order to simplify error handling by a significant margin and also preserve performance, we will
            // not throw when getting this particular error code.
            if (errorCode == 299)
                return null;
            
            // ERROR_NOACCESS (998): Error raised when the memory we are trying to read is protected for whatever
            // reason. Since this can be due to trying to access an invalid address, for the same reasons as noted
            // above (error 299), we will not throw. This behaviour might change or be configurable in later releases.
            if (errorCode == 998)
                return null;

            // In other cases, throw.
            throw new Win32Exception(errorCode);
        }
        
        return result;
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
    public MemoryProtection ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bits, UIntPtr targetAddress,
        MemoryProtection newProtection)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (targetAddress == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(targetAddress),"The target address cannot be a zero pointer.");

        bool result = VirtualProtectEx(processHandle, targetAddress, (IntPtr)(is64Bits ? 8 : 4), newProtection,
            out var previousProtection);

        if (!result)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return previousProtection;
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
    public void WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, byte[] value, int? size = null)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (targetAddress == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(targetAddress),"The target address cannot be a zero pointer.");
        if (size != null && size.Value > value.Length)
            throw new ArgumentOutOfRangeException(nameof(size),"The size cannot exceed the length of the value array.");

        bool result = WriteProcessMemory(processHandle, targetAddress, value,
            (UIntPtr)(size ?? value.Length), IntPtr.Zero);
        
        if (!result)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
    }

    /// <summary>
    /// Allocates memory in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A pointer to the start of the allocated memory.</returns>
    public UIntPtr AllocateMemory(IntPtr processHandle, int size, MemoryAllocationType allocationType,
        MemoryProtection protection) => AllocateMemory(processHandle, UIntPtr.Zero, size, allocationType, protection);
    
    /// <summary>
    /// Allocates memory in the specified process at the specified address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="address">Address where the memory will be allocated.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A pointer to the start of the allocated memory.</returns>
    public UIntPtr AllocateMemory(IntPtr processHandle, UIntPtr address, int size, MemoryAllocationType allocationType,
        MemoryProtection protection)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size),"The size to allocate must be strictly positive.");
        
        var result = VirtualAllocEx(processHandle, address, (uint)size, (uint)allocationType, (uint)protection);
        if (result == UIntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return result;
    }

    /// <summary>
    /// Gets the address of a function in the specified module.
    /// </summary>
    /// <param name="moduleName">Name or path of the module. This module must be loaded in the current process.</param>
    /// <param name="functionName">Name of the target function in the specified module.</param>
    /// <returns>The address of the function if located, or null otherwise.</returns>
    private UIntPtr GetFunctionAddress(string moduleName, string functionName)
    {
        var moduleHandle = GetModuleHandle(moduleName);
        if (moduleHandle == IntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
        
        var functionAddress = GetProcAddress(moduleHandle, functionName);
        if (functionAddress == UIntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return functionAddress;
    }

    /// <summary>
    /// Gets the address of the function used to load a library in the current process.
    /// </summary>
    public UIntPtr GetLoadLibraryFunctionAddress() => GetFunctionAddress("kernel32.dll", "LoadLibraryW");

    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>Handle of the thread.</returns>
    public IntPtr CreateRemoteThread(IntPtr processHandle, UIntPtr startAddress, UIntPtr parameterAddress)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (startAddress == UIntPtr.Zero)
            throw new ArgumentException("The start address is invalid (zero pointer).", nameof(startAddress));

        var result = CreateRemoteThread(processHandle, IntPtr.Zero, 0, startAddress, parameterAddress, 0, out _);
        if (result == IntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return result;
    }

    /// <summary>
    /// Waits for the specified thread to finish execution.
    /// </summary>
    /// <param name="threadHandle">Handle of the target thread.</param>
    /// <param name="timeout">Maximum time to wait for the thread to finish.</param>
    /// <returns>True if the thread finished execution, false if the timeout was reached. Other failures will throw an
    /// exception.</returns>
    public bool WaitThread(IntPtr threadHandle, TimeSpan timeout)
    {
        if (threadHandle == IntPtr.Zero)
            throw new ArgumentException("The thread handle is invalid (zero pointer).", nameof(threadHandle));

        uint result = WaitForSingleObject(threadHandle, (uint)timeout.TotalMilliseconds);
        if (WaitForSingleObjectResult.IsSuccessful(result))
            return true;
        if (result == WaitForSingleObjectResult.Timeout)
            return false;
        
        throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
    }
    
    /// <summary>
    /// Frees the memory allocated in the specified process for a region or a placeholder.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="regionBaseAddress">Base address of the region or placeholder to free, as returned by
    /// <see cref="AllocateMemory"/>.</param>
    public void ReleaseMemory(IntPtr processHandle, UIntPtr regionBaseAddress)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (regionBaseAddress == UIntPtr.Zero)
            throw new ArgumentException("The region base address is invalid (zero pointer).",
                nameof(regionBaseAddress));
        
        if (!VirtualFreeEx(processHandle, regionBaseAddress, 0, (uint)MemoryFreeType.Release))
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
    }

    /// <summary>
    /// Closes the given handle.
    /// </summary>
    /// <param name="handle">Handle to close.</param>
    public void CloseHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            throw new ArgumentException("The handle is invalid (zero pointer).", nameof(handle));
        
        if (!WinCloseHandle(handle))
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
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
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.
    /// If left null, the method will automatically determine the bitness of the process.</param>
    public MemoryRangeMetadata GetRegionMetadata(IntPtr processHandle, UIntPtr baseAddress, bool? is64Bits = null)
    {
        bool is64 = is64Bits ?? IsProcess64Bits(Process.GetCurrentProcess().Id);

        MemoryBasicInformation memoryBasicInformation;
        if (is64)
        {
            // Use the 64-bit variant of the structure.
            var memInfo64 = new MemoryBasicInformation64();
            if (VirtualQueryEx(processHandle, baseAddress, out memInfo64,
                    (UIntPtr)Marshal.SizeOf(memInfo64)) == UIntPtr.Zero)
                throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

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
                throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself;

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