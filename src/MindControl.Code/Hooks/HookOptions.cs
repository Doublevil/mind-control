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
/// Flags that define how the injected code is isolated from the original code.
/// </summary>
[Flags]
public enum HookIsolationMode
{
    /// <summary>
    /// No isolation. Modifications to flags and registers in the injected code will affect the original code, unless
    /// the injected code contains instructions to save and restore the state in itself.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Save and restore CPU flags. This allows the injected code to modify flags without affecting the original code.
    /// This option is a flag, it can be combined with other options.
    /// </summary>
    PreserveFlags = 1,
    
    /// <summary>
    /// Save and restore general-purpose registers. This allows the injected code to modify most registers without
    /// affecting the original code.
    /// This option is a flag, it can be combined with other options.
    /// </summary>
    PreserveGeneralPurposeRegisters = 2,
    
    /// <summary>
    /// Save and restore XMM registers. This allows the injected code to modify XMM registers (for floating-point
    /// operations) without affecting the original code.
    /// This option is a flag, it can be combined with other options.
    /// </summary>
    PreserveXmmRegisters = 4,
    
    /// <summary>
    /// Save and restore the FPU stack. This allows the injected code to modify the FPU stack without affecting the
    /// original code. Note that the FPU stack is not used in modern code, so this option is usually not needed.
    /// This option is a flag, it can be combined with other options.
    /// </summary>
    PreserveFpuStack = 8,
    
    /// <summary>
    /// Save and restore the FPU stack. This allows the injected code to modify the FPU stack without affecting the
    /// original code. Note that the FPU stack is not used in modern code, so this option is usually not needed.
    /// This option is a flag, it can be combined with other options.
    /// </summary>
    PreserveMmRegisters = 16,
    
    /// <summary>
    /// This option is a combination of the <see cref="PreserveFlags"/>, <see cref="PreserveGeneralPurposeRegisters"/>,
    /// and <see cref="PreserveXmmRegisters"/> options.
    /// This allows the injected code to modify flags and registers without affecting the original code. It is the
    /// recommended option.
    /// </summary>
    FullIsolation = PreserveFlags | PreserveGeneralPurposeRegisters | PreserveXmmRegisters,
    
    /// <summary>
    /// This option is a combination of all isolation options. 
    /// It allows the injected code to modify flags and registers without affecting the original code. It also includes
    /// the FPU stack and MM registers, which are usually not needed. This will make the hook code slower.
    /// Prefer <see cref="FullIsolation"/> unless you know ST and MM registers are used both in the original code and
    /// the injected code.
    /// </summary>
    FullCompatibilityIsolation = FullIsolation | PreserveFpuStack | PreserveMmRegisters
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
    /// </summary>
    NearJumpWithFallbackOnFarJump,
    
    /// <summary>
    /// Use a near jump only. If a near jump is not possible, the hook operation will fail.
    /// Use this only if hook performance is critical and a far jump would not be acceptable.
    /// </summary>
    NearJumpOnly
}

/// <summary>
/// Holds settings for a hook operation.
/// </summary>
public class HookOptions
{
    /// <summary>
    /// Lists the general purpose registers that are preserved by the
    /// <see cref="HookIsolationMode.PreserveGeneralPurposeRegisters"/> isolation flag.
    /// </summary>
    private static readonly Register[] PreservableGeneralPurposeRegisters =
    [
        // General purpose 32-bit registers
        Register.EAX, Register.EBX, Register.ECX, Register.EDX,
        Register.ESI, Register.EDI, Register.EBP, Register.ESP,
        
        // General purpose 64-bit registers
        Register.RAX, Register.RBX, Register.RCX, Register.RDX,
        Register.RSI, Register.RDI, Register.RBP, Register.RSP,
        Register.R8, Register.R9, Register.R10, Register.R11,
        Register.R12, Register.R13, Register.R14, Register.R15
    ];

    /// <summary>
    /// Lists the XMM registers to be preserved by the <see cref="HookIsolationMode.PreserveXmmRegisters"/> isolation
    /// flag.
    /// </summary>
    private static readonly Register[] XmmRegisters =
    [
        // (8-15 are 64-bit only)
        Register.XMM0, Register.XMM1, Register.XMM2, Register.XMM3,
        Register.XMM4, Register.XMM5, Register.XMM6, Register.XMM7,
        Register.XMM8, Register.XMM9, Register.XMM10, Register.XMM11,
        Register.XMM12, Register.XMM13, Register.XMM14, Register.XMM15
    ];

    /// <summary>
    /// Lists the MM registers to be preserved by the <see cref="HookIsolationMode.PreserveMmRegisters"/> isolation
    /// flag.
    /// </summary>
    private static readonly Register[] MmRegisters =
    [
        Register.MM0, Register.MM1, Register.MM2, Register.MM3,
        Register.MM4, Register.MM5, Register.MM6, Register.MM7
    ];
    
    /// <summary>
    /// Gets the execution mode of the injected code in relation to the original code. Defines whether the original
    /// code should be overwritten, executed before, or executed after the injected code.
    /// </summary>
    public HookExecutionMode ExecutionMode { get; init; }

    /// <summary>
    /// Gets the isolation mode flags defining how the injected code is isolated from the original code.
    /// Depending on the mode, instructions may be prepended and appended to the injected code to save and restore
    /// registers and flags, allowing the injected code to run without affecting the original code.
    /// </summary>
    public HookIsolationMode IsolationMode { get; init; }
    
    /// <summary>
    /// Gets the registers to exclude from preservation, regardless of the <see cref="IsolationMode"/>.
    /// Use this if you want specific registers to NOT be saved and restored, either to improve performance or to
    /// modify the behavior of the original code.
    /// </summary>
    public Register[] RegistersExcludedFromPreservation { get; init; }
    
    /// <summary>
    /// Gets the jump mode, which defines what kind of jump should be used to redirect the code flow to the injected
    /// code. Most of the time, you should leave it to its default
    /// <see cref="HookJumpMode.NearJumpWithFallbackOnFarJump"/> value.
    /// </summary>
    public HookJumpMode JumpMode { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HookOptions"/> class with the given values.
    /// The <see cref="IsolationMode"/> is set to <see cref="HookIsolationMode.FullIsolation"/>.
    /// </summary>
    /// <param name="executionMode">Execution mode of the injected code in relation to the original code. Defines
    /// whether the original code should be overwritten, executed before, or executed after the injected code.</param>
    /// <param name="registersExcludedFromPreservation">Optional registers to exclude from preservation, regardless of
    /// the <see cref="IsolationMode"/>. Use this if you want specific registers to NOT be saved and restored, either to
    /// improve performance or to modify the behavior of the original code.</param>
    public HookOptions(HookExecutionMode executionMode, params Register[] registersExcludedFromPreservation)
        : this(executionMode, HookIsolationMode.FullIsolation, HookJumpMode.NearJumpWithFallbackOnFarJump,
            registersExcludedFromPreservation) { } 
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HookOptions"/> class with the given values.
    /// </summary>
    /// <param name="executionMode">Execution mode of the injected code in relation to the original code. Defines
    /// whether the original code should be overwritten, executed before, or executed after the injected code.</param>
    /// <param name="isolationMode">Isolation mode flags defining how the injected code is isolated from the original
    /// code. Depending on the mode, instructions may be prepended and appended to the injected code to save and
    /// restore registers and flags, allowing the injected code to run without affecting the original code.</param>
    /// <param name="registersExcludedFromPreservation">Optional registers to exclude from preservation, regardless of
    /// the <paramref name="isolationMode"/>. Use this if you want specific registers to NOT be saved and restored,
    /// either to improve performance or to modify the behavior of the original code.</param>
    public HookOptions(HookExecutionMode executionMode, HookIsolationMode isolationMode,
        params Register[] registersExcludedFromPreservation)
        : this(executionMode, isolationMode, HookJumpMode.NearJumpWithFallbackOnFarJump,
            registersExcludedFromPreservation) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HookOptions"/> class with the given values.
    /// </summary>
    /// <param name="executionMode">Execution mode of the injected code in relation to the original code. Defines
    /// whether the original code should be overwritten, executed before, or executed after the injected code.</param>
    /// <param name="isolationMode">Isolation mode flags defining how the injected code is isolated from the original
    /// code. Depending on the mode, instructions may be prepended and appended to the injected code to save and
    /// restore registers and flags, allowing the injected code to run without affecting the original code.</param>
    /// <param name="jumpMode">Jump mode, which defines what kind of jump should be used to redirect the code flow to
    /// the injected code. Use <see cref="HookJumpMode.NearJumpWithFallbackOnFarJump"/>, unless performance is critical
    /// and a far jump would be unacceptable.</param>
    /// <param name="registersExcludedFromPreservation">Optional registers to exclude from preservation, regardless of
    /// the <paramref name="isolationMode"/>. Use this if you want specific registers to NOT be saved and restored,
    /// either to improve performance or to modify the behavior of the original code.</param>
    public HookOptions(HookExecutionMode executionMode, HookIsolationMode isolationMode, HookJumpMode jumpMode,
        params Register[] registersExcludedFromPreservation)
    {
        ExecutionMode = executionMode;
        IsolationMode = isolationMode;
        JumpMode = jumpMode;
        RegistersExcludedFromPreservation = registersExcludedFromPreservation;
        _registersToPreserve = GetRegistersToPreserve();
    }

    private readonly Register[] _registersToPreserve;

    /// <summary>
    /// Gets the registers to save and restore, based on the <see cref="IsolationMode"/> and
    /// <see cref="RegistersExcludedFromPreservation"/> properties.
    /// </summary>
    internal IEnumerable<Register> RegistersToPreserve => _registersToPreserve;

    /// <summary>
    /// Builds the array of registers to save and restore, based on the <see cref="IsolationMode"/> and
    /// <see cref="RegistersExcludedFromPreservation"/> properties.
    /// </summary>
    private Register[] GetRegistersToPreserve()
    {
        List<Register> registersToPreserve = new(64);
        if (IsolationMode.HasFlag(HookIsolationMode.PreserveGeneralPurposeRegisters))
            registersToPreserve.AddRange(PreservableGeneralPurposeRegisters);
        if (IsolationMode.HasFlag(HookIsolationMode.PreserveXmmRegisters))
            registersToPreserve.AddRange(XmmRegisters);
        if (IsolationMode.HasFlag(HookIsolationMode.PreserveMmRegisters))
            registersToPreserve.AddRange(MmRegisters);

        return registersToPreserve.Except(RegistersExcludedFromPreservation).ToArray();
    }
    
    /// <summary>
    /// Gets the predicted length in bytes of the additional code that will be prepended and appended to the hook code.
    /// </summary>
    /// <param name="is64Bits">Indicates if the target process is 64-bit.</param>
    /// <returns>The total size in bytes of the additional code.</returns>
    internal int GetExpectedGeneratedCodeSize(bool is64Bits)
    {
        int totalSize = 0;
        if (IsolationMode.HasFlag(HookIsolationMode.PreserveFlags))
            totalSize += AssemblerExtensions.GetSizeOfFlagsSave(is64Bits)
                + AssemblerExtensions.GetSizeOfFlagsRestore(is64Bits);

        foreach (var register in RegistersToPreserve)
        {
            totalSize += AssemblerExtensions.GetSizeOfSaveInstructions(register, is64Bits);
            totalSize += AssemblerExtensions.GetSizeOfRestoreInstructions(register, is64Bits);
        }

        if (IsolationMode.HasFlag(HookIsolationMode.PreserveFpuStack))
            totalSize += AssemblerExtensions.GetSizeOfFpuStackSave(is64Bits)
                + AssemblerExtensions.GetSizeOfFpuStackRestore(is64Bits);
        
        return totalSize;
    }
}