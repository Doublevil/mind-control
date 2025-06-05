using System.Diagnostics;
using MindControl.Modules;
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
    public Result<UIntPtr> EvaluateMemoryAddress(PointerPath pointerPath)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        
        if (pointerPath.IsStrictly64Bit && (IntPtr.Size == 4 || !Is64Bit))
            return new IncompatiblePointerPathBitnessFailure();
        
        UIntPtr? baseAddress;
        if (pointerPath.BaseModuleName != null)
        {
            baseAddress = GetModule(pointerPath.BaseModuleName)?.GetRange().Start;
            if (baseAddress == null)
                return new BaseModuleNotFoundFailure(pointerPath.BaseModuleName);
        }
        else
        {
            var firstOffset = pointerPath.PointerOffsets.FirstOrDefault();
            baseAddress = firstOffset.AsAddress();
            if (baseAddress == null)
                return new PointerOutOfRangeFailure(null, firstOffset);
        }

        // Apply the base offset if there is one
        if (pointerPath.BaseModuleOffset.Offset > 0)
        {
            var baseAddressWithOffset = pointerPath.BaseModuleOffset.OffsetAddress(baseAddress.Value);
            if (baseAddressWithOffset == null)
                return new PointerOutOfRangeFailure(baseAddress, pointerPath.BaseModuleOffset);
            
            baseAddress = baseAddressWithOffset.Value;
        }

        // Check if the base address is valid
        if (baseAddress == UIntPtr.Zero)
            return new PointerOutOfRangeFailure(UIntPtr.Zero, PointerOffset.Zero);
        if (!IsBitnessCompatible(baseAddress.Value))
            return new IncompatibleBitnessPointerFailure(baseAddress.Value);
        
        // Follow the pointer path offset by offset
        var currentAddress = baseAddress.Value;
        int startIndex = pointerPath.BaseModuleName == null ? 1 : 0;
        for (int i = startIndex; i < pointerPath.PointerOffsets.Length; i++)
        {
            // Read the value pointed by the current address as a pointer address
            var nextAddressResult = Read<UIntPtr>(currentAddress);
            if (nextAddressResult.IsFailure)
                return nextAddressResult.Failure;
            
            var nextAddress = nextAddressResult.Value;

            // Apply the offset to the value we just read and check the result
            var offset = pointerPath.PointerOffsets[i];
            var nextValue = offset.OffsetAddress(nextAddress);
            
            // Check for invalid address values
            if (nextValue == null || nextValue.Value == UIntPtr.Zero)
                return new PointerOutOfRangeFailure(nextAddress, offset);
            if (!IsBitnessCompatible(nextValue.Value))
                return new IncompatibleBitnessPointerFailure(nextAddress);
            
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
    public DisposableResult<ProcessMemoryStream> GetMemoryStream(PointerPath pointerPath)
    {
        var addressResult = EvaluateMemoryAddress(pointerPath);
        if (addressResult.IsFailure)
            return addressResult.Failure;

        return GetMemoryStream(addressResult.Value);
    }
    
    private Dictionary<string, RemoteModule>? _cachedModules;
    
    /// <summary>
    /// Refreshes the module cache used when evaluating pointer paths.
    /// Call this method when your process' modules have changed due to external factors such as plugins being loaded,
    /// or other third-party software interacting with the process.
    /// </summary>
    public void RefreshModuleCache()
    {
        using var process = GetAttachedProcessInstance();
        if (process.IsFailure)
            return;
        
        _cachedModules = new Dictionary<string, RemoteModule>(StringComparer.OrdinalIgnoreCase);
        foreach (ProcessModule module in process.Value.Modules)
            _cachedModules[module.ModuleName] = new RemoteModule(this, module);
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
    /// Gets the module with the given name, if it exists.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The module if found, null otherwise.</returns>
    public RemoteModule? GetModule(string moduleName)
    {
        if (_cachedModules == null)
            RefreshModuleCache();
        
        return _cachedModules!.GetValueOrDefault(moduleName);
    }
    
    /// <summary>
    /// Returns the intersection of the input range with the full addressable memory range.
    /// If the input memory range is null, the full memory range is returned.
    /// </summary>
    /// <param name="input">Input memory range to clamp. If null, the full memory range is returned.</param>
    /// <returns>The clamped memory range, or the full memory range if the input is null.</returns>
    private MemoryRange GetClampedMemoryRange(MemoryRange? input)
    {
        var fullMemoryRange = _osService.GetFullMemoryRange(Is64Bit);
        return input == null ? fullMemoryRange : new MemoryRange(
            Start: input.Value.Start.ToUInt64() < fullMemoryRange.Start.ToUInt64()
                ? fullMemoryRange.Start : input.Value.Start,
            End: input.Value.End.ToUInt64() > fullMemoryRange.End.ToUInt64()
                ? fullMemoryRange.End : input.Value.End
        );
    }
}