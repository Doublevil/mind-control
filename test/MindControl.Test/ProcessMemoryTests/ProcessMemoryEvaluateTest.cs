using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to evaluating a pointer path.
/// </summary>
/// <remarks>Most pointer path evaluation features are implicitly tested through memory reading and writing methods.
/// This test class focuses on special cases.</remarks>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryEvaluateTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Tests the nominal case, with a path that evaluates to a valid address.
    /// </summary>
    [Test]
    public void EvaluateOnKnownPointerTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToMaxAddress());
        
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(GetMaxPointerValue()));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to a value located after the last possible byte in memory
    /// (the maximum value of a UIntPtr + 1).
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnPointerOutOfRange"/> on an x64
    /// process, or a <see cref="PathEvaluationFailureOnIncompatibleBitness"/> on an x86 process.
    /// </summary>
    [Test]
    public void EvaluateOverMaxPointerValueTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToPointerToMaxAddressPlusOne());
        
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;

        if (Is64Bit)
        {
            Assert.That(error, Is.TypeOf<PathEvaluationFailureOnPointerOutOfRange>());
            var pathError = (PathEvaluationFailureOnPointerOutOfRange)error;
            Assert.That(pathError.Offset, Is.EqualTo(new PointerOffset(1, false)));
            Assert.That(pathError.PreviousAddress, Is.EqualTo(UIntPtr.MaxValue));
        }
        else
            Assert.That(error, Is.TypeOf<PathEvaluationFailureOnIncompatibleBitness>());
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to an unreachable value because one of the pointers in the
    /// path (but not the last one) points to an unreadable address.
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnPointerReadFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithUnreadableAddressHalfwayThroughTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToPointerThroughMaxAddress());
        
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf<PathEvaluationFailureOnPointerReadFailure>());
        var pathError = (PathEvaluationFailureOnPointerReadFailure)error;
        Assert.That(pathError.Address, Is.EqualTo(GetMaxPointerValue()));
        Assert.That(pathError.Details, Is.TypeOf<ReadFailureOnSystemRead>());
        var readFailure = (ReadFailureOnSystemRead)pathError.Details;
        Assert.That(readFailure.Details, Is.TypeOf<OperatingSystemCallFailure>());
        var osFailure = (OperatingSystemCallFailure)readFailure.Details;
        Assert.That(osFailure.ErrorCode, Is.GreaterThan(0));
        Assert.That(osFailure.ErrorMessage, Is.Not.Empty);
    }
    
    /// <summary>
    /// Tests an error case where the pointer path given to a read operation points to zero.
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnPointerOutOfRange"/>.
    /// </summary>
    [Test]
    public void EvaluateOnZeroPointerTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("0");
        
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf<PathEvaluationFailureOnPointerOutOfRange>());
        var pathError = (PathEvaluationFailureOnPointerOutOfRange)error;
        Assert.That(pathError.Offset, Is.EqualTo(PointerOffset.Zero));
        Assert.That(pathError.PreviousAddress, Is.EqualTo(UIntPtr.Zero));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path has a module that is not part of the target process.
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnBaseModuleNotFound"/>.
    /// </summary>
    [Test]
    public void EvaluateWithUnknownModuleTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("ThisModuleDoesNotExist.dll+10,10");
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<PathEvaluationFailureOnBaseModuleNotFound>());
        var error = (PathEvaluationFailureOnBaseModuleNotFound)result.Error;
        Assert.That(error.ModuleName, Is.EqualTo("ThisModuleDoesNotExist.dll"));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.EvaluateMemoryAddress"/> with a detached process.
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void EvaluateWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToMaxAddress());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<PathEvaluationFailureOnDetachedProcess>());
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryEvaluateTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryEvaluateTestX86 : ProcessMemoryEvaluateTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;

    /// <summary>
    /// Tests <see cref="ProcessMemory.EvaluateMemoryAddress"/> with a pointer path that starts with an address that is
    /// not within the 32-bit address space.
    /// Expect a <see cref="PathEvaluationFailureOnIncompatibleBitness"/>.
    /// </summary>
    [Test]
    public void EvaluateWithX64PathOnX86ProcessTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("1000000000,4");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<PathEvaluationFailureOnIncompatibleBitness>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.EvaluateMemoryAddress"/> with a pointer path that starts at a module address,
    /// with the maximum 32-bit offset added to it.
    /// Expect a <see cref="PathEvaluationFailureOnIncompatibleBitness"/>.
    /// </summary>
    [Test]
    public void EvaluateWithX64ModuleOffsetOnX86ProcessTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{MainModuleName}+FFFFFFFF");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<PathEvaluationFailureOnIncompatibleBitness>());
    }
}