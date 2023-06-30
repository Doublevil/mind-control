namespace MindControl.Internal;

/// <summary>
/// Provides extension methods for byte arrays.
/// </summary>
internal static class ByteArrayExtension
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
}