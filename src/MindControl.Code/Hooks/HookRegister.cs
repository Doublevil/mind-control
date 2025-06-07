using Iced.Intel;

namespace MindControl.Hooks;

/// <summary>
/// Registers or register groups that can be saved and restored in hooks.
/// </summary>
public enum HookRegister
{
    /// <summary>Represents the RFLAGS (in 64-bit) or EFLAGS (in 32-bit) register, holding CPU flags.</summary>
    Flags,
    /// <summary>Represents the RAX (in 64-bit) or EAX (in 32-bit) register.</summary>
    RaxEax,
    /// <summary>Represents the RBX (in 64-bit) or EBX (in 32-bit) register.</summary>
    RbxEbx,
    /// <summary>Represents the RCX (in 64-bit) or ECX (in 32-bit) register.</summary>
    RcxEcx,
    /// <summary>Represents the RDX (in 64-bit) or EDX (in 32-bit) register.</summary>
    RdxEdx,
    /// <summary>Represents the RSI (in 64-bit) or ESI (in 32-bit) register.</summary>
    RsiEsi,
    /// <summary>Represents the RDI (in 64-bit) or EDI (in 32-bit) register.</summary>
    RdiEdi,
    /// <summary>Represents the RBP (in 64-bit) or EBP (in 32-bit) register.</summary>
    RbpEbp,
    /// <summary>Represents the RSP (in 64-bit) or ESP (in 32-bit) register.</summary>
    RspEsp,
    /// <summary>Represents the R8 register (64-bit only).</summary>
    R8,
    /// <summary>Represents the R9 register (64-bit only).</summary>
    R9,
    /// <summary>Represents the R10 register (64-bit only).</summary>
    R10,
    /// <summary>Represents the R11 register (64-bit only).</summary>
    R11,
    /// <summary>Represents the R12 register (64-bit only).</summary>
    R12,
    /// <summary>Represents the R13 register (64-bit only).</summary>
    R13,
    /// <summary>Represents the R14 register (64-bit only).</summary>
    R14,
    /// <summary>Represents the R15 register (64-bit only).</summary>
    R15,
    /// <summary>Represents the XMM0 register.</summary>
    Xmm0,
    /// <summary>Represents the XMM1 register.</summary>
    Xmm1,
    /// <summary>Represents the XMM2 register.</summary>
    Xmm2,
    /// <summary>Represents the XMM3 register.</summary>
    Xmm3,
    /// <summary>Represents the XMM4 register.</summary>
    Xmm4,
    /// <summary>Represents the XMM5 register.</summary>
    Xmm5,
    /// <summary>Represents the XMM6 register.</summary>
    Xmm6,
    /// <summary>Represents the XMM7 register.</summary>
    Xmm7,
    /// <summary>Represents the XMM8 register (64-bit only).</summary>
    Xmm8,
    /// <summary>Represents the XMM9 register (64-bit only).</summary>
    Xmm9,
    /// <summary>Represents the XMM10 register (64-bit only).</summary>
    Xmm10,
    /// <summary>Represents the XMM11 register (64-bit only).</summary>
    Xmm11,
    /// <summary>Represents the XMM12 register (64-bit only).</summary>
    Xmm12,
    /// <summary>Represents the XMM13 register (64-bit only).</summary>
    Xmm13,
    /// <summary>Represents the XMM14 register (64-bit only).</summary>
    Xmm14,
    /// <summary>Represents the XMM15 register (64-bit only).</summary>
    Xmm15,
    /// <summary>Represents the MM0 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm0,
    /// <summary>Represents the MM1 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm1,
    /// <summary>Represents the MM2 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm2,
    /// <summary>Represents the MM3 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm3,
    /// <summary>Represents the MM4 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm4,
    /// <summary>Represents the MM5 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm5,
    /// <summary>Represents the MM6 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm6,
    /// <summary>Represents the MM7 register. This register is not normally used. It is shared with the FPU stack. Do
    /// not use both at the same time.</summary>
    Mm7,
    /// <summary>Represents all ST registers (ST0-ST7). Since they work as a stack, the whole stack is saved and
    /// restored coordinatedly. These registers are not normally used. They are shared with the MMX registers (MM0-MM7).
    /// Do not use both at the same time.</summary>
    FpuStack
}

/// <summary>
/// Provides preset collections of registers for hooks.
/// </summary>
public static class HookRegisters
{
    /// <summary>
    /// Use this collection to preserve flags and general-purpose registers. Works for both 32-bit and 64-bit processes
    /// (incompatible registers are filtered out). This collection includes RFLAGS/EFLAGS, RAX/EAX, RBX/EBX, RCX/ECX,
    /// RDX/EDX, RSI/ESI, RDI/EDI, RBP/EBP, RSP/ESP, and R8-R15 for 64-bit processes. It does not include the XMM
    /// registers commonly used for floating-point operations.
    /// </summary>
    public static readonly HookRegister[] GeneralPurposeRegisters =
    [
        HookRegister.Flags,
        HookRegister.RaxEax,
        HookRegister.RbxEbx,
        HookRegister.RcxEcx,
        HookRegister.RdxEdx,
        HookRegister.RsiEsi,
        HookRegister.RdiEdi,
        HookRegister.RbpEbp,
        HookRegister.RspEsp,
        HookRegister.R8,
        HookRegister.R9,
        HookRegister.R10,
        HookRegister.R11,
        HookRegister.R12,
        HookRegister.R13,
        HookRegister.R14,
        HookRegister.R15
    ];
    
    /// <summary>
    /// Use this collection to preserve XMM registers. Works for both 32-bit and 64-bit processes (incompatible
    /// registers are filtered out). This collection includes all XMM registers, from XMM0 to XMM15 (XMM8 to XMM15 are
    /// only available in 64-bit processes).
    /// </summary>
    public static readonly HookRegister[] XmmRegisters =
    [
        HookRegister.Xmm0,
        HookRegister.Xmm1,
        HookRegister.Xmm2,
        HookRegister.Xmm3,
        HookRegister.Xmm4,
        HookRegister.Xmm5,
        HookRegister.Xmm6,
        HookRegister.Xmm7,
        HookRegister.Xmm8,
        HookRegister.Xmm9,
        HookRegister.Xmm10,
        HookRegister.Xmm11,
        HookRegister.Xmm12,
        HookRegister.Xmm13,
        HookRegister.Xmm14,
        HookRegister.Xmm15
    ];
    
    /// <summary>
    /// Use this collection to preserve MMX registers. Works for both 32-bit and 64-bit processes. This collection
    /// includes all MM registers, from MM0 to MM7. These registers are not normally used. They are shared with the FPU
    /// stack. Do not use both at the same time.
    /// </summary>
    public static readonly HookRegister[] MmRegisters =
    [
        HookRegister.Mm0,
        HookRegister.Mm1,
        HookRegister.Mm2,
        HookRegister.Mm3,
        HookRegister.Mm4,
        HookRegister.Mm5,
        HookRegister.Mm6,
        HookRegister.Mm7
    ];
    
    /// <summary>
    /// Use this collection to preserve all commonly used registers. Works for both 32-bit and 64-bit processes
    /// (incompatible registers are filtered out). This collection includes all general-purpose registers and XMM
    /// registers. This is a good choice if you want to isolate the injected code from the original code, and are not
    /// sure which registers the injected code will use (e.g. when calling an injected DLL function).
    /// </summary>
    public static readonly HookRegister[] AllCommonRegisters = GeneralPurposeRegisters.Concat(XmmRegisters).ToArray();

    /// <summary>
    /// Converts a <see cref="HookRegister"/> to a <see cref="Register"/> if possible and when compatible with the
    /// specified bitness.
    /// </summary>
    /// <param name="hookRegister">Hook register to convert.</param>
    /// <param name="is64Bit">Indicates if the target process bitness is 64-bit (true) or 32-bit (false).</param>
    /// <returns>The equivalent <see cref="Register"/> if the conversion is possible and compatible with the bitness,
    /// or null otherwise.</returns>
    internal static Register? ToRegister(this HookRegister hookRegister, bool is64Bit) => hookRegister switch
    {
        HookRegister.RaxEax => is64Bit ? Register.RAX : Register.EAX,
        HookRegister.RbxEbx => is64Bit ? Register.RBX : Register.EBX,
        HookRegister.RcxEcx => is64Bit ? Register.RCX : Register.ECX,
        HookRegister.RdxEdx => is64Bit ? Register.RDX : Register.EDX,
        HookRegister.RsiEsi => is64Bit ? Register.RSI : Register.ESI,
        HookRegister.RdiEdi => is64Bit ? Register.RDI : Register.EDI,
        HookRegister.RbpEbp => is64Bit ? Register.RBP : Register.EBP,
        HookRegister.RspEsp => is64Bit ? Register.RSP : Register.ESP,
        HookRegister.R8 => is64Bit ? Register.R8 : null,
        HookRegister.R9 => is64Bit ? Register.R9 : null,
        HookRegister.R10 => is64Bit ? Register.R10 : null,
        HookRegister.R11 => is64Bit ? Register.R11 : null,
        HookRegister.R12 => is64Bit ? Register.R12 : null,
        HookRegister.R13 => is64Bit ? Register.R13 : null,
        HookRegister.R14 => is64Bit ? Register.R14 : null,
        HookRegister.R15 => is64Bit ? Register.R15 : null,
        HookRegister.Xmm0 => Register.XMM0,
        HookRegister.Xmm1 => Register.XMM1,
        HookRegister.Xmm2 => Register.XMM2,
        HookRegister.Xmm3 => Register.XMM3,
        HookRegister.Xmm4 => Register.XMM4,
        HookRegister.Xmm5 => Register.XMM5,
        HookRegister.Xmm6 => Register.XMM6,
        HookRegister.Xmm7 => Register.XMM7,
        HookRegister.Xmm8 => is64Bit ? Register.XMM8 : null,
        HookRegister.Xmm9 => is64Bit ? Register.XMM9 : null,
        HookRegister.Xmm10 => is64Bit ? Register.XMM10 : null,
        HookRegister.Xmm11 => is64Bit ? Register.XMM11 : null,
        HookRegister.Xmm12 => is64Bit ? Register.XMM12 : null,
        HookRegister.Xmm13 => is64Bit ? Register.XMM13 : null,
        HookRegister.Xmm14 => is64Bit ? Register.XMM14 : null,
        HookRegister.Xmm15 => is64Bit ? Register.XMM15 : null,
        HookRegister.Mm0 => Register.MM0,
        HookRegister.Mm1 => Register.MM1,
        HookRegister.Mm2 => Register.MM2,
        HookRegister.Mm3 => Register.MM3,
        HookRegister.Mm4 => Register.MM4,
        HookRegister.Mm5 => Register.MM5,
        HookRegister.Mm6 => Register.MM6,
        HookRegister.Mm7 => Register.MM7,
        _ => null
    };
}