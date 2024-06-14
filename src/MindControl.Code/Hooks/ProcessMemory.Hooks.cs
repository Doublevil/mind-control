using Iced.Intel;
using MindControl.Code;
using MindControl.Results;

namespace MindControl.Hooks;

/// <summary>
/// Provides extension methods for <see cref="ProcessMemory"/> related to code hooks.
/// </summary>
public static class ProcessMemoryHookExtensions
{
    /// <summary>
    /// Byte count for a near jump instruction.
    /// </summary>
    private const int NearJumpInstructionLength = 5;
    
    /// <summary>
    /// Byte count for a far jump instruction.
    /// 64-bit far jumps can take up to 15 bytes to encode, because the full address to jump to must be written in the
    /// code, and aligned properly.
    /// </summary>
    private const int FarJumpInstructionLength = 15;
    
    /// <summary>Maximum byte count for a single instruction.</summary>
    private const int MaxInstructionLength = 15;
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the given executable address is
    /// reached.
    /// Execution of the original code will continue normally (unless the given hook code is designed otherwise).
    /// With default options, some code will be generated to save and restore state, so that the hook code is isolated
    /// from the original code.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the instruction to hook into.</param>
    /// <param name="injectedCode">Assembled code to inject into the target process. Warning: if your code contains
    /// instructions with relative operands (like most jumps or calls), they will break. Use the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature to make sure they still point to the
    /// right address.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory, UIntPtr targetInstructionAddress,
        byte[] injectedCode, HookOptions options)
    {
        ulong sizeToReserve = (ulong)(options.GetExpectedGeneratedCodeSize(processMemory.Is64Bits)
            + injectedCode.Length
            + MaxInstructionLength // Extra room for the original instructions
            + FarJumpInstructionLength); // Extra room for the jump back to the original code

        // Reserve memory for the injected code. Try to reserve memory close enough to the target instruction to use a
        // near jump, if possible.
        var reservationResult = ReserveNearHookTarget(processMemory, sizeToReserve, targetInstructionAddress);
        if (reservationResult.IsFailure && options.JumpMode == HookJumpMode.NearJumpOnly)
            return new HookFailureOnAllocationFailure(reservationResult.Error);
        
        if (reservationResult.IsFailure)
        {
            // If we cannot reserve memory for a near jump, try going for a far jump
            reservationResult = processMemory.Reserve(sizeToReserve, true);
            if (reservationResult.IsFailure)
                return new HookFailureOnAllocationFailure(reservationResult.Error);
        }
        
        // Assemble the jump to the injected code
        var reservation = reservationResult.Value;
        var jmpAssembler = new Assembler(processMemory.Is64Bits ? 64 : 32);
        jmpAssembler.jmp(reservation.Address);
        var jmpAssembleResult = jmpAssembler.AssembleToBytes(targetInstructionAddress);
        if (jmpAssembleResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.JumpToInjectedCode, jmpAssembleResult.Error);
        }

        byte[] jmpBytes = jmpAssembleResult.Value;
        
        // Read the original instructions to replace, until we have enough bytes to fit the jump to the injected code
        using var stream = processMemory.GetMemoryStream(targetInstructionAddress);
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(processMemory.Is64Bits ? 64 : 32, codeReader, targetInstructionAddress);
        var instructionsToReplace = new List<Instruction>();
        int bytesRead = 0;
        while (bytesRead < jmpBytes.Length)
        {
            var instruction = decoder.Decode();
            if (instruction.IsInvalid)
            {
                reservation.Dispose();
                return new HookFailureOnDecodingFailure(decoder.LastError);
            }

            instructionsToReplace.Add(instruction);
            bytesRead += instruction.Length;
        }
        
        // Read the original bytes to replace (so that the hook can be reverted)
        var originalBytesResult = processMemory.ReadBytes(targetInstructionAddress, bytesRead);
        if (originalBytesResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnReadFailure(originalBytesResult.Error);
        }

        // Pad the jump bytes with NOPs if needed (example: we inject a 5-byte jump, but we read 2 instructions of 3
        // bytes each at the target address, meaning we need to add one NOP instruction so that the jump instruction is
        // now as long as the instructions it replaces).
        if (bytesRead > jmpBytes.Length)
            jmpBytes = jmpBytes.Concat(Enumerable.Repeat(ProcessMemoryCodeExtensions.NopByte,
                bytesRead - jmpBytes.Length)).ToArray();
        
        var nextOriginalInstructionAddress = (UIntPtr)(targetInstructionAddress + (ulong)bytesRead);
        
        // Assemble the pre-code
        var preHookCodeResult = BuildPreHookCode(reservation.Address, instructionsToReplace, processMemory.Is64Bits,
            options);
        if (preHookCodeResult.IsFailure)
        {
            reservation.Dispose();
            return preHookCodeResult.Error;
        }
        byte[] preHookCodeBytes = preHookCodeResult.Value;

        // Assemble the post-code
        var postHookCodeAddress = (UIntPtr)(reservation.Address + (ulong)preHookCodeBytes.Length
            + (ulong)injectedCode.Length);
        var postHookCodeResult = BuildPostHookCode(postHookCodeAddress, instructionsToReplace, processMemory.Is64Bits,
            options, nextOriginalInstructionAddress);
        if (postHookCodeResult.IsFailure)
        {
            reservation.Dispose();
            return postHookCodeResult.Error;
        }
        byte[] postHookCodeBytes = postHookCodeResult.Value;
        
        // Assemble the full code to inject
        var fullCodeLength = preHookCodeBytes.Length + injectedCode.Length + postHookCodeBytes.Length;
        if ((ulong)fullCodeLength > reservation.Size)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.Unkown,
                $"The assembled code is too large to fit in the reserved memory (reserved {reservation.Size} bytes, but assembled a total of {fullCodeLength} bytes). Please report this, as it is a bug in the hook code generation.");
        }
        var fullInjectedCode = new byte[fullCodeLength];
        Buffer.BlockCopy(preHookCodeBytes, 0, fullInjectedCode, 0, preHookCodeBytes.Length);
        Buffer.BlockCopy(injectedCode, 0, fullInjectedCode, preHookCodeBytes.Length, injectedCode.Length);
        Buffer.BlockCopy(postHookCodeBytes, 0, fullInjectedCode, preHookCodeBytes.Length + injectedCode.Length,
            postHookCodeBytes.Length);
        
        // Write the assembled code to the reserved memory
        var writeResult = processMemory.WriteBytes(reservation.Address, fullInjectedCode);
        if (writeResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnWriteFailure(writeResult.Error);
        }

        // Write the jump to the injected code
        var jmpWriteResult = processMemory.WriteBytes(targetInstructionAddress, jmpBytes);
        if (jmpWriteResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnWriteFailure(jmpWriteResult.Error);
        }

        return new CodeHook(processMemory, targetInstructionAddress, originalBytesResult.Value, reservation);
    }

    /// <summary>
    /// Assembles the code that comes before the user-made injected code in a hook operation.
    /// </summary>
    /// <param name="baseAddress">Address where the pre-hook code is going to be written.</param>
    /// <param name="instructionsToReplace">Original instructions to be replaced by a jump to the injected code.</param>
    /// <param name="is64Bits">Boolean indicating if the target process is 64-bits.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <returns>A result holding either the assembled code bytes, or a hook failure.</returns>
    private static Result<byte[], HookFailure> BuildPreHookCode(ulong baseAddress,
        IList<Instruction> instructionsToReplace, bool is64Bits, HookOptions options)
    {
        var assembler = new Assembler(is64Bits ? 64 : 32);

        // If the hook mode specifies that the original instruction should be executed first, add it to the code
        if (options.ExecutionMode == HookExecutionMode.ExecuteOriginalInstructionFirst
            && instructionsToReplace.Count > 0)
        {
            assembler.AddInstruction(instructionsToReplace.First());
        }
        
        // Save flags if needed
        if (options.IsolationMode.HasFlag(HookIsolationMode.PreserveFlags))
            assembler.SaveFlags();
        
        // Save registers if needed
        foreach (var register in options.RegistersToPreserve)
        {
            // Skip the register if it's not supported or not compatible with the target architecture
            if (!register.IsIndividualPreservationSupported(is64Bits))
                continue;
            
            assembler.SaveRegister(register);
        }
        
        // Save the FPU stack if needed
        if (options.IsolationMode.HasFlag(HookIsolationMode.PreserveFpuStack))
            assembler.SaveFpuStack();
        
        // Assemble the code and return the resulting bytes
        var result = assembler.AssembleToBytes(baseAddress, 128);
        if (result.IsFailure)
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.PrependedCode, result.Error);
        
        return result.Value;
    }
    
    /// <summary>
    /// Assembles the code that comes after the user-made injected code in a hook operation.
    /// </summary>
    /// <param name="baseAddress">Address where the post-hook code is going to be written.</param>
    /// <param name="instructionsToReplace">Original instructions to be replaced by a jump to the injected code.</param>
    /// <param name="is64Bits">Boolean indicating if the target process is 64-bits.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <param name="originalCodeJumpTarget">Address of the first byte of the instruction to jump back to.</param>
    /// <returns>A result holding either the assembled code bytes, or a hook failure.</returns>
    private static Result<byte[], HookFailure> BuildPostHookCode(ulong baseAddress,
        IList<Instruction> instructionsToReplace, bool is64Bits, HookOptions options, UIntPtr originalCodeJumpTarget)
    {
        var assembler = new Assembler(is64Bits ? 64 : 32);
        
        // Restore the FPU stack if needed
        if (options.IsolationMode.HasFlag(HookIsolationMode.PreserveFpuStack))
            assembler.RestoreFpuStack();
        
        // Restore registers if needed
        foreach (var register in options.RegistersToPreserve.Reverse())
        {
            // Skip the register if it's not supported or not compatible with the target architecture
            if (!register.IsIndividualPreservationSupported(is64Bits))
                continue;
            
            assembler.RestoreRegister(register);
        }
        
        // Restore flags if needed
        if (options.IsolationMode.HasFlag(HookIsolationMode.PreserveFlags))
            assembler.RestoreFlags();
        
        // If the hook mode specifies that the hook code should be executed first, append the original instructions
        if (options.ExecutionMode == HookExecutionMode.ExecuteInjectedCodeFirst
            && instructionsToReplace.Count > 0)
        {
            assembler.AddInstruction(instructionsToReplace.First());
        }
        
        // In all cases, add any additional instructions that were replaced by the jump to the injected code
        foreach (var instruction in instructionsToReplace.Skip(1))
            assembler.AddInstruction(instruction);
        
        // Jump back to the original code
        assembler.jmp(originalCodeJumpTarget);
        
        // Assemble the code and return the resulting bytes
        var result = assembler.AssembleToBytes(baseAddress, 128);
        if (result.IsFailure)
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.AppendedCode, result.Error);

        return result.Value;
    }
    
    /// <summary>
    /// Attempts to reserve executable memory close to the hook target instruction.
    /// </summary>
    /// <param name="processMemory">Target process memory instance.</param>
    /// <param name="sizeToReserve">Size of the memory to reserve.</param>
    /// <param name="jumpAddress">Address of the jump instruction that will jump to the injected code.</param>
    /// <returns>A result holding either the memory reservation, or an allocation failure.</returns>
    private static Result<MemoryReservation, AllocationFailure> ReserveNearHookTarget(ProcessMemory processMemory,
        ulong sizeToReserve, UIntPtr jumpAddress)
    {
        // The range of a near jump is limited by the signed byte displacement that follows the opcode.
        // The displacement is a signed 4-byte integer, so the range is from -2GB to +2GB.
        
        // In 32-bits processes, near jumps can be made to any address, despite the offset being a signed integer.
        // For example, a jump from 0x00000000 to 0xFFFFFFFF can be made with a near jump with an offset of -1.
        // Which means that for 32-bit processes, we can reserve memory anywhere in the address space.
        if (!processMemory.Is64Bits)
            return processMemory.Reserve(sizeToReserve, true, nearAddress: jumpAddress);
        
        // In 64-bits processes, however, the offset is still a signed 4-byte integer, but the full range is much
        // larger, meaning we can't reach any address.
        
        // For practical purposes, we disregard wrap-around jumps for 64-bits processes (cases where the address plus
        // the offset is outside the 64-bits address space), because they mostly make sense for 32-bits processes and
        // would complicate things unnecessarily.
        
        // The displacement is relative to the address of the next instruction. A near jump instruction is 5 bytes long.
        var nextInstructionAddress = jumpAddress + 5;
        
        // The aim of this method is to reserve memory close enough to perform a near jump from the target instruction.
        // However, to potentially avoid having to do far jumps from the injected code, we do a first attempt to reserve
        // memory that is extra close to the target instruction, if possible.
        var extraCloseRange = nextInstructionAddress.GetRangeAround(int.MaxValue);
        var extraCloseReservation = processMemory.Reserve(sizeToReserve, true, extraCloseRange, nextInstructionAddress);
        if (extraCloseReservation.IsSuccess)
            return extraCloseReservation;
        
        // If the extra close reservation failed, use the full range of a near jump
        var nearJumpRange = nextInstructionAddress.GetRangeAround(uint.MaxValue);
        return processMemory.Reserve(sizeToReserve, true, nearJumpRange, nextInstructionAddress);
    }
}