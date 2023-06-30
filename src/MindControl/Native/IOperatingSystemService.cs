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
    IntPtr OpenProcess(int pid);

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>True if the process is 64-bits, false otherwise.</returns>
    bool IsProcess64Bits(int pid);

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>An array of bytes containing the data read from the memory.</returns>
    byte[] ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress, ulong length);
}
