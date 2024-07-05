using System.Globalization;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using MindControl.Code;
using MindControl.Hooks;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code hooks.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryHookExtensionsTest : BaseProcessMemoryCodeExtensionTest
{
    protected const int AlternativeOutputIntValue = 123456;
    
    /// <summary>Builds an assembler that moves the alternative output int value in the class instance field.</summary>
    protected virtual Assembler AssembleAlternativeMovInt()
    {
        var assembler = new Assembler(64);
        assembler.mov(__dword_ptr[rcx+0x38], AlternativeOutputIntValue);
        return assembler;
    }

    /// <summary>Builds an assembler that moves the given value in register RCX/ECX.</summary>
    protected virtual Assembler AssembleRcxMov(uint value)
    {
        var assembler = new Assembler(64);
        assembler.mov(rcx, value);
        return assembler;
    }
    
    #region Hook
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook replaces the MOV instruction that assigns the output int value in the target app with an alternative
    /// MOV instruction that assigns a different value.
    /// No registers are preserved.
    /// After hooking, we let the program run, and check that the output long value is the one written by the hook.
    /// </summary>
    [Test]
    public void HookAndReplaceMovWithByteArrayTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var movIntAddress = FindMovIntAddress();
        
        var hookResult = TestProcessMemory!.Hook(movIntAddress, bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(movIntAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedUntilProcessEnds();
        
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook inserts a MOV instruction that changes RCX/ECX to the address of an empty section of memory before the
    /// MOV instruction that assigns the output int value in the target app, which uses RCX/ECX. We specify that we do
    /// not want to preserve any register.
    /// After hooking, we let the program run, and check the output. The int value must be the initial one, because the
    /// original instruction should write its new value in the empty section of memory specified by our injected code
    /// instead. 
    /// </summary>
    [Test]
    public void HookAndInsertMovWithoutRegisterIsolationTest()
    {
        var reservation = TestProcessMemory!.Reserve(0x1000, false, MemoryRange.Full32BitRange).Value;
        var bytes = AssembleRcxMov(reservation.Address.ToUInt32()).AssembleToBytes().Value;
        
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), bytes,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst));
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        
        Assert.That(FinalResults[IndexOfOutputInt], Is.EqualTo(InitialIntValue.ToString()));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook inserts a MOV instruction that changes RCX/ECX to 0 before the MOV instruction that assigns the output
    /// int value in the target app, which uses RCX/ECX. We specify that we want to preserve the RCX/ECX register.
    /// After hooking, we let the program run, and check the output. The output int value must be the expected one,
    /// because the RCX/ECX register is isolated (reverted to its original state after the injected code).
    /// </summary>
    [Test]
    public void HookAndInsertMovWithRegisterIsolationWithByteArrayTest()
    {
        var bytes = AssembleRcxMov(0).AssembleToBytes().Value;
        
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), bytes,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, HookRegister.RcxEcx));
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> method.
    /// Equivalent of <see cref="HookAndReplaceMovWithByteArrayTest"/>, but using an assembler to build the hook code.
    /// </summary>
    [Test]
    public void HookAndReplaceMovWithAssemblerTest()
    {
        var assembler = AssembleAlternativeMovInt();
        var targetInstructionAddress = FindMovIntAddress();
        var hookResult = TestProcessMemory!.Hook(targetInstructionAddress, assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(targetInstructionAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> method.
    /// Equivalent of <see cref="HookAndInsertMovWithRegisterIsolationWithByteArrayTest"/>, but using an assembler to
    /// build the hook code.
    /// </summary>
    [Test]
    public void HookAndInsertMovWithRegisterIsolationWithAssemblerTest()
    {
        var assembler = AssembleRcxMov(0);
        
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), assembler,
            new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst, HookRegister.RcxEcx));
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
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
        var hookResult = TestProcessMemory!.Hook(new PointerPath("bad pointer path"), AssembleAlternativeMovInt(),
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
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), [],
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
        var assembler = new Assembler(Is64Bit ? 64 : 32);
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsFailure, Is.True);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,Assembler,HookOptions)"/> with a
    /// detached process. Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void HookWithDetachedProcessTest()
    {
        var assembler = new Assembler(Is64Bit ? 64 : 32);
        assembler.ret();
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.Hook(0x1234, assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,PointerPath,Assembler,HookOptions)"/> with a
    /// detached process. Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void HookWithPointerPathWithDetachedProcessTest()
    {
        var assembler = new Assembler(Is64Bit ? 64 : 32);
        assembler.ret();
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.Hook("1234", assembler,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> with a
    /// detached process. Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void HookWithByteArrayWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.Hook(0x1234, [0xCC],
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,PointerPath,byte[],HookOptions)"/> with a
    /// detached process. Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void HookWithByteArrayWithPointerPathWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.Hook("1234", [0xCC],
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryHookExtensions.Hook(ProcessMemory,UIntPtr,byte[],HookOptions)"/> method.
    /// The hook is equivalent to the one built in <see cref="HookAndReplaceMovWithByteArrayTest"/>, but we revert it
    /// right after building it.
    /// We then let the program run, and check that the output is the normal, expected one.
    /// </summary>
    [Test]
    public void HookRevertTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        
        var hookResult = TestProcessMemory!.Hook(FindMovIntAddress(), bytes,
            new HookOptions(HookExecutionMode.ReplaceOriginalInstruction));
        hookResult.Value.Revert(); // Revert the hook to restore the original code.
        
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    #endregion
    
    #region InsertCodeAt

    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>.
    /// Inserts a MOV instruction that changes RCX/ECX before the MOV instruction that assigns the output int value in
    /// the target app. The output int value must be the initial one.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayTest()
    {
        var reservation = TestProcessMemory!.Reserve(0x1000, false, MemoryRange.Full32BitRange).Value;
        var bytes = AssembleRcxMov(reservation.Address.ToUInt32()).AssembleToBytes().Value;
        var targetInstructionAddress = FindMovIntAddress();
        
        var hookResult = TestProcessMemory!.InsertCodeAt(targetInstructionAddress, bytes);
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value.InjectedCodeReservation, Is.Not.Null);
        Assert.That(hookResult.Value.Address, Is.EqualTo(targetInstructionAddress));
        Assert.That(hookResult.Value.Length, Is.AtLeast(5));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>.
    /// Inserts a MOV instruction that changes RCX/ECX before the MOV instruction that assigns the output int value in
    /// the target app, with RCX/ECX preservation. The output should be the normal, expected one (injected code should
    /// not affect the original instructions because the register is preserved).
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithIsolationTest()
    {
        var bytes = AssembleRcxMov(0).AssembleToBytes().Value;
        
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress(), bytes, HookRegister.RcxEcx);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="InsertCodeAtWithByteArrayTest"/>, but using an assembler instead of a byte array.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerTest()
    {
        var reservation = TestProcessMemory!.Reserve(0x1000, false, MemoryRange.Full32BitRange).Value;
        var assembler = AssembleRcxMov(reservation.Address.ToUInt32());
        
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress(), assembler);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="InsertCodeAtWithByteArrayWithIsolationTest"/>, but using an assembler.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithIsolationTest()
    {
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress(), AssembleRcxMov(0), HookRegister.RcxEcx);
        Assert.That(hookResult.IsSuccess, Is.True);
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,byte[],HookRegister[])"/>.
    /// Equivalent of <see cref="InsertCodeAtWithByteArrayTest"/>, but using a pointer path instead of an address.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithPointerPathTest()
    {
        var reservation = TestProcessMemory!.Reserve(0x1000, false, MemoryRange.Full32BitRange).Value;
        var bytes = AssembleRcxMov(reservation.Address.ToUInt32()).AssembleToBytes().Value;
        PointerPath targetInstructionPath = FindMovIntAddress().ToString("X");
        
        var hookResult = TestProcessMemory!.InsertCodeAt(targetInstructionPath, bytes);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="InsertCodeAtWithAssemblerTest"/>, but using a pointer path instead of an address.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithPointerPathTest()
    {
        var reservation = TestProcessMemory!.Reserve(0x1000, false, MemoryRange.Full32BitRange).Value;
        var assembler = AssembleRcxMov(reservation.Address.ToUInt32());
        var targetInstructionPath = FindMovIntAddress().ToString("X");
        
        var hookResult = TestProcessMemory!.InsertCodeAt(targetInstructionPath, assembler);
        Assert.That(hookResult.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString());
    }

    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,byte[],HookRegister[])"/>.
    /// Specifies a pointer path that does not evaluate to a valid address.
    /// Expects a <see cref="HookFailureOnPathEvaluation"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithBadPointerTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.InsertCodeAt("bad pointer path", bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,Assembler,HookRegister[])"/>.
    /// Specifies a pointer path that does not evaluate to a valid address.
    /// Expects a <see cref="HookFailureOnPathEvaluation"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithBadPointerTest()
    {
        var hookResult = TestProcessMemory!.InsertCodeAt("bad pointer path", AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>.
    /// Specifies an address of 0. Expects a <see cref="HookFailureOnZeroPointer"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithZeroPointerTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.InsertCodeAt(0, bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnZeroPointer>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>.
    /// Specifies an address of 0. Expects a <see cref="HookFailureOnZeroPointer"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithZeroPointerTest()
    {
        var hookResult = TestProcessMemory!.InsertCodeAt(0, AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnZeroPointer>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,byte[],HookRegister[])"/>.
    /// Disposes the process memory instance and then call the method with valid parameters.
    /// Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithAddressWithDisposedInstanceTest()
    {
        TestProcessMemory!.Dispose();
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress(), bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,byte[],HookRegister[])"/>.
    /// Disposes the process memory instance and then call the method with valid parameters.
    /// Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithByteArrayWithPointerPathWithDisposedInstanceTest()
    {
        TestProcessMemory!.Dispose();
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress().ToString("X"), bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,UIntPtr,Assembler,HookRegister[])"/>.
    /// Disposes the process memory instance and then call the method with valid parameters.
    /// Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithAddressWithDisposedInstanceTest()
    {
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress(), AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.InsertCodeAt(ProcessMemory,PointerPath,Assembler,HookRegister[])"/>.
    /// Disposes the process memory instance and then call the method with valid parameters.
    /// Expects a <see cref="HookFailureOnDetachedProcess"/> result.
    /// </summary>
    [Test]
    public void InsertCodeAtWithAssemblerWithPointerPathWithDisposedInstanceTest()
    {
        TestProcessMemory!.Dispose();
        var hookResult = TestProcessMemory!.InsertCodeAt(FindMovIntAddress().ToString("X"),
            AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnDetachedProcess>());
    }
    
    #endregion
    
    #region ReplaceCodeAt
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,byte[],HookRegister[])"/>.
    /// Replaces the MOV instruction that assigns the output int value in the target app with an alternative MOV
    /// instruction that assigns a different value. After replacing, we let the program run, and check that the output
    /// int value is the one written by the replacement code.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        
        var replaceResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, bytes);
        Assert.That(replaceResult.IsSuccess, Is.True);
        // Check that the result is a CodeChange and not a hook, because the new instructions should fit. 
        Assert.That(replaceResult.Value.GetType(), Is.EqualTo(typeof(CodeChange)));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,byte[],HookRegister[])"/>.
    /// Replaces the MOV instruction that assigns the output int value and the next 2 instructions in the target app
    /// with an alternative MOV instruction that assigns a different value. After replacing, we let the program run, and
    /// check that the output int value is the one written by the replacement code, but also that the output uint value
    /// that should have been written right after the target instruction is the initial, untouched one. 
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayOnMultipleInstructionsTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        
        var replaceResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 3, bytes);
        Assert.That(replaceResult.IsSuccess, Is.True);
        Assert.That(replaceResult.Value.GetType(), Is.EqualTo(typeof(CodeChange)));
        
        ProceedUntilProcessEnds();
        Assert.That(FinalResults[IndexOfOutputInt],
            Is.EqualTo(AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture)));
        Assert.That(FinalResults[IndexOfOutputUInt],
            Is.EqualTo(InitialUIntValue.ToString(CultureInfo.InvariantCulture)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,byte[],HookRegister[])"/>.
    /// Replaces the MOV instruction that assigns the output int value in the target app with an alternative MOV
    /// instruction that assigns a different value, and specify that we want to preserve register RAX. Because the
    /// replaced instruction is the same as the replacement instruction, it would normally fit and not trigger a hook.
    /// However, because we have register preservation code that adds a couple bytes to the replacement code, it cannot
    /// fit anymore. This means a hook should be performed, and so we should get a CodeHook result.
    /// After replacing, we let the program run, and check that the output int value is the one written by the
    /// replacement code.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayWithPreservedRegistersTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, bytes, HookRegister.RaxEax);
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value, Is.TypeOf<CodeHook>());
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,PointerPath,int,byte[],HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithByteArrayTest"/>, but using a pointer path instead of an address.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayWithPointerPathTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        PointerPath targetPath = FindMovIntAddress().ToString("X");
        
        var replaceResult = TestProcessMemory!.ReplaceCodeAt(targetPath, 1, bytes);
        Assert.That(replaceResult.IsSuccess, Is.True);
        Assert.That(replaceResult.Value.GetType(), Is.EqualTo(typeof(CodeChange)));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithByteArrayTest"/>, but using an assembler instead of a byte array.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerTest()
    {
        var assembler = AssembleAlternativeMovInt();
        
        var replaceResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, assembler);
        Assert.That(replaceResult.IsSuccess, Is.True); 
        Assert.That(replaceResult.Value.GetType(), Is.EqualTo(typeof(CodeChange)));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithByteArrayWithPreservedRegistersTest"/>, but using an assembler instead
    /// of a byte array.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerWithPreservedRegistersTest()
    {
        var assembler = AssembleAlternativeMovInt();
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, assembler, HookRegister.RaxEax);
        Assert.That(hookResult.IsSuccess, Is.True);
        Assert.That(hookResult.Value, Is.TypeOf<CodeHook>());
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,PointerPath,int,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithAssemblerTest"/>, but using a pointer path instead of an address.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerWithPointerPathTest()
    {
        var assembler = AssembleAlternativeMovInt();
        PointerPath targetPath = FindMovIntAddress().ToString("X");
        
        var replaceResult = TestProcessMemory!.ReplaceCodeAt(targetPath, 1, assembler);
        Assert.That(replaceResult.IsSuccess, Is.True); 
        Assert.That(replaceResult.Value.GetType(), Is.EqualTo(typeof(CodeChange)));
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, AlternativeOutputIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,PointerPath,int,byte[],HookRegister[])"/>.
    /// Use a pointer path that does not evaluate to a valid address, but otherwise valid parameters.
    /// Expects the result to be a <see cref="HookFailureOnPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayWithBadPointerPathTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.ReplaceCodeAt("bad pointer path", 1, bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,PointerPath,int,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithByteArrayWithBadPointerPathTest"/>, but using an assembler instead of
    /// a byte array.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerWithBadPointerPathTest()
    {
        var hookResult = TestProcessMemory!.ReplaceCodeAt("bad pointer path", 1, AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnPathEvaluation>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,byte[],HookRegister[])"/>.
    /// Specify an empty code array, but otherwise valid parameters.
    /// Expects the result to be a <see cref="HookFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayWithNoCodeTest()
    {
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, []);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,Assembler,HookRegister[])"/>.
    /// Specify an empty assembler, but otherwise valid parameters.
    /// Expects the result to be a <see cref="HookFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerWithNoCodeTest()
    {
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 1, new Assembler(Is64Bit ? 64 : 32));
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,byte[],HookRegister[])"/>.
    /// Specifies a number of instructions to replace of 0, but otherwise valid parameters.
    /// Expects the result to be a <see cref="HookFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithByteArrayWithZeroInstructionTest()
    {
        var bytes = AssembleAlternativeMovInt().AssembleToBytes().Value;
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 0, bytes);
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }
    
    /// <summary>Tests
    /// <see cref="ProcessMemoryHookExtensions.ReplaceCodeAt(ProcessMemory,UIntPtr,int,Assembler,HookRegister[])"/>.
    /// Equivalent of <see cref="ReplaceCodeAtWithByteArrayWithZeroInstructionTest"/>, but using an assembler instead of
    /// a byte array.
    /// </summary>
    [Test]
    public void ReplaceCodeAtWithAssemblerWithZeroInstructionTest()
    {
        var hookResult = TestProcessMemory!.ReplaceCodeAt(FindMovIntAddress(), 0, AssembleAlternativeMovInt());
        Assert.That(hookResult.IsSuccess, Is.False);
        Assert.That(hookResult.Error, Is.TypeOf<HookFailureOnInvalidArguments>());
    }
    
    #endregion
}

/// <summary>
/// Runs the <see cref="ProcessMemoryHookExtensionsTest"/> tests with a 32-bit target process.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryHookExtensionsTestX86 : ProcessMemoryHookExtensionsTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;

    /// <summary>Builds an assembler that moves the alternative output int value in the class instance field.</summary>
    protected override Assembler AssembleAlternativeMovInt()
    {
        var assembler = new Assembler(32);
        assembler.mov(__dword_ptr[ecx+0x28], AlternativeOutputIntValue);
        return assembler;
    }

    /// <summary>Builds an assembler that moves the given value in register RCX/ECX.</summary>
    protected override Assembler AssembleRcxMov(uint value)
    {
        var assembler = new Assembler(32);
        assembler.mov(ecx, value);
        return assembler;
    }
}