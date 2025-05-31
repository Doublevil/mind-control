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
        
        Assert.That(result.IsSuccess, Is.True, result.ToString());
        Assert.That(result.Value, Is.EqualTo(GetMaxPointerValue()));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path has a correct module name.
    /// </summary>
    [Test]
    public void EvaluateWithModuleTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{MainModuleName}+8");
        
        Assert.That(result.IsSuccess, Is.True, result.ToString());
        Assert.That(result.Value, Is.Not.EqualTo(UIntPtr.Zero));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to a value located after the last possible byte in memory
    /// (the maximum value of a UIntPtr + 1).
    /// The operation is expected to fail with a <see cref="PointerOutOfRangeFailure"/> on an x64
    /// process, or a <see cref="IncompatibleBitnessPointerFailure"/> on an x86 process.
    /// </summary>
    [Test]
    public void EvaluateOverMaxPointerValueTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToPointerToMaxAddressPlusOne());
        
        Assert.That(result.IsSuccess, Is.False);
        var failure = result.Failure;

        if (Is64Bit)
        {
            Assert.That(failure, Is.TypeOf<PointerOutOfRangeFailure>());
            var pathError = (PointerOutOfRangeFailure)failure;
            Assert.That(pathError.Offset, Is.EqualTo(new PointerOffset(1, false)));
            Assert.That(pathError.PreviousAddress, Is.EqualTo(UIntPtr.MaxValue));
        }
        else
            Assert.That(failure, Is.TypeOf<IncompatibleBitnessPointerFailure>());
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to an unreachable value because one of the pointers in the
    /// path (but not the last one) points to an unreadable address.
    /// The operation is expected to fail with a failure.
    /// </summary>
    [Test]
    public void EvaluateWithUnreadableAddressHalfwayThroughTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToPointerThroughMaxAddress());
        
        Assert.That(result.IsSuccess, Is.False);
        var failure = result.Failure;
        Assert.That(failure, Is.TypeOf<OperatingSystemCallFailure>());
        var osFailure = (OperatingSystemCallFailure)failure;
        Assert.That(osFailure.ErrorCode, Is.GreaterThan(0));
        Assert.That(osFailure.Message, Is.Not.Empty);
    }
    
    /// <summary>
    /// Tests an error case where the pointer path given to a read operation points to zero.
    /// The operation is expected to fail with a <see cref="PointerOutOfRangeFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateOnZeroPointerTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("0");
        
        Assert.That(result.IsSuccess, Is.False);
        var failure = result.Failure;
        Assert.That(failure, Is.TypeOf<PointerOutOfRangeFailure>());
        var pathError = (PointerOutOfRangeFailure)failure;
        Assert.That(pathError.Offset, Is.EqualTo(PointerOffset.Zero));
        Assert.That(pathError.PreviousAddress, Is.EqualTo(UIntPtr.Zero));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path has a module that is not part of the target process.
    /// The operation is expected to fail with a <see cref="BaseModuleNotFoundFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithUnknownModuleTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("ThisModuleDoesNotExist.dll+10,10");
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<BaseModuleNotFoundFailure>());
        var failure = (BaseModuleNotFoundFailure)result.Failure;
        Assert.That(failure.ModuleName, Is.EqualTo("ThisModuleDoesNotExist.dll"));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.EvaluateMemoryAddress"/> with a detached process.
    /// The operation is expected to fail with a <see cref="DetachedProcessFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory!.EvaluateMemoryAddress(GetPathToMaxAddress());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<DetachedProcessFailure>());
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
    /// Expect a <see cref="IncompatiblePointerPathBitnessFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithX64PathOnX86ProcessTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress("1000000000,4");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<IncompatiblePointerPathBitnessFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.EvaluateMemoryAddress"/> with a pointer path that starts at a module address,
    /// with the maximum 32-bit offset added to it.
    /// Expect a <see cref="IncompatibleBitnessPointerFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithX64ModuleOffsetOnX86ProcessTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{MainModuleName}+FFFFFFFF");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<IncompatibleBitnessPointerFailure>());
    }
}