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
}