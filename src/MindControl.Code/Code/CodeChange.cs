using MindControl.Results;

namespace MindControl.Code;

/// <summary>
/// Represents a modification to a code section in the memory of a process.
/// Allows reverting the alteration by writing the original bytes back to the code section.
/// </summary>
public class CodeChange
{
    private readonly ProcessMemory _processMemory;
    private readonly byte[] _originalBytes;

    /// <summary>
    /// Gets the address of the first byte of the modified code section.
    /// </summary>
    public UIntPtr Address { get; }
    
    /// <summary>
    /// Gets the length in bytes of the modified code section.
    /// </summary>
    public int Length => _originalBytes.Length;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeChange"/> class.
    /// </summary>
    /// <param name="processMemory">Process memory instance that contains the code section.</param>
    /// <param name="address">Address of the first byte of the code section.</param>
    /// <param name="originalBytes">Original bytes that were replaced by the alteration.</param>
    internal CodeChange(ProcessMemory processMemory, UIntPtr address, byte[] originalBytes)
    {
        _processMemory = processMemory;
        _originalBytes = originalBytes;
        Address = address;
    }
    
    /// <summary>
    /// Reverts the code alteration, writing the original bytes back to the code section.
    /// </summary>
    /// <returns>A result indicating either a success or a failure.</returns>
    public Result Revert() => _processMemory.WriteBytes(Address, _originalBytes);
}
