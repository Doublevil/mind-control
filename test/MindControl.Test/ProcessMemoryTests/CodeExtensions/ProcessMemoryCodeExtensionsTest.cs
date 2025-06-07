using System.Globalization;
using MindControl.Code;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code manipulation.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryCodeExtensionsTest : BaseProcessMemoryCodeExtensionTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a MOV instruction that changes the value of the int value in the target app after the
    /// first step.
    /// After disabling the instruction, we let the program run to the end, and check that the output int value is the
    /// original one (it was not modified because we disabled the instruction).
    /// </summary>
    [Test]
    public void DisableCodeAtTest()
    {
        var movIntAddress = FindMovIntAddress();
        var result = TestProcessMemory!.DisableCodeAt(movIntAddress);
        Assert.That(result.IsSuccess, Is.True, result.ToString());
        Assert.That(result.Value.Address, Is.EqualTo(movIntAddress));
        Assert.That(result.Value.Length, Is.AtLeast(1)); // We don't care how long it is but we check that it is set.

        ProceedUntilProcessEnds();
        
        // Test that the output long is the initial value set when the program starts, not the one that was
        // supposed to be set by the disabled instruction.
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,PointerPath,int)"/> method.
    /// This is a variant of <see cref="DisableCodeAtTest"/> that uses a pointer path instead of an address.
    /// </summary>
    [Test]
    public void DisableCodeAtWithPointerPathTest()
    {
        var pointerPath = FindMovIntAddress().ToString("X");
        var result = TestProcessMemory!.DisableCodeAt(pointerPath);
        Assert.That(result.IsSuccess, Is.True, result.ToString());
        
        ProceedUntilProcessEnds();
        AssertFinalResults(IndexOfOutputInt, InitialIntValue.ToString(CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a series of MOV instruction that changes the value of the int and uint values in the
    /// target app after the first step.
    /// After disabling the instructions, we let the program run to the end, and check that the output int and uint
    /// values are the original ones (they were not modified because we disabled the instructions).
    /// </summary>
    [Test]
    public void DisableCodeAtWithMultipleInstructionsTest()
    {
        var result = TestProcessMemory!.DisableCodeAt(FindMovIntAddress(), 3);
        Assert.That(result.IsSuccess, Is.True, result.ToString());
        
        ProceedUntilProcessEnds();
        
        // Test that the output int and uint are the first values set when the program starts, instead of the ones that
        // were supposed to be set by the disabled instructions.
        Assert.That(FinalResults[IndexOfOutputInt], Is.EqualTo(InitialIntValue.ToString(CultureInfo.InvariantCulture)));
        Assert.That(FinalResults[IndexOfOutputUInt],
            Is.EqualTo(InitialUIntValue.ToString(CultureInfo.InvariantCulture)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a MOV instruction that changes the value of the long value in the target app after the
    /// first step.
    /// After disabling the instruction, we revert the change.
    /// We then let the program run to the end, and check that the output long value is the expected one (the one set by
    /// the instruction we disabled and reverted).
    /// </summary>
    [Test]
    public void DisableCodeAtRevertTest()
    {
        var result = TestProcessMemory!.DisableCodeAt(FindMovIntAddress());
        result.Value.Revert();
        
        ProceedUntilProcessEnds();
        AssertExpectedFinalResults();
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called with a zero pointer.
    /// Expects a <see cref="ZeroPointerFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithZeroAddressTest()
    {
        var result = TestProcessMemory!.DisableCodeAt(UIntPtr.Zero);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<ZeroPointerFailure>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called with an instruction count of zero.
    /// Expects a <see cref="InvalidArgumentFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithInvalidInstructionCountTest()
    {
        var result = TestProcessMemory!.DisableCodeAt(FindMovIntAddress(), 0);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<InvalidArgumentFailure>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,PointerPath,int)"/> method.
    /// The method is called with a pointer path that does not point to a valid address. Expects a failure.
    /// </summary>
    [Test]
    public void DisableCodeAtWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.DisableCodeAt("bad pointer path");
        Assert.That(result.IsSuccess, Is.False);
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a freshly allocated memory address that holds only FF bytes instead of valid code.
    /// Expects a <see cref="CodeDecodingFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithBadInstructionsTest()
    {
        var address = TestProcessMemory!.Reserve(0x1000, true).Value.Address;
        TestProcessMemory.WriteBytes(address, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }); // Write invalid code
        var result = TestProcessMemory.DisableCodeAt(address);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<CodeDecodingFailure>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method with a
    /// detached process. Expects a <see cref="DetachedProcessFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory!.DisableCodeAt(FindMovIntAddress());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<DetachedProcessFailure>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,PointerPath,int)"/> method with a
    /// detached process. Expects a <see cref="DetachedProcessFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeWithPointerPathWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory!.DisableCodeAt(FindMovIntAddress().ToString("X"));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<DetachedProcessFailure>());
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryCodeExtensionsTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryCodeExtensionsTestX86 : ProcessMemoryCodeExtensionsTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;

    /// <summary>
    /// Tests <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> with an x64 address on
    /// an x86 process. Expects a <see cref="IncompatibleBitnessPointerFailure"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtX64AddressOnX86ProcessTest()
    {
        var address = (ulong)uint.MaxValue + 1;
        var result = TestProcessMemory!.DisableCodeAt(new UIntPtr(address));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<IncompatibleBitnessPointerFailure>());
    }
}