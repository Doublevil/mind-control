using Iced.Intel;
using MindControl.Results;

namespace MindControl.Code;

/// <summary>
/// Provides extension methods for <see cref="ProcessMemory"/> related to executable memory.
/// </summary>
public static class ProcessMemoryCodeExtensions
{
    /// <summary>NOP instruction opcode.</summary>
    internal const byte NopByte = 0x90;
    
    /// <summary>Maximum byte count for a single instruction.</summary>
    internal const int MaxInstructionLength = 15;
    
    /// <summary>
    /// Assembles and stores the instructions registered in the given assembler as executable code in the process
    /// memory. If needed, memory is allocated to store the data. Returns the reservation that holds the data.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="assembler">Assembler holding the instructions to store.</param>
    /// <param name="nearAddress"></param>
    /// <returns>A result holding either the reservation where the code has been written, or an allocation failure.
    /// </returns>
    public static Result<MemoryReservation, StoreFailure> StoreCode(this ProcessMemory processMemory,
        Assembler assembler, UIntPtr? nearAddress = null)
    {
        if (!processMemory.IsAttached)
            return new StoreFailureOnDetachedProcess();
        if (!assembler.Instructions.Any())
            return new StoreFailureOnInvalidArguments("The given assembler has no instructions to assemble.");
        
        // The length of the assembled code will vary depending on where we store it (because of relative operands).
        // So the problem is, we need a memory reservation to assemble the code, but we need to assemble the code to
        // know how much memory to reserve.
        // Fortunately, we know that instructions are at most 15 bytes long, and we know the instruction count, so we
        // can start by reserving the maximum possible length of the assembled code.
        var codeMaxLength = assembler.Instructions.Count * MaxInstructionLength;
        var reservationResult = processMemory.Reserve((ulong)codeMaxLength, true, nearAddress: nearAddress);
        if (reservationResult.IsFailure)
            return new StoreFailureOnAllocation(reservationResult.Error);

        var reservation = reservationResult.Value;
        
        // Once we have the reservation, we can assemble it, using its address as a base address.
        var assemblyResult = assembler.AssembleToBytes(reservation.Address);
        if (assemblyResult.IsFailure)
            return new StoreFailureOnInvalidArguments(
                $"The given assembler failed to assemble the code: {assemblyResult.Error}");
        
        // Shrink the reservation to the actual size of the assembled code to avoid wasting memory as much as possible.
        var assembledCode = assemblyResult.Value;
        reservation.Shrink((ulong)(codeMaxLength - assembledCode.Length));
        
        // Write the assembled code to the reservation.
        var writeResult = processMemory.WriteBytes(reservation.Address, assembledCode, MemoryProtectionStrategy.Ignore);
        if (writeResult.IsFailure)
        {
            reservation.Dispose();
            return new StoreFailureOnWrite(writeResult.Error);
        }

        return reservation;
    }
    
    /// <summary>
    /// Replaces the instruction (or multiple consecutive instructions) referenced by the given pointer path with NOP
    /// instructions that do nothing, effectively disabling the original code.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="pointerPath">Path to the address of the first instruction to replace.</param>
    /// <param name="instructionCount">Number of consecutive instructions to replace. Default is 1.</param>
    /// <returns>A result holding either a code change instance, allowing you to revert modifications, or a code writing
    /// failure.</returns>
    public static Result<CodeChange, CodeWritingFailure> DisableCodeAt(this ProcessMemory processMemory,
        PointerPath pointerPath, int instructionCount = 1)
    {
        if (!processMemory.IsAttached)
            return new CodeWritingFailureOnDetachedProcess();
        if (instructionCount < 1)
            return new CodeWritingFailureOnInvalidArguments(
                "The number of instructions to replace must be at least 1.");
        
        var addressResult = processMemory.EvaluateMemoryAddress(pointerPath);
        if (addressResult.IsFailure)
            return new CodeWritingFailureOnPathEvaluation(addressResult.Error);
        
        return processMemory.DisableCodeAt(addressResult.Value, instructionCount);
    }
    
    /// <summary>
    /// Replaces the instruction (or multiple consecutive instructions) at the given address with NOP instructions that
    /// do nothing, effectively disabling the original code.
    /// </summary>
    /// <param name="processMemory">Process memory instance to use.</param>
    /// <param name="address">Address of the first instruction to replace.</param>
    /// <param name="instructionCount">Number of consecutive instructions to replace. Default is 1.</param>
    /// <returns>A result holding either a code change instance, allowing you to revert modifications, or a code writing
    /// failure.</returns>
    public static Result<CodeChange, CodeWritingFailure> DisableCodeAt(this ProcessMemory processMemory,
        UIntPtr address, int instructionCount = 1)
    {
        if (!processMemory.IsAttached)
            return new CodeWritingFailureOnDetachedProcess();
        if (address == UIntPtr.Zero)
            return new CodeWritingFailureOnZeroPointer();
        if (!processMemory.Is64Bit && address.ToUInt64() > uint.MaxValue)
            return new CodeWritingFailureOnIncompatibleBitness(address);
        if (instructionCount < 1)
            return new CodeWritingFailureOnInvalidArguments(
                "The number of instructions to replace must be at least 1.");
        
        // For convenience, this method uses an instruction count, not a byte count.
        // The problem is that instructions can take from 1 to 15 bytes, so we need to use a decoder to know how many
        // bytes to replace.
        
        using var stream = processMemory.GetMemoryStream(address);
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(processMemory.Is64Bit ? 64 : 32, codeReader);

        ulong fullLength = 0;
        for (int i = 0; i < instructionCount; i++)
        {
            var instruction = decoder.Decode();
            if (instruction.IsInvalid)
                return new CodeWritingFailureOnDecoding(decoder.LastError);

            fullLength += (ulong)instruction.Length;
        }
        
        var originalBytesResult = processMemory.ReadBytes(address, (int)fullLength);
        if (originalBytesResult.IsFailure)
            return new CodeWritingFailureOnReadFailure(originalBytesResult.Error);
        
        var nopInstructions = new byte[fullLength];
        nopInstructions.AsSpan().Fill(NopByte);
        var writeResult = processMemory.WriteBytes(address, nopInstructions, MemoryProtectionStrategy.Remove);
        if (writeResult.IsFailure)
            return new CodeWritingFailureOnWriteFailure(writeResult.Error);
        
        return new CodeChange(processMemory, address, originalBytesResult.Value);
    }
}