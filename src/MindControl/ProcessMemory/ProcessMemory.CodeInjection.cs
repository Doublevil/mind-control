using System.Text;
using MindControl.Native;
using MindControl.Results;

namespace MindControl;

// This partial class implements methods related to code injection.
public partial class ProcessMemory
{
    /// <summary>
    /// Gets or sets the time to wait for the spawned thread to return when injecting a library using the
    /// <see cref="InjectLibrary"/> method.
    /// The default value is 10 seconds.
    /// </summary>
    public TimeSpan LibraryInjectionThreadTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Injects a library into the attached process.
    /// </summary>
    /// <param name="libraryPath">Path to the library file to inject into the process.</param>
    /// <returns>A successful result, or an injection failure detailing how the operation failed.</returns>
    public Result<InjectionFailure> InjectLibrary(string libraryPath)
    {
        if (!IsAttached)
            throw new InvalidOperationException(DetachedErrorMessage);

        // Check if the library file exists
        string absoluteLibraryPath = Path.GetFullPath(libraryPath);
        if (!File.Exists(absoluteLibraryPath))
            return new InjectionFailureOnLibraryFileNotFound(absoluteLibraryPath);

        // The goal here is to call the LoadLibrary function from inside the target process.
        // We need to pass the address of the library path string as a parameter to the function.
        // To do this, we first need to write the path of the library to load into the target process memory.
        
        // Write the library path string into the process memory
        var libraryPathBytes = Encoding.Unicode.GetBytes(absoluteLibraryPath);
        var allocateStringResult = _osService.AllocateMemory(ProcessHandle, libraryPathBytes.Length + 1,
            MemoryAllocationType.Commit | MemoryAllocationType.Reserve, MemoryProtection.ReadWrite);
        if (allocateStringResult.IsFailure)
            return new InjectionFailureOnSystemFailure("Could not allocate memory to store the library file path.",
                allocateStringResult.Error);
        var allocatedLibPathAddress = allocateStringResult.Value;
        
        var writeStringResult = _osService.WriteProcessMemory(ProcessHandle, allocatedLibPathAddress,
            libraryPathBytes);
        if (writeStringResult.IsFailure)
            return new InjectionFailureOnSystemFailure(
                "Could not write the library file path to the target process memory.",
                writeStringResult.Error);
        
        // Create a thread that runs in the target process to run the LoadLibrary function, using the address of
        // the library path string as a parameter, so that it knows to load that library.
        var loadLibraryAddressResult = _osService.GetLoadLibraryFunctionAddress();
        if (loadLibraryAddressResult.IsFailure)
            return new InjectionFailureOnSystemFailure(
                "Could not get the address of the LoadLibrary system API function from the current process.",
                loadLibraryAddressResult.Error);

        var loadLibraryFunctionAddress = loadLibraryAddressResult.Value;
        var threadHandleResult = _osService.CreateRemoteThread(ProcessHandle, loadLibraryFunctionAddress,
            allocatedLibPathAddress);
        if (threadHandleResult.IsFailure)
            return new InjectionFailureOnSystemFailure(
                "Could not create a remote thread in the target process to load the library.",
                threadHandleResult.Error);
        
        var threadHandle = threadHandleResult.Value;

        // Wait for the thread to finish
        var waitResult = _osService.WaitThread(threadHandle, LibraryInjectionThreadTimeout);
        if (waitResult.IsFailure)
            return new InjectionFailureOnSystemFailure("Could not wait for the thread to finish execution.",
                waitResult.Error);
        if (waitResult.Value == false)
            return new InjectionFailureOnTimeout();
        
        // Free the memory used for the library path string
        _osService.ReleaseMemory(ProcessHandle, allocatedLibPathAddress);
        
        // Close the thread handle
        _osService.CloseHandle(threadHandle);

        // Check that the module is correctly loaded.
        // We do this because we don't know if the LoadLibrary function succeeded or not.
        string expectedModuleName = Path.GetFileName(libraryPath);
        if (GetModuleAddress(expectedModuleName) == null)
            return new InjectionFailureOnModuleNotFound();
        
        return Result<InjectionFailure>.Success;
    }
}