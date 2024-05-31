using System.Runtime.InteropServices;
using System.Text;
using MindControl.Internal;
using MindControl.Results;

namespace MindControl;

// This partial class implements the memory reading features of ProcessMemory.
public partial class ProcessMemory
{
    /// <summary>
    /// Gets or sets the default maximum length of strings to read when the length is not specified.
    /// The default value is a completely arbitrary 100.
    /// </summary>
    public int DefaultMaxStringLength { get; set; } = 100;
    
    #region Bytes reading
    
    /// <summary>
    /// Reads a sequence of bytes from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte[], ReadFailure> ReadBytes(PointerPath pointerPath, long length)
        => ReadBytes(pointerPath, (ulong)length);

    /// <summary>
    /// Reads a sequence of bytes from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte[], ReadFailure> ReadBytes(PointerPath pointerPath, ulong length)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadBytes(addressResult.Value, length)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte[], ReadFailure> ReadBytes(UIntPtr address, long length)
        => ReadBytes(address, (ulong)length);
    
    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte[], ReadFailure> ReadBytes(UIntPtr address, ulong length)
    {
        if (address == UIntPtr.Zero)
            return new ReadFailureOnZeroPointer();

        if (!IsBitnessCompatible(address))
            return new ReadFailureOnIncompatibleBitness(address);
        
        var readResult = _osService.ReadProcessMemory(_processHandle, address, length);
        return readResult.IsSuccess ? readResult.Value
            : new ReadFailureOnSystemRead(readResult.Error);
    }
    
    #endregion
    
    #region Primitive types reading
    
