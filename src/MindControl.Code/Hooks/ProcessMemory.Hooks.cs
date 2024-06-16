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

    #region Public hook methods
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the executable address pointed by
    /// the given pointer path is reached. Depending on the options, the injected code may replace the original target
    /// instruction, or get executed either before or after it. If specified, additional instrutions that save and
    /// restore registers will be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the right addresses. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the instruction to hook into.
    /// </param>
    /// <param name="code">Assembled code to inject into the target process. The jump back to the original code will be
    /// appended automatically, so it is not necessary to include it. Warning: if your code contains instructions with
    /// relative operands (like jumps or calls), they may not point to the right addresses. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,PointerPath,Assembler,HookOptions)"/> signature.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, byte[] code, HookOptions options)
    {
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.Hook(addressResult.Value, code, options);
    }
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the given executable address is
    /// reached. Depending on the options, the injected code may replace the original target instruction, or get
    /// executed either before or after it. If specified, additional instrutions that save and restore registers will be
    /// added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the right addresses. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the instruction to hook into.</param>
    /// <param name="code">Assembled code to inject into the target process. The jump back to the original code will be
    /// appended automatically, so it is not necessary to include it. Warning: if your code contains instructions with
    /// relative operands (like jumps or calls), they may not point to the right addresses. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory, UIntPtr targetInstructionAddress,
        byte[] code, HookOptions options)
    {
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (code.Length == 0)
            return new HookFailureOnInvalidArguments("The code to inject must contain at least one byte.");
        
        ulong sizeToReserve = (ulong)(options.GetExpectedGeneratedCodeSize(processMemory.Is64Bit)
            + code.Length
            + MaxInstructionLength // Extra room for the original instructions
            + FarJumpInstructionLength); // Extra room for the jump back to the original code

        // Reserve memory for the injected code as close as possible to the target instruction
        var reservationResult = ReserveHookTarget(processMemory, sizeToReserve, targetInstructionAddress,
            options.JumpMode);
        if (reservationResult.IsFailure)
            return reservationResult.Error;
        var reservation = reservationResult.Value;
        
        return PerformHook(processMemory, targetInstructionAddress, code, reservation, options);
    }

    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the executable address pointed by
    /// the given pointer path is reached. Depending on the options, the injected code may replace the original target
    /// instruction, or get executed either before or after it. If specified, additional instrutions that save and
    /// restore registers will be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the right locations.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the instruction to hook into.
    /// </param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process. The jump
    /// back to the original code will be appended automatically, so it is not necessary to include it.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, Assembler codeAssembler, HookOptions options)
    {
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.Hook(addressResult.Value, codeAssembler, options);
    }
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the given executable address is
    /// reached. Depending on the options, the injected code may replace the original target instruction, or get
    /// executed either before or after it. If specified, additional instrutions that save and restore registers will be
    /// added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the right locations.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the instruction to hook into.</param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process. The jump
    /// back to the original code will be appended automatically, so it is not necessary to include it.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory, UIntPtr targetInstructionAddress,
        Assembler codeAssembler, HookOptions options)
    {
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (codeAssembler.Instructions.Count == 0)
            return new HookFailureOnInvalidArguments("The given code assembler must have at least one instruction.");
        
        // The problem with using an assembler is that we don't know how many bytes the assembled code will take, so we
        // have to use the most conservative estimate possible, which is the maximum length of an instruction multiplied
        // by the number of instructions in the assembler.
        int codeMaxLength = MaxInstructionLength * codeAssembler.Instructions.Count;
        ulong sizeToReserve = (ulong)(options.GetExpectedGeneratedCodeSize(processMemory.Is64Bit)
                                      + codeMaxLength
                                      + MaxInstructionLength // Extra room for the original instructions
                                      + FarJumpInstructionLength); // Extra room for the jump back to the original code

        // Reserve memory for the injected code, as close as possible to the target instruction
        var reservationResult = ReserveHookTarget(processMemory, sizeToReserve, targetInstructionAddress,
            options.JumpMode);
        if (reservationResult.IsFailure)
            return reservationResult.Error;
        var reservation = reservationResult.Value;
        
        // Assemble the code to inject
        var assemblingResult = codeAssembler.AssembleToBytes(reservation.Address, 128);
        if (assemblingResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.InjectedCode, assemblingResult.Error);
        }
        byte[] codeBytes = assemblingResult.Value;
        
        // Resize the reservation now that we have more accurate information
        int difference = codeMaxLength - codeBytes.Length;
        if (difference > 0)
            reservation.Shrink((ulong)difference);
        
        return PerformHook(processMemory, targetInstructionAddress, codeBytes, reservation, options);
    }

    #endregion
    
    #region Internal hook methods
    
    /// <summary>
    /// Method called internally by the hook methods to assemble the full code to inject, write it in the given
    /// reserved memory, and write the jump to the injected code at the target instruction address.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the instruction to hook into.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="reservation">Memory reservation where the injected code will be written.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <returns>A result holding either a code hook instance, or a hook failure when the operation failed.</returns>
    private static Result<CodeHook, HookFailure> PerformHook(ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, byte[] code, MemoryReservation reservation, HookOptions options)
    {
        // Assemble the jump to the injected code
        var jmpAssembler = new Assembler(processMemory.Is64Bit ? 64 : 32);
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
        var decoder = Decoder.Create(processMemory.Is64Bit ? 64 : 32, codeReader, targetInstructionAddress);
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
        var preHookCodeResult = BuildPreHookCode(reservation.Address, instructionsToReplace, processMemory.Is64Bit,
            options);
        if (preHookCodeResult.IsFailure)
        {
            reservation.Dispose();
            return preHookCodeResult.Error;
        }
        byte[] preHookCodeBytes = preHookCodeResult.Value;

        // Assemble the post-code
        var postHookCodeAddress = (UIntPtr)(reservation.Address + (ulong)preHookCodeBytes.Length
            + (ulong)code.Length);
        var postHookCodeResult = BuildPostHookCode(postHookCodeAddress, instructionsToReplace, processMemory.Is64Bit,
            options, nextOriginalInstructionAddress);
        if (postHookCodeResult.IsFailure)
        {
            reservation.Dispose();
            return postHookCodeResult.Error;
        }
        byte[] postHookCodeBytes = postHookCodeResult.Value;
        
        // Assemble the full code to inject
        var fullCodeLength = preHookCodeBytes.Length + code.Length + postHookCodeBytes.Length;
        if ((ulong)fullCodeLength > reservation.Size)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.Unkown,
                $"The assembled code is too large to fit in the reserved memory (reserved {reservation.Size} bytes, but assembled a total of {fullCodeLength} bytes). Please report this, as it is a bug in the hook code generation.");
        }
        var fullInjectedCode = new byte[fullCodeLength];
        Buffer.BlockCopy(preHookCodeBytes, 0, fullInjectedCode, 0, preHookCodeBytes.Length);
        Buffer.BlockCopy(code, 0, fullInjectedCode, preHookCodeBytes.Length, code.Length);
        Buffer.BlockCopy(postHookCodeBytes, 0, fullInjectedCode, preHookCodeBytes.Length + code.Length,
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
    /// <param name="is64Bit">Boolean indicating if the target process is 64-bit.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <returns>A result holding either the assembled code bytes, or a hook failure.</returns>
    private static Result<byte[], HookFailure> BuildPreHookCode(ulong baseAddress,
        IList<Instruction> instructionsToReplace, bool is64Bit, HookOptions options)
    {
        var assembler = new Assembler(is64Bit ? 64 : 32);

        // If the hook mode specifies that the original instruction should be executed first, add it to the code
        if (options.ExecutionMode == HookExecutionMode.ExecuteOriginalInstructionFirst
            && instructionsToReplace.Count > 0)
        {
            assembler.AddInstruction(instructionsToReplace.First());
        }
        
        // Save flags if needed
        if (options.RegistersToPreserve.Contains(HookRegister.Flags))
            assembler.SaveFlags();
        
        // Save registers if needed
        foreach (var hookRegister in options.RegistersToPreserve)
        {
            var register = hookRegister.ToRegister(is64Bit);
            
            // Skip the register if it's not supported or not compatible with the target architecture
            if (register == null)
                continue;
            
            assembler.SaveRegister(register.Value);
        }
        
        // Save the FPU stack if needed
        if (options.RegistersToPreserve.Contains(HookRegister.FpuStack))
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
    /// <param name="is64Bit">Boolean indicating if the target process is 64-bit.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <param name="originalCodeJumpTarget">Address of the first byte of the instruction to jump back to.</param>
    /// <returns>A result holding either the assembled code bytes, or a hook failure.</returns>
    private static Result<byte[], HookFailure> BuildPostHookCode(ulong baseAddress,
        IList<Instruction> instructionsToReplace, bool is64Bit, HookOptions options, UIntPtr originalCodeJumpTarget)
    {
        var assembler = new Assembler(is64Bit ? 64 : 32);
        
        // Restore the FPU stack if needed
        if (options.RegistersToPreserve.Contains(HookRegister.FpuStack))
            assembler.RestoreFpuStack();
        
        // Restore registers if needed
        foreach (var hookRegister in options.RegistersToPreserve.Reverse())
        {
            var register = hookRegister.ToRegister(is64Bit);
            
            // Skip the register if it's not supported or not compatible with the target architecture
            if (register == null)
                continue;
            
            assembler.RestoreRegister(register.Value);
        }
        
        // Restore flags if needed
        if (options.RegistersToPreserve.Contains(HookRegister.Flags))
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
    /// Attempts to reserve executable memory as close as possible to the hook target instruction.
    /// </summary>
    /// <param name="processMemory">Target process memory instance.</param>
    /// <param name="sizeToReserve">Size of the memory to reserve.</param>
    /// <param name="jumpAddress">Address of the jump instruction that will jump to the injected code.</param>
    /// <param name="jumpMode">Jump mode of the hook.</param>
    /// <returns>A result holding either the memory reservation, or an allocation failure.</returns>
    private static Result<MemoryReservation, HookFailure> ReserveHookTarget(ProcessMemory processMemory,
        ulong sizeToReserve, UIntPtr jumpAddress, HookJumpMode jumpMode)
    {
        // The range of a near jump is limited by the signed byte displacement that follows the opcode.
        // The displacement is a signed 4-byte integer, so the range is from -2GB to +2GB.
        
        // In 32-bit processes, near jumps can be made to any address, despite the offset being a signed integer.
        // For example, a jump from 0x00000000 to 0xFFFFFFFF can be made with a near jump with an offset of -1.
        // Which means that for 32-bit processes, we can reserve memory anywhere in the address space.
        if (!processMemory.Is64Bit)
        {
            var reservationResult = processMemory.Reserve(sizeToReserve, true, nearAddress: jumpAddress);
            if (reservationResult.IsFailure)
                return new HookFailureOnAllocationFailure(reservationResult.Error);
            return reservationResult.Value;
        }

        // In 64-bit processes, however, the offset is still a signed 4-byte integer, but the full range is much
        // larger, meaning we can't reach any address.
        
        // For practical purposes, we disregard wrap-around jumps for 64-bit processes (cases where the address plus
        // the offset is outside the 64-bit address space), because they mostly make sense for 32-bit processes and
        // would complicate things unnecessarily.
        
        // The displacement is relative to the address of the next instruction. A near jump instruction is 5 bytes long.
        var nextInstructionAddress = jumpAddress + 5;
        
        // The aim of this method is to reserve memory close enough to perform a near jump from the target instruction.
        // However, to potentially avoid having to do far jumps from the injected code, we do a first attempt to reserve
        // memory that is extra close to the target instruction, if possible.
        var extraCloseRange = nextInstructionAddress.GetRangeAround(int.MaxValue);
        var extraCloseReservation = processMemory.Reserve(sizeToReserve, true, extraCloseRange, nextInstructionAddress);
        if (extraCloseReservation.IsSuccess)
            return extraCloseReservation.Value;
        
        // If the extra close reservation failed, use the full range of a near jump
        var nearJumpRange = nextInstructionAddress.GetRangeAround(uint.MaxValue);
        var nearJumpResult = processMemory.Reserve(sizeToReserve, true, nearJumpRange, nextInstructionAddress);
        if (nearJumpResult.IsSuccess)
            return nearJumpResult.Value;
        
        // If the jump mode is set to near jump only, we return the failure, as we would not be able to use a near jump.
        if (jumpMode == HookJumpMode.NearJumpOnly)
            return new HookFailureOnAllocationFailure(nearJumpResult.Error);
        
        // Otherwise, we try to reserve memory anywhere in the address space
        var fullRangeResult = processMemory.Reserve(sizeToReserve, true, nearAddress: jumpAddress);
        if (fullRangeResult.IsFailure)
            return new HookFailureOnAllocationFailure(fullRangeResult.Error);
        return fullRangeResult.Value;
    }
    
    #endregion
}