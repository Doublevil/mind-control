using MindControl.Internal;
using MindControl.Native;

namespace MindControl;

// This partial class implements the memory writing features of ProcessMemory.
public partial class ProcessMemory
{
    /// <summary>
    /// Writes a value to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
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
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <typeparam name="T">Type of the value to write.</typeparam>
    /// <exception cref="ArgumentException">Thrown when the type of the value is not supported.</exception>
    public void Write<T>(UIntPtr address, T value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The value to write cannot be null.");
        
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
            default: WriteBytes(address, value.ToBytes(), memoryProtectionStrategy); break;
        }
    }
    
    /// <summary>
    /// Writes a boolean value to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteBool(PointerPath path, bool value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBool(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a boolean value to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteBool(UIntPtr address, bool value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { (byte)(value ? 1 : 0) }, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a byte to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteByte(PointerPath path, byte value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteByte(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a byte to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteByte(UIntPtr address, byte value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { value }, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteShort(PointerPath path, short value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteShort(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteShort(UIntPtr address, short value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteUShort(PointerPath path, ushort value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteUShort(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteUShort(UIntPtr address, ushort value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteInt(PointerPath path, int value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteInt(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteInt(UIntPtr address, int value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteUInt(PointerPath path, uint value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteUInt(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteUInt(UIntPtr address, uint value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a pointer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteIntPtr(PointerPath path, IntPtr value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteIntPtr(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a pointer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteIntPtr(UIntPtr address, IntPtr value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, value.ToBytes(_is64Bits), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a float to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteFloat(PointerPath path, float value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteFloat(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a float to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteFloat(UIntPtr address, float value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteLong(PointerPath path, long value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteLong(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteLong(UIntPtr address, long value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteULong(PointerPath path, ulong value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteULong(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes an unsigned long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteULong(UIntPtr address, ulong value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a double to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteDouble(PointerPath path, double value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteDouble(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a double to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteDouble(UIntPtr address, double value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a sequence of bytes to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteBytes(PointerPath path, byte[] value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(EvaluateMemoryAddressOrThrow(path), value, memoryProtectionStrategy);
    
    /// <summary>
    /// Writes a sequence of bytes to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    public void WriteBytes(UIntPtr address, byte[] value, MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        // Remove protection if needed
        memoryProtectionStrategy ??= DefaultWriteStrategy;
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
}