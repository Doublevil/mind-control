using MindControl.Results;

namespace MindControl;

/// <summary>
/// Provides a way to resolve an address in the target process.
/// </summary>
public interface IAddressResolver<TFailure>
{
    /// <summary>
    /// Resolves the address in the target process using the given <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the resolved address, or a failure.</returns>
    Result<UIntPtr, TFailure> ResolveFor(ProcessMemory processMemory);
}