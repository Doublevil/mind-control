using Iced.Intel;
using MindControl.Results;

namespace MindControl;

/// <summary>
/// Provides extension methods around code assembling.
/// </summary>
public static class AssemblerExtensions
{
    /// <summary>
    /// Checks if the register is compatible with the target architecture and can be saved and restored individually.
    /// </summary>
    /// <param name="register">Register to check.</param>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>True if the register is compatible with the target architecture, false otherwise.</returns>
    /// <remarks>For ST registers, this method will always return false, because ST registers must be saved and restored
    /// as a whole (to preserve the whole FPU stack).</remarks>
    internal static bool IsIndividualPreservationSupported(this Register register, bool is64Bit)
    {
        return (is64Bit && register.IsGPR64()) || (!is64Bit && register.IsGPR32())
            || (register.IsXMM() && register.GetNumber() <= GetMaxXmmRegisterNumber(is64Bit))
            || register.IsST()
            || register.IsMM();
    }
    
    /// <summary>
    /// Gets the maximum number of XMM registers available for the target architecture.
    /// </summary>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The maximum number of XMM registers available for the target architecture.</returns>
    private static int GetMaxXmmRegisterNumber(bool is64Bit) => is64Bit ? 15 : 7;

    /// <summary>
    /// Saves the given register to the stack (push it on the stack using instructions that depend on the register and
    /// bitness).
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    /// <param name="register">Register to save.</param>
    /// <exception cref="ArgumentException">Thrown if the register is not supported.</exception>
    public static void SaveRegister(this Assembler assembler, Register register)
    {
        if (register.IsGPR32())
            assembler.push(new AssemblerRegister32(register));
        else if (register.IsGPR64())
            assembler.push(new AssemblerRegister64(register));
        else if (register.IsST())
            throw new ArgumentException(
                $"Cannot save ST registers individually. They must be saved as a whole using the {nameof(SaveFpuStack)} method.");
        else if (register.IsMM())
        {
            if (assembler.Bitness == 64)
            {
                var rsp = new AssemblerRegister64(Register.RSP);
                assembler.sub(rsp, 8);
                assembler.movq(rsp+0, new AssemblerRegisterMM(register));
            }
            else
            {
                var esp = new AssemblerRegister32(Register.ESP);
                assembler.sub(esp, 8);
                assembler.movq(esp+0, new AssemblerRegisterMM(register));
            }
        }
        else if (register.IsXMM())
        {
            if (assembler.Bitness == 64)
            {
                var rsp = new AssemblerRegister64(Register.RSP);
                assembler.sub(rsp, 16);
                assembler.movq(rsp+0, new AssemblerRegisterXMM(register));
            }
            else
            {
                var esp = new AssemblerRegister32(Register.ESP);
                assembler.sub(esp, 16);
                assembler.movq(esp+0, new AssemblerRegisterXMM(register));
            }
        }
        else
            throw new ArgumentException($"Cannot save register {register} because it is not supported.");
    }

    /// <summary>Registers that are pushed and popped on 2-byte instructions.</summary>
    private static readonly Register[] TwoBytePushGprRegisters =
    [
        Register.R8, Register.R9, Register.R10, Register.R11, Register.R12, Register.R13, Register.R14, Register.R15
    ];
    
    /// <summary>
    /// Gets the size in bytes of the instructions needed to save the given register to the stack.
    /// </summary>
    /// <param name="register">Register to save.</param>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to save the register to the stack.</returns>
    /// <exception cref="ArgumentException">Thrown if the register is not supported.</exception>
    internal static int GetSizeOfSaveInstructions(Register register, bool is64Bit)
    {
        // Some registers are pushed on 2-byte instructions
        if (TwoBytePushGprRegisters.Contains(register))
            return 2;
        
        if (register.IsGPR32() || register.IsGPR64())
            return 1; // Other GPR registers are pushed on 1-byte
        
        if (register.IsMM() || register.IsXMM())
        {
            // Example in x86:
            // sub esp, 16       ; 83 EC 10 (3 bytes)
            // movq [esp], xmm0  ; 0F 7F 04 24 (4 bytes)
            
            // For x64, there is an added REX prefix byte (0x48) for both instructions.
            // Additionally, the movq instruction for XMM registers with numbers 8 and above (only available in x64) is
            // one byte longer.
            
            int subSize = is64Bit ? 4 : 3;
            int movSize = is64Bit ? (register.GetNumber() >= 8 ? 6 : 5) : 4;
            return subSize + movSize;
        }
        
        if (register.IsST())
            throw new ArgumentException(
                $"Cannot evaluate ST registers individually. They must be evaluated as a whole using the {nameof(GetSizeOfFpuStackSave)} method.");
        
        throw new ArgumentException($"Cannot save register {register} because it is not supported.");
    }

    /// <summary>
    /// Restores the given register from the stack (pop it from the stack using instructions that depend on the register
    /// and bitness).
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    /// <param name="register">Register to restore.</param>
    public static void RestoreRegister(this Assembler assembler, Register register)
    {
        if (register.IsGPR32())
            assembler.pop(new AssemblerRegister32(register));
        else if (register.IsGPR64())
            assembler.pop(new AssemblerRegister64(register));
        else if (register.IsST())
            throw new ArgumentException(
                $"Cannot restore ST registers individually. They must be restored as a whole using the {nameof(RestoreFpuStack)} method.");
        else if (register.IsMM())
        {
            if (assembler.Bitness == 64)
            {
                var rsp = new AssemblerRegister64(Register.RSP);
                assembler.movq(new AssemblerRegisterMM(register), rsp+0);
                assembler.add(rsp, 8);
            }
            else
            {
                var esp = new AssemblerRegister32(Register.ESP);
                assembler.movq(new AssemblerRegisterMM(register), esp+0);
                assembler.add(esp, 8);
            }
        }
        else if (register.IsXMM())
        {
            if (assembler.Bitness == 64)
            {
                var rsp = new AssemblerRegister64(Register.RSP);
                assembler.movq(new AssemblerRegisterXMM(register), rsp+0);
                assembler.add(rsp, 16);
            }
            else
            {
                var esp = new AssemblerRegister32(Register.ESP);
                assembler.movq(new AssemblerRegisterXMM(register), esp+0);
                assembler.add(esp, 16);
            }
        }
        else
            throw new ArgumentException($"Cannot restore register {register} because it is not supported.");
    }

    /// <summary>
    /// Gets the size in bytes of the instructions needed to restore the given register to the stack.
    /// </summary>
    /// <param name="register">Register to save.</param>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to restore the register from the stack.</returns>
    /// <exception cref="ArgumentException">Thrown if the register is not supported.</exception>
    internal static int GetSizeOfRestoreInstructions(Register register, bool is64Bit)
        => GetSizeOfSaveInstructions(register, is64Bit); // The instructions are the opposite, but the size is the same
    
    /// <summary>
    /// Saves the FPU stack state as a whole (pushes all 8 ST registers to the stack).
    /// This cannot be done individually because ST registers are part of a stack and thus save and restore operations
    /// must be performed coordinatedly.
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    public static void SaveFpuStack(this Assembler assembler)
    {
        // The FPU stack is a stack of 8 ST registers, so we must save and restore them as a whole.
        // Each ST register is 10 bytes long, so we should push 80 bytes to the stack.
        // However, for alignment purposes, we consider them as 12 bytes each, and so we push 96 bytes to the stack.
        // This allows for optimal memory access.
        
        if (assembler.Bitness == 64)
        {
            var rsp = new AssemblerRegister64(Register.RSP);
            assembler.sub(rsp, 12 * 8); // Allocate space for all 8 FPU values
            for (int i = 0; i < 8; i++)
                assembler.fstp(rsp + i * 12);
        }
        else
        {
            var esp = new AssemblerRegister32(Register.ESP);
            assembler.sub(esp, 12 * 8); // Allocate space for all 8 FPU values
            for (int i = 0; i < 8; i++)
                assembler.fstp(esp + i * 12);
        }
    }
    
    /// <summary>
    /// Gets the size in bytes of the instructions needed to save the FPU stack state as a whole.
    /// </summary>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to save the FPU stack state as a whole.</returns>
    internal static int GetSizeOfFpuStackSave(bool is64Bit)
    {
        // Example in x86:
        // sub esp, 64       ; 83 EC 40 (3 bytes)
        // fstp [esp]        ; D9 1C 24 (3 bytes)
        // fstp [esp+8]      ; D9 5C 24 08 (4 bytes)
        // ...
        // fstp [esp+54]     ; D9 5C 24 38 (4 bytes)
        
        // For x64, there is an added REX prefix byte (0x48) for each instruction.
        // So it should be 3 + 3 + 4 * 7 = 6 + 28 = 34 bytes for x86 and 4 + 4 + 5 * 7 = 8 + 35 = 43 bytes for x64.
        
        return is64Bit ? 43 : 34;
    }
    
    /// <summary>
    /// Restores the FPU stack state as a whole (pops all 8 ST registers from the stack).
    /// This cannot be done individually because ST registers are part of a stack and thus save and restore operations
    /// must be performed coordinatedly.
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    public static void RestoreFpuStack(this Assembler assembler)
    {
        if (assembler.Bitness == 64)
        {
            var rsp = new AssemblerRegister64(Register.RSP);
            for (int i = 7; i >= 0; i--)
                assembler.fld(rsp + i * 12);
            assembler.add(rsp, 12 * 8); // Deallocate space for all 8 FPU values
        }
        else
        {
            var esp = new AssemblerRegister32(Register.ESP);
            for (int i = 7; i >= 0; i--)
                assembler.fld(esp + i * 12);
            assembler.add(esp, 12 * 8); // Deallocate space for all 8 FPU values
        }
    }
    
    /// <summary>
    /// Gets the size in bytes of the instructions needed to restore the FPU stack state as a whole.
    /// </summary>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to restore the FPU stack state as a whole.</returns>
    internal static int GetSizeOfFpuStackRestore(bool is64Bit)
        => GetSizeOfFpuStackSave(is64Bit); // The instructions are the opposite, but the size is the same
    
    /// <summary>
    /// Saves the CPU flags to the stack (pushes the flags to the stack).
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    internal static void SaveFlags(this Assembler assembler)
    {
        if (assembler.Bitness == 64)
            assembler.pushfq();
        else
            assembler.pushf();
    }

    /// <summary>
    /// Gets the size in bytes of the instructions needed to save the CPU flags.
    /// </summary>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to save the CPU flags.</returns>
    internal static int GetSizeOfFlagsSave(bool is64Bit) => 1; // pushf is always single-byte
    
    /// <summary>
    /// Restores the CPU flags from the stack (pops the flags from the stack).
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    internal static void RestoreFlags(this Assembler assembler)
    {
        if (assembler.Bitness == 64)
            assembler.popfq();
        else
            assembler.popf();
    }
    
    /// <summary>
    /// Gets the size in bytes of the instructions needed to restore the CPU flags.
    /// </summary>
    /// <param name="is64Bit">True if the target architecture is 64-bit, false if it is 32-bit.</param>
    /// <returns>The size in bytes of the instructions needed to restore the CPU flags.</returns>
    internal static int GetSizeOfFlagsRestore(bool is64Bit) => 1; // popf is always single-byte
    
    /// <summary>
    /// Attempts to assemble the instructions registered on this assembler instance and return the resulting bytes.
    /// </summary>
    /// <param name="assembler">Assembler instance to use to generate the instructions.</param>
    /// <param name="baseAddress">Base address to use for the assembled instructions. Default is 0.</param>
    /// <param name="bufferSize">Size of the buffer to use for the assembled instructions. Default is 128 bytes.</param>
    /// <returns>A result holding either the assembled instructions as a byte array, or an error message if the
    /// assembling failed.</returns>
    public static Result<byte[], string> AssembleToBytes(this Assembler assembler, ulong baseAddress = 0,
        int bufferSize = 128)
    {
        using var memoryStream = new MemoryStream(bufferSize);
        var writer = new StreamCodeWriter(memoryStream);
        if (!assembler.TryAssemble(writer, baseAddress, out var error, out _))
            return error;
        return memoryStream.ToArray();
    }
}