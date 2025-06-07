using MindControl.Code;

namespace MindControl.Hooks;

/// <summary>
/// Represents a modification to a code section in the memory of a process.
/// The modification is a hook, which replaces code instructions with a jump to a block of injected code.
/// Allows reverting the jump by writing the original instructions back to the code section. Reverting does not remove
/// the injected code, but will remove the entry point to the code.
/// </summary>
public class CodeHook : CodeChange
{
    /// <summary>
    /// Gets the reservation holding the injected code.
    /// </summary>
    public MemoryReservation InjectedCodeReservation { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeHook"/> class.
    /// </summary>
    /// <param name="processMemory">Process memory instance that contains the code section.</param>
    /// <param name="address">Address of the first byte of the code section.</param>
    /// <param name="originalBytes">Original bytes that were replaced by the alteration.</param>
    /// <param name="injectedCodeReservation">Reservation holding the injected code.</param>
    internal CodeHook(ProcessMemory processMemory, UIntPtr address, byte[] originalBytes,
        MemoryReservation injectedCodeReservation)
        : base(processMemory, address, originalBytes)
    {
        InjectedCodeReservation = injectedCodeReservation;
    }
}