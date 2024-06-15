using System.Diagnostics;
using MindControl.Internal;
using MindControl.Results;

namespace MindControl;

// This partial class implements methods related to retrieving memory addresses.
public partial class ProcessMemory
{
    /// <summary>
    /// Evaluates the given pointer path to the memory address it points to in the process.
    /// </summary>
    /// <param name="pointerPath">Pointer path to evaluate.</param>
    /// <returns>The memory address pointed by the pointer path.</returns>
    public Result<UIntPtr, PathEvaluationFailure> EvaluateMemoryAddress(PointerPath pointerPath)
    {
        if (pointerPath.IsStrictly64Bit && (IntPtr.Size == 4 || !Is64Bit))
            return new PathEvaluationFailureOnIncompatibleBitness();
        
        UIntPtr? baseAddress;
        if (pointerPath.BaseModuleName != null)
        {
            baseAddress = GetModuleAddress(pointerPath.BaseModuleName);
            if (baseAddress == null)
                return new PathEvaluationFailureOnBaseModuleNotFound(pointerPath.BaseModuleName);
        }
        else
        {
            var firstOffset = pointerPath.PointerOffsets.FirstOrDefault();
            baseAddress = firstOffset.AsAddress();
            if (baseAddress == null)
                return new PathEvaluationFailureOnPointerOutOfRange(null, firstOffset);
        }

        // Apply the base offset if there is one
        if (pointerPath.BaseModuleOffset.Offset > 0)
        {
            var baseAddressWithOffset = pointerPath.BaseModuleOffset.OffsetAddress(baseAddress.Value);
            if (baseAddressWithOffset == null)
                return new PathEvaluationFailureOnPointerOutOfRange(baseAddress, pointerPath.BaseModuleOffset);
            
            baseAddress = baseAddressWithOffset.Value;
        }

        // Check if the base address is valid
        if (baseAddress == UIntPtr.Zero)
            return new PathEvaluationFailureOnPointerOutOfRange(UIntPtr.Zero, PointerOffset.Zero);
        
        // Follow the pointer path offset by offset
        var currentAddress = baseAddress.Value;
        int startIndex = pointerPath.BaseModuleName == null ? 1 : 0;
        for (int i = startIndex; i < pointerPath.PointerOffsets.Length; i++)
        {
            // Read the value pointed by the current address as a pointer address
            var nextAddressResult = Read<UIntPtr>(currentAddress);
            if (nextAddressResult.IsFailure)
                return new PathEvaluationFailureOnPointerReadFailure(currentAddress, nextAddressResult.Error);
            
            var nextAddress = nextAddressResult.Value;

            // Apply the offset to the value we just read and check the result
            var offset = pointerPath.PointerOffsets[i];
            var nextValue = offset.OffsetAddress(nextAddress);
            
            // Check for invalid address values
            if (nextValue == null || nextValue.Value == UIntPtr.Zero)
                return new PathEvaluationFailureOnPointerOutOfRange(nextAddress, offset);
            if (!IsBitnessCompatible(nextValue.Value))
                return new PathEvaluationFailureOnIncompatibleBitness(nextAddress);
            
            // The next value has been vetted. Keep going with it as the current address
            currentAddress = nextValue.Value;
        }

        return currentAddress;
    }

    /// <summary>
    /// Creates and returns a new <see cref="ProcessMemoryStream"/> instance that starts at the address pointed by the
    /// given path.
    /// The stream can be used to read or write into the process memory. It is owned by the caller and must be disposed
    /// when no longer needed.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the starting address of the stream.</param>
    /// <returns>A result holding either the created process memory stream, or a path evaluation failure.</returns>
    public Result<ProcessMemoryStream, PathEvaluationFailure> GetMemoryStream(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        if (addressResult.IsFailure)
            return addressResult.Error;

        return GetMemoryStream(addressResult.Value);
    }
    
    /// <summary>
    /// Creates and returns a new <see cref="ProcessMemoryStream"/> instance that starts at the given address.
    /// The stream can be used to read or write into the process memory. It is owned by the caller and must be disposed
    /// when no longer needed.
    /// </summary>
    /// <param name="startAddress">Starting address of the stream.</param>
    /// <returns>The created process memory stream.</returns>
    public ProcessMemoryStream GetMemoryStream(UIntPtr startAddress)
        => new(_osService, ProcessHandle, startAddress);
    
    /// <summary>
    /// Gets the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <param name="process">The target process instance to get the module from.</param>
    /// <returns>The module if found, null otherwise.</returns>
    private static ProcessModule? GetModule(string moduleName, Process process)
    {
        return process.Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the process module loaded in the attached process by its name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The module if found, null otherwise.</returns>
    public ProcessModule? GetModule(string moduleName)
    {
        using var process = GetAttachedProcessInstance();
        return GetModule(moduleName, process);
    }
    
    /// <summary>
    /// Gets the base address of the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The base address of the module if found, null otherwise.</returns>
    public UIntPtr? GetModuleAddress(string moduleName)
    {
        using var process = GetAttachedProcessInstance();

        var module = GetModule(moduleName, process);
        IntPtr? baseAddress = module?.BaseAddress;

        return baseAddress == null ? null : (UIntPtr)(long)baseAddress;
    }

    /// <summary>
    /// Gets the memory range of the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The memory range of the module if found, null otherwise.</returns>
    public MemoryRange? GetModuleRange(string moduleName)
    {
        using var process = GetAttachedProcessInstance();

        var module = GetModule(moduleName, process);

        if (module == null)
            return null;
        
        return MemoryRange.FromStartAndSize((UIntPtr)(long)module.BaseAddress, (ulong)module.ModuleMemorySize - 1);
    }
    
    /// <summary>
    /// Returns the intersection of the input range with the full addressable memory range.
    /// If the input memory range is null, the full memory range is returned.
    /// </summary>
    /// <param name="input">Input memory range to clamp. If null, the full memory range is returned.</param>
    /// <returns>The clamped memory range, or the full memory range if the input is null.</returns>
    private MemoryRange GetClampedMemoryRange(MemoryRange? input)
    {
        var fullMemoryRange = _osService.GetFullMemoryRange();
        return input == null ? fullMemoryRange : new MemoryRange(
            Start: input.Value.Start.ToUInt64() < fullMemoryRange.Start.ToUInt64()
                ? fullMemoryRange.Start : input.Value.Start,
            End: input.Value.End.ToUInt64() > fullMemoryRange.End.ToUInt64()
                ? fullMemoryRange.End : input.Value.End
        );
    }
}