using System.Diagnostics;
using MindControl.Internal;

namespace MindControl;

// This partial class implements methods related to retrieving memory addresses.
public partial class ProcessMemory
{
    /// <summary>
    /// Evaluates the given pointer path to the memory address it points to in the process.
    /// </summary>
    /// <param name="pointerPath">Pointer path to evaluate.</param>
    /// <returns>The memory address pointed by the pointer path.</returns>
    public UIntPtr? EvaluateMemoryAddress(PointerPath pointerPath)
    {
        if (pointerPath.IsStrictly64Bits && (IntPtr.Size == 4 || !_is64Bits))
            throw new ArgumentException(
                $"The pointer path \"{pointerPath.Expression}\" uses addresses intended for a 64-bits process, but this instance is targeting a 32-bits process.");
        
        UIntPtr? baseAddress = pointerPath.BaseModuleName != null
            ? GetModuleAddress(pointerPath.BaseModuleName)
            : pointerPath.PointerOffsets.FirstOrDefault().ToUIntPtr();

        if (baseAddress == null)
            return null; // Module not found

        if (pointerPath.BaseModuleOffset > 0)
            baseAddress = (baseAddress.Value.ToUInt64() + pointerPath.BaseModuleOffset).ToUIntPtr();

        if (baseAddress == null || baseAddress == UIntPtr.Zero)
            return null; // Overflow after applying the module offset, or zero pointer
        
        // Follow the pointer path offset by offset
        var currentAddress = baseAddress.Value;
        int startIndex = pointerPath.BaseModuleName == null ? 1 : 0;
        for (int i = startIndex; i < pointerPath.PointerOffsets.Length; i++)
        {
            // Read the value pointed by the current address as a pointer address
            UIntPtr? nextAddress = ReadIntPtr(currentAddress);
            if (nextAddress == null)
                return null; // Read operation failed on the address

            // Apply the offset to the value we just read and check the result
            var offset = pointerPath.PointerOffsets[i];
            var nextValue = (nextAddress.Value.ToUInt64() + offset).ToUIntPtr();
            if (nextValue == null || nextValue == UIntPtr.Zero || !IsBitnessCompatible(nextValue.Value))
                return null; // Overflow after applying the offset; zero pointer; 64-bits pointer on 32-bits target
            
            // The next value has been vetted. Keep going with it as the current address
            currentAddress = nextValue.Value;
        }

        return currentAddress;
    }

    /// <summary>
    /// Evaluates the given pointer path to the memory address it points to in the process.
    /// If the path does not evaluate to a proper address, throws a <see cref="MemoryException"/>.
    /// </summary>
    /// <param name="pointerPath">Pointer path to evaluate.</param>
    /// <returns>The memory address pointed by the pointer path.</returns>
    private UIntPtr EvaluateMemoryAddressOrThrow(PointerPath pointerPath) => EvaluateMemoryAddress(pointerPath)
        ?? throw new MemoryException($"Could not evaluate pointer path \"{pointerPath}\".");
    
    /// <summary>
    /// Gets the base address of the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The base address of the module if found, null otherwise.</returns>
    public UIntPtr? GetModuleAddress(string moduleName)
    {
        IntPtr? baseAddress = _process.Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
            ?.BaseAddress;

        return baseAddress == null ? null : (UIntPtr)(long)baseAddress;
    }

    /// <summary>
    /// Gets the memory range of the process module with the given name.
    /// </summary>
    /// <param name="moduleName">Name of the target module.</param>
    /// <returns>The memory range of the module if found, null otherwise.</returns>
    public MemoryRange? GetModuleRange(string moduleName)
    {
        var module = _process.Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));

        if (module == null)
            return null;
        
        return MemoryRange.FromStartAndSize((UIntPtr)(long)module.BaseAddress, (ulong)module.ModuleMemorySize - 1);
    }
}