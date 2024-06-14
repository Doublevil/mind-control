using Iced.Intel;
using MindControl.Hooks;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code hooks.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryHookTest : ProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook replaces a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, with a new MOV instruction that assigns a different value.
    /// It is performed with full isolation, with the RAX register as an exception (because we want to interfere with
    /// that register).
    /// After hooking, we let the program run, and check that the output long value is the one written by the hook.
    /// </summary>
    [Test]
    public void HookAndReplaceValueOfMovInstructionWithFullIsolationTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First();

        // Hook the instruction that writes the long value to RAX, and replace it with code that writes another value.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction, Register.RAX));
        
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(movLongAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedToNextStep();
        ProceedToNextStep();
        
        // After execution, the output must reflect the new value written by the hook.
        Assert.That(FinalResults[5], Is.EqualTo("1234567890"));
    }
}