using System.Numerics;
using System.Runtime.InteropServices;

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
    public static ulong ReadUnsignedNumber(this byte[] bytes)
    {
        if (bytes.Length > 8)
            throw new ArgumentException("The byte array cannot be read as a ulong because it is longer than 8 bytes.",
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
    /// <param name="is64">A boolean value indicating if the target architecture of the pointer is 64-bits or not.
    /// This value is used to determine the size of the returned array.</param>
    /// <returns>The array of bytes representing the pointer.</returns>
    public static byte[] ToBytes(this IntPtr pointer, bool is64)
    {
        var result = new byte[is64 ? 8 : 4];
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
}