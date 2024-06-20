namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Base class for tests of the <see cref="ProcessMemory"/> class related to code manipulation.
/// Provides methods and properties related to code manipulation.
/// </summary>
public abstract class BaseProcessMemoryCodeExtensionTest : ProcessMemoryTest
{
    /// <summary>
    /// Finds and returns the address of the MOV instruction that loads the new long value in the target app into the
    /// RAX register, before assigning it to the output long value.
    /// </summary>
    protected UIntPtr FindMovLongAddress()
    {
        // Only search executable memory for two reasons:
        // - It is faster.
        // - When built on .net 8, there are 2 instances of the code. Only the right one is in executable memory.
        
        return TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF",
            settings: new FindBytesSettings { SearchExecutable = true }).First();
        
        // > MOV RAX, 0xFFFD3CBA2B0954DF  ; Loads the new long value into the RAX register. This is the one we get.
        // MOV [RCX+20], RAX              ; Changes the long value in the class instance that is output at the end.
    } 
}