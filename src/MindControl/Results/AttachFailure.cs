namespace MindControl.Results;

/// <summary>Represents a failure in a process attach operation when the target process could not be found.</summary>
public record TargetProcessNotFoundFailure()
    : Failure("The target process could not be found. Make sure the process is running.");

/// <summary>
/// Represents a failure in a process attach operation when multiple processes with the given name were found.
/// </summary>
/// <param name="Pids">Identifiers of running processes with the given name.</param>
public record MultipleTargetProcessesFailure(int[] Pids)
    : Failure("Multiple processes with the given name were found. Use a PID to disambiguate.");

/// <summary>Represents a failure in a process attach operation when the target process is not running.</summary>
public record TargetProcessNotRunningFailure() : Failure("The target process is not running.");

/// <summary>
/// Represents a failure in a process attach operation when the target process is 64-bit and the current process is
/// 32-bit.
/// </summary>
public record IncompatibleBitnessProcessFailure() : Failure("Cannot attach to a 64-bit process from a 32-bit process.");