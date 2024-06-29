namespace MindControl.Results;

/// <summary>
/// Provides members to be used in error results.
/// </summary>
public static class Failure
{
    /// <summary>Message used in error results when the process is detached.</summary>
    public const string DetachedErrorMessage =
        "The process is not attached. It may have exited or the process memory instance may have been disposed.";
}