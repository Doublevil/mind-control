using System.Diagnostics;
using MindControl.Native;

namespace MindControl;

// This is the main ProcessMemory class file.
// The class is partial and its features are cut into multiple files.
// See the other files in the same directory for the rest of the implementation.

/// <summary>
/// Provides methods to manipulate the memory of a process.
/// </summary>
public partial class ProcessMemory : IDisposable
{
    private readonly Process _process;
    private readonly IOperatingSystemService _osService;
    private IntPtr _processHandle;
    private bool _is64Bits;
    private readonly bool _ownsProcessInstance;
    
    /// <summary>
    /// Gets a value indicating if the process is currently attached or not.
    /// </summary>
    public bool IsAttached { get; private set; }

    /// <summary>
    /// Gets or sets the default way this instance deals with memory protection.
    /// This value is used when no strategy is specified in memory write operations.
    /// By default, this value will be <see cref="MemoryProtectionStrategy.RemoveAndRestore"/>.
    /// </summary>
    public MemoryProtectionStrategy DefaultWriteStrategy { get; set; } = MemoryProtectionStrategy.RemoveAndRestore;

    /// <summary>
    /// Gets or sets the time to wait for the spawned thread to return when injecting a library using the
    /// <see cref="InjectLibrary"/> method.
    /// By default, this value will be set to 10 seconds.
    /// </summary>
    public TimeSpan LibraryInjectionThreadTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Event raised when the process detaches for any reason.
    /// </summary>
    public event EventHandler? ProcessDetached;

    /// <summary>
    /// Attaches to a process with the given name and returns the resulting <see cref="ProcessMemory"/> instance.
    /// If multiple processes with the specified name are running, one of them will be targeted arbitrarily.
    /// When there is any risk of this happening, it is recommended to use <see cref="OpenProcessById"/> instead.
    /// </summary>
    /// <param name="processName">Name of the process to open.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    /// <exception cref="ProcessException">Thrown when no running process with the given name can be found.</exception>
    public static ProcessMemory OpenProcessByName(string processName)
    {
        var matches = Process.GetProcessesByName(processName);
        if (matches.Length == 0)
            throw new ProcessException($"No running process with the name \"{processName}\" could be found.");

        // If we have multiple results, we need to dispose those that we will not be using.
        // Arbitrarily, we will use the first result. So we dispose everything starting from index 1.
        for (var i = 1; i < matches.Length; i++)
            matches[i].Dispose();
        
        return OpenProcess(matches.First(), true);
    }

    /// <summary>
    /// Attaches to the process with the given identifier and returns the resulting <see cref="ProcessMemory"/>
    /// instance.
    /// </summary>
    /// <param name="pid">Identifier of the process to attach to.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    public static ProcessMemory OpenProcessById(int pid)
    {
        var process = Process.GetProcessById(pid);
        if (process == null)
            throw new ProcessException(pid, $"No running process with the PID {pid} could be found.");
        return OpenProcess(process, true);
    }

    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    public static ProcessMemory OpenProcess(Process target) => OpenProcess(target, false);

    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <param name="ownsProcessInstance">Indicates if this instance should take ownership of the
    /// <paramref name="target"/>, meaning it has the responsibility to dispose it.</param>
    /// <returns>The attached process instance resulting from the operation.</returns>
    private static ProcessMemory OpenProcess(Process target, bool ownsProcessInstance)
    {
        if (target.HasExited)
            throw new ProcessException(target.Id, $"Process {target.Id} has exited.");

        return new ProcessMemory(target, ownsProcessInstance);
    }
    
    /// <summary>
    /// Builds a new instance that attaches to the given process.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="ownsProcessInstance">Indicates if this instance should take ownership of the
    /// <paramref name="process"/>, meaning it has the responsibility to dispose it.</param>
    public ProcessMemory(Process process, bool ownsProcessInstance)
        : this(process, ownsProcessInstance, new Win32Service()) {}

    /// <summary>
    /// Builds a new instance that attaches to the given process.
    /// Using this constructor directly is discouraged. See the static methods <see cref="OpenProcess(Process)"/>,
    /// <see cref="OpenProcessById"/> and <see cref="OpenProcessByName"/>.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="ownsProcessInstance">Indicates if this instance should take ownership of the
    /// <paramref name="process"/>, meaning it has the responsibility to dispose it.</param>
    /// <param name="osService">Service that provides system-specific process-oriented features.</param>
    private ProcessMemory(Process process, bool ownsProcessInstance, IOperatingSystemService osService)
    {
        _process = process;
        _osService = osService;
        _ownsProcessInstance = ownsProcessInstance;
        Attach();
    }
    
    /// <summary>
    /// Attaches to the process.
    /// </summary>
    private void Attach()
    {
        try
        {
            _is64Bits = _osService.IsProcess64Bits(_process.Id);
            
            if (_is64Bits && !Environment.Is64BitOperatingSystem)
                throw new ProcessException(_process.Id, "A 32-bit program cannot attach to a 64-bit process.");
            
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _processHandle = _osService.OpenProcess(_process.Id);

            IsAttached = true;
        }
        catch (Exception e)
        {
            Detach();
            throw new ProcessException(_process.Id, $"Failed to attach to the process {_process.Id}. Check the internal exception for more information. Common causes include insufficient privileges (administrator rights might be required) or trying to attach to a x64 process with a x86 program.", e);
        }
    }
    
    /// <summary>
    /// Returns True if and only if the given pointer is compatible with the bitness of the target process.
    /// In other words, returns false if the pointer is a 64-bit address but the target process is 32-bit. 
    /// </summary>
    /// <param name="pointer">Pointer to test.</param>
    private bool IsBitnessCompatible(UIntPtr pointer) => _is64Bits || pointer.ToUInt64() <= uint.MaxValue;

    /// <summary>
    /// Gets a new instance of <see cref="Process"/> representing the attached process.
    /// The returned instance is owned by the caller and should be disposed when no longer needed.
    /// </summary>
    public Process GetAttachedProcessInstance() => Process.GetProcessById(_process.Id);
    
    #region Dispose
    
    /// <summary>
    /// Detaches from the process.
    /// </summary>
    private void Detach()
    {
        if (IsAttached)
        {
            IsAttached = false;
            _process.Exited -= OnProcessExited;
            ProcessDetached?.Invoke(this, EventArgs.Empty);
        }

        if (_ownsProcessInstance)
            _process.Dispose();
    }
    
    /// <summary>
    /// Event callback. Called when the attached process exits.
    /// Raises the related event.
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e) => Detach();

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() => Detach();

    #endregion
}