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
    /// <returns>A result holding either the handle of the opened process, or a system failure.</returns>
    Result<IntPtr, SystemFailure> OpenProcess(int pid);

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>A result holding either a boolean indicating if the process is 64-bit, or a system failure.</returns>
    Result<bool, SystemFailure> IsProcess64Bit(int pid);

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>A result holding either an array of bytes containing the data read from the process memory, or a
    /// system failure.</returns>
    Result<byte[], SystemFailure> ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length);

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
    /// <returns>A result holding either the number of bytes actually read from memory, or a system failure when no byte
    /// were successfully read.</returns>
    Result<ulong, SystemFailure> ReadProcessMemoryPartial(IntPtr processHandle, UIntPtr baseAddress, byte[] buffer,
        int offset, ulong length);
    
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
    /// changed, or a system failure.</returns>
    /// <exception cref="ArgumentException">The process handle is invalid (zero pointer).</exception>
    /// <exception cref="ArgumentOutOfRangeException">The target address is invalid (zero pointer).</exception>
    Result<MemoryProtection, SystemFailure> ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bit,
        UIntPtr targetAddress, MemoryProtection newProtection);

    /// <summary>
    /// Writes the given bytes into the memory of the specified process, at the target address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access.</param>
    /// <param name="targetAddress">Base address in the memory of the process to which data will be written.</param>
    /// <param name="value">Bytes to write in the process memory.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    Result<SystemFailure> WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, Span<byte> value);
    
    /// <summary>
    /// Allocates memory in the specified process. The address is determined automatically by the operating system.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="size">Size in bytes of the memory to allocate.</param>
    /// <param name="allocationType">Type of memory allocation.</param>
    /// <param name="protection">Protection flags of the memory to allocate.</param>
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a system failure.</returns>
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
    /// <returns>A result holding either a pointer to the start of the allocated memory, or a system failure.</returns>
    Result<UIntPtr, SystemFailure> AllocateMemory(IntPtr processHandle, UIntPtr address, int size,
        MemoryAllocationType allocationType, MemoryProtection protection);

    /// <summary>
    /// Gets the address of the function used to load a library in the current process.
    /// </summary>
    /// <returns>A result holding either the address of the function, or a system failure.</returns>
    Result<UIntPtr, SystemFailure> GetLoadLibraryFunctionAddress();
    
    /// <summary>
    /// Spawns a thread in the specified process, starting at the given address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="startAddress">Address of the start routine to be executed by the thread.</param>
    /// <param name="parameterAddress">Address of any parameter to be passed to the start routine.</param>
    /// <returns>A result holding either the handle of the thread, or a system failure.</returns>
    Result<IntPtr, SystemFailure> CreateRemoteThread(IntPtr processHandle, UIntPtr startAddress,
        UIntPtr parameterAddress);

    /// <summary>
    /// Waits for the specified thread to finish execution.
    /// </summary>
    /// <param name="threadHandle">Handle of the target thread.</param>
    /// <param name="timeout">Maximum time to wait for the thread to finish.</param>
    /// <returns>A result holding either a boolean indicating if the thread returned (True) or timed out (False), or a
    /// system failure for other error cases.</returns>
    Result<bool, SystemFailure> WaitThread(IntPtr threadHandle, TimeSpan timeout);

    /// <summary>
    /// Frees the memory allocated in the specified process for a region or a placeholder.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="regionBaseAddress">Base address of the region or placeholder to free, as returned by the allocation
    /// methods.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
    Result<SystemFailure> ReleaseMemory(IntPtr processHandle, UIntPtr regionBaseAddress);

    /// <summary>
    /// Closes the given handle.
    /// </summary>
    /// <param name="handle">Handle to close.</param>
    /// <returns>A result indicating either a success or a system failure.</returns>
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
    /// <returns>A result holding either the metadata of the target memory region, or a system failure.</returns>
    Result<MemoryRangeMetadata, SystemFailure> GetRegionMetadata(IntPtr processHandle, UIntPtr baseAddress);

    /// <summary>
    /// Gets the allocation granularity (minimal allocation size) of the system.
    /// </summary>
    uint GetAllocationGranularity();

    /// <summary>
    /// Gets the page size of the system.
    /// </summary>
    uint GetPageSize();
}
