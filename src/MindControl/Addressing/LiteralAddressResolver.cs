using MindControl.Results;

namespace MindControl;

/// <summary>
/// Provides a way to resolve an address in the target process.
/// This implementation takes a literal address and always resolves to that same address.
/// </summary>
/// <param name="address">Literal address to return in <see cref="ResolveFor"/>.</param>
public class LiteralAddressResolver(UIntPtr address) : IAddressResolver
{
    /// <summary>Gets the literal address to return in <see cref="ResolveFor"/>.</summary>
    public UIntPtr Address => address;
    
    /// <summary>
    /// Resolves the address in the target process using the given <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the resolved address, or a failure expressed as a string.</returns>
    public Result<UIntPtr> ResolveFor(ProcessMemory processMemory)
    {
        if (!processMemory.Is64Bit && Address > uint.MaxValue)
            return new IncompatibleBitnessPointerFailure(Address);
        return Address;
    }
}