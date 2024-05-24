using System.Diagnostics;
using System.Runtime.InteropServices;
using MindControl.Internal;
using MindControl.Results;

namespace MindControl;

// This partial class implements the memory reading features of ProcessMemory.
public partial class ProcessMemory
{
    #region Public methods
    
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

    /// <summary>
    /// Reads a string from the address referred by the given pointer path, in the process memory.
    /// Optionally uses the given string settings and restricts the string to the specified max size.
    /// If no max size is specified, a default 256 bytes value is used.
    /// If no settings are specified, an attempt to guess the right settings will be made.
    /// If you are getting corrupt characters in your strings, try different string settings preset or a custom string
    /// settings instance. See the documentation for more information on reading strings.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="maxSizeInBytes">Maximal number of bytes to read as a string in memory. This value will be used as
    /// a fixed size when the string settings define no null terminator and no length prefix, but will restrict the
    /// number of bytes read in all cases. If not specified, a default value of 256 will be used.</param>
    /// <param name="stringSettings">Settings to use when attempting to read the string. Defines how the string is
    /// arranged in memory (encoding, null terminators and length prefix). If you are not sure, leaving this parameter
    /// to its default null value will cause an attempt to guess the right settings. If the guess does not seem to work,
    /// you can try one of the presets (e.g. <see cref="StringSettings.DefaultUtf8"/>), or try to figure out the details
    /// and provide your own string settings. It is recommended to at least use a preset, for performance and accuracy
    /// reasons.</param>
    /// <returns>The string read from memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadString(PointerPath pointerPath, int maxSizeInBytes = 256,
        StringSettings? stringSettings = null)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadString(addressResult.Value, maxSizeInBytes, stringSettings)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a string from the given address in the process memory.
    /// Optionally uses the given string settings and restricts the string to the specified max size.
    /// If no max size is specified, a default 256 bytes value is used.
    /// If no settings are specified, an attempt to guess the right settings will be made.
    /// If you are getting corrupt characters in your strings, try different string settings preset or a custom string
    /// settings instance. See the documentation for more information on reading strings.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="maxSizeInBytes">Maximal number of bytes to read as a string in memory. This value will be used as
    /// a fixed size when the string settings define no null terminator and no length prefix, but will restrict the
    /// number of bytes read in all cases. If not specified, a default value of 256 will be used.</param>
    /// <param name="stringSettings">Settings to use when attempting to read the string. Defines how the string is
    /// arranged in memory (encoding, null terminators and length prefix). If you are not sure, leaving this parameter
    /// to its default null value will cause an attempt to guess the right settings. If the guess does not seem to work,
    /// you can try one of the presets (e.g. <see cref="StringSettings.DefaultUtf8"/>), or try to figure out the details
    /// and provide your own string settings. It is recommended to at least use a preset, for performance and accuracy
    /// reasons.</param>
    /// <returns>The string read from memory, or a read failure.</returns>
    public Result<string, ReadFailure> ReadString(UIntPtr address, int maxSizeInBytes = 256,
        StringSettings? stringSettings = null)
    {
        var actualStringSettings = stringSettings ?? GuessStringSettings();
        var lengthToRead = (ulong)maxSizeInBytes;

        if (actualStringSettings.PrefixSettings != null)
        {
            var lengthPrefixBytesResult = ReadBytes(address, actualStringSettings.PrefixSettings.PrefixSize);
            
            if (lengthPrefixBytesResult.IsFailure)
                return lengthPrefixBytesResult.Error;

            byte[] lengthPrefixBytes = lengthPrefixBytesResult.Value;
            
            // Automatically determine the length unit if not provided:
            // Should be the minimal number of bytes supported by the encoding for a single character.
            // To get that, we make the encoding output the bytes for the string "a" and count the bytes.
            var lengthUnit = actualStringSettings.PrefixSettings.LengthUnit
                             ?? actualStringSettings.Encoding.GetBytes("a").Length;
            ulong lengthPrefixValue = lengthPrefixBytes.ReadUnsignedNumber() * (ulong)lengthUnit;
            
            lengthToRead = Math.Min(lengthToRead, lengthPrefixValue);
            address += actualStringSettings.PrefixSettings.PrefixSize;
        }

        // Read the actual bytes on the full length
        var stringBytesResult = ReadBytes(address, lengthToRead);
        if (!stringBytesResult.IsSuccess)
            return stringBytesResult.Error;
        byte[] bytes = stringBytesResult.Value;
        
        // Convert the whole byte array to a string
        string fullString = actualStringSettings.Encoding.GetString(bytes);
        
        // Cut it to the first null terminator if the settings allow it
        return actualStringSettings.IsNullTerminated ? fullString.Split('\0')[0] : fullString;
    }

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
    
    #region Utility
    
    /// <summary>
    /// Attempts to guess what string settings to use to read from or write into this process.
    /// </summary>
    private StringSettings GuessStringSettings()
    {
        // The idea is to try and find out what language/framework the process was compiled from, by looking for
        // specific markers of that language/framework.
        // If we can figure that out, we can pick the string setting preset that seems more likely.
        // For instance, in .net programs, strings will be UTF-16, with a prefixed length and null terminator.
        // If we can't figure anything out, we'll just return the default string settings.
        // This method is intended to provide a rough "guesstimation" that should work in as many cases as possible, as
        // an attempt to simplify things for hacking beginners who might not be able to figure out what their string
        // settings should be. It is designed to only be called when the user doesn't provide a string setting.
        
        var moduleNames = _process.Modules
            .Cast<ProcessModule>()
            .Select(m => m.ModuleName?.ToLowerInvariant())
            .Where(m => m != null)
            .Cast<string>()
            .ToHashSet();
        
        if (moduleNames.Contains("java.exe"))
            return StringSettings.Java;
        
        if (moduleNames.Contains("clrjit.dll")
            || moduleNames.Contains("unityplayer.dll")
            || moduleNames.Any(m => m.StartsWith("mono-")))
            return StringSettings.DotNet;

        // Any additional language/framework detection can be added here.
        // Be aware that this method might be called frequently. Mind the performance cost.
        // Implementing a cache might be a good idea if we need to analyze symbols or costly stuff like that.
        
        // If we're not in any of the specifically identified cases, return the catch-all default settings.
        // This setting might very well not work at all with the process, but we've done our best!
        return StringSettings.Default;
    }
    
    #endregion
}