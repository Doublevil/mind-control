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
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <typeparam name="T">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</typeparam>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<T, ReadFailure> Read<T>(PointerPath pointerPath)
    {
        var objectReadResult = Read(typeof(T), pointerPath);
        return objectReadResult.IsSuccess ? (T)objectReadResult.Value : objectReadResult.Error;
    }

    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <typeparam name="T">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use reference (nullable) types.</typeparam>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<T, ReadFailure> Read<T>(UIntPtr address)
    {
        var objectReadResult = Read(typeof(T), address);
        return objectReadResult.IsSuccess ? (T)objectReadResult.Value : objectReadResult.Error;
    }

    /// <summary>
    /// Reads a specific type of data from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use reference (nullable) types.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<object, ReadFailure> Read(Type dataType, PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? Read(dataType, addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<object, ReadFailure> Read(Type dataType, UIntPtr address)
    {
        if (dataType == typeof(bool)) return Result<object, ReadFailure>.CastValueFrom(ReadBool(address));
        if (dataType == typeof(byte)) return Result<object, ReadFailure>.CastValueFrom(ReadByte(address));
        if (dataType == typeof(short)) return Result<object, ReadFailure>.CastValueFrom(ReadShort(address));
        if (dataType == typeof(ushort)) return Result<object, ReadFailure>.CastValueFrom(ReadUShort(address));
        if (dataType == typeof(int)) return Result<object, ReadFailure>.CastValueFrom(ReadInt(address));
        if (dataType == typeof(uint)) return Result<object, ReadFailure>.CastValueFrom(ReadUInt(address));
        if (dataType == typeof(IntPtr)) return Result<object, ReadFailure>.CastValueFrom(ReadIntPtr(address));
        if (dataType == typeof(float)) return Result<object, ReadFailure>.CastValueFrom(ReadFloat(address));
        if (dataType == typeof(long)) return Result<object, ReadFailure>.CastValueFrom(ReadLong(address));
        if (dataType == typeof(ulong)) return Result<object, ReadFailure>.CastValueFrom(ReadULong(address));
        if (dataType == typeof(double)) return Result<object, ReadFailure>.CastValueFrom(ReadDouble(address));
        
        // Not a primitive type. Try to read it as a structure.
        // To do that, we first have to determine the size of the structure.
        int size = Marshal.SizeOf(dataType);
        
        // Then we read the bytes from the process memory.
        var bytesResult = ReadBytes(address, (ulong)size);
        if (!bytesResult.IsSuccess)
            return bytesResult;
        
        // We have the bytes. Now we need to convert them to the structure.
        // We cannot use MemoryMarshal.Read here because the data type is a variable, not a generic type.
        // So we have to use a GCHandle to pin the bytes in memory and then use Marshal.PtrToStructure.
        var handle = GCHandle.Alloc(bytesResult.Value, GCHandleType.Pinned);
        try
        {
            var pointer = handle.AddrOfPinnedObject();
            object? structure = Marshal.PtrToStructure(pointer, dataType);
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
    /// Reads a boolean from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<bool, ReadFailure> ReadBool(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadBool(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a boolean from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<bool, ReadFailure> ReadBool(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 1);
        if (bytesResult.IsFailure)
            return bytesResult.Error;
        return BitConverter.ToBoolean(bytesResult.Value);
    }

    /// <summary>
    /// Reads a byte from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte, ReadFailure> ReadByte(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadByte(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a byte from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<byte, ReadFailure> ReadByte(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 1);
        return bytesResult.IsSuccess ? bytesResult.Value[0] : bytesResult.Error;
    }

    /// <summary>
    /// Reads a short from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<short, ReadFailure> ReadShort(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadShort(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<short, ReadFailure> ReadShort(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 2);
        return bytesResult.IsSuccess ? BitConverter.ToInt16(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads an unsigned short from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<ushort, ReadFailure> ReadUShort(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadUShort(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads an unsigned short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<ushort, ReadFailure> ReadUShort(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 2);
        return bytesResult.IsSuccess ? BitConverter.ToUInt16(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads an integer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<int, ReadFailure> ReadInt(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadInt(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads an integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<int, ReadFailure> ReadInt(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 4);
        return bytesResult.IsSuccess ? BitConverter.ToInt32(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads an unsigned integer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<uint, ReadFailure> ReadUInt(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadUInt(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads an unsigned integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<uint, ReadFailure> ReadUInt(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 4);
        return bytesResult.IsSuccess ? BitConverter.ToUInt32(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads a pointer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<UIntPtr, ReadFailure> ReadIntPtr(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadIntPtr(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Reads a pointer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<UIntPtr, ReadFailure> ReadIntPtr(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, (ulong)IntPtr.Size);
        if (bytesResult.IsFailure)
            return bytesResult.Error;
        
        byte[] bytes = bytesResult.Value;
        return _is64Bits ? (UIntPtr)BitConverter.ToUInt64(bytes)
            : (UIntPtr)BitConverter.ToUInt32(bytes);
    }

    /// <summary>
    /// Reads a float from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<float, ReadFailure> ReadFloat(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadFloat(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a float from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<float, ReadFailure> ReadFloat(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 4);
        return bytesResult.IsSuccess ? BitConverter.ToSingle(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads a long from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<long, ReadFailure> ReadLong(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadLong(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<long, ReadFailure> ReadLong(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 8);
        return bytesResult.IsSuccess ? BitConverter.ToInt64(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads an unsigned long from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<ulong, ReadFailure> ReadULong(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadULong(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads an unsigned long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<ulong, ReadFailure> ReadULong(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 8);
        return bytesResult.IsSuccess ? BitConverter.ToUInt64(bytesResult.Value) : bytesResult.Error;
    }

    /// <summary>
    /// Reads a double from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<double, ReadFailure> ReadDouble(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        return addressResult.IsSuccess ? ReadDouble(addressResult.Value)
            : new ReadFailureOnPointerPathEvaluation(addressResult.Error);
    }

    /// <summary>
    /// Reads a double from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or a read failure.</returns>
    public Result<double, ReadFailure> ReadDouble(UIntPtr address)
    {
        var bytesResult = ReadBytes(address, 8);
        return bytesResult.IsSuccess ? BitConverter.ToDouble(bytesResult.Value) : bytesResult.Error;
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