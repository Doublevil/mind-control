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
        // This path is known to point to 0xFFFFFFFFFFFFFFFF (i.e. the max 8-byte value).
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{OuterClassPointer:X}+10,10,0");
        
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(unchecked((UIntPtr)ulong.MaxValue)));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to a value located after the last possible byte in memory
    /// (the maximum value of a UIntPtr + 1).
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnPointerOutOfRange"/>.
    /// </summary>
    [Test]
    public void EvaluateOverMaxPointerValueTest()
    {
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{OuterClassPointer:X}+10,10,1");
        
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf<PathEvaluationFailureOnPointerOutOfRange>());
        var pathError = (PathEvaluationFailureOnPointerOutOfRange)error;
        Assert.That(pathError.Offset, Is.EqualTo(new PointerOffset(1, false)));
        Assert.That(pathError.PreviousAddress, Is.EqualTo(UIntPtr.MaxValue));
    }
    
    /// <summary>
    /// Tests an error case where the pointer path points to a value that is not readable.
    /// The operation is expected to fail with a <see cref="PathEvaluationFailureOnPointerReadFailure"/>.
    /// </summary>
    [Test]
    public void EvaluateWithUnreadableAddressTest()
    {
        // This path will try to follow a pointer to 0xFFFFFFFFFFFFFFFF, which is not readable
        var result = TestProcessMemory!.EvaluateMemoryAddress($"{OuterClassPointer:X}+10,10,0,0");
        
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf<PathEvaluationFailureOnPointerReadFailure>());
        var pathError = (PathEvaluationFailureOnPointerReadFailure)error;
        Assert.That(pathError.Address, Is.EqualTo(unchecked((UIntPtr)ulong.MaxValue)));
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
}