using MindControl.Results;

namespace MindControl.Native;

/// <summary>
/// Provides process-related features.
/// </summary>
public interface IOperatingSystemService
{
    /// <summary>
    /// Opens the process with the given identifier, in a way that allows memory manipulation.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>Handle of the opened process.</returns>
    Result<IntPtr, SystemFailure> OpenProcess(int pid);

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>True if the process is 64-bits, false otherwise.</returns>
    Result<bool, SystemFailure> IsProcess64Bits(int pid);

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>An array of bytes containing the data read from the memory.</returns>
    Result<byte[], SystemFailure> ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length);

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
    /// <exception cref="ArgumentException">The process handle is invalid (zero pointer).</exception>
    /// <exception cref="ArgumentOutOfRangeException">The target address is invalid (zero pointer).</exception>
    Result<MemoryProtection, SystemFailure> ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bits, UIntPtr targetAddress,
        MemoryProtection newProtection);

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
    Result<SystemFailure> WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, byte[] value, int? size = null);
    
    /// <summary>
    /// Allocates memory in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A pointer to the start of the allocated memory.</returns>
    Result<UIntPtr, SystemFailure> AllocateMemory(IntPtr processHandle, int size, MemoryAllocationType allocationType,
        MemoryProtection protection);

    /// <summary>
    /// Allocates memory in the specified process at the specified address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="address">Address where the memory will be allocated.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A pointer to the start of the allocated memory.</returns>
    Result<UIntPtr, SystemFailure> AllocateMemory(IntPtr processHandle, UIntPtr address, int size, MemoryAllocationType allocationType,
        MemoryProtection protection);

    /// <summary>
    /// Gets the address of the function used to load a library in the current process.
    /// </summary>
    Result<UIntPtr, SystemFailure> GetLoadLibraryFunctionAddress();
    
    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>Handle of the thread.</returns>
    Result<IntPtr, SystemFailure> CreateRemoteThread(IntPtr processHandle, UIntPtr startAddress, UIntPtr parameterAddress);

    /// <summary>
    /// Waits for the specified thread to finish execution.
    /// </summary>
    /// <param name="threadHandle">Handle of the target thread.</param>
    /// <param name="timeout">Maximum time to wait for the thread to finish.</param>
    /// <returns>True if the thread finished execution, false if the timeout was reached. Other failures will return a
    /// failure.</returns>
    Result<bool, SystemFailure> WaitThread(IntPtr threadHandle, TimeSpan timeout);

    /// <summary>
    /// Frees the memory allocated in the specified process for a region or a placeholder.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="regionBaseAddress">Base address of the region or placeholder to free, as returned by the allocation
    /// methods.</param>
    Result<SystemFailure> ReleaseMemory(IntPtr processHandle, UIntPtr regionBaseAddress);

    /// <summary>
    /// Closes the given handle.
    /// </summary>
    /// <param name="handle">Handle to close.</param>
    Result<SystemFailure> CloseHandle(IntPtr handle);
    
    /// <summary>
    /// Gets the range of memory addressable by applications in the current system.
    /// </summary>
    MemoryRange GetFullMemoryRange();
    
    /// <summary>
    /// Gets the metadata of a memory region in the virtual address space of a process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="baseAddress">Base address of the target memory region.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.
    /// If left null, the method will automatically determine the bitness of the process.</param>
    Result<MemoryRangeMetadata, SystemFailure> GetRegionMetadata(IntPtr processHandle, UIntPtr baseAddress,
        bool is64Bits);

    /// <summary>
    /// Gets the allocation granularity (minimal allocation size) of the system.
    /// </summary>
    uint GetAllocationGranularity();

    /// <summary>
    /// Gets the page size of the system.
    /// </summary>
    uint GetPageSize();
}
