using System.Runtime.InteropServices;
using System.Text;
using MindControl.Internal;
using MindControl.Results;

namespace MindControl;

// This partial class implements the memory reading features of ProcessMemory.
public partial class ProcessMemory
{
    /// <summary>
    /// Gets or sets the default maximum length of strings to read with
    /// <see cref="ReadRawString(UIntPtr,Encoding,System.Nullable{int},bool)"/> when the length is not specified.
    /// The default value is an arbitrary 100.
    /// </summary>
    public int DefaultRawStringMaxLength { get; set; } = 100;
    
    #region Bytes reading
    
    /// <summary>
    /// Reads a sequence of bytes from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte[], ReadFailure> ReadBytes(PointerPath pointerPath, long length)
    {
        if (length < 0)
            return new ReadFailureOnInvalidArguments("The length to read cannot be negative.");
        
        return ReadBytes(pointerPath, (ulong)length);
    }

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
    {
        if (length < 0)
            return new ReadFailureOnInvalidArguments("The length to read cannot be negative.");
        
        return ReadBytes(address, (ulong)length);
    }

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
        
        var readResult = _osService.ReadProcessMemory(ProcessHandle, address, length);
        return readResult.IsSuccess ? readResult.Value
            : new ReadFailureOnSystemRead(readResult.Error);
    }

    /// <summary>
    /// Reads a sequence of bytes from the address pointed by the given path in the process memory. The resulting bytes
    /// are read into the provided buffer array. Unlike <see cref="ReadBytes(UIntPtr,long)"/>, this method succeeds even
    /// when only a part of the bytes are read.
    /// Use it when you are not sure how many bytes you need to read, or when you want to read as many bytes as
    /// possible.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the target address in the process memory.</param>
    /// <param name="buffer">Buffer to store the bytes read.</param>
    /// <param name="maxLength">Number of bytes to read, at most.</param>
    /// <returns>The value read from the process memory, or a read failure in case no bytes could be read.</returns>
    public Result<ulong, ReadFailure> ReadBytesPartial(PointerPath pointerPath, byte[] buffer, ulong maxLength)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadBytesPartial(addressResult.Value, buffer, maxLength)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory. The resulting bytes are read into the
    /// provided buffer array. Unlike <see cref="ReadBytes(UIntPtr,long)"/>, this method succeeds even when only a part
    /// of the bytes are read.
    /// Use it when you are not sure how many bytes you need to read, or when you want to read as many bytes as
    /// possible.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="buffer">Buffer to store the bytes read.</param>
    /// <param name="maxLength">Number of bytes to read, at most.</param>
    /// <returns>The value read from the process memory, or a read failure in case no bytes could be read.</returns>
    public Result<ulong, ReadFailure> ReadBytesPartial(UIntPtr address, byte[] buffer, ulong maxLength)
    {
        if (maxLength == 0)
            return 0;
        if ((ulong)buffer.Length < maxLength)
            return new ReadFailureOnInvalidArguments("The buffer length must be at least the provided length to read.");
        if (address == UIntPtr.Zero)
            return new ReadFailureOnZeroPointer();
        if (!IsBitnessCompatible(address))
            return new ReadFailureOnIncompatibleBitness(address);
        
        var readResult = _osService.ReadProcessMemoryPartial(ProcessHandle, address, buffer, 0, maxLength);
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
        
        // Exception for UIntPtr to use the size of the attached process platform, not the system platform
        if (typeof(T) == typeof(UIntPtr))
            size = Is64Bit ? 8 : 4;
        else
        {
            try
            {
                size = Marshal.SizeOf<T>();
            }
            catch (ArgumentException)
            {
                return new ReadFailureOnConversionFailure();
            }
        }

        // Read the bytes from the process memory
        var readResult = _osService.ReadProcessMemory(ProcessHandle, address, (ulong)size);
        if (readResult.IsFailure)
            return new ReadFailureOnSystemRead(readResult.Error);
        byte[] bytes = readResult.Value;
        
        // Convert the bytes into the target type
        try
        {
            // Exception for UIntPtr to use the size of the attached process platform, not the system platform
            if (typeof(T) == typeof(UIntPtr) && !Is64Bit)
                return (T)(object)new UIntPtr(BitConverter.ToUInt32(bytes, 0));
            
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
        var readResult = _osService.ReadProcessMemory(ProcessHandle, address, (ulong)size);
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
    private static readonly StringLengthUnit[] FindStringLengthPrefixUnits =
        { StringLengthUnit.Bytes, StringLengthUnit.Characters };
    
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
        // false positives where the type prefix mistakenly contains the length prefix.
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
    /// Maximum size of the stream buffer when reading strings.
    /// Setting it too low may multiply the number of reads, which is inefficient.
    /// Setting it too high will lead to very large, wasteful reads for bigger strings, and may also trigger reading
    /// failures, with failure mitigation algorithms kicking in but taking resources that might not be needed.
    /// </summary>
    private const int StringReadingBufferMaxSize = 512;

    /// <summary>
    /// Size of the stream buffer when reading strings with an unknown length.
    /// Setting it too low may multiply the number of reads, which is inefficient.
    /// Setting it too high may lead to wasteful reads for smaller strings.
    /// </summary>
    /// <remarks>
    /// This setting is related to <see cref="StringReadingBuilderDefaultSize"/>, the difference being that this one
    /// measures the size in bytes, while the other measures the size in characters.
    /// Setting this to 2 times the other allows for better performance with characters encoded on fewer bytes.
    /// Setting this to 4 times the other allows for better performance with characters encoded on more bytes.
    /// </remarks>
    private const int StringReadingBufferDefaultSize = 128;
    
    /// <summary>
    /// Initial size of the string builder when reading characters of a string that has an unknown length.
    /// Setting this value too low may lead to multiple resizes of the string builder, which is inefficient.
    /// Setting this value too high may lead to unnecessary memory usage.
    /// </summary>
    private const int StringReadingBuilderDefaultSize = 32;
    
    /// <summary>
    /// Reads a string from the address referred by the given pointer path, in the process memory.
    /// The address must point to the start of the actual string bytes. Consider
    /// <see cref="ReadStringPointer(PointerPath,StringSettings)"/> to read strings from pointers more efficiently.
    /// Read the documentation for more information.
    /// </summary>
    /// <param name="pointerPath">Path to the first byte of the raw string in the process memory.</param>
    /// <param name="encoding">Encoding of the string to use when decoding. Try changing this parameter if you get
    /// garbage characters or empty strings. Common values include <see cref="Encoding.UTF8"/> and
    /// <see cref="Encoding.Unicode"/>.
    /// </param>
    /// <param name="maxLength">Maximum length of the string to read, in characters. If left null (default), the
    /// <see cref="DefaultRawStringMaxLength"/> will be used.</param>
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
    /// The address must point to the start of the actual string bytes. Consider
    /// <see cref="ReadStringPointer(UIntPtr,StringSettings)"/> to read strings from pointers more efficiently.
    /// Read the documentation for more information.
    /// </summary>
    /// <param name="address">Address of the first byte of the raw string in the process memory.</param>
    /// <param name="encoding">Encoding of the string to use when decoding. Try changing this parameter if you get
    /// garbage characters or empty strings. Common values include <see cref="Encoding.UTF8"/> and
    /// <see cref="Encoding.Unicode"/>.
    /// </param>
    /// <param name="maxLength">Maximum length of the string to read, in characters. If left null (default), the
    /// <see cref="DefaultRawStringMaxLength"/> will be used.</param>
    /// <param name="isNullTerminated">Boolean indicating if the string is null-terminated. If true, the string will be
    /// read until the first null character. If false, the string will be read up to the maximum length specified.
    /// </param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadRawString(UIntPtr address, Encoding encoding,
        int? maxLength = null, bool isNullTerminated = true)
    {
        if (maxLength is < 0)
            return new ReadFailureOnInvalidArguments("The maximum length cannot be negative.");
        if (maxLength == 0)
            return string.Empty;
        
        // We don't know how many bytes the string will take, because encodings can have variable byte sizes.
        // So we read the maximum amount of bytes that the string could take, and then cut it to the max length.
        
        // Calculate the maximum byte size to read
        maxLength = maxLength ?? DefaultRawStringMaxLength;
        int byteSizeToRead = encoding.GetMaxByteCount(maxLength.Value)
            + (isNullTerminated ? encoding.GetNullTerminator().Length : 0);
        
        // Read the bytes using a buffer (in case we can't read the whole max size)
        var buffer = new byte[byteSizeToRead];
        var readResult = ReadBytesPartial(address, buffer, (ulong)byteSizeToRead);
        if (readResult.IsFailure)
            return readResult.Error;
        
        // Check the number of bytes read
        ulong readByteCount = readResult.Value;
        if (readByteCount == 0)
            return string.Empty;
        
        // Convert the bytes to a string
        var readBytes = buffer.AsSpan(0, (int)readByteCount);
        string? result = new StringSettings(encoding, isNullTerminated).GetString(readBytes);
        
        // Cut the string to the max length if needed
        if (result?.Length > maxLength)
            result = result[..maxLength.Value];
        
        return result ?? string.Empty;
    }

    /// <summary>
    /// Reads the string pointed by the pointer evaluated from the given pointer path from the process memory.
    /// This method uses a <see cref="StringSettings"/> instance to determine how to read the string.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the pointer to the string in the process memory.</param>
    /// <param name="settings">Settings that define how to read the string. If you cannot figure out what settings to
    /// use, try <see cref="FindStringSettings(UIntPtr,string)"/> to automatically determine the right settings for a
    /// known string pointer. See the documentation for more information.</param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    public Result<string, StringReadFailure> ReadStringPointer(PointerPath pointerPath, StringSettings settings)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadStringPointer(addressResult.Value, settings)
            : new StringReadFailureOnPointerPathEvaluation(addressResult.Error);
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
    public Result<string, StringReadFailure> ReadStringPointer(UIntPtr address, StringSettings settings)
    {
        if (!IsBitnessCompatible(address))
            return new StringReadFailureOnIncompatibleBitness(address);
        if (address == UIntPtr.Zero)
            return new StringReadFailureOnZeroPointer();
        if (!settings.IsValid)
            return new StringReadFailureOnInvalidSettings();
        
        // Start by reading the address of the string bytes
        var stringAddressResult = Read<UIntPtr>(address);
        if (stringAddressResult.IsFailure)
            return new StringReadFailureOnPointerReadFailure(stringAddressResult.Error);
        if (stringAddressResult.Value == UIntPtr.Zero)
            return new StringReadFailureOnZeroPointer();
        var stringAddress = stringAddressResult.Value;
        
        // The string settings will either have a null terminator or a length prefix.
        // The length prefix can also either be in bytes or in characters.
        // That leaves us with 3 potential ways to read the string.
        // Each way will have a different implementation.
        
        // In case we have both a length prefix and a null terminator, we will prioritize the length prefix, as it is
        // more reliable and usually more efficient.
        
        if (settings.LengthPrefix?.Unit == StringLengthUnit.Bytes)
            return ReadStringWithLengthPrefixInBytes(stringAddress, settings);
        if (settings.LengthPrefix?.Unit == StringLengthUnit.Characters)
            return ReadStringWithLengthPrefixInCharacters(stringAddress, settings);
        
        // If we reach this point, we have a null-terminated string, because settings must have either a length prefix
        // or a null terminator, otherwise they're not valid and already caused a failure.
        return ReadStringWithNullTerminator(stringAddress, settings);
    }
    
    /// <summary>
    /// Reads a string starting at the given address, using the specified settings, and assuming that the settings
    /// have a length prefix in bytes.
    /// </summary>
    /// <param name="address">Address of the string in the process memory.</param>
    /// <param name="settings">Settings that define how to read the string.</param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    private Result<string, StringReadFailure> ReadStringWithLengthPrefixInBytes(UIntPtr address,
        StringSettings settings)
    {
        // The strategy when the string has a length prefix in bytes is the easiest one:
        // Read the length prefix, then we know exactly how many bytes to read, and then use the encoding to decode
        // the bytes read.
        
        // Read the length prefix
        int lengthPrefixOffset = settings.TypePrefix?.Length ?? 0;
        var lengthPrefixAddress = (UIntPtr)(address.ToUInt64() + (ulong)lengthPrefixOffset);
        var lengthPrefixBytesResult = ReadBytes(lengthPrefixAddress, settings.LengthPrefix!.Size);
        if (lengthPrefixBytesResult.IsFailure)
            return new StringReadFailureOnStringBytesReadFailure(lengthPrefixAddress, lengthPrefixBytesResult.Error);
        ulong length = lengthPrefixBytesResult.Value.ReadUnsignedNumber();
        
        if (length == 0)
            return string.Empty;
        if (length > (ulong)settings.MaxLength)
            return new StringReadFailureOnStringTooLong(length);
        
        // Read the string bytes (after the prefixes)
        int stringBytesOffset = lengthPrefixOffset + settings.LengthPrefix.Size;
        var stringBytesAddress = (UIntPtr)(address.ToUInt64() + (ulong)stringBytesOffset);
        var stringBytesResult = ReadBytes(stringBytesAddress, length);
        if (stringBytesResult.IsFailure)
            return new StringReadFailureOnStringBytesReadFailure(stringBytesAddress, lengthPrefixBytesResult.Error);
        var stringBytes = stringBytesResult.Value;
        
        // Decode the bytes into a string using the encoding specified in the settings
        return settings.Encoding.GetString(stringBytes);
    }
    
    /// <summary>
    /// Reads a string starting at the given address, using the specified settings, and assuming that the settings
    /// have a length prefix in characters.
    /// </summary>
    /// <param name="address">Address of the string in the process memory.</param>
    /// <param name="settings">Settings that define how to read the string.</param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    private Result<string, StringReadFailure> ReadStringWithLengthPrefixInCharacters(UIntPtr address,
        StringSettings settings)
    {
        // The strategy when the string has a length prefix in characters is the following:
        // Read the length prefix, then get a stream that reads bytes from the process memory, and use a stream reader
        // to read characters until we reach the length specified by the length prefix.
        
        // Read the length prefix
        int lengthPrefixOffset = settings.TypePrefix?.Length ?? 0;
        var lengthPrefixAddress = (UIntPtr)(address.ToUInt64() + (ulong)lengthPrefixOffset);
        var lengthPrefixBytesResult = ReadBytes(lengthPrefixAddress, settings.LengthPrefix!.Size);
        if (lengthPrefixBytesResult.IsFailure)
            return new StringReadFailureOnStringBytesReadFailure(lengthPrefixAddress, lengthPrefixBytesResult.Error);
        ulong expectedStringLength = lengthPrefixBytesResult.Value.ReadUnsignedNumber();

        if (expectedStringLength == 0)
            return string.Empty;
        if (expectedStringLength > (ulong)settings.MaxLength)
            return new StringReadFailureOnStringTooLong(expectedStringLength);
        
        // Get a memory stream after the prefixes
        int stringBytesOffset = lengthPrefixOffset + settings.LengthPrefix.Size;
        int bufferSize = Math.Min(settings.Encoding.GetMaxByteCount((int)expectedStringLength),
            StringReadingBufferMaxSize);
        using var stream = GetMemoryStream(address + (UIntPtr)stringBytesOffset);
        using var streamReader = new StreamReader(stream, settings.Encoding, false, bufferSize, false);
        
        // Read characters until we reach the expected length
        // Using a char array here is a very small optimization over using a string builder
        var characters = new char[expectedStringLength];
        for (ulong i = 0; i < expectedStringLength; i++)
        {
            int nextChar = streamReader.Read();
            
            // If we reach the end of the stream, return the string we have so far
            if (nextChar == -1)
                return new string(characters, 0, (int)i);

            characters[i] = (char)nextChar;
        }

        return new string(characters);
    }
    
    /// <summary>
    /// Reads a string starting at the given address, using the specified settings, and assuming that the settings
    /// have a null terminator.
    /// </summary>
    /// <param name="address">Address of the string in the process memory.</param>
    /// <param name="settings">Settings that define how to read the string.</param>
    /// <returns>The string read from the process memory, or a read failure.</returns>
    private Result<string, StringReadFailure> ReadStringWithNullTerminator(UIntPtr address, StringSettings settings)
    {
        // The strategy when the string has no length prefix but has a null-terminator is the following:
        // Get a stream that reads bytes from the process memory, and use a stream reader to read characters until we
        // find a null terminator.
        
        // Performance note: Because we cannot know in advance how long the string is going to be, we have to use
        // default values for buffer size and string builder size.
        // This can result in wasteful memory usage, or multiple read operations, depending on the size of the string.
        // But there is no way around this.
        
        // Get a memory stream after the prefixes
        int stringBytesOffset = (settings.TypePrefix?.Length ?? 0) + (settings.LengthPrefix?.Size ?? 0);
        using var stream = GetMemoryStream(address + (UIntPtr)stringBytesOffset);
        using var streamReader = new StreamReader(stream, settings.Encoding, false, StringReadingBufferDefaultSize,
            false);
        
        // Read characters until we reach a null terminator (0) or the end of the stream (-1)
        var stringBuilder = new StringBuilder(StringReadingBuilderDefaultSize);
        for (var nextChar = streamReader.Read(); nextChar != -1 && nextChar != 0; nextChar = streamReader.Read())
        {
            stringBuilder.Append((char)nextChar);
            
            if (stringBuilder.Length > settings.MaxLength)
                return new StringReadFailureOnStringTooLong(null);
        }

        return stringBuilder.ToString();
    }
    
    #endregion
}