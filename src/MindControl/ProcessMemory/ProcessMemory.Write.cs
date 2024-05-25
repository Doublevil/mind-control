using MindControl.Internal;
using MindControl.Native;
using MindControl.Results;

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
    /// <returns>A successful result, or a write failure</returns>
    public Result<WriteFailure> Write<T>(PointerPath path, T value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? Write(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a value to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <typeparam name="T">Type of the value to write.</typeparam>
    /// <exception cref="ArgumentException">Thrown when the type of the value is not supported.</exception>
    /// <returns>A successful result, or a write failure</returns>
    public Result<WriteFailure> Write<T>(UIntPtr address, T value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The value to write cannot be null.");

        return value switch
        {
            bool v => WriteBool(address, v, memoryProtectionStrategy),
            byte v => WriteByte(address, v, memoryProtectionStrategy),
            short v => WriteShort(address, v, memoryProtectionStrategy),
            ushort v => WriteUShort(address, v, memoryProtectionStrategy),
            int v => WriteInt(address, v, memoryProtectionStrategy),
            uint v => WriteUInt(address, v, memoryProtectionStrategy),
            IntPtr v => WriteIntPtr(address, v, memoryProtectionStrategy),
            float v => WriteFloat(address, v, memoryProtectionStrategy),
            long v => WriteLong(address, v, memoryProtectionStrategy),
            ulong v => WriteULong(address, v, memoryProtectionStrategy),
            double v => WriteDouble(address, v, memoryProtectionStrategy),
            byte[] v => WriteBytes(address, v, memoryProtectionStrategy),
            _ => WriteBytes(address, value.ToBytes(), memoryProtectionStrategy)
        };
    }

    /// <summary>
    /// Writes a boolean value to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteBool(PointerPath path, bool value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteBool(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a boolean value to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write. True will be written as a byte with the value 1. False will be written
    /// as a byte with the value 0.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteBool(UIntPtr address, bool value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { (byte)(value ? 1 : 0) }, memoryProtectionStrategy);

    /// <summary>
    /// Writes a byte to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteByte(PointerPath path, byte value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteByte(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a byte to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteByte(UIntPtr address, byte value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, new[] { value }, memoryProtectionStrategy);

    /// <summary>
    /// Writes a short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteShort(PointerPath path, short value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteShort(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteShort(UIntPtr address, short value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an unsigned short to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteUShort(PointerPath path, ushort value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteUShort(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes an unsigned short to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteUShort(UIntPtr address, ushort value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteInt(PointerPath path, int value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteInt(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes an integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteInt(UIntPtr address, int value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an unsigned integer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteUInt(PointerPath path, uint value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteUInt(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes an unsigned integer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteUInt(UIntPtr address, uint value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes a pointer to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteIntPtr(PointerPath path, IntPtr value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteIntPtr(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a pointer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteIntPtr(UIntPtr address, IntPtr value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, value.ToBytes(_is64Bits), memoryProtectionStrategy);

    /// <summary>
    /// Writes a float to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteFloat(PointerPath path, float value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteFloat(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a float to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteFloat(UIntPtr address, float value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes a long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteLong(PointerPath path, long value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteLong(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteLong(UIntPtr address, long value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes an unsigned long to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteULong(PointerPath path, ulong value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteULong(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes an unsigned long to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteULong(UIntPtr address, ulong value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes a double to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteDouble(PointerPath path, double value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteDouble(addressResult.Value, value, memoryProtectionStrategy)
            : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a double to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteDouble(UIntPtr address, double value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, BitConverter.GetBytes(value), memoryProtectionStrategy);

    /// <summary>
    /// Writes a sequence of bytes to the address referred by the given pointer path in the process memory.
    /// </summary>
    /// <param name="path">Optimized, reusable path to the target address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    public Result<WriteFailure> WriteBytes(PointerPath path, byte[] value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        var addressResult = EvaluateMemoryAddress(path);
        return addressResult.IsSuccess ? WriteBytes(addressResult.Value, value, memoryProtectionStrategy)
                : new WriteFailureOnPointerPathEvaluation(addressResult.Error);
    }
    
    /// <summary>
    /// Writes a sequence of bytes to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    public Result<WriteFailure> WriteBytes(UIntPtr address, byte[] value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        // Remove protection if needed
        memoryProtectionStrategy ??= DefaultWriteStrategy;
        MemoryProtection? previousProtection = null;
        if (memoryProtectionStrategy is MemoryProtectionStrategy.Remove or MemoryProtectionStrategy.RemoveAndRestore)
        {
            var protectionRemovalResult = _osService.ReadAndOverwriteProtection(_processHandle, _is64Bits,
                address, MemoryProtection.ExecuteReadWrite);
            
            if (protectionRemovalResult.IsFailure)
                return new WriteFailureOnSystemProtectionRemoval(address, protectionRemovalResult.Error);

            previousProtection = protectionRemovalResult.Value;
        }
        
        // Write memory
        var writeResult = _osService.WriteProcessMemory(_processHandle, address, value);
        if (writeResult.IsFailure)
            return new WriteFailureOnSystemWrite(address, writeResult.Error);
        
        // Restore protection if needed
        if (memoryProtectionStrategy == MemoryProtectionStrategy.RemoveAndRestore
            && previousProtection != MemoryProtection.ExecuteReadWrite)
        {
            var protectionRestorationResult = _osService.ReadAndOverwriteProtection(_processHandle, _is64Bits,
                address, previousProtection!.Value);
            
            if (protectionRestorationResult.IsFailure)
                return new WriteFailureOnSystemProtectionRestoration(address, protectionRestorationResult.Error);
        }
        
        return Result<WriteFailure>.Success;
    }
}