using System.Diagnostics;
using MindControl.Results;

namespace MindControl.Modules;

/// <summary>
/// Represents a module loaded into another process.
/// </summary>
public class RemoteModule
{
    private readonly ProcessMemory _processMemory;
    private readonly ProcessModule _internalModule;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteModule"/> class.
    /// </summary>
    /// <param name="processMemory">Process memory instance initializing this module.</param>
    /// <param name="internalModule">Managed module instance to wrap.</param>
    internal RemoteModule(ProcessMemory processMemory, ProcessModule internalModule)
    {
        _processMemory = processMemory;
        _internalModule = internalModule;
    }
    
    /// <summary>
    /// Gets the managed module instance.
    /// </summary>
    /// <returns>The managed module instance.</returns>
    public ProcessModule GetManagedModule()
        => _internalModule;
    
    /// <summary>
    /// Gets the range of memory occupied by the module.
    /// </summary>
    /// <returns>The memory range of the module.</returns>
    public MemoryRange GetRange()
        => MemoryRange.FromStartAndSize((UIntPtr)_internalModule.BaseAddress, (ulong)_internalModule.ModuleMemorySize);

    /// <summary>
    /// Attempts to read the export table of the module, associating the names of the exported functions with their
    /// absolute addresses in the process memory. This is useful to locate specific functions in a DLL, like Windows API
    /// functions from kernel32.dll or user32.dll, or your own functions in a DLL you have injected into the process.
    /// </summary>
    /// <returns>A result holding either a dictionary containing the names and addresses of the exported functions, or
    /// an error message in case the export table could not be read.</returns>
    public Result<Dictionary<string, UIntPtr>, string> ReadExportTable()
    {
        var peParser = new PeParser(_processMemory, (UIntPtr)_internalModule.BaseAddress);
        return peParser.ReadExportTable();
    }
}
