using Iced.Intel;
using MindControl.Hooks;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code hooks.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryHookExtensionsTest : ProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook replaces a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, with a new MOV instruction that assigns a different value.
    /// No registers are preserved.
    /// After hooking, we let the program run, and check that the output long value is the one written by the hook.
    /// </summary>
    [Test]
    public void HookAndReplaceValueOfMovInstructionTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First();

        // Hook the instruction that writes the long value to RAX, and replace it with code that writes another value.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(movLongAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedUntilProcessEnds();
        
        // After execution, the long in the output at index 5 must reflect the new value written by the hook.
        AssertFinalResults(5, "1234567890");
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook targets a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, and inserts a new MOV instruction that assigns a different value after the
    /// instruction.
    /// However, we specify that the <see cref="HookRegister.RaxEax"/> should be preserved.
    /// After hooking, we let the program run, and check the output. The long value must be the expected, original one,
    /// because we specified that the RAX register should be isolated.
    /// </summary>
    [Test]
    public void HookAndInsertValueOfMovInstructionWithRegisterIsolationTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First();

        // Hook the instruction that writes the long value to RAX, to append code that writes another value.
        // Specify that the RAX register should be isolated.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, bytes,
            new HookOptions(HookExecutionMode.ExecuteOriginalInstructionFirst, HookRegister.RaxEax));
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        
        // After execution, all values should be the expected ones.
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook replaces a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, with a new MOV instruction that assigns a different value.
    /// After hooking, we revert the hook.
    /// We then let the program run, and check that the output long value is the original, expected one.
    /// </summary>
    [Test]
    public void HookRevertTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First();

        // Hook the instruction that writes the long value to RAX, and replace it with code that writes another value.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        hookResult.Value.Revert(); // Revert the hook to restore the original code.
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, ExpectedFinalValues[5]);
    }
}