using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MindControl.Internal;

/// <summary>
/// Provides extension methods for byte arrays.
/// </summary>
internal static class ConversionExtensions
{
    /// <summary>
    /// Reads an unsigned number from the byte array.
    /// The byte array can be of any length comprised between 0 and 8 included.
    /// The number returned will be an unsigned long in all cases.
    /// </summary>
    /// <param name="bytes">Bytes to read.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the array's length is more than 8.</exception>
    public static ulong ReadUnsignedNumber(this byte[] bytes) => ReadUnsignedNumber(bytes.AsSpan());
    
    /// <summary>
    /// Reads an unsigned number from the byte span.
    /// The byte span can be of any length comprised between 0 and 8 included.
    /// The number returned will be an unsigned long in all cases.
    /// </summary>
    /// <param name="bytes">Bytes to read.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the span is too large.</exception>
    public static ulong ReadUnsignedNumber(this Span<byte> bytes)
    {
        if (bytes.Length > 8)
            throw new ArgumentException("The bytes cannot be read as a ulong because there are more than 8 bytes.",
                nameof(bytes));
        
        ulong result = 0;
        for (int i = 0; i < bytes.Length; i++)
            result += (ulong)(bytes[i] * Math.Pow(256, i));
        return result;
    }

    /// <summary>
    /// Converts the pointer to an array of bytes.
    /// </summary>
    /// <param name="pointer">Pointer to convert.</param>
    /// <param name="is64Bit">A boolean value indicating if the target architecture of the pointer is 64-bit or not.
    /// This value is used to determine the size of the returned array.</param>
    /// <returns>The array of bytes representing the pointer.</returns>
    public static byte[] ToBytes(this IntPtr pointer, bool is64Bit)
    {
        var result = new byte[is64Bit ? 8 : 4];
        Marshal.Copy(pointer, result, 0, result.Length);
        return result;
    }
    
    /// <summary>
    /// Attempts to read an IntPtr from the given BigInteger value.
    /// </summary>
    /// <param name="value">Value to read as an IntPtr.</param>
    public static UIntPtr? ToUIntPtr(this BigInteger value)
    {
        if ((IntPtr.Size == 4 && value > uint.MaxValue)
            || (IntPtr.Size == 8 && value > ulong.MaxValue)
            || value < 0)
        {
            // Don't let arithmetic overflows occur.
            // The input value is just not addressable.
            return null;
        }

        return IntPtr.Size == 4 ? (UIntPtr)(uint)value : (UIntPtr)(ulong)value;
    }

    /// <summary>
    /// Converts the given structure to an array of bytes.
    /// </summary>
    /// <param name="value">Value to convert. Must be a value type.</param>
    /// <returns>The array of bytes representing the value.</returns>
    private static byte[] StructToBytes(object value)
    {
        // Note: this code only works with structs.
        // Do not try this at home with reference types!
        
        int size = Marshal.SizeOf(value);
        var arr = new byte[size];

        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }
    
    /// <summary>
    /// Converts the object to an array of bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>The array of bytes representing the value.</returns>
    public static byte[] ToBytes(this object? value)
    {
        switch (value)
        {
            case null:
                throw new ArgumentNullException(nameof(value));
            case byte[] bytes:
                return bytes;
        }

        // For now, we will only handle structs.
        if (value is not ValueType)
            throw new ArgumentException($"The value must be a value type. {value.GetType().Name}", nameof(value));
        
        return StructToBytes(value);
    }
    
    /// <summary>Caches the null terminator byte sequences for each encoding. Used in <see cref="GetNullTerminator"/>.
    /// </summary>
    private static readonly Dictionary<Encoding, byte[]> StringNullTerminatorsCache = new();
    
    /// <summary>
    /// Gets the null terminator byte sequence for this encoding.
    /// </summary>
    /// <param name="encoding">Target encoding.</param>
    /// <returns>The null terminator byte sequence.</returns>
    public static byte[] GetNullTerminator(this Encoding encoding)
    {
        if (StringNullTerminatorsCache.TryGetValue(encoding, out byte[]? terminator))
            return terminator;

        terminator = encoding.GetBytes("\0");
        StringNullTerminatorsCache.TryAdd(encoding, terminator);
        return terminator;
    }
}