namespace MindControl;

/// <summary>
/// Describes a pointer offset as part of a <see cref="PointerPath"/>.
/// Effectively, this is a number ranging from -0xFFFFFFFFFFFFFFFF to 0xFFFFFFFFFFFFFFFF.
/// </summary>
/// <param name="Offset">Absolute value of the offset.</param>
/// <param name="IsNegative">Sign of the offset.</param>
public readonly record struct PointerOffset(ulong Offset, bool IsNegative)
{
    /// <summary>
    /// Represents a zero offset.
    /// </summary>
    public static readonly PointerOffset Zero = new(0, false);
    
    /// <summary>
    /// Gets a value indicating whether this offset is 64-bit.
    /// </summary>
    public bool Is64Bit => Offset > uint.MaxValue;
    
    /// <summary>
    /// Produces the result of the addition between this offset and the given value.
    /// </summary>
    /// <param name="value">Value to add to this offset.</param>
    /// <param name="isNegative">Sign of the value to add.</param>
    /// <returns>The result of the addition, or null if the result overflows or underflows.</returns>
    public PointerOffset? Plus(ulong value, bool isNegative)
    {
        if (IsNegative == isNegative)
        {
            // This handles both overflow (value going above 0xFFFFFFFFFFFFFFFF) and underflow (value going below
            // -0xFFFFFFFFFFFFFFFF)
            if (Offset > ulong.MaxValue - value)
                return null;
            
            return new(Offset + value, IsNegative);
        }
        
        // If the signs are different, we need to subtract the smaller from the larger number.
        // The sign of the result will be the sign of the larger number.
        if (Offset > value)
            return new(Offset - value, IsNegative);
        if (Offset == value)
            return Zero;
        return new(value - Offset, isNegative);
    }
    
    /// <summary>
    /// Produces the result of the addition between this offset and the given offset.
    /// </summary>
    /// <param name="other">Offset to add to this offset.</param>
    /// <returns>The result of the addition, or null if the result overflows or underflows.</returns>
    public PointerOffset? Plus(PointerOffset other)
        => Plus(other.Offset, other.IsNegative);

    /// <summary>
    /// Offsets the given address by this offset.
    /// </summary>
    /// <param name="address">Address to offset.</param>
    /// <returns>The offset address, or null if the result overflows or is negative.</returns>
    public UIntPtr? OffsetAddress(UIntPtr address)
    {
        var sum = Plus((ulong)address, false);
        if (sum == null || sum.Value.IsNegative || sum.Value.Offset > (ulong)UIntPtr.MaxValue)
            return null;

        return (UIntPtr)sum.Value.Offset;
    }
    
    /// <summary>
    /// Returns the value of this offset as an address, or null if the offset is negative.
    /// </summary>
    /// <returns>The value of this offset as an address, or null if the offset is negative.</returns>
    public UIntPtr? AsAddress()
    {
        if (IsNegative || Offset > (ulong)UIntPtr.MaxValue)
            return null;
        return (UIntPtr)Offset;
    }

    /// <summary>
    /// Produces the result of the sum of the multiplication of the value of this offset by 16 and the given value.
    /// The resulting sign will always be the sign of this offset.
    /// This is useful when building up a pointer offset from a sequence of bytes.
    /// </summary>
    /// <param name="value">Value to add to the result of the multiplication.</param>
    /// <returns>The result of the multiplication and addition, or null if the result overflows or underflows.</returns>
    public PointerOffset? ShiftAndAdd(byte value)
    {
        // Check if multiplying by 16 will overflow
        if (Offset > ulong.MaxValue / 16)
            return null;

        ulong shiftedOffset = Offset * 16;
        
        // Check if adding the value will overflow
        if (shiftedOffset > ulong.MaxValue - value)
            return null;
        
        return new(shiftedOffset + value, IsNegative);
    }

    /// <summary>Returns the fully qualified type name of this instance.</summary>
    /// <returns>The fully qualified type name.</returns>
    public override string ToString() => IsNegative ? $"-{Offset:X}" : $"{Offset:X}";
}