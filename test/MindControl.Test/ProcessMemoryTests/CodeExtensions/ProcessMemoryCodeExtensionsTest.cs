using MindControl.Code;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests.CodeExtensions;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to code manipulation.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryCodeExtensionsTest : ProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a MOV instruction that changes the value of the long value in the target app after the
    /// first step.
    /// After disabling the instruction, we let the program run to the end, and check that the output long value is the
    /// original one (it was not modified because we disabled the instruction).
    /// </summary>
    [Test]
    public void DisableCodeAtTest()
    {
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First() + 10;
        var result = TestProcessMemory.DisableCodeAt(movLongAddress);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Address, Is.EqualTo(movLongAddress));
        Assert.That(result.Value.Length, Is.AtLeast(1)); // We don't care how long it is but we check that it is set.
        
        ProceedUntilProcessEnds();
        
        // Test that the output long at index 5 is the first value set when the program starts, not the one that was
        // supposed to be set by the disabled instruction.
        AssertFinalResults(5, "-65746876815103");
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,PointerPath,int)"/> method.
    /// This is a variant of <see cref="DisableCodeAtTest"/> that uses a pointer path instead of an address.
    /// </summary>
    [Test]
    public void DisableCodeAtWithPointerPathTest()
    {
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First() + 10;
        var pointerPath = movLongAddress.ToString("X");
        var result = TestProcessMemory.DisableCodeAt(pointerPath);
        Assert.That(result.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, "-65746876815103");
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a series of MOV instruction that changes the value of the long and ulong values in the
    /// target app after the first step.
    /// After disabling the instructions, we let the program run to the end, and check that the output long and ulong
    /// values are the original ones (they were not modified because we disabled the instructions).
    /// </summary>
    [Test]
    public void DisableCodeAtWithMultipleInstructionsTest()
    {
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First();
        var result = TestProcessMemory.DisableCodeAt(movLongAddress, 5);
        Assert.That(result.IsSuccess, Is.True);
        
        ProceedUntilProcessEnds();
        
        // Test that the output long at index 5 and ulong at index 6 are the first values set when the program starts,
        // not the ones that were supposed to be set by the disabled instructions.
        Assert.That(FinalResults[5], Is.EqualTo("-65746876815103"));
        Assert.That(FinalResults[6], Is.EqualTo("76354111324644"));
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
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First() + 10;
        var result = TestProcessMemory.DisableCodeAt(movLongAddress);
        result.Value.Revert();
        
        ProceedUntilProcessEnds();
        AssertFinalResults(5, ExpectedFinalValues[5]);
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called with a zero pointer.
    /// Expects a <see cref="CodeWritingFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithZeroAddressTest()
    {
        var result = TestProcessMemory!.DisableCodeAt(UIntPtr.Zero);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<CodeWritingFailureOnZeroPointer>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called with an instruction count of zero.
    /// Expects a <see cref="CodeWritingFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithInvalidInstructionCountTest()
    {
        var movLongAddress = TestProcessMemory!.FindBytes("48 B8 DF 54 09 2B BA 3C FD FF").First() + 10;
        var result = TestProcessMemory.DisableCodeAt(movLongAddress, 0);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<CodeWritingFailureOnInvalidArguments>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,PointerPath,int)"/> method.
    /// The method is called with a pointer path that does not point to a valid address.
    /// Expects a <see cref="CodeWritingFailureOnPathEvaluation"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.DisableCodeAt("bad pointer path");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<CodeWritingFailureOnPathEvaluation>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryCodeExtensions.DisableCodeAt(ProcessMemory,UIntPtr,int)"/> method.
    /// The method is called on a freshly allocated memory address that holds only FF bytes instead of valid code.
    /// Expects a <see cref="CodeWritingFailureOnDecoding"/>.
    /// </summary>
    [Test]
    public void DisableCodeAtWithBadInstructionsTest()
    {
        var address = TestProcessMemory!.Reserve(0x1000, true).Value.Address;
        TestProcessMemory.WriteBytes(address, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        var result = TestProcessMemory.DisableCodeAt(address);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<CodeWritingFailureOnDecoding>());
    }
}