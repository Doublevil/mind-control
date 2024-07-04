namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Base class for tests of the <see cref="ProcessMemory"/> class related to code manipulation.
/// Provides methods and properties related to code manipulation.
/// </summary>
public abstract class BaseProcessMemoryCodeExtensionTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Finds and returns the address of the MOV instruction that loads the new int value in the target app into the
    /// RAX register, before assigning it to the output int value.
    /// </summary>
    protected UIntPtr FindMovIntAddress()
    {
        // Only search executable memory for two reasons:
        // - It is faster.
        // - When built on .net 8, there are 2 instances of the code. Only the right one is in executable memory.
        
        string signature = Is64Bit ? "C7 41 38 13 11 0F 00 48 8B 4D F8"
            : "C7 41 28 13 11 0F 00 8B 4D F8";
        return TestProcessMemory!.FindBytes(signature,
            settings: new FindBytesSettings { SearchExecutable = true }).First();
        
        // x64: MOV [RCX+38],000F1113
        // x86: MOV [ECX+28],000F1113
    }
    
    /// <summary>
    /// Finds and returns the address of the MOV instruction that loads the new long value in the target app into the
    /// RAX register, before assigning it to the output long value.
    /// </summary>
    protected UIntPtr FindMovLongAddress()
    {
        // Only search executable memory for two reasons:
        // - It is faster.
        // - When built on .net 8, there are 2 instances of the code. Only the right one is in executable memory.
        
        string signature = Is64Bit ? "48 B8 DF 54 09 2B BA 3C FD FF"
            : "C7 01 DF 54 09 2B 8B 4D C4 C7 41 04 BA 3C FD FF";
        return TestProcessMemory!.FindBytes(signature,
            settings: new FindBytesSettings { SearchExecutable = true }).First();
        
        // For x64:
        // > MOV RAX, 0xFFFD3CBA2B0954DF  ; Loads the new long value into the RAX register. This is the one we get.
        // MOV [RCX+20], RAX              ; Changes the long value in the class instance that is output at the end.
        
        // For x86:
        // MOV ECX,[EBP-3C]      ; Loads the address of the class instance field into ECX
        // > MOV [ECX],2B0954DF  ; Writes the first 4 bytes of the long value in the class instance field
        // MOV ECX,[EBP-3C]      ; Loads the address of the class instance field into ECX
        // MOV [ECX+4],FFFD3CBA  ; Writes the last 4 bytes of the long value in the class instance field
    }
}