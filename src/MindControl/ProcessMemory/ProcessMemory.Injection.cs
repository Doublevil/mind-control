using System.Text;
using MindControl.Results;

namespace MindControl;

// This partial class implements methods related to library injection.
public partial class ProcessMemory
{
    /// <summary>Default time to wait for the spawned thread to return when injecting a library.</summary>
    private static readonly TimeSpan DefaultLibraryInjectionThreadTimeout = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Injects a library into the attached process.
    /// </summary>
    /// <param name="libraryPath">Path to the library file to inject into the process.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result InjectLibrary(string libraryPath) => InjectLibrary(libraryPath, DefaultLibraryInjectionThreadTimeout);
    
    /// <summary>
    /// Injects a library into the attached process.
    /// </summary>
    /// <param name="libraryPath">Path to the library file to inject into the process.</param>
    /// <param name="waitTimeout">Time to wait for the injection thread to return.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result InjectLibrary(string libraryPath, TimeSpan waitTimeout)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();

        // Check if the library file exists
        string absoluteLibraryPath = Path.GetFullPath(libraryPath);
        if (!File.Exists(absoluteLibraryPath))
            return new LibraryFileNotFoundFailure(absoluteLibraryPath);
        
        // Check if the module is already loaded
        string expectedModuleName = Path.GetFileName(libraryPath);
        if (GetModule(expectedModuleName) != null)
            return new ModuleAlreadyLoadedFailure();

        // Store the library path string in the target process memory
        var reservationResult = StoreString(absoluteLibraryPath, new StringSettings(Encoding.Unicode));
        if (reservationResult.IsFailure)
            return reservationResult;
        var reservation = reservationResult.Value;
        
        // Run LoadLibraryW from inside the target process to have it load the library itself, which is usually safer
        using var threadResult = RunThread("kernel32.dll", "LoadLibraryW", reservation.Address);
        if (threadResult.IsFailure)
            return threadResult.Failure;
        
        // Wait for the thread to return
        var waitResult = threadResult.Value.WaitForCompletion(waitTimeout);
        if (waitResult.IsFailure)
            return waitResult;
        
        // The exit code of the thread should be a handle to the loaded module.
        // We don't need the handle, but we can use it to verify that the library was loaded successfully.
        var moduleHandle = waitResult.Value;
        if (moduleHandle == 0)
            return new LibraryLoadFailure();
        
        // Refresh the module cache so that the new module is usable
        RefreshModuleCache();
        
        return Result.Success;
    }
}