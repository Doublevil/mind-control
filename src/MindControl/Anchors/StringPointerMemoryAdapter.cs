using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>
/// Represents an adapter for reading and writing a value from and to memory.
/// This implementation reads and writes a string from and to memory using an address resolver and the string settings.
/// </summary>
/// <param name="addressResolver">Resolver that provides the address of the string in memory.</param>
/// <param name="stringSettings">Settings that define how the string is read and written.</param>
public class StringPointerMemoryAdapter(IAddressResolver addressResolver,
    StringSettings stringSettings) : IMemoryAdapter<string>
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<string> Read(ProcessMemory processMemory)
    {
        var addressResult = addressResolver.ResolveFor(processMemory);
        if (addressResult.IsFailure)
            return addressResult.Failure;

        return processMemory.ReadStringPointer(addressResult.Value, stringSettings);
    }

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Write(ProcessMemory processMemory, string value)
    {
        // Not supported for now, might be in the future if we implement internal string instance management.
        return new NotSupportedFailure("Writing a string to memory is not supported in this context, because it involves memory allocations that must be handled separately. As an alternative, use StoreString, and write a pointer to the returned address.");
    }
}