using Iced.Intel;
using MindControl.Hooks;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code hooks.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryHookExtensionsTest : ProcessMemoryTest
{
    private const string MovLongValueInstructionBytePattern = "48 B8 DF 54 09 2B BA 3C FD FF";
    
    #region Hook
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook replaces a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, with a new MOV instruction that assigns a different value.
    /// No registers are preserved.
    /// After hooking, we let the program run, and check that the output long value is the one written by the hook.
    /// </summary>
    [Test]
    public void HookAndReplaceMovWithByteArrayTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongAddress = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();

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
    public void HookAndInsertMovWithRegisterIsolationWithByteArrayTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        
        var movLongAddress = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();

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
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> method.
    /// The hook replaces a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, with a new MOV instruction that assigns a different value.
    /// No registers are preserved.
    /// After hooking, we let the program run, and check that the output long value is the one written by the hook.
    /// </summary>
    [Test]
    public void HookAndReplaceMovWithAssemblerTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var movLongAddress = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();

        // Hook the instruction that writes the long value to RAX, and replace it with code that writes another value.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, assembler,
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
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> method.
    /// The hook targets a 10-bytes MOV instruction that feeds the RAX register with a new value to be assigned to the
    /// long value in the target app, and inserts a new MOV instruction that assigns a different value after the
    /// instruction.
    /// However, we specify that the <see cref="HookRegister.RaxEax"/> should be preserved.
    /// After hooking, we let the program run, and check the output. The long value must be the expected, original one,
    /// because we specified that the RAX register should be isolated.
    /// </summary>
    [Test]
    public void HookAndInsertMovWithRegisterIsolationWithAssemblerTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        
        var movLongAddress = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();

        // Hook the instruction that writes the long value to RAX, to append code that writes another value.
        // Specify that the RAX register should be isolated.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, assembler,
            new HookOptions(HookExecutionMode.ExecuteOriginalInstructionFirst, HookRegister.RaxEax));
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        
        // After execution, all values should be the expected ones.
        AssertExpectedFinalResults();
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The method is called with a zero pointer address.
    /// Expects a <see cref="HookFailureOnZeroPointer"/> result.
    /// </summary>
    [Test]
    public void HookWithZeroAddressTest()
    {
        var hookResult = TestProcessMemory!.Hook(UIntPtr.Zero, new byte[5],
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnZeroPointer>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,PointerPath,byte[],HookOptions)"/> method.
    /// The method is called with a pointer path that does not evaluate to a valid address.
    /// Expects a <see cref="HookFailureOnPathEvaluation"/> result.
    /// </summary>
    [Test]
    public void HookWithBadPathWithByteArrayTest()
    {
        var hookResult = TestProcessMemory!.Hook(new PointerPath("bad pointer path"), new byte[5],
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
        Assert.That(((HookFailureOnPathEvaluation)hookResult.Error).Details, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,PointerPath,Assembler,HookOptions)"/>
    /// method.
    /// The method is called with a pointer path that does not evaluate to a valid address.
    /// Expects a <see cref="HookFailureOnPathEvaluation"/> result.
    /// </summary>
    [Test]
    public void HookWithBadPathWithAssemblerTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var hookResult = TestProcessMemory!.Hook(new PointerPath("bad pointer path"), assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
        Assert.That(((HookFailureOnPathEvaluation)hookResult.Error).Details, Is.Not.Null);
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook is called with an empty code byte array.
    /// Expects a <see cref="HookFailureOnInvalidArguments"/> result.
    /// </summary>
    [Test]
    public void HookWithEmptyCodeArrayTest()
    {
        var address = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();
        var hookResult = TestProcessMemory!.Hook(address, [],
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> method.
    /// The hook is called with an assembler that does not have any instructions.
    /// Expects a <see cref="HookFailureOnInvalidArguments"/> result.
    /// </summary>
    [Test]
    public void HookWithEmptyAssemblerTest()
    {
        var address = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();
        var assembler = new Assembler(64);
        var hookResult = TestProcessMemory!.Hook(address, assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
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
        var movLongAddress = TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First();

        // Hook the instruction that writes the long value to RAX, and replace it with code that writes another value.
        var hookResult = TestProcessMemory!.Hook(movLongAddress, bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        hookResult.Value.Revert(); // Revert the hook to restore the original code.
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, ExpectedFinalValues[5]);
    }
    
    #endregion
    
    #region InsertCodeAt

    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>
    /// method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. This should change the output long value.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        
        // Insert the code right after our target MOV instruction.
        // That way, the RAX register will be set to the value we want before it's used to write the new long value.
        var hookResult = TestProcessMemory!.InsertCodeAt(movLongNextInstructionAddress, bytes);
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(movLongNextInstructionAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedUntilProcessEnds();
        
        // After execution, the long in the output at index 5 must reflect the new value written by the hook.
        AssertFinalResults(5, "1234567890");
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>
    /// method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. However, we specify that RAX should be preserved.
    /// The output long value must be the original one, because the RAX register is isolated.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithIsolationTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        
        // Insert the code right after our target MOV instruction.
        // That way, the RAX register will be set to the value we want before it's used to write the new long value.
        // But because we specify that the RAX register should be preserved, after the hook, the RAX register will
        // be restored to its original value.
        var hookResult = TestProcessMemory!.InsertCodeAt(movLongNextInstructionAddress, bytes, HookRegister.RaxEax);
        
        Assert.That(hookResult.IsSuccess, Is.True);
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>
    /// method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. This should change the output long value.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        var hookResult = TestProcessMemory!.InsertCodeAt(movLongNextInstructionAddress, assembler);
        
        Assert.That(hookResult.IsSuccess, Is.True);
        ProceedUntilProcessEnds();
        AssertFinalResults(5, "1234567890");
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>
    /// method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. However, we specify that RAX should be preserved.
    /// The output long value must be the original one, because the RAX register is isolated.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithIsolationTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        var hookResult = TestProcessMemory!.InsertCodeAt(movLongNextInstructionAddress, assembler, HookRegister.RaxEax);
        
        Assert.That(hookResult.IsSuccess, Is.True);
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests the
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,byte[],HookRegister[])"/> method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. This should change the output long value.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithPointerPathTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var bytes = assembler.AssembleToBytes().Value;
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        var pointerPath = movLongNextInstructionAddress.ToString("X");
        
        var hookResult = TestProcessMemory!.InsertCodeAt(pointerPath, bytes);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, "1234567890");
    }
    
    /// <summary>
    /// Tests the
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,Assembler,HookRegister[])"/>
    /// method.
    /// Inserts a new MOV instruction that assigns a different value after the instruction that writes a long value to
    /// the RAX register. This should change the output long value.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithPointerPathTest()
    {
        var assembler = new Assembler(64);
        assembler.mov(new AssemblerRegister64(Register.RAX), (ulong)1234567890);
        var movLongNextInstructionAddress =
            TestProcessMemory!.FindBytes(MovLongValueInstructionBytePattern).First() + 10;
        var pointerPath = movLongNextInstructionAddress.ToString("X");
        
        var hookResult = TestProcessMemory!.InsertCodeAt(pointerPath, assembler);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, "1234567890");
    }

    #endregion
}