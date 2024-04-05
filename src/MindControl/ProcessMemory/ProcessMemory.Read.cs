using System.Diagnostics;
using System.Runtime.InteropServices;
using MindControl.Internal;

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
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public T? Read<T>(PointerPath pointerPath) => (T?)Read(typeof(T), pointerPath);

    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <typeparam name="T">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</typeparam>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public T? Read<T>(UIntPtr address) => (T?)Read(typeof(T), address);

    /// <summary>
    /// Reads a specific type of data from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public object? Read(Type dataType, PointerPath pointerPath)
        => Read(dataType, EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public object? Read(Type dataType, UIntPtr address)
    {
        if (dataType == typeof(bool)) return ReadBool(address);
        if (dataType == typeof(byte)) return ReadByte(address);
        if (dataType == typeof(short)) return ReadShort(address);
        if (dataType == typeof(ushort)) return ReadUShort(address);
        if (dataType == typeof(int)) return ReadInt(address);
        if (dataType == typeof(uint)) return ReadUInt(address);
        if (dataType == typeof(IntPtr)) return ReadIntPtr(address);
        if (dataType == typeof(float)) return ReadFloat(address);
        if (dataType == typeof(long)) return ReadLong(address);
        if (dataType == typeof(ulong)) return ReadULong(address);
        if (dataType == typeof(double)) return ReadDouble(address);
        
        // Not a primitive type. Try to read it as a structure.
        // To do that, we first have to determine the size of the structure.
        int size = Marshal.SizeOf(dataType);
        
        // Then we read the bytes from the process memory.
        var bytes = ReadBytes(address, (ulong)size);
        if (bytes == null)
            return null;
        
        // We have the bytes. Now we need to convert them to the structure.
        // We cannot use MemoryMarshal.Read here because the data type is a variable, not a generic type.
        // So we have to use a GCHandle to pin the bytes in memory and then use Marshal.PtrToStructure.
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var pointer = handle.AddrOfPinnedObject();
            return Marshal.PtrToStructure(pointer, dataType);
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
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public bool? ReadBool(PointerPath pointerPath) => ReadBool(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a boolean from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public bool? ReadBool(UIntPtr address)
    {
        var bytes = ReadBytes(address, 1);
        if (bytes == null)
            return null;
        
        return BitConverter.ToBoolean(bytes);
    }
    
    /// <summary>
    /// Reads a byte from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte? ReadByte(PointerPath pointerPath) => ReadByte(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a byte from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte? ReadByte(UIntPtr address)
    {
        var bytes = ReadBytes(address, 1);
        return bytes?[0];
    }
    
    /// <summary>
    /// Reads a short from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public short? ReadShort(PointerPath pointerPath) => ReadShort(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public short? ReadShort(UIntPtr address)
    {
        var bytes = ReadBytes(address, 2);
        if (bytes == null)
            return null;
        
        return BitConverter.ToInt16(bytes);
    }
    
    /// <summary>
    /// Reads an unsigned short from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ushort? ReadUShort(PointerPath pointerPath) => ReadUShort(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads an unsigned short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ushort? ReadUShort(UIntPtr address)
    {
        var bytes = ReadBytes(address, 2);
        if (bytes == null)
            return null;
        
        return BitConverter.ToUInt16(bytes);
    }
    
    /// <summary>
    /// Reads an integer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public int? ReadInt(PointerPath pointerPath) => ReadInt(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads an integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public int? ReadInt(UIntPtr address)
    {
        var bytes = ReadBytes(address, 4);
        if (bytes == null)
            return null;
        
        return BitConverter.ToInt32(bytes);
    }

    /// <summary>
    /// Reads an unsigned integer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public uint? ReadUInt(PointerPath pointerPath) => ReadUInt(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads an unsigned integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public uint? ReadUInt(UIntPtr address)
    {
        var bytes = ReadBytes(address, 4);
        if (bytes == null)
            return null;

        return BitConverter.ToUInt32(bytes);
    }

    /// <summary>
    /// Reads a pointer from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public UIntPtr? ReadIntPtr(PointerPath pointerPath) => ReadIntPtr(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);
    
    /// <summary>
    /// Reads a pointer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public UIntPtr? ReadIntPtr(UIntPtr address)
    {
        var bytes = ReadBytes(address, (ulong)IntPtr.Size);
        if (bytes == null)
            return null;
        return _is64Bits ?
            (UIntPtr)BitConverter.ToUInt64(bytes)
            : (UIntPtr)BitConverter.ToUInt32(bytes);
    }
    
    /// <summary>
    /// Reads a float from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public float? ReadFloat(PointerPath pointerPath) => ReadFloat(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a float from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public float? ReadFloat(UIntPtr address)
    {
        var bytes = ReadBytes(address, 4);
        if (bytes == null)
            return null;
        
        return BitConverter.ToSingle(bytes);
    }
    
    /// <summary>
    /// Reads a long from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public long? ReadLong(PointerPath pointerPath) => ReadLong(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public long? ReadLong(UIntPtr address)
    {
        var bytes = ReadBytes(address, 8);
        if (bytes == null)
            return null;
        
        return BitConverter.ToInt64(bytes);
    }
    
    /// <summary>
    /// Reads an unsigned long from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ulong? ReadULong(PointerPath pointerPath) => ReadULong(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads an unsigned long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ulong? ReadULong(UIntPtr address)
    {
        var bytes = ReadBytes(address, 8);
        if (bytes == null)
            return null;
        
        return BitConverter.ToUInt64(bytes);
    }
    
    /// <summary>
    /// Reads a double from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public double? ReadDouble(PointerPath pointerPath) => ReadDouble(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero);

    /// <summary>
    /// Reads a double from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public double? ReadDouble(UIntPtr address)
    {
        var bytes = ReadBytes(address, 8);
        if (bytes == null)
            return null;
        
        return BitConverter.ToDouble(bytes);
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
    /// <returns>The string read from memory, or null if any read operation fails.</returns>
    public string? ReadString(PointerPath pointerPath, int maxSizeInBytes = 256, StringSettings? stringSettings = null)
        => ReadString(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero, maxSizeInBytes, stringSettings);

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
    /// <returns>The string read from memory, or null if any read operation fails.</returns>
    public string? ReadString(UIntPtr address, int maxSizeInBytes = 256, StringSettings? stringSettings = null)
    {
        var actualStringSettings = stringSettings ?? GuessStringSettings();
        var lengthToRead = (ulong)maxSizeInBytes;

        if (actualStringSettings.PrefixSettings != null)
        {
            var lengthPrefixBytes = ReadBytes(address, actualStringSettings.PrefixSettings.PrefixSize);
            if (lengthPrefixBytes == null)
                return null;

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
        var bytes = ReadBytes(address, lengthToRead);
        if (bytes == null)
            return null;
        
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
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(PointerPath pointerPath, long length) => ReadBytes(pointerPath, (ulong)length);
    
    /// <summary>
    /// Reads a sequence of bytes from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(PointerPath pointerPath, ulong length)
        => ReadBytes(EvaluateMemoryAddress(pointerPath) ?? UIntPtr.Zero, length);

    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(UIntPtr address, long length) => ReadBytes(address, (ulong)length);
    
    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(UIntPtr address, ulong length)
    {
        if (address == UIntPtr.Zero || !IsBitnessCompatible(address))
            return null;
        return _osService.ReadProcessMemory(_processHandle, address, length);
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
        
        string[] moduleNames = _process.Modules
            .Cast<ProcessModule>()
            .Select(m => m.ModuleName?.ToLowerInvariant())
            .Where(m => m != null)
            .Cast<string>()
            .ToArray();
        
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