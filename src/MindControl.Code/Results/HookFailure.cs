namespace MindControl.Results;

/// <summary>Enumerates potential blocks for a <see cref="CodeAssemblyFailure"/>.</summary>
public enum HookCodeAssemblySource
{
    /// <summary>Default value used when the source is unknown.</summary>
    Unknown,
    /// <summary>Designates the jump instruction that forwards execution to the injected code.</summary>
    JumpToInjectedCode,
    /// <summary>Designates the code block that is prepended to the injected code.</summary>
    PrependedCode,
    /// <summary>Designates the injected code block itself.</summary>
    InjectedCode,
    /// <summary>Designates the code block that is appended to the injected code.</summary>
    AppendedCode
}

/// <summary>
/// Represents a failure that occurred in a hook operation when instructions could not be assembled into a code block.
/// </summary>
/// <param name="Source">Block where the code assembly failed.</param>
/// <param name="Message">Message that describes the failure.</param>
public record CodeAssemblyFailure(HookCodeAssemblySource Source, string Message)
    : Failure($"Failed to assemble code in {GetSourceAsString(Source)}: {Message}")
{
    /// <summary>Block where the code assembly failed.</summary>
    public HookCodeAssemblySource Source { get; init; } = Source;
    
    /// <summary>Returns a string representation of the given <see cref="HookCodeAssemblySource"/>.</summary>
    /// <param name="source">The source to convert to a string.</param>
    /// <returns>A string representation of the given <see cref="HookCodeAssemblySource"/>.</returns>
    private static string GetSourceAsString(HookCodeAssemblySource source) => source switch
    {
        HookCodeAssemblySource.JumpToInjectedCode => "the jump to the injected code",
        HookCodeAssemblySource.PrependedCode => "the code block generated before the injected code",
        HookCodeAssemblySource.InjectedCode => "the given code to inject",
        HookCodeAssemblySource.AppendedCode => "the code block generated after the injected code",
        _ => "an undetermined code block"
    };
}