    /// <summary>
    /// Reads a specific type of data from the address referred by the given pointer path, in the process memory.
    /// This method only supports value types (primitive types and structures).
    /// </summary>
    /// <param name="pointerPath">Pointer path to the target address. Can be implicitly converted from a string.
    /// Example: "MyGame.exe+1F1688,1F,4". Reuse <see cref="PointerPath"/> instances to optimize execution time.</param>
    /// <typeparam name="T">Type of data to read. Only value types are supported (primitive types and structures).
    /// </typeparam>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<T, ReadFailure> Read<T>(PointerPath pointerPath) where T : struct
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? Read<T>(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory. This method only supports value
    /// types (primitive types and structures).
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <typeparam name="T">Type of data to read. Only value types are supported (primitive types and structures).
    /// </typeparam>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<T, ReadFailure> Read<T>(UIntPtr address) where T : struct
    {
        // Check the address
        if (address == UIntPtr.Zero)
            return new ReadFailureOnZeroPointer();
        if (!IsBitnessCompatible(address))
            return new ReadFailureOnIncompatibleBitness(address);
        
        // Get the size of the target type
        int size;
        try
        {
            size = Marshal.SizeOf<T>();
        }
        catch (ArgumentException)
        {
            return new ReadFailureOnConversionFailure();
        }

        // Read the bytes from the process memory
        var readResult = _osService.ReadProcessMemory(_processHandle, address, (ulong)size);
        if (readResult.IsFailure)
            return new ReadFailureOnSystemRead(readResult.Error);
        byte[] bytes = readResult.Value;
        
        // Convert the bytes into the target type
        try
        {
            return MemoryMarshal.Read<T>(bytes);
        }
        catch (Exception)
        {
            return new ReadFailureOnConversionFailure();
        }
    }

    /// <summary>
    /// Reads a specific type of data from the address referred by the given pointer path, in the process memory.
    /// This method only supports value types (primitive types and structures).
    /// </summary>
    /// <param name="type">Type of data to read. Only value types are supported (primitive types and structures).
    /// </param>
    /// <param name="pointerPath">Pointer path to the target address. Can be implicitly converted from a string.
    /// Example: "MyGame.exe+1F1688,1F,4". Reuse <see cref="PointerPath"/> instances to optimize execution time.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<object, ReadFailure> Read(Type type, PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? Read(type, addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory. This method only supports value
    /// types (primitive types and structures).
    /// </summary>
    /// <param name="type">Type of data to read. Only value types are supported (primitive types and structures).
    /// </param>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<object, ReadFailure> Read(Type type, UIntPtr address)
    {
        // Check that the type is not a reference type
        if (!type.IsValueType)
            return new ReadFailureOnUnsupportedType(type);
        
        // Check the address
        if (address == UIntPtr.Zero)
            return new ReadFailureOnZeroPointer();
        if (!IsBitnessCompatible(address))
            return new ReadFailureOnIncompatibleBitness(address);
        
        // Get the size of the target type
        int size = Marshal.SizeOf(type);
        
        // Read the bytes from the process memory
        var readResult = _osService.ReadProcessMemory(_processHandle, address, (ulong)size);
        if (!readResult.IsSuccess)
            return new ReadFailureOnSystemRead(readResult.Error);
        
        // Convert the bytes into the target type
        // Special case for booleans, which do not work well with Marshal.PtrToStructure
        if (type == typeof(bool))
            return readResult.Value[0] != 0;
        
        // We cannot use MemoryMarshal.Read here because the data type is a variable, not a generic type.
        // So we have to use a GCHandle to pin the bytes in memory and then use Marshal.PtrToStructure.
        var handle = GCHandle.Alloc(readResult.Value, GCHandleType.Pinned);
        try
        {
            var pointer = handle.AddrOfPinnedObject();
            object? structure = Marshal.PtrToStructure(pointer, type);
            if (structure == null)
                return new ReadFailureOnConversionFailure();
            return structure;
        }
        catch (Exception)
        {
            return new ReadFailureOnConversionFailure();
        }
        finally
        {
            if (handle.IsAllocated)
                handle.Free();
        }
    }

    #endregion
    
    #region FindStringSettings

    /// <summary>Encodings to use when trying to find the encoding of a string, in order of priority.</summary>
    /// <remarks>This list does not include ASCII, as there is no way to tell if a valid ASCII string is ASCII or UTF-8,
    /// and we choose to prioritize UTF-8.</remarks>
    private static readonly Encoding[] FindStringEncodings = { Encoding.UTF8, Encoding.Latin1, Encoding.Unicode };
    
    /// <summary>Length prefix sizes to try when trying to find the length prefix of a string, in order of priority.
    /// </summary>
    private static readonly int[] FindStringLengthPrefixSizes = { 0, 4, 2, 1, 8 };
    
    /// <summary>Length prefix sizes to try when trying to find the type prefix of a string, in order of priority.
    /// </summary>
    private static readonly int[] FindStringTypePrefixSizes = { 0, 4, 8, 16 };

    /// <summary>Length prefix units to try when trying to find the length prefix of a string, in order of priority.
    /// </summary>
    private static readonly StringLengthPrefixUnit[] FindStringLengthPrefixUnits =
        { StringLengthPrefixUnit.Bytes, StringLengthPrefixUnit.Characters };
    
    /// <summary>Null-termination settings to try when trying to find the null-termination of a string, in order of
    /// priority.</summary>
    private static readonly bool[] FindStringNullTerminated = { true, false };

    /// <summary>
    /// Attempts to find appropriate settings to read and write strings at the pointer referred by the given path,
    /// pointing to a known string.
    /// As this is based on guesses, the settings may not be determined correctly for every possible case, but they
    /// should be accurate most of the time. 
    /// </summary>
    /// <param name="pointerPath">Path to a pointer that points to a known string.</param>
    /// <param name="expectedString">Known string that the pointer points to. Very short strings consisting of only a
    /// few characters may lead to unaccurate results. Strings containing diacritics or non-Latin characters provide
    /// better results.</param>
    /// <returns>A result holding the potential string settings if they were successfully determined, or a read failure
    /// otherwise.</returns>
    public Result<StringSettings, FindStringSettingsFailure> FindStringSettings(PointerPath pointerPath,
        string expectedString)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? FindStringSettings(addressResult.Value, expectedString)
            : new FindStringSettingsFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Attempts to find appropriate settings to read and write strings at the given address pointing to a known string.
    /// As this is based on guesses, the settings may not be determined correctly for every possible case, but they
    /// should be accurate most of the time. 
    /// </summary>
    /// <param name="stringPointerAddress">Address to a pointer that points to a known string.</param>
    /// <param name="expectedString">Known string that the pointer points to. Very short strings consisting of only a
    /// few characters may lead to unaccurate results. Strings containing diacritics or non-Latin characters provide
    /// better results.</param>
    /// <returns>A result holding the potential string settings if they were successfully determined, or a read failure
    /// otherwise.</returns>
    public Result<StringSettings, FindStringSettingsFailure> FindStringSettings(UIntPtr stringPointerAddress,
        string expectedString)
    {
        if (string.IsNullOrWhiteSpace(expectedString))
            return new FindStringSettingsFailureOnNoSettingsFound();
        
        // We have to determine 4 parameters:
        // - The encoding of the string
        // - The length prefix settings (an optional prefix that holds the length of the string)
        // - The type prefix (an optional prefix that comes before the string bytes, usually a type pointer)
        // - Whether the string is null-terminated
        
        // We can attempt to pinpoint all these through trial and error, because we know the expected string.
        // We will try to read the string with different setting combinations in a specific order until we read the
        // expected string.

        // Read the address of the actual string from the pointer
        var pointerResult = Read<UIntPtr>(stringPointerAddress);
        if (pointerResult.IsFailure)
            return new FindStringSettingsFailureOnPointerReadFailure(pointerResult.Error);
        
        var pointerValue = pointerResult.Value;
        if (pointerValue == UIntPtr.Zero)
            return new FindStringSettingsFailureOnZeroPointer();
        
        // Read the max amount of bytes we might need to read the string from
        // This is computed from the sum of the longest considered type prefix, the longest considered length prefix,
        // the max byte count of the most complex encoding considered, and the longest considered null terminator.
        int maxBytesFromRawString = FindStringEncodings.Max(e => e.GetMaxByteCount(expectedString.Length));
        int maxBytesFromLengthPrefix = FindStringLengthPrefixSizes.Max();
        int maxBytesFromTypePrefix = FindStringTypePrefixSizes.Max();
        int maxBytesFromNullTerminator = FindStringEncodings.Max(e => e.GetNullTerminator().Length);
        int maxBytesTotal = maxBytesFromRawString + maxBytesFromLengthPrefix + maxBytesFromTypePrefix
            + maxBytesFromNullTerminator;
        
        var bytesResult = ReadBytes(pointerValue, (ulong)maxBytesTotal);
        if (bytesResult.IsFailure)
            return new FindStringSettingsFailureOnStringReadFailure(bytesResult.Error);
        byte[] bytes = bytesResult.Value;
        
        // Iterate through all possible setting combinations from the considered setting values
        // Note that the order of the loops is important, as it determines the priority of the settings.
        // The order of values in the arrays is also important, as it determines the priority of the values.
        // This is important not only for performance, but also for accuracy (to return the most probable match first,
        // even when multiple combinations work).
        // For instance, the type prefix loop comes first, and should have a value of 0 at first, in order to prevent
        // false positives where the type prefix wrongly contains the length prefix.
        foreach (int typePrefixSize in FindStringTypePrefixSizes)
        {
            foreach (var encoding in FindStringEncodings)
            {
                foreach (int lengthPrefixSize in FindStringLengthPrefixSizes)
                {
                    foreach (var lengthPrefixUnit in FindStringLengthPrefixUnits)
                    {
                        // Avoid going multiple times through the same length prefix settings
                        if (lengthPrefixSize == 0 && lengthPrefixUnit != FindStringLengthPrefixUnits.First())
                            break;
                        
                        foreach (bool isNullTerminated in FindStringNullTerminated)
                        {
                            // Skip the case where the string is null-terminated but has no length prefix, as this is
                            // not a valid setting.
                            if (!isNullTerminated && lengthPrefixSize == 0)
                                continue;
                            
                            // Build a settings instance with the current combination
                            var settings = new StringSettings(encoding)
                            {
                                IsNullTerminated = isNullTerminated,
                                LengthPrefix = lengthPrefixSize > 0 ? new StringLengthPrefix(lengthPrefixSize,
                                    lengthPrefixUnit) : null,
                                TypePrefix = typePrefixSize > 0 ? bytes.Take(typePrefixSize).ToArray() : null
                            };
                            
                            // Try to read the string with the current settings
                            string? result = settings.GetString(bytes);
                            
                            // We have a match if the result matches the expected string.
                            // Otherwise, we continue with the next settings.
                            if (result == expectedString)
                                return settings;
                        }
                    }
                }
            }
        }
        
        // If we reach this point, we could not find any settings that would allow us to read the expected string.
        return new FindStringSettingsFailureOnNoSettingsFound();
    }
    
    #endregion
    
    #region String reading

    /// <summary>
    /// Reads a string from the address referred by the given pointer path, in the process memory.
    /// The address must point to the start of the actual string bytes. Consider <see cref="ReadStringPointer"/> to
    /// read strings from pointers and with added capabilities.
    /// Read the documentation for more information.
    /// </summary>
    /// <param name="pointerPath">Path to the first byte of the raw string in the process memory.</param>
    /// <param name="encoding">Encoding of the string to use when decoding. Try changing this parameter if you get
    /// garbage characters or empty strings. Common values include <see cref="Encoding.UTF8"/> and
    /// <see cref="Encoding.Unicode"/>.
    /// </param>
    /// <param name="maxLength">Maximum length of the string to read, in characters. If left null (default), the
    /// <see cref="DefaultMaxStringLength"/> will be used.</param>
    /// <param name="isNullTerminated">Boolean indicating if the string is null-terminated. If true, the string will be
    /// read until the first null character. If false, the string will be read up to the maximum length specified.
    /// </param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadRawString(PointerPath pointerPath, Encoding encoding,
        int? maxLength = null, bool isNullTerminated = true)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadRawString(addressResult.Value, encoding, maxLength, isNullTerminated)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a string from the given address in the process memory.
    /// The address must point to the start of the actual string bytes. Consider <see cref="ReadStringPointer"/> to
    /// read strings from pointers and with added capabilities.
    /// Read the documentation for more information.
    /// </summary>
    /// <param name="address">Address of the first byte of the raw string in the process memory.</param>
    /// <param name="encoding">Encoding of the string to use when decoding. Try changing this parameter if you get
    /// garbage characters or empty strings. Common values include <see cref="Encoding.UTF8"/> and
    /// <see cref="Encoding.Unicode"/>.
    /// </param>
    /// <param name="maxLength">Maximum length of the string to read, in characters. If left null (default), the
    /// <see cref="DefaultMaxStringLength"/> will be used.</param>
    /// <param name="isNullTerminated">Boolean indicating if the string is null-terminated. If true, the string will be
    /// read until the first null character. If false, the string will be read up to the maximum length specified.
    /// </param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadRawString(UIntPtr address, Encoding encoding,
        int? maxLength = null, bool isNullTerminated = true)
    {
        if (maxLength is < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "The maximum length cannot be negative.");
        
        // We don't know how many bytes the string will take, because encodings can have variable byte sizes.
        // So we read the maximum amount of bytes that the string could take, and then cut it to the max length.
        
        // Calculate the maximum byte size to read
        maxLength = maxLength ?? DefaultMaxStringLength;
        int byteSizeToRead = encoding.GetMaxByteCount(maxLength.Value)
            + (isNullTerminated ? encoding.GetNullTerminator().Length : 0);
        
        // Read the bytes
        //todo: Use a read method that handles cases where bytes are only partially read, because we don't want the
        //operation to fail if we read a string that's at the end of a region and the next region is not readable.
        var readResult = ReadBytes(address, (ulong)byteSizeToRead);
        if (readResult.IsFailure)
            return readResult.Error;
        
        // Convert the bytes to a string
        byte[] bytes = readResult.Value;
        string? result = new StringSettings(encoding, isNullTerminated).GetString(bytes);
        
        // Cut the string to the max length if needed
        if (result?.Length > maxLength)
            result = result[..maxLength.Value];
        
        return result ?? string.Empty;
    }

    /// <summary>
    /// Reads the string pointed by the pointer at the given address from the process memory.
    /// This method uses a <see cref="StringSettings"/> instance to determine how to read the string.
    /// </summary>
    /// <param name="address">Address of the pointer to the string in the process memory.</param>
    /// <param name="settings">Settings that define how to read the string. If you cannot figure out what settings to
    /// use, try <see cref="FindStringSettings(UIntPtr,string)"/> to automatically determine the right settings for a
    /// known string pointer. See the documentation for more information.</param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadStringPointer(UIntPtr address, StringSettings settings)
    {
        // The string settings will either have a null terminator or a length prefix.
        // The reading strategy depends on which of these two we have.
        
        // If we have a null terminator, read small chunks of bytes until we find a null terminator.
        // For this, we may need to make an alternative way to read bytes that returns a custom stream. The stream
        // would end whenever we reach unreadable memory.
        // In this method, we would read from the stream until we find a null terminator.
        // Multi-bytes null terminators could be handled this way too, because if we get a sequence of 2 null bytes,
        // we know that the null terminator is either at the start of the sequence, or the start of the sequence + 1.
        
        // If we have a length prefix, read the length prefix and then read the string bytes at once.

        throw new NotImplementedException();
    }
    
    #endregion
}