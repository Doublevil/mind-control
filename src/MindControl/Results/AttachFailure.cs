namespace MindControl.Results;

/// <summary>Represents a reason for a process attach operation to fail.</summary>
public enum AttachFailureReason
{
    /// <summary>The process with the given name or PID could not be found.</summary>
    TargetProcessNotFound,
    /// <summary>Multiple processes with the given name were found.</summary>
    MultipleTargetProcessesFound,
    /// <summary>The target process instance is not running.</summary>
    TargetProcessNotRunning,
    /// <summary>Attempting to attach to a 64-bit process with a 32-bit process.</summary>
    IncompatibleBitness,
    /// <summary>A system call involved in the process attaching operation has failed.</summary>
    SystemError,
}

/// <summary>
/// Represents a failure in a process attach operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record AttachFailure(AttachFailureReason Reason);

/// <summary>
/// Represents a failure in a process attach operation when the target process could not be found.
/// </summary>
public record AttachFailureOnTargetProcessNotFound()
    : AttachFailure(AttachFailureReason.TargetProcessNotFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target process could not be found. Make sure the process is running.";
}

/// <summary>
/// Represents a failure in a process attach operation when multiple processes with the given name were found.
/// </summary>
/// <param name="Pids">Identifiers of running processes with the given name.</param>
public record AttachFailureOnMultipleTargetProcessesFound(int[] Pids)
    : AttachFailure(AttachFailureReason.MultipleTargetProcessesFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "Multiple processes with the given name were found. Use a PID to disambiguate.";
}

/// <summary>
/// Represents a failure in a process attach operation when the target process is not running.
/// </summary>
public record AttachFailureOnTargetProcessNotRunning()
    : AttachFailure(AttachFailureReason.TargetProcessNotRunning)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target process is not running.";
}

/// <summary>
/// Represents a failure in a process attach operation when the target process is 64-bit and the current process is
/// 32-bit.
/// </summary>
public record AttachFailureOnIncompatibleBitness()
    : AttachFailure(AttachFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "Cannot open a 64-bit process from a 32-bit process.";
}

/// <summary>
/// Represents a failure in a process attach operation when a system error occurs.
/// </summary>
/// <param name="Details">Details about the system error that occurred.</param>
public record AttachFailureOnSystemError(SystemFailure Details)
    : AttachFailure(AttachFailureReason.SystemError)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"A system error occurred while attempting to attach to the process: {Details}";
}