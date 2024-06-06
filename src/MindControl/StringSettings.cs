using System.Buffers.Binary;
using System.Text;
using MindControl.Internal;

namespace MindControl;

/// <summary>
/// Defines how strings are read and written in a process' memory.
/// </summary>
public class StringSettings
{
    /// <summary>
    /// Default value for <see cref="MaxLength"/>.
    /// </summary>
    public const int DefaultMaxLength = 1024;

    /// <summary>
    /// Gets or sets the maximum length of the strings that can be read with this instance.
    /// When reading strings, if the length of the string is evaluated to a value exceeding the maximum, the read
    /// operation will be aborted and fail to prevent reading unexpected data.
    /// The unit of this value depends on the <see cref="LengthPrefix"/> settings. If no length prefix is used, the unit
    /// used is the number of characters in the string.
    /// The default value is defined by the <see cref="DefaultMaxLength"/> constant.
    /// </summary>
    public int MaxLength { get; set; } = DefaultMaxLength;
    
    /// <summary>
    /// Gets or sets an optional prefix that comes before the string bytes. This is useful for type pointers in
    /// frameworks that use them. If the string also has a length prefix, the type prefix comes first, before the
    /// length.
    /// </summary>
    public byte[]? TypePrefix { get; set; }
    
    /// <summary>
    /// Gets or sets a boolean indicating if strings should have a \0 delimitation character at the end.
    /// </summary>
    public bool IsNullTerminated { get; set; }
    
    /// <summary>
    /// Gets or sets the encoding of the strings.
    /// </summary>
    public Encoding Encoding { get; set; }
    
    /// <summary>
    /// Gets or sets the length prefix settings.
    /// If null, strings are considered to have no length prefix.
    /// </summary>
    public StringLengthPrefix? LengthPrefix { get; set; }

    /// <summary>
    /// Gets a boolean indicating if the settings are valid.
    /// </summary>
    public bool IsValid => IsNullTerminated || LengthPrefix != null;
    
    /// <summary>
    /// Builds settings with the given properties.
    /// If you are unsure what settings to use, consider using
    /// <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> if possible to automatically determine the
    /// appropriate settings for a known string pointer.
    /// </summary>
    /// <param name="encoding">Encoding of the strings.</param>
    /// <param name="isNullTerminated">Boolean indicating if strings should have a \0 delimitation character at the end.
    /// The default value is true.</param>
    /// <param name="lengthPrefix">Length prefix settings. If null, strings are considered to have no length prefix.
    /// The default value is null.</param>
    /// <param name="typePrefix">Optional prefix that comes before the string bytes. This is useful for type pointers in
    /// frameworks that use them. If the string also has a length prefix, the type prefix comes first, before the
    /// length. The default value is null.</param>
    public StringSettings(Encoding encoding, bool isNullTerminated = true,
        StringLengthPrefix? lengthPrefix = null,
        byte[]? typePrefix = null)
    {
        Encoding = encoding;
        IsNullTerminated = isNullTerminated;
        LengthPrefix = lengthPrefix;
        TypePrefix = typePrefix;
    }
    
    /// <summary>
    /// Throws an exception if the settings are invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the settings are not valid.</exception>
    protected void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException(
                "The settings are not valid. Either a length prefix or null terminator is required.");
    }
    
    /// <summary>
    /// Computes the number of bytes to read when reading a string with these settings, given a maximum string length.
    /// </summary>
    /// <param name="maxStringLength">Maximum length of the string to read.</param>
    /// <returns>The number of bytes to read.</returns>
    public int GetMaxByteLength(int maxStringLength)
    {
        ThrowIfInvalid();
        
        return Encoding.GetMaxByteCount(maxStringLength)
            + (LengthPrefix?.Size ?? 0)
            + (TypePrefix?.Length ?? 0)
            + (IsNullTerminated ? Encoding.GetNullTerminator().Length : 0);
    }
    
    /// <summary>
    /// Converts the given string into a byte array using these settings. May fail and return null when using a length
    /// prefix that is too small to represent the string.
    /// </summary>
    /// <param name="value">String to convert.</param>
    /// <returns>The byte array representing the string, or null if the string is too long to be represented with the
    /// length prefix setting.</returns>
    public byte[]? GetBytes(string value)
    {
        ThrowIfInvalid();
        
        // Note: this method is optimized to avoid unnecessary allocations.
        // Because of this, we cannot use certain methods like Encoding.GetBytes.
        
        // Check if the string is too long to be represented with the length prefix
        // We do this first to avoid allocating memory when the string is too long
        int byteCount = Encoding.GetByteCount(value);
        int? lengthToWrite = null;
        if (LengthPrefix != null)
        {
            lengthToWrite = LengthPrefix.Unit switch
            {
                StringLengthUnit.Characters => value.Length,
                _ => byteCount
            };

            switch (LengthPrefix.Size)
            {
                case 1 when lengthToWrite > byte.MaxValue:
                case 2 when lengthToWrite > short.MaxValue:
                    return null;
            }
        }
        
        // Avoid unnecessary allocations. Only allocate a single byte array
        var bytes = new byte[GetByteCount(value)];
        
        // Write the type prefix
        int currentIndex = 0;
        TypePrefix?.CopyTo(bytes, 0);
        currentIndex += TypePrefix?.Length ?? 0;

        // Write the length prefix
        if (lengthToWrite != null)
        {
            // Write the length into the byte array, after the type prefix if any
            var span = bytes.AsSpan(currentIndex);
            if (LengthPrefix!.Size == 1)
                span[0] = (byte)lengthToWrite.Value;
            else if (LengthPrefix.Size == 2)
                BinaryPrimitives.WriteInt16LittleEndian(span, (short)lengthToWrite.Value);
            else if (LengthPrefix.Size == 4)
                BinaryPrimitives.WriteInt32LittleEndian(span, lengthToWrite.Value);
            else
                BinaryPrimitives.WriteInt64LittleEndian(span, lengthToWrite.Value);

            currentIndex += LengthPrefix.Size;
        }
        
        // Write the string bytes. Encoder.Convert is used to avoid unnecessary allocations.
        Encoding.GetEncoder().Convert(value, bytes.AsSpan(currentIndex, byteCount), true, out _, out _, out _);
        currentIndex += byteCount;
        
        // Write the null terminator if needed
        if (IsNullTerminated)
            Encoding.GetNullTerminator().CopyTo(bytes, currentIndex);

        return bytes;
    }

    /// <summary>
    /// Gets the number of bytes that a specific string would occupy in memory with these settings.
    /// </summary>
    /// <param name="value">String to measure.</param>
    /// <returns>The number of bytes that the string would occupy in memory.</returns>
    public int GetByteCount(string value)
    {
        ThrowIfInvalid();
        
        return (TypePrefix?.Length ?? 0)
            + (LengthPrefix?.Size ?? 0)
            + Encoding.GetByteCount(value)
            + (IsNullTerminated ? Encoding.GetNullTerminator().Length : 0);
    }
    
    /// <summary>
    /// Attempts to read a string from the given bytes with this settings instance.
    /// </summary>
    /// <param name="bytes">Bytes to read the string from.</param>
    /// <returns>The string read from the bytes, or null if the string could not be read.</returns>
    /// <remarks>This method ignores the <see cref="MaxLength"/> constraint, because the full span of bytes is already
    /// provided as a parameter.</remarks>
    public string? GetString(Span<byte> bytes)
    {
        // Figure out the start index of the actual string bytes, after the prefixes (if any)
        int lengthPrefixSize = LengthPrefix?.Size ?? 0;
        int typePrefixSize = TypePrefix?.Length ?? 0;
        int startIndex = lengthPrefixSize + typePrefixSize;
        
        // Calculate the remaining bytes to read
        int remainingBytes = bytes.Length - startIndex;
        if (remainingBytes <= 0)
            return string.Empty;
        
        // Calculate how many bytes we have to read.
        // If the length prefix is in bytes, read the length prefix and use it as the length to read.
        // If we have a null terminator, we also have to read it, so add its length to the length to read.
        // Otherwise, read the remaining bytes.
        bool hasBytesLengthPrefix = LengthPrefix is { Size: > 0, Unit: StringLengthUnit.Bytes };
        ulong lengthToRead = hasBytesLengthPrefix ?
            bytes.Slice(typePrefixSize, lengthPrefixSize).ReadUnsignedNumber()
                + (ulong)(IsNullTerminated ? Encoding.GetNullTerminator().Length : 0)
            : (ulong)remainingBytes;
        
        // Check if we have enough bytes to read the string.
        // This will fail if we read a byte length prefix that is too large.
        // In that case, return null, as this means the string settings don't work with the input bytes.
        if ((ulong)remainingBytes < lengthToRead)
            return null;
        
        // Check the bounds of the length to read
        if (lengthToRead is 0 or > int.MaxValue)
            return string.Empty;
        
        // Read the string bytes using the encoding in the settings
        var stringBytes = bytes.Slice(startIndex, (int)lengthToRead);
        if (stringBytes.Length == 0)
            return null;
        string resultingString = Encoding.GetString(stringBytes);

        // If we have a length prefix in characters, we have to cut the string to the correct length
        if (LengthPrefix is { Size: > 0, Unit: StringLengthUnit.Characters })
        {
            ulong characterLength = bytes.Slice(typePrefixSize, lengthPrefixSize).ReadUnsignedNumber();
            if (IsNullTerminated)
                characterLength++;
            
            if (characterLength == (ulong)resultingString.Length)
                return resultingString;
            if (characterLength > (ulong)resultingString.Length)
                return null;
            resultingString = resultingString[..(int)characterLength];
        }

        // If the string is null-terminated, we have to cut the string at the null-terminator
        if (IsNullTerminated)
        {
            int nullTerminatorIndex = resultingString.IndexOf('\0');
            return nullTerminatorIndex >= 0 ? resultingString[..nullTerminatorIndex]
                : resultingString; // If there is no null terminator, we choose to return the full string
        }
        
        // No length prefix, or length prefix in bytes. Return the full string.
        return resultingString;
    }
}

/// <summary>
/// Defines what is counted by a string length prefix.
/// </summary>
public enum StringLengthUnit
{
    /// <summary>
    /// The length prefix is a count of characters.
    /// </summary>
    Characters,
    
    /// <summary>
    /// The length prefix is a count of bytes.
    /// </summary>
    Bytes
}

/// <summary>
/// Defines settings about the prefix that holds the length of a string.
/// </summary>
public class StringLengthPrefix
{
    /// <summary>
    /// Gets or sets the number of bytes storing the length of the string.
    /// </summary>
    public int Size { get; }
    
    /// <summary>
    /// Gets or sets what the length prefix counts.
    /// </summary>
    public StringLengthUnit Unit { get; }

    /// <summary>
    /// Builds length prefix settings with the given properties.
    /// </summary>
    /// <param name="size">Number of bytes storing the length of the string.</param>
    /// <param name="unit">What the length prefix counts.</param>
    public StringLengthPrefix(int size, StringLengthUnit unit)
    {
        if (size != 1 && size != 2 && size != 4 && size != 8)
            throw new ArgumentOutOfRangeException(nameof(size), "Prefix size must be either 1, 2, 4 or 8.");
        
        Size = size;
        Unit = unit;
    }
}
