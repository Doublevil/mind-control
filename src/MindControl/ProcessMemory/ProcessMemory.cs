using System.Diagnostics;
using MindControl.Native;
using MindControl.Results;

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
    private readonly bool _ownsProcessInstance;
    
    /// <summary>
    /// Gets a value indicating if the process is currently attached or not.
    /// </summary>
    public bool IsAttached { get; private set; }
    
    /// <summary>
    /// Gets a boolean indicating if the process is 64-bit.
    /// </summary>
    public bool Is64Bit { get; }
    
    /// <summary>
    /// Gets the handle of the attached process.
    /// Use this if you need to manually call Win32 API functions.
    /// </summary>
    public IntPtr ProcessHandle { get; }

    /// <summary>
    /// Gets or sets the default way this instance deals with memory protection.
    /// This value is used when no strategy is specified in memory write operations.
    /// By default, this value will be <see cref="MemoryProtectionStrategy.RemoveAndRestore"/>.
    /// </summary>
    public MemoryProtectionStrategy DefaultWriteStrategy { get; set; } = MemoryProtectionStrategy.RemoveAndRestore;
    
    /// <summary>
    /// Event raised when the process detaches for any reason.
    /// </summary>
    public event EventHandler? ProcessDetached;

    /// <summary>
    /// Attaches to a process with the given name and returns the resulting <see cref="ProcessMemory"/> instance.
    /// If multiple processes with the specified name are running, a
    /// <see cref="AttachFailureOnMultipleTargetProcessesFound"/> will be returned.
    /// When there is any risk of this happening, it is recommended to use <see cref="OpenProcessById"/> instead.
    /// </summary>
    /// <param name="processName">Name of the process to open.</param>
    /// <returns>A result holding either the attached process instance, or an error.</returns>
    public static Result<ProcessMemory, AttachFailure> OpenProcessByName(string processName)
    {
        var matches = Process.GetProcessesByName(processName);
        if (matches.Length == 0)
            return new AttachFailureOnTargetProcessNotFound();
        if (matches.Length > 1)
        {
            var pids = matches.Select(p => p.Id).ToArray();
            foreach (var process in matches)
                process.Dispose();
            return new AttachFailureOnMultipleTargetProcessesFound(pids);
        }
        
        return OpenProcess(matches.First(), true, new Win32Service());
    }

    /// <summary>
    /// Attaches to the process with the given identifier and returns the resulting <see cref="ProcessMemory"/>
    /// instance.
    /// </summary>
    /// <param name="pid">Identifier of the process to attach to.</param>
    /// <returns>A result holding either the attached process instance, or an error.</returns>
    public static Result<ProcessMemory, AttachFailure> OpenProcessById(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return OpenProcess(process, true, new Win32Service());
        }
        catch (ArgumentException)
        {
            // Process.GetProcessById throws an ArgumentException when the PID does not match any process.
            return new AttachFailureOnTargetProcessNotFound();
        }
    }

    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <returns>A result holding either the attached process instance, or an error.</returns>
    public static Result<ProcessMemory, AttachFailure> OpenProcess(Process target)
        => OpenProcess(target, false, new Win32Service());
    
    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance. This variant
    /// allows you to specify an implementation of <see cref="IOperatingSystemService"/> to use instead of the default
    /// implementation. Unless you have very specific needs, use another overload of this method.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <param name="osService">Service that provides system-specific process-oriented features.</param>
    /// <returns>A result holding either the attached process instance, or an error.</returns>
    public static Result<ProcessMemory, AttachFailure> OpenProcess(Process target, IOperatingSystemService osService)
        => OpenProcess(target, false, osService);

    /// <summary>
    /// Attaches to the given process, and returns the resulting <see cref="ProcessMemory"/> instance.
    /// </summary>
    /// <param name="target">Process to attach to.</param>
    /// <param name="ownsProcessInstance">Indicates if this instance should take ownership of the
    /// <paramref name="target"/>, meaning it has the responsibility to dispose it.</param>
    /// <param name="osService">Service that provides system-specific process-oriented features.</param>
    /// <returns>A result holding either the attached process instance, or an error.</returns>
    internal static Result<ProcessMemory, AttachFailure> OpenProcess(Process target, bool ownsProcessInstance,
        IOperatingSystemService osService)
    {
        if (target.HasExited)
            return new AttachFailureOnTargetProcessNotRunning();

        // Determine target bitness
        var is64BitResult = osService.IsProcess64Bit(target.Id);
        if (is64BitResult.IsFailure)
            return new AttachFailureOnSystemError(is64BitResult.Error);
        var is64Bit = is64BitResult.Value;
        if (is64Bit && IntPtr.Size != 8)
            return new AttachFailureOnIncompatibleBitness();
        
        // Open the process with the required access flags
        var openResult = osService.OpenProcess(target.Id);
        if (openResult.IsFailure)
            return new AttachFailureOnSystemError(openResult.Error);
        var processHandle = openResult.Value;
        
        // Build the instance with the handle and bitness information
        return new ProcessMemory(target, ownsProcessInstance, processHandle, is64Bit, osService);
    }

    /// <summary>
    /// Builds a new instance that attaches to the given process.
    /// Using this constructor directly is discouraged. See the static methods <see cref="OpenProcess(Process)"/>,
    /// <see cref="OpenProcessById"/> and <see cref="OpenProcessByName"/>.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="ownsProcessInstance">Indicates if this instance should take ownership of the
    /// <paramref name="process"/>, meaning it has the responsibility to dispose it.</param>
    /// <param name="processHandle">Handle of the target process, open with specific access flags to allow memory
    /// manipulation.</param>
    /// <param name="is64Bit">Indicates if the target process is 64-bit.</param>
    /// <param name="osService">Service that provides system-specific process-oriented features.</param>
    private ProcessMemory(Process process, bool ownsProcessInstance, IntPtr processHandle, bool is64Bit,
        IOperatingSystemService osService)
    {
        _process = process;
        _osService = osService;
        _ownsProcessInstance = ownsProcessInstance;
        ProcessHandle = processHandle;
        Is64Bit = is64Bit;
        IsAttached = true;
        
        // Make sure this instance gets notified when the process exits
        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;
    }
    
    /// <summary>
    /// Returns True if and only if the given pointer is compatible with the bitness of the target process.
    /// In other words, returns false if the pointer is a 64-bit address but the target process is 32-bit. 
    /// </summary>
    /// <param name="pointer">Pointer to test.</param>
    private bool IsBitnessCompatible(UIntPtr pointer) => Is64Bit || pointer.ToUInt64() <= uint.MaxValue;

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