using System.Diagnostics;
using System.Numerics;
using MindControl.Internal;
using MindControl.Native;

namespace MindControl;

/// <summary>
/// Provides methods to manipulate the memory of a process.
/// </summary>
public class ProcessMemory : IDisposable
{
    private readonly Process _process;
    private readonly IOperatingSystemService _osService;
    private IntPtr _processHandle;
    private bool _is64Bits;
    
    /// <summary>
    /// Gets a value indicating if the process is currently attached or not.
    /// </summary>
    public bool IsAttached { get; private set; }

    /// <summary>
    /// Gets or sets the default way this instance deals with memory protection.
    /// This value is used when no strategy is specified in memory write operations.
    /// By default, this value will be <see cref="MemoryProtectionStrategy.RemoveAndRestore"/>.
    /// </summary>
    public MemoryProtectionStrategy DefaultStrategy { get; set; } = MemoryProtectionStrategy.RemoveAndRestore;

    /// <summary>
    /// Event raised when the process detaches for any reason.
    /// </summary>
    public event EventHandler? ProcessDetached;

    /// <summary>
    /// Attaches to a process with the given name and returns the resulting <see cref="ProcessMemory"/> instance.
    /// If multiple processes with the specified name are running, one of them will be targeted arbitrarily.
    /// When there is any risk of this happening, it is recommended to use <see cref="OpenProcessById"/> instead.
    /// </summary>
    /// <param name="processName">Name of the process to open.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    /// <exception cref="ProcessException">Thrown when no running process with the given name can be found.</exception>
    public static ProcessMemory OpenProcessByName(string processName)
    {
        var firstMatch = Process.GetProcessesByName(processName).FirstOrDefault();
        if (firstMatch == null)
            throw new ProcessException($"No running process with the name \"{processName}\" could be found.");
        return OpenProcess(firstMatch);
    }

    /// <summary>
    /// Attaches to the process with the given identifier and returns the resulting <see cref="ProcessMemory"/>
    /// instance.
    /// </summary>
    /// <param name="pid">Identifier of the process to attach to.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    public static ProcessMemory OpenProcessById(int pid)
    {
        var process = Process.GetProcessById(pid);
        if (process == null)
            throw new ProcessException(pid, $"No running process with the PID {pid} could be found.");
        return OpenProcess(process);
    }

    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    public static ProcessMemory OpenProcess(Process target)
    {
        if (target.HasExited)
            throw new ProcessException(target.Id, $"Process {target.Id} has exited.");

        return new ProcessMemory(target);
    }
    
    /// <summary>
    /// Builds a new instance that attaches to the given process.
    /// </summary>
    /// <param name="process">Target process.</param>
    public ProcessMemory(Process process) : this(process, new Win32Service()) {}

    /// <summary>
    /// Builds a new instance that attaches to the given process.
    /// Using this constructor directly is discouraged. See the static methods <see cref="OpenProcess"/>,
    /// <see cref="OpenProcessById"/> and <see cref="OpenProcessByName"/>.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="osService">Service that provides system-specific process-oriented features.</param>
    private ProcessMemory(Process process, IOperatingSystemService osService)
    {
        _process = process;
        _osService = osService;
        Attach();
    }
    
    /// <summary>
    /// Attaches to the process.
    /// </summary>
    private void Attach()
    {
        try
        {
            _is64Bits = _osService.IsProcess64Bits(_process.Id);
            
            if (_is64Bits && !Environment.Is64BitOperatingSystem)
                throw new ProcessException(_process.Id, "A 32-bit program cannot attach to a 64-bit process.");
            
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _processHandle = _osService.OpenProcess(_process.Id);

            IsAttached = true;
        }
        catch (Exception e)
        {
            Detach();
            throw new ProcessException(_process.Id, $"Failed to attach to the process {_process.Id}. Check the internal exception for more information. Common causes include insufficient privileges (administrator rights might be required) or trying to attach to a x64 process with a x86 program.", e);
        }
    }
    
    #region EvaluateMemoryAddress

    /// <summary>
    /// Returns True if and only if the given pointer is compatible with the bitness of the target process.
    /// In other words, returns false if the pointer is a 64-bit address but the target process is 32-bit. 
    /// </summary>
    /// <param name="pointer">Pointer to test.</param>
    private bool IsBitnessCompatible(IntPtr pointer) => _is64Bits || pointer.ToInt64() <= uint.MaxValue;
    
    /// <summary>
    /// Evaluates the given pointer path to the memory address it points to in the process.
    /// </summary>
    /// <param name="pointerPath">Pointer path to evaluate.</param>
    /// <returns>The memory address pointed by the pointer path.</returns>
    public IntPtr? EvaluateMemoryAddress(PointerPath pointerPath)
    {
        if (pointerPath.IsStrictly64Bits && (IntPtr.Size == 4 || !_is64Bits))
            throw new ArgumentException(
                $"The pointer path \"{pointerPath.Expression}\" uses addresses intended for a 64-bits process, but this instance is targeting a 32-bits process.");
        
        IntPtr? baseAddress = pointerPath.BaseModuleName != null
            ? GetModuleAddress(pointerPath.BaseModuleName)
            : ReadIntPtrFromBigInteger(pointerPath.PointerOffsets.FirstOrDefault());

        if (baseAddress == null || baseAddress == IntPtr.Zero)
            return null;

        if (pointerPath.BaseModuleOffset > 0)
            baseAddress = ReadIntPtrFromBigInteger(baseAddress.Value.ToInt64() + pointerPath.BaseModuleOffset);

        // Follow the pointer path offset by offset
        IntPtr currentAddress = baseAddress.Value;
        int startIndex = pointerPath.BaseModuleName == null ? 1 : 0;
        for (int i = startIndex; i < pointerPath.PointerOffsets.Length; i++)
        {
            // Read the value pointed by the current address as a pointer address
            IntPtr? nextAddress = ReadIntPtr(currentAddress);
            if (nextAddress == null || !IsBitnessCompatible(nextAddress.Value))
                return null; // Return null if the pointer is invalid (null pointer / 64-bit pointer in 32-bit process)

            // Apply the offset to the value we just read and keep going
            var offset = pointerPath.PointerOffsets[i];
            currentAddress = ReadIntPtrFromBigInteger(nextAddress.Value.ToInt64() + offset);
            if (currentAddress == IntPtr.Zero)
                return null;
        }

        return currentAddress;
    }

    /// <summary>
    /// Evaluates the given pointer path to the memory address it points to in the process.
    /// If the path does not evaluate to a proper address, throws a <see cref="MemoryException"/>.
    /// </summary>
    /// <param name="pointerPath">Pointer path to evaluate.</param>
    /// <returns>The memory address pointed by the pointer path.</returns>
    private IntPtr EvaluateMemoryAddressOrThrow(PointerPath pointerPath) => EvaluateMemoryAddress(pointerPath)
        ?? throw new MemoryException($"Could not evaluate pointer path \"{pointerPath}\".");

    /// <summary>
    /// Attempts to read an IntPtr from the given BigInteger value.
    /// </summary>
    /// <param name="value">Value to read as an IntPtr.</param>
    private static IntPtr ReadIntPtrFromBigInteger(BigInteger value)
    {
        return IntPtr.Size == 4 ? (IntPtr)(uint)value : (IntPtr)(ulong)value;
    }
    
    /// <summary>
    /// Gets the base address of the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The base address of the module if found, null otherwise.</returns>
    private IntPtr? GetModuleAddress(string moduleName)
    {
        return _process.Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
            ?.BaseAddress;
    }
    
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

    #region Read methods

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
    public T? Read<T>(IntPtr address) => (T?)Read(typeof(T), address);

    /// <summary>
    /// Reads a specific type of data from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public object? Read(Type dataType, PointerPath pointerPath)
        => Read(dataType, EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a specific type of data from the given address, in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="dataType">Type of data to read. Some types are not supported and will cause the method to throw
    /// an <see cref="ArgumentException"/>. Do not use Nullable types.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public object? Read(Type dataType, IntPtr address)
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

        throw new ArgumentException($"Reading the type \"{dataType.FullName}\" from memory is not supported.", nameof(dataType));
    }
    
    /// <summary>
    /// Reads a boolean from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public bool? ReadBool(PointerPath pointerPath) => ReadBool(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a boolean from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public bool? ReadBool(IntPtr address)
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
    public byte? ReadByte(PointerPath pointerPath) => ReadByte(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a byte from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte? ReadByte(IntPtr address)
    {
        var bytes = ReadBytes(address, 1);
        return bytes?[0];
    }
    
    /// <summary>
    /// Reads a short from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public short? ReadShort(PointerPath pointerPath) => ReadShort(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public short? ReadShort(IntPtr address)
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
    public ushort? ReadUShort(PointerPath pointerPath) => ReadUShort(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads an unsigned short from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ushort? ReadUShort(IntPtr address)
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
    public int? ReadInt(PointerPath pointerPath) => ReadInt(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads an integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public int? ReadInt(IntPtr address)
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
    public uint? ReadUInt(PointerPath pointerPath) => ReadUInt(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads an unsigned integer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public uint? ReadUInt(IntPtr address)
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
    public IntPtr? ReadIntPtr(PointerPath pointerPath) => ReadIntPtr(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);
    
    /// <summary>
    /// Reads a pointer from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public IntPtr? ReadIntPtr(IntPtr address)
    {
        var bytes = ReadBytes(address, (ulong)IntPtr.Size);
        if (bytes == null)
            return null;
        return IntPtr.Size == 4 ?
            (IntPtr)BitConverter.ToUInt32(bytes)
            : (IntPtr)BitConverter.ToUInt64(bytes);
    }
    
    /// <summary>
    /// Reads a float from the address referred by the given pointer path, in the process memory.
    /// </summary>
    /// <param name="pointerPath">Optimized, reusable path to the target address.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public float? ReadFloat(PointerPath pointerPath) => ReadFloat(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a float from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public float? ReadFloat(IntPtr address)
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
    public long? ReadLong(PointerPath pointerPath) => ReadLong(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public long? ReadLong(IntPtr address)
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
    public ulong? ReadULong(PointerPath pointerPath) => ReadULong(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads an unsigned long from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public ulong? ReadULong(IntPtr address)
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
    public double? ReadDouble(PointerPath pointerPath) => ReadDouble(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero);

    /// <summary>
    /// Reads a double from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public double? ReadDouble(IntPtr address)
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
        => ReadString(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero, maxSizeInBytes, stringSettings);

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
    public string? ReadString(IntPtr address, int maxSizeInBytes = 256, StringSettings? stringSettings = null)
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
        => ReadBytes(EvaluateMemoryAddress(pointerPath) ?? IntPtr.Zero, length);

    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(IntPtr address, long length) => ReadBytes(address, (ulong)length);
    
    /// <summary>
    /// Reads a sequence of bytes from the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The value read from the process memory, or null if no value could be read.</returns>
    public byte[]? ReadBytes(IntPtr address, ulong length)
    {
        if (address == IntPtr.Zero || !IsBitnessCompatible(address))
            return null;
        return _osService.ReadProcessMemory(_processHandle, address, length);
    }
    
    #endregion
    
    #region Write methods

    /// <summary>
    /// Writes a value to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    /// <typeparam name="T">Type of the value to write.</typeparam>
    /// <exception cref="ArgumentException">Thrown when the type of the value is not supported.</exception>
    public void Write<T>(PointerPath path, T value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => Write(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a value to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    /// <typeparam name="T">Type of the value to write.</typeparam>
    /// <exception cref="ArgumentException">Thrown when the type of the value is not supported.</exception>
    public void Write<T>(IntPtr address, T value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        switch (value)
        {
            case bool v: WriteBool(address, v, memoryProtectionStrategy); break;
            case byte v: WriteByte(address, v, memoryProtectionStrategy); break;
            case short v: WriteShort(address, v, memoryProtectionStrategy); break;
            case ushort v: WriteUShort(address, v, memoryProtectionStrategy); break;
            case int v: WriteInt(address, v, memoryProtectionStrategy); break;
            case uint v: WriteUInt(address, v, memoryProtectionStrategy); break;
            case IntPtr v: WriteIntPtr(address, v, memoryProtectionStrategy); break;
            case float v: WriteFloat(address, v, memoryProtectionStrategy); break;
            case long v: WriteLong(address, v, memoryProtectionStrategy); break;
            case ulong v: WriteULong(address, v, memoryProtectionStrategy); break;
            case double v: WriteDouble(address, v, memoryProtectionStrategy); break;
            case byte[] v: WriteBytes(address, v, memoryProtectionStrategy); break;
            default: throw new ArgumentException($"Writing a value of type \"{typeof(T)}\" is not supported.");
        }
    }
    
    /// <summary>
    /// Writes a boolean value to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteBool(PointerPath path, bool value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBool(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a boolean value to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteBool(IntPtr address, bool value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { (byte)(value ? 1 : 0) }, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a byte to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteByte(PointerPath path, byte value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteByte(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a byte to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteByte(IntPtr address, byte value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { value }, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteShort(PointerPath path, short value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteShort(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteShort(IntPtr address, short value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteUShort(PointerPath path, ushort value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteUShort(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteUShort(IntPtr address, ushort value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteInt(PointerPath path, int value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteInt(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteInt(IntPtr address, int value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteUInt(PointerPath path, uint value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteUInt(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteUInt(IntPtr address, uint value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a pointer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteIntPtr(PointerPath path, IntPtr value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteIntPtr(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a pointer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteIntPtr(IntPtr address, IntPtr value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, value.ToBytes(_is64Bits), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a float to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteFloat(PointerPath path, float value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteFloat(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a float to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteFloat(IntPtr address, float value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteLong(PointerPath path, long value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteLong(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteLong(IntPtr address, long value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteULong(PointerPath path, ulong value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteULong(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteULong(IntPtr address, ulong value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a double to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteDouble(PointerPath path, double value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteDouble(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a double to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteDouble(IntPtr address, double value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a sequence of bytes to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteBytes(PointerPath path, byte[] value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a sequence of bytes to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultStrategy"/> of this instance is used.</param>
    public void WriteBytes(IntPtr address, byte[] value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        // Remove protection if needed
        MemoryProtection? previousProtection = null;
        if (memoryProtectionStrategy is MemoryProtectionStrategy.Remove or MemoryProtectionStrategy.RemoveAndRestore)
        {
            previousProtection = _osService.ReadAndOverwriteProtection(
                _processHandle, _is64Bits, address, MemoryProtection.ExecuteReadWrite);
        }
        
        // Write memory
        _osService.WriteProcessMemory(_processHandle, address, value);
        
        // Restore protection if needed
        if (memoryProtectionStrategy == MemoryProtectionStrategy.RemoveAndRestore
            && previousProtection != MemoryProtection.ExecuteReadWrite)
        {
            _osService.ReadAndOverwriteProtection(_processHandle, _is64Bits, address, previousProtection!.Value);
        }
    }
    
    #endregion
    
    #region Dispose
    
    /// <summary>
    /// Detaches from the process.
    /// </summary>
    private void Detach()
    {
        IsAttached = false;
        _process.Exited -= OnProcessExited;
        ProcessDetached?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Event callback. Called when the attached process exits.
    /// Raises the related event.
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e) => Detach();

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() => Detach();

    #endregion
}