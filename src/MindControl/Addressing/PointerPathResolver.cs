using MindControl.Results;

namespace MindControl;

/// <summary>
/// Provides a way to resolve an address in the target process.
/// This implementation takes a pointer path and resolves it to an address in the target process.
/// </summary>
/// <param name="pointerPath"></param>
public class PointerPathResolver(PointerPath pointerPath) : IAddressResolver<PathEvaluationFailure>
{
    /// <summary>Gets the pointer path to resolve.</summary>
    public PointerPath PointerPath { get; } = pointerPath;
    
    /// <summary>
    /// Evaluates the pointer path in the target process using the given <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the resolved address, or a failure.</returns>
    public Result<UIntPtr, PathEvaluationFailure> ResolveFor(ProcessMemory processMemory) =>
        processMemory.EvaluateMemoryAddress(PointerPath);
}