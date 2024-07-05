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

    #region Public hook methods
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the executable address pointed by
    /// the given pointer path is reached. Depending on the options, the injected code may replace the original target
    /// instruction, or get executed either before or after it. If specified, additional instructions that save and
    /// restore registers will be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the instruction to hook into.
    /// </param>
    /// <param name="code">Assembled code to inject into the target process. The jump back to the original code will be
    /// appended automatically, so it is not necessary to include it. Warning: if your code contains instructions with
    /// relative operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,PointerPath,Assembler,HookOptions)"/> signature.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, byte[] code, HookOptions options)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.Hook(addressResult.Value, code, options);
    }
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the given executable address is
    /// reached. Depending on the options, the injected code may replace the original target instruction, or get
    /// executed either before or after it. If specified, additional instructions that save and restore registers will
    /// be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the instruction to hook into.</param>
    /// <param name="code">Assembled code to inject into the target process. The jump back to the original code will be
    /// appended automatically, so it is not necessary to include it. Warning: if your code contains instructions with
    /// relative operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> signature.</param>
    /// <param name="options">Options defining how the hook works.</param>
    /// <returns>A result holding either a code hook instance that contains a reference to the injected code reservation
    /// and allows you to revert the hook, or a hook failure when the operation failed.</returns>
    public static Result<CodeHook, HookFailure> Hook(this ProcessMemory processMemory, UIntPtr targetInstructionAddress,
        byte[] code, HookOptions options)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (!processMemory.Is64Bit && targetInstructionAddress.ToUInt64() > uint.MaxValue)
            return new HookFailureOnIncompatibleBitness(targetInstructionAddress);
        if (code.Length == 0)
            return new HookFailureOnInvalidArguments("The code to inject must contain at least one byte.");

        // Reserve memory for the injected code as close as possible to the target instruction
        ulong sizeToReserve = GetSizeToReserveForCodeInjection(processMemory, code.Length, options);
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
    /// instruction, or get executed either before or after it. If specified, additional instructions that save and
    /// restore registers will be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
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
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.Hook(addressResult.Value, codeAssembler, options);
    }
    
    /// <summary>
    /// Injects code into the target process to be executed when the instruction at the given executable address is
    /// reached. Depending on the options, the injected code may replace the original target instruction, or get
    /// executed either before or after it. If specified, additional instructions that save and restore registers will
    /// be added to the injected code.
    /// Execution of the original code will then continue normally (unless the provided code is designed otherwise).
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
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
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (!processMemory.Is64Bit && targetInstructionAddress.ToUInt64() > uint.MaxValue)
            return new HookFailureOnIncompatibleBitness(targetInstructionAddress);
        if (codeAssembler.Instructions.Count == 0)
            return new HookFailureOnInvalidArguments("The given code assembler must have at least one instruction.");
        
        // The problem with using an assembler is that we don't know how many bytes the assembled code will take, so we
        // have to use the most conservative estimate possible, which is the maximum length of an instruction multiplied
        // by the number of instructions in the assembler.
        int codeMaxLength = ProcessMemoryCodeExtensions.MaxInstructionLength * codeAssembler.Instructions.Count;
        ulong sizeToReserve = GetSizeToReserveForCodeInjection(processMemory, codeMaxLength, options);

        // Reserve memory for the injected code, as close as possible to the target instruction
        var reservationResult = ReserveHookTarget(processMemory, sizeToReserve, targetInstructionAddress,
            options.JumpMode);
        if (reservationResult.IsFailure)
            return reservationResult.Error;
        var reservation = reservationResult.Value;
        
        // Assemble the code to inject
        UIntPtr injectedCodeStartAddress = reservation.Address
            + (UIntPtr)options.GetExpectedPreCodeSize(processMemory.Is64Bit);
        var assemblingResult = codeAssembler.AssembleToBytes(injectedCodeStartAddress, 128);
        if (assemblingResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.InjectedCode, assemblingResult.Error);
        }
        byte[] codeBytes = assemblingResult.Value;
        
        // Resize the reservation now that we know the exact code size
        int difference = codeMaxLength - codeBytes.Length;
        if (difference > 0)
            reservation.Shrink((ulong)difference);
        
        return PerformHook(processMemory, targetInstructionAddress, codeBytes, reservation, options);
    }

    /// <summary>
    /// Injects code in the process to be executed right before the instruction pointed by the given pointer path, by
    /// performing a hook.
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/> signature.
    /// </summary>
    /// <remarks>
    /// This method is essentially a shortcut for <see cref="Hook(ProcessMemory,PointerPath,byte[],HookOptions)"/> with
    /// the execution mode set to <see cref="HookExecutionMode.ExecuteInjectedCodeFirst"/>. It is provided for
    /// convenience, readability, and discoverability for users who might not be familiar with hooks but are looking to
    /// achieve the same result.
    /// </remarks>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the target instruction. The
    /// injected code will be executed just before the instruction.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code hook instance that contains the memory reservation holding the
    /// injected code and also allows you to revert the operation, or a hook failure when the operation failed.
    /// </returns>
    public static Result<CodeHook, HookFailure> InsertCodeAt(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, byte[] code, params HookRegister[] registersToPreserve)
        => processMemory.Hook(targetInstructionPointerPath, code,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, registersToPreserve));

    /// <summary>
    /// Injects code in the process to be executed right before the instruction at the given address, by performing a
    /// hook.
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/> signature.
    /// </summary>
    /// <remarks>
    /// This method is essentially a shortcut for <see cref="Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> with the
    /// execution mode set to <see cref="HookExecutionMode.ExecuteInjectedCodeFirst"/>. It is provided for convenience,
    /// readability, and discoverability for users who might not be familiar with hooks but are looking to achieve the
    /// same result.
    /// </remarks>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the target instruction. The injected code
    /// will be executed just before the instruction.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code hook instance that contains the memory reservation holding the
    /// injected code and also allows you to revert the operation, or a hook failure when the operation failed.
    /// </returns>
    public static Result<CodeHook, HookFailure> InsertCodeAt(this ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, byte[] code, params HookRegister[] registersToPreserve)
        => processMemory.Hook(targetInstructionAddress, code,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, registersToPreserve));
    
    /// <summary>
    /// Injects code in the process to be executed right before the instruction pointed by the given pointer path, by
    /// performing a hook.
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
    /// </summary>
    /// <remarks>
    /// This method is essentially a shortcut for <see cref="Hook(ProcessMemory,PointerPath,Assembler,HookOptions)"/>
    /// with the execution mode set to <see cref="HookExecutionMode.ExecuteInjectedCodeFirst"/>. It is provided for
    /// convenience, readability, and discoverability for users who might not be familiar with hooks but are looking to
    /// achieve the same result.
    /// </remarks>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the target instruction. The
    /// injected code will be executed just before the instruction.</param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code hook instance that contains the memory reservation holding the
    /// injected code and also allows you to revert the operation, or a hook failure when the operation failed.
    /// </returns>
    public static Result<CodeHook, HookFailure> InsertCodeAt(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, Assembler codeAssembler, params HookRegister[] registersToPreserve)
        => processMemory.Hook(targetInstructionPointerPath, codeAssembler,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, registersToPreserve));
    
    /// <summary>
    /// Injects code in the process to be executed right before the instruction at the given address, by performing a
    /// hook.
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
    /// </summary>
    /// <remarks>
    /// This method is essentially a shortcut for <see cref="Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> with
    /// the execution mode set to <see cref="HookExecutionMode.ExecuteInjectedCodeFirst"/>. It is provided for
    /// convenience, readability, and discoverability for users who might not be familiar with hooks but are looking to
    /// achieve the same result.
    /// </remarks>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the target instruction. The injected code
    /// will be executed just before the instruction.</param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code hook instance that contains the memory reservation holding the
    /// injected code and also allows you to revert the operation, or a hook failure when the operation failed.
    /// </returns>
    public static Result<CodeHook, HookFailure> InsertCodeAt(this ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, Assembler codeAssembler, params HookRegister[] registersToPreserve)
        => processMemory.Hook(targetInstructionAddress, codeAssembler,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, registersToPreserve));

    /// <summary>
    /// Replaces the instruction or instructions at the address pointed by the given path with the provided code. If the
    /// injected code does not fit in the space occupied by the original instructions, a hook will be performed so that
    /// the injected code can still be executed instead of the original instructions.
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="ReplaceCodeAt(ProcessMemory,PointerPath,int,Assembler,HookRegister[])"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the first instruction to replace.
    /// </param>
    /// <param name="instructionCount">Number of consecutive instructions to replace.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code change instance that allows you to revert the operation, or a hook
    /// failure when the operation failed. If the operation performed a hook, the result will be a
    /// <see cref="CodeHook"/> that also contains the reservation holding the injected code.</returns>
    public static Result<CodeChange, HookFailure> ReplaceCodeAt(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, int instructionCount, byte[] code,
        params HookRegister[] registersToPreserve)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.ReplaceCodeAt(addressResult.Value, instructionCount, code, registersToPreserve);
    }
    
    /// <summary>
    /// Replaces the instruction or instructions at the given address with the provided code. If the injected code does
    /// not fit in the space occupied by the original instructions, a hook will be performed so that the injected code
    /// can still be executed instead of the original instructions.
    /// This signature uses a byte array containing the code to inject. If your code contains instructions with relative
    /// operands (like jumps or calls), they may not point to the intended address. In these cases, prefer the
    /// <see cref="ReplaceCodeAt(ProcessMemory,UIntPtr,int,Assembler,HookRegister[])"/> signature.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the first instruction to replace.</param>
    /// <param name="instructionCount">Number of consecutive instructions to replace.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code change instance that allows you to revert the operation, or a hook
    /// failure when the operation failed. If the operation performed a hook, the result will be a
    /// <see cref="CodeHook"/> that also contains the reservation holding the injected code.</returns>
    public static Result<CodeChange, HookFailure> ReplaceCodeAt(this ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, int instructionCount, byte[] code, params HookRegister[] registersToPreserve)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (!processMemory.Is64Bit && targetInstructionAddress.ToUInt64() > uint.MaxValue)
            return new HookFailureOnIncompatibleBitness(targetInstructionAddress);
        if (instructionCount < 1)
            return new HookFailureOnInvalidArguments("The number of instructions to replace must be at least 1.");
        if (code.Length == 0)
            return new HookFailureOnInvalidArguments("The code to inject must contain at least one byte.");
        
        var hookOptions = new HookOptions(HookExecutionMode.ReplaceOriginalInstruction, registersToPreserve);
        
        // Attempt to replace the code directly (write the injected code bytes directly on top of the original
        // instruction bytes if it can fit).
        var fullCodeAssemblyResult = AssembleFullCodeToInject(processMemory, code, targetInstructionAddress,
            hookOptions);
        if (fullCodeAssemblyResult.IsFailure)
            return fullCodeAssemblyResult.Error;
        var directReplaceResult = TryReplaceCodeBytes(processMemory, targetInstructionAddress, instructionCount,
            fullCodeAssemblyResult.Value);
        if (directReplaceResult.IsFailure)
            return directReplaceResult.Error;
        if (directReplaceResult.Value != null)
            return directReplaceResult.Value;
        
        // The code to inject is larger than the original instructions, so we need to perform a hook
        ulong sizeToReserve = GetSizeToReserveForCodeInjection(processMemory, code.Length, hookOptions);
        var reservationResult = ReserveHookTarget(processMemory, sizeToReserve, targetInstructionAddress,
            hookOptions.JumpMode);
        if (reservationResult.IsFailure)
            return reservationResult.Error;
        var reservation = reservationResult.Value;
        
        var hookResult = PerformHook(processMemory, targetInstructionAddress, code, reservation, hookOptions);
        if (hookResult.IsFailure)
            return hookResult.Error;
        return hookResult.Value;
    }
    
    /// <summary>
    /// Replaces the instruction or instructions pointed by the given pointer path with the provided code. If the
    /// injected code does not fit in the space occupied by the original instructions, a hook will be performed so that
    /// the injected code can still be executed instead of the original instructions.
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionPointerPath">Pointer path to the first byte of the first instruction to replace.
    /// </param>
    /// <param name="instructionCount">Number of consecutive instructions to replace.</param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code change instance that allows you to revert the operation, or a hook
    /// failure when the operation failed. If the operation performed a hook, the result will be a
    /// <see cref="CodeHook"/> that also contains the reservation holding the injected code.</returns>
    public static Result<CodeChange, HookFailure> ReplaceCodeAt(this ProcessMemory processMemory,
        PointerPath targetInstructionPointerPath, int instructionCount, Assembler codeAssembler,
        params HookRegister[] registersToPreserve)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        
        var addressResult = processMemory.EvaluateMemoryAddress(targetInstructionPointerPath);
        if (addressResult.IsFailure)
            return new HookFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.ReplaceCodeAt(addressResult.Value, instructionCount, codeAssembler, registersToPreserve);
    }
    
    /// <summary>
    /// Replaces the instruction or instructions at the given address with the provided code. If the injected code does
    /// not fit in the space occupied by the original instructions, a hook will be performed so that the injected code
    /// can still be executed instead of the original instructions.
    /// This signature uses an assembler, which is recommended, especially if your code contains instructions with
    /// relative operands (like jumps or calls), because the assembler will adjust addresses to guarantee they point to
    /// the intended locations.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the first instruction to replace.</param>
    /// <param name="instructionCount">Number of consecutive instructions to replace.</param>
    /// <param name="codeAssembler">Code assembler loaded with instructions to inject into the target process.</param>
    /// <param name="registersToPreserve">Optionally provided registers to be saved and restored around the injected
    /// code. This allows the injected code to modify registers without affecting the original code, which could
    /// otherwise lead to crashes or unexpected behavior.</param>
    /// <returns>A result holding either a code change instance that allows you to revert the operation, or a hook
    /// failure when the operation failed. If the operation performed a hook, the result will be a
    /// <see cref="CodeHook"/> that also contains the reservation holding the injected code.</returns>
    public static Result<CodeChange, HookFailure> ReplaceCodeAt(this ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, int instructionCount, Assembler codeAssembler,
        params HookRegister[] registersToPreserve)
    {
        if (!processMemory.IsAttached)
            return new HookFailureOnDetachedProcess();
        if (targetInstructionAddress == UIntPtr.Zero)
            return new HookFailureOnZeroPointer();
        if (!processMemory.Is64Bit && targetInstructionAddress.ToUInt64() > uint.MaxValue)
            return new HookFailureOnIncompatibleBitness(targetInstructionAddress);
        if (instructionCount < 1)
            return new HookFailureOnInvalidArguments("The number of instructions to replace must be at least 1.");
        if (codeAssembler.Instructions.Count == 0)
            return new HookFailureOnInvalidArguments("The given code assembler must have at least one instruction.");
        
        var hookOptions = new HookOptions(HookExecutionMode.ReplaceOriginalInstruction, registersToPreserve);
        int preCodeSize = hookOptions.GetExpectedPreCodeSize(processMemory.Is64Bit);
        
        // Attempt to replace the code directly (write the injected code bytes directly on top of the original
        // instruction bytes if it can fit).
        // For that, we need to assemble the code to inject at the target address.
        var targetAddressAssemblyResult = codeAssembler.AssembleToBytes(
            targetInstructionAddress + (UIntPtr)preCodeSize, 128);
        if (targetAddressAssemblyResult.IsFailure)
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.InjectedCode,
                targetAddressAssemblyResult.Error);
        var fullCodeAssemblyAtTargetAddressResult = AssembleFullCodeToInject(processMemory,
            targetAddressAssemblyResult.Value, targetInstructionAddress, hookOptions);
        if (fullCodeAssemblyAtTargetAddressResult.IsFailure)
            return fullCodeAssemblyAtTargetAddressResult.Error;
        byte[] codeAssembledAtTargetAddress = fullCodeAssemblyAtTargetAddressResult.Value;
        var directReplaceResult = TryReplaceCodeBytes(processMemory, targetInstructionAddress, instructionCount,
            codeAssembledAtTargetAddress);
        if (directReplaceResult.IsFailure)
            return directReplaceResult.Error;
        if (directReplaceResult.Value != null)
            return directReplaceResult.Value;
        
        // The code to inject is larger than the original instructions, so we need to perform a hook
        // Reserve memory
        int codeMaxLength = ProcessMemoryCodeExtensions.MaxInstructionLength * codeAssembler.Instructions.Count;
        ulong sizeToReserve = GetSizeToReserveForCodeInjection(processMemory, codeMaxLength, hookOptions);
        var reservationResult = ReserveHookTarget(processMemory, sizeToReserve, targetInstructionAddress,
            hookOptions.JumpMode);
        if (reservationResult.IsFailure)
            return reservationResult.Error;
        var reservation = reservationResult.Value;
        
        // Assemble the code to inject (even though we already assembled the code before, it was at a different address,
        // which could lead to different instruction bytes).
        var injectedCodeStartAddress = reservation.Address
            + (UIntPtr)hookOptions.GetExpectedPreCodeSize(processMemory.Is64Bit);
        var assemblyResult = codeAssembler.AssembleToBytes(injectedCodeStartAddress, 128);
        if (assemblyResult.IsFailure)
        {
            reservation.Dispose();
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.InjectedCode, assemblyResult.Error);
        }
        byte[] code = assemblyResult.Value;
        
        // Resize the reservation now that we know the exact code size
        int difference = codeMaxLength - code.Length;
        if (difference > 0)
            reservation.Shrink((ulong)difference);
        
        var hookResult = PerformHook(processMemory, targetInstructionAddress, code, reservation, hookOptions);
        if (hookResult.IsFailure)
        {
            reservation.Dispose();
            return hookResult.Error;
        }
        return hookResult.Value;
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
        
        // Assemble the full code to inject
        var nextOriginalInstructionAddress = (UIntPtr)(targetInstructionAddress + (ulong)bytesRead);
        var fullCodeResult = AssembleFullCodeToInject(processMemory, code, reservation.Address, options,
            instructionsToReplace, nextOriginalInstructionAddress);
        if (fullCodeResult.IsFailure)
        {
            reservation.Dispose();
            return fullCodeResult.Error;
        }

        byte[] fullCode = fullCodeResult.Value;
        if ((ulong)fullCode.Length > reservation.Size)
        {
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.Unknown,
                $"The assembled code is too large to fit in the reserved memory (reserved {reservation.Size} bytes, but assembled a total of {fullCode.Length} bytes). Please report this, as it is a bug in the hook code generation.");
        }
        
        // Write the assembled code to the reserved memory
        var writeResult = processMemory.WriteBytes(reservation.Address, fullCodeResult.Value);
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
    /// Assembles the full code to inject, including the pre-hook code, the injected code, and the post-hook code.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <param name="injectionAddress">Address where the injected code will be written.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <param name="instructionsToReplace">Original instructions that will be replaced by the injected code. These
    /// instructions are used to insert original instructions before or after the injected code depending on the hook
    /// options, if required at all. If null, no original instructions will be considered.</param>
    /// <param name="nextInstructionAddress">Address of the first byte of the instruction that comes after the original
    /// instructions to replace. This is used to generate the jump instruction in the post-hook code. If null, no jump
    /// will be assembled.</param>
    /// <returns></returns>
    private static Result<byte[], HookFailure> AssembleFullCodeToInject(ProcessMemory processMemory, byte[] code,
        UIntPtr injectionAddress, HookOptions options, IList<Instruction>? instructionsToReplace = null,
        UIntPtr? nextInstructionAddress = null)
    {
        // Assemble the pre-code
        var preHookCodeResult = BuildPreHookCode(injectionAddress, instructionsToReplace, processMemory.Is64Bit,
            options);
        if (preHookCodeResult.IsFailure)
            return preHookCodeResult.Error;
        byte[] preHookCodeBytes = preHookCodeResult.Value;

        // Assemble the post-code
        var postHookCodeAddress = (UIntPtr)(injectionAddress + (ulong)preHookCodeBytes.Length + (ulong)code.Length);
        var postHookCodeResult = BuildPostHookCode(postHookCodeAddress, instructionsToReplace, processMemory.Is64Bit,
            options, nextInstructionAddress);
        if (postHookCodeResult.IsFailure)
            return postHookCodeResult.Error;
        byte[] postHookCodeBytes = postHookCodeResult.Value;
        
        // Assemble the full code to inject
        var fullCodeLength = preHookCodeBytes.Length + code.Length + postHookCodeBytes.Length;
        var fullInjectedCode = new byte[fullCodeLength];
        Buffer.BlockCopy(preHookCodeBytes, 0, fullInjectedCode, 0, preHookCodeBytes.Length);
        Buffer.BlockCopy(code, 0, fullInjectedCode, preHookCodeBytes.Length, code.Length);
        Buffer.BlockCopy(postHookCodeBytes, 0, fullInjectedCode, preHookCodeBytes.Length + code.Length,
            postHookCodeBytes.Length);

        return fullInjectedCode;
    }

    /// <summary>
    /// Attempts to replace the bytes of the specified number of instructions, starting at the instruction at the given
    /// address, with the provided code. If the injected code is larger than the original instructions, the operation
    /// cannot be performed and the result will be successful but null, signaling that a hook should be performed
    /// instead.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="targetInstructionAddress">Address of the first byte of the first instruction to replace.</param>
    /// <param name="instructionCount">Number of consecutive instructions to replace.</param>
    /// <param name="code">Assembled code to inject into the target process.</param>
    /// <returns>A result that can hold either a code change instance, or a hook failure when the operation failed.
    /// If the operation cannot be performed, the result will be successful but the value will be null.</returns>
    private static Result<CodeChange?, HookFailure> TryReplaceCodeBytes(ProcessMemory processMemory,
        UIntPtr targetInstructionAddress, int instructionCount, byte[] code)
    {
        // Read the original instructions to replace to determine how many bytes it makes up.
        using var stream = processMemory.GetMemoryStream(targetInstructionAddress);
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(processMemory.Is64Bit ? 64 : 32, codeReader, targetInstructionAddress);
        var instructionsToReplace = new List<Instruction>();
        int bytesRead = 0;
        while (instructionsToReplace.Count < instructionCount)
        {
            var instruction = decoder.Decode();
            if (instruction.IsInvalid)
                return new HookFailureOnDecodingFailure(decoder.LastError);
            
            instructionsToReplace.Add(instruction);
            bytesRead += instruction.Length;
        }
        
        // If the instructions to replace are shorter than the injected code, we can't replace them directly
        if (bytesRead < code.Length)
            return (CodeChange?)null;
        
        // If we reach this point, we can replace the code bytes.
        // Read the original bytes to replace (so that the change can be reverted)
        var originalBytesResult = processMemory.ReadBytes(targetInstructionAddress, bytesRead);
        if (originalBytesResult.IsFailure)
            return new HookFailureOnReadFailure(originalBytesResult.Error);
        
        // Pad the injected code with NOPs if needed, to avoid leaving bytes from the original instructions in memory
        if (bytesRead > code.Length)
            code = code.Concat(Enumerable.Repeat(ProcessMemoryCodeExtensions.NopByte, bytesRead - code.Length))
                .ToArray();
        
        // Write the injected code directly
        var writeResult = processMemory.WriteBytes(targetInstructionAddress, code);
        if (writeResult.IsFailure)
            return new HookFailureOnWriteFailure(writeResult.Error);
        
        return new CodeChange(processMemory, targetInstructionAddress, originalBytesResult.Value);
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
        IList<Instruction>? instructionsToReplace, bool is64Bit, HookOptions options)
    {
        var assembler = new Assembler(is64Bit ? 64 : 32);

        // If the hook mode specifies that the original instruction should be executed first, add it to the code
        if (options.ExecutionMode == HookExecutionMode.ExecuteOriginalInstructionFirst
            && instructionsToReplace?.Count > 0)
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
    /// <param name="originalCodeJumpTarget">Address of the first byte of the instruction to jump back to. If null,
    /// the jump back will not be assembled.</param>
    /// <returns>A result holding either the assembled code bytes, or a hook failure.</returns>
    private static Result<byte[], HookFailure> BuildPostHookCode(ulong baseAddress,
        IList<Instruction>? instructionsToReplace, bool is64Bit, HookOptions options, UIntPtr? originalCodeJumpTarget)
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
        
        // If the hook mode specifies that the injected code should be executed first, append the original instruction
        if (options.ExecutionMode == HookExecutionMode.ExecuteInjectedCodeFirst
            && instructionsToReplace?.Count > 0)
        {
            assembler.AddInstruction(instructionsToReplace.First());
        }
        
        // In all cases, add any additional instructions that were replaced by the jump to the injected code
        foreach (var instruction in instructionsToReplace?.Skip(1) ?? [])
            assembler.AddInstruction(instruction);
        
        // Jump back to the original code
        if (originalCodeJumpTarget.HasValue)
            assembler.jmp(originalCodeJumpTarget.Value);
        
        // Assemble the code and return the resulting bytes
        var result = assembler.AssembleToBytes(baseAddress, 128);
        if (result.IsFailure)
            return new HookFailureOnCodeAssembly(HookCodeAssemblySource.AppendedCode, result.Error);

        return result.Value;
    }

    /// <summary>
    /// Gets the number of bytes to reserve to inject a given code with the specified hook options.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="codeLength">Maximum length in bytes of the code instructions to inject.</param>
    /// <param name="options">Options defining how the hook behaves.</param>
    /// <returns>The size to reserve in bytes.</returns>
    private static ulong GetSizeToReserveForCodeInjection(ProcessMemory processMemory, int codeLength,
        HookOptions options)
    {
        return (ulong)(options.GetExpectedGeneratedCodeSize(processMemory.Is64Bit)
            + codeLength
            + ProcessMemoryCodeExtensions.MaxInstructionLength // Extra room for the original instruction(s)
            + FarJumpInstructionLength); // Extra room for the jump back to the original code
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