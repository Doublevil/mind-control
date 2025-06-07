using Iced.Intel;

namespace MindControl.Hooks;

/// <summary>
/// Defines how the injected code is executed in relation to the original code.
/// </summary>
public enum HookExecutionMode
{
    /// <summary>
    /// Executes the injected code before the original target instruction.
    /// </summary>
    ExecuteInjectedCodeFirst,
    
    /// <summary>
    /// Executes the original target instruction first, and then the injected code. If additional instructions are
    /// overwritten by the hook jump, they will be executed after the injected code.
    /// </summary>
    ExecuteOriginalInstructionFirst,
    
    /// <summary>
    /// Executes only the injected code, overwriting the instruction at the hook address. If additional instructions are
    /// overwritten by the hook jump, they will be executed after the injected code.
    /// </summary>
    ReplaceOriginalInstruction
}

/// <summary>
/// Defines how the hook jump should be performed.
/// </summary>
public enum HookJumpMode
{
    /// <summary>
    /// Use a near jump if possible. If a near jump is not possible, fall back to a far jump.
    /// This is the safest option, as it will always work, but may not always give you the best performance (although
    /// it should in most cases).
    /// For 32-bit processes, this mode is equivalent to <see cref="NearJumpOnly"/>, as near jumps are always possible.
    /// </summary>
    NearJumpWithFallbackOnFarJump,
    
    /// <summary>
    /// Use a near jump only. If a near jump is not possible, the hook operation will fail.
    /// Use this only if hook performance is critical and a far jump would not be acceptable.
    /// For 32-bit processes, this mode is equivalent to <see cref="NearJumpWithFallbackOnFarJump"/>, as near jumps are
    /// always possible.
    /// </summary>
    NearJumpOnly
}

/// <summary>
/// Holds settings for a hook operation.
/// </summary>
public class HookOptions
{
    /// <summary>
    /// Gets the execution mode of the injected code in relation to the original code. Defines whether the original
    /// code should be overwritten, executed before, or executed after the injected code.
    /// </summary>
    public HookExecutionMode ExecutionMode { get; init; }
    
    /// <summary>
    /// Gets the jump mode, which defines what kind of jump should be used to redirect the code flow to the injected
    /// code. Most of the time, you should leave it to its default
    /// <see cref="HookJumpMode.NearJumpWithFallbackOnFarJump"/> value.
    /// </summary>
    public HookJumpMode JumpMode { get; init; }
    
    /// <summary>
    /// Gets a collection of registers that should be saved before the injected code is executed, and restored after it
    /// is executed. This is used to isolate the injected code from the original code, to prevent it from affecting the
    /// original code's behavior or causing crashes.
    /// </summary>
    public HookRegister[] RegistersToPreserve { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HookOptions"/> class with the given values.
    /// </summary>
    /// <param name="executionMode">Execution mode of the injected code in relation to the original code. Defines
    /// whether the original code should be overwritten, executed before, or executed after the injected code.</param>
    /// <param name="registersToPreserve">Optional registers to save before the injected code is executed, and restore
    /// after it is executed. Use this to isolate the injected code from the original code, to prevent it from affecting
    /// the original code's behavior or causing crashes.</param>
    public HookOptions(HookExecutionMode executionMode, params HookRegister[] registersToPreserve)
        : this(executionMode, HookJumpMode.NearJumpWithFallbackOnFarJump, registersToPreserve) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HookOptions"/> class with the given values.
    /// </summary>
    /// <param name="executionMode">Execution mode of the injected code in relation to the original code. Defines
    /// whether the original code should be overwritten, executed before, or executed after the injected code.</param>
    /// <param name="jumpMode">Jump mode, which defines what kind of jump should be used to redirect the code flow to
    /// the injected code. Use <see cref="HookJumpMode.NearJumpWithFallbackOnFarJump"/>, unless performance is critical
    /// and a far jump would be unacceptable.</param>
    /// <param name="registersToPreserve">Optional registers to save before the injected code is executed, and restore
    /// after it is executed. Use this to isolate the injected code from the original code, to prevent it from affecting
    /// the original code behavior or causing crashes.</param>
    public HookOptions(HookExecutionMode executionMode, HookJumpMode jumpMode,
        params HookRegister[] registersToPreserve)
    {
        ExecutionMode = executionMode;
        JumpMode = jumpMode;
        RegistersToPreserve = registersToPreserve;
    }
    
    /// <summary>
    /// Gets the sum of <see cref="GetExpectedPreCodeSize"/> and <see cref="GetExpectedPostCodeSize"/>.
    /// </summary>
    /// <param name="is64Bit">Indicates if the target process is 64-bit.</param>
    /// <returns>The total size in bytes of the additional code that will be prepended and appended to the injected
    /// code.</returns>
    internal int GetExpectedGeneratedCodeSize(bool is64Bit)
        => GetExpectedPreCodeSize(is64Bit) + GetExpectedPostCodeSize(is64Bit);

    /// <summary>
    /// Gets the predicted length in bytes of the additional code that will be prepended to the hook code.
    /// </summary>
    /// <param name="is64Bit">Indicates if the target process is 64-bit.</param>
    /// <returns>The size in bytes of the prepended code.</returns>
    internal int GetExpectedPreCodeSize(bool is64Bit)
    {
        int totalSize = 0;
        if (RegistersToPreserve.Contains(HookRegister.Flags))
            totalSize += AssemblerExtensions.GetSizeOfFlagsSave(is64Bit);

        totalSize += RegistersToPreserve.Select(r => r.ToRegister(is64Bit))
            .Where(r => r != null)
            .Sum(register => AssemblerExtensions.GetSizeOfSaveInstructions(register!.Value, is64Bit));

        if (RegistersToPreserve.Contains(HookRegister.FpuStack))
            totalSize += AssemblerExtensions.GetSizeOfFpuStackSave(is64Bit);
        
        return totalSize;
    }
    
    /// <summary>
    /// Gets the predicted length in bytes of the additional code that will be appended to the hook code.
    /// </summary>
    /// <param name="is64Bit">Indicates if the target process is 64-bit.</param>
    /// <returns>The size in bytes of the appended code.</returns>
    internal int GetExpectedPostCodeSize(bool is64Bit)
    {
        int totalSize = 0;
        if (RegistersToPreserve.Contains(HookRegister.Flags))
            totalSize += AssemblerExtensions.GetSizeOfFlagsRestore(is64Bit);

        totalSize += RegistersToPreserve.Select(r => r.ToRegister(is64Bit))
            .Where(r => r != null)
            .Sum(register => AssemblerExtensions.GetSizeOfRestoreInstructions(register!.Value, is64Bit));

        if (RegistersToPreserve.Contains(HookRegister.FpuStack))
            totalSize += AssemblerExtensions.GetSizeOfFpuStackRestore(is64Bit);
        
        return totalSize;
    }
}