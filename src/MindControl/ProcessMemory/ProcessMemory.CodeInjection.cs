using System.Text;
using MindControl.Native;

namespace MindControl;

// This partial class implements methods related to code injection.
public partial class ProcessMemory
{
    /// <summary>
    /// Gets or sets the time to wait for the spawned thread to return when injecting a library using the
    /// <see cref="InjectLibrary"/> method.
    /// By default, this value will be set to 10 seconds.
    /// </summary>
    public TimeSpan LibraryInjectionThreadTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Injects a library into the attached process.
    /// </summary>
    /// <param name="libraryPath">Path to the library file to inject into the process.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void InjectLibrary(string libraryPath)
    {
        if (!IsAttached)
            throw new InvalidOperationException("Cannot inject a library into a process that is not attached.");

        string absoluteLibraryPath = Path.GetFullPath(libraryPath);
        if (!File.Exists(absoluteLibraryPath))
            throw new FileNotFoundException("The library file to inject was not found.", absoluteLibraryPath);
        
        var allocatedLibPathAddress = UIntPtr.Zero;
        var threadHandle = IntPtr.Zero;
        
        // The goal here is to call the LoadLibrary function from inside the target process.
        // We need to pass the address of the library path string as a parameter to the function.
        // To do this, we first need to write the path of the library to load into the target process memory.
        
        try
        {
            // Write the library path string into the process memory
            var libraryPathBytes = Encoding.Unicode.GetBytes(absoluteLibraryPath);
            allocatedLibPathAddress = _osService.AllocateMemory(_processHandle, libraryPathBytes.Length + 1,
                MemoryAllocationType.Commit | MemoryAllocationType.Reserve, MemoryProtection.ReadWrite);
            _osService.WriteProcessMemory(_processHandle, allocatedLibPathAddress, libraryPathBytes);

            // Create a thread that runs in the target process to run the LoadLibrary function, using the address of
            // the library path string as a parameter, so that it knows to load that library. 
            var loadLibraryFunctionAddress = _osService.GetLoadLibraryFunctionAddress();
            threadHandle = _osService.CreateRemoteThread(_processHandle, loadLibraryFunctionAddress,
                allocatedLibPathAddress);

            // Wait for the thread to finish
            if (!_osService.WaitThread(threadHandle, LibraryInjectionThreadTimeout))
                throw new TimeoutException("The injection timed out.");
        }
        catch (Exception ex)
        {
            var exceptions = new List<Exception> {ex};
            
            // Free the memory used for the library path string
            if (allocatedLibPathAddress != UIntPtr.Zero)
            {
                try
                {
                    _osService.ReleaseMemory(_processHandle, allocatedLibPathAddress);
                }
                catch (Exception freeEx)
                {
                    exceptions.Add(freeEx);
                }
            }

            // Close the thread handle
            if (threadHandle != IntPtr.Zero)
            {
                try
                {
                    _osService.CloseHandle(threadHandle);
                }
                catch (Exception closeEx)
                {
                    exceptions.Add(closeEx);
                }
            }
            
            if (exceptions.Count == 1)
                throw;
            
            throw new AggregateException("An error occurred while injecting the library into the process. Additional errors also occurred when trying to release resources.", exceptions);
        }
        
        // Free the memory used for the library path string
        _osService.ReleaseMemory(_processHandle, allocatedLibPathAddress);
        
        // Close the thread handle
        _osService.CloseHandle(threadHandle);

        // Check that the module is correctly loaded.
        // We do this because we don't know if the LoadLibrary function succeeded or not.
        string expectedModuleName = Path.GetFileName(libraryPath);
        if (GetModuleAddress(expectedModuleName) == null)
        {
            throw new MemoryException("The module was not found in the process after the injection. The injection may have failed. Please check that your DLL is compatible with the target process (x64/x86).");
        }
    }
}