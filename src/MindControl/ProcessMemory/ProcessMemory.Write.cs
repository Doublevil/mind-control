using MindControl.Internal;
using MindControl.Native;
using MindControl.Results;

namespace MindControl;

// This partial class implements the memory writing features of ProcessMemory.
public partial class ProcessMemory
{
    #region Bytes writing
    
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
        if (!IsAttached)
            return new WriteFailureOnDetachedProcess();
        
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
    public Result<WriteFailure> WriteBytes(UIntPtr address, Span<byte> value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        if (!IsAttached)
            return new WriteFailureOnDetachedProcess();
        if (address == UIntPtr.Zero)
            return new WriteFailureOnZeroPointer();
        if (!IsBitnessCompatible(address))
            return new WriteFailureOnIncompatibleBitness(address);
        
        // Remove protection if needed
        memoryProtectionStrategy ??= DefaultWriteStrategy;
        MemoryProtection? previousProtection = null;
        if (memoryProtectionStrategy is MemoryProtectionStrategy.Remove or MemoryProtectionStrategy.RemoveAndRestore)
        {
            var protectionRemovalResult = _osService.ReadAndOverwriteProtection(ProcessHandle, Is64Bit,
                address, MemoryProtection.ExecuteReadWrite);
            
            if (protectionRemovalResult.IsFailure)
                return new WriteFailureOnSystemProtectionRemoval(address, protectionRemovalResult.Error);

            previousProtection = protectionRemovalResult.Value;
        }
        
        // Write memory
        var writeResult = _osService.WriteProcessMemory(ProcessHandle, address, value);
        if (writeResult.IsFailure)
            return new WriteFailureOnSystemWrite(address, writeResult.Error);
        
        // Restore protection if needed
        if (memoryProtectionStrategy == MemoryProtectionStrategy.RemoveAndRestore
            && previousProtection != MemoryProtection.ExecuteReadWrite)
        {
            var protectionRestorationResult = _osService.ReadAndOverwriteProtection(ProcessHandle, Is64Bit,
                address, previousProtection!.Value);
            
            if (protectionRestorationResult.IsFailure)
                return new WriteFailureOnSystemProtectionRestoration(address, protectionRestorationResult.Error);
        }
        
        return Result<WriteFailure>.Success;
    }
    
    #endregion
    
    #region Primitive types writing
    
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
        if (!IsAttached)
            return new WriteFailureOnDetachedProcess();
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
    /// <returns>A successful result, or a write failure</returns>
    public Result<WriteFailure> Write<T>(UIntPtr address, T value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        if (!IsAttached)
            return new WriteFailureOnDetachedProcess();
        if (address == UIntPtr.Zero)
            return new WriteFailureOnZeroPointer();
        if (!IsBitnessCompatible(address))
            return new WriteFailureOnIncompatibleBitness(address);
        if (value == null)
            return new WriteFailureOnInvalidArguments("The value to write cannot be null.");
        if (value is IntPtr ptr && ptr.ToInt64() > uint.MaxValue)
            return new WriteFailureOnIncompatibleBitness((UIntPtr)ptr);
        if (value is UIntPtr uptr && uptr.ToUInt64() > uint.MaxValue)
            return new WriteFailureOnIncompatibleBitness(uptr);
        
        return value switch
        {
            bool v => WriteBool(address, v, memoryProtectionStrategy),
            byte v => WriteByte(address, v, memoryProtectionStrategy),
            short v => WriteShort(address, v, memoryProtectionStrategy),
            ushort v => WriteUShort(address, v, memoryProtectionStrategy),
            int v => WriteInt(address, v, memoryProtectionStrategy),
            uint v => WriteUInt(address, v, memoryProtectionStrategy),
            IntPtr v => WriteIntPtr(address, v, memoryProtectionStrategy),
            UIntPtr v => Is64Bit ? WriteULong(address, v.ToUInt64(), memoryProtectionStrategy)
                : WriteUInt(address, v.ToUInt32(), memoryProtectionStrategy),
            float v => WriteFloat(address, v, memoryProtectionStrategy),
            long v => WriteLong(address, v, memoryProtectionStrategy),
            ulong v => WriteULong(address, v, memoryProtectionStrategy),
            double v => WriteDouble(address, v, memoryProtectionStrategy),
            byte[] v => WriteBytes(address, v, memoryProtectionStrategy),
            _ => ConvertAndWriteBytes(address, value, memoryProtectionStrategy)
        };
    }

    /// <summary>
    /// Converts the given value to an array of bytes, then writes it to the given address in the process memory.
    /// This method is used when the type of the value to write is not supported by the other write methods (typically
    /// for structs).
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <typeparam name="T">Type of the value to write.</typeparam>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> ConvertAndWriteBytes<T>(UIntPtr address, T value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
    {
        if (value == null)
            return new WriteFailureOnInvalidArguments("The value to write cannot be null.");
        if (value is not ValueType)
            return new WriteFailureOnUnsupportedType(value.GetType());

        byte[] bytes;
        try
        {
            bytes = value.ToBytes();
        }
        catch (Exception e)
        {
            return new WriteFailureOnConversion(typeof(T), e);
        }
        
        return WriteBytes(address, bytes, memoryProtectionStrategy);
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
    /// Writes a pointer to the given address in the process memory.
    /// </summary>
    /// <param name="address">Target address in the process memory.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="memoryProtectionStrategy">Strategy to use to deal with memory protection. If null (default), the
    /// <see cref="DefaultWriteStrategy"/> of this instance is used.</param>
    /// <returns>A successful result, or a write failure</returns>
    private Result<WriteFailure> WriteIntPtr(UIntPtr address, IntPtr value,
        MemoryProtectionStrategy? memoryProtectionStrategy = null)
        => WriteBytes(address, value.ToBytes(Is64Bit), memoryProtectionStrategy);
    
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
    
    #endregion
}