using Iced.Intel;
using MindControl.Results;

namespace MindControl.Code;

/// <summary>
/// Provides extension methods for <see cref="ProcessMemory"/> related to executable memory.
/// </summary>
public static class ProcessMemoryCodeExtensions
{
    /// <summary>NOP instruction opcode.</summary>
    public const byte NopByte = 0x90;
    
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
        if (address == UIntPtr.Zero)
            return new CodeWritingFailureOnZeroPointer();
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