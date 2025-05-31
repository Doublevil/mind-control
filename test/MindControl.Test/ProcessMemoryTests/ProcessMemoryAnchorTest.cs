using System.Globalization;
using MindControl.Anchors;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>Tests the methods of <see cref="ProcessMemory"/> related to value anchors.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAnchorTest : BaseProcessMemoryTest
{
    #region Primitive type anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetAnchor{T}(UIntPtr)"/> with the static address of the int value in the target
    /// app and reads the value. The result should be the initial int value.
    /// </summary>
    [Test]
    public void ReadIntAtStaticAddressTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialIntValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetAnchor{T}(PointerPath)"/> with a pointer path to the int value in the target
    /// app and reads the value. The result should be the initial int value.
    /// </summary>
    [Test]
    public void ReadIntAtPointerPathTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetPointerPathForValueAtIndex(IndexOfOutputInt));
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialIntValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetAnchor{T}(UIntPtr)"/> with the static address of the int value in the target
    /// app. Before the app outputs values, writes a new int value. The output should contain the value written.
    /// </summary>
    [Test]
    public void WriteIntTest()
    {
        ProceedToNextStep();
        int newValue = 1234567;
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var writeResult = anchor.Write(newValue);
        Assert.That(writeResult.IsSuccess, Is.True);
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputInt, newValue.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.GetAnchor{T}(UIntPtr)"/> with an address of 1, which is not readable.
    /// When trying to read the value, the result should be a <see cref="OperatingSystemCallFailure"/>.
    /// </summary>
    [Test]
    public void ReadIntWithOutOfRangeAddressTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(1);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.False);
        Assert.That(readResult.Failure, Is.InstanceOf<OperatingSystemCallFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetAnchor{T}(UIntPtr)"/> with an address of 1, which is not writeable.
    /// When trying to write the value, the result should be a <see cref="OperatingSystemCallFailure"/>.
    /// </summary>
    [Test]
    public void WriteIntWithOutOfRangeAddressTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(1);
        var writeResult = anchor.Write(1234567);
        Assert.That(writeResult.IsSuccess, Is.False);
        Assert.That(writeResult.Failure, Is.InstanceOf<OperatingSystemCallFailure>());
    }
    
    /// <summary>
    /// Tests the Freeze method of the ValueAnchor.
    /// Before the app outputs values, freezes the int value to 1234567. The output should contain the frozen value,
    /// even though the app changes the value before outputting it.
    /// </summary>
    [Test]
    public void FreezeIntTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        using var freezer = anchor.Freeze(1234567);
        ProceedToNextStep();
        Thread.Sleep(100); // Make sure the freezer has time to write the value to make the test consistent
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputInt, "1234567");
    }

    /// <summary>
    /// Tests the Dispose method of the default freezer implementation.
    /// Before the app outputs values, freezes the int value to 1234567, and immediately dispose the freezer. The output
    /// should contain the expected output value of the target app.
    /// </summary>
    [Test]
    public void FreezeIntAndDisposeFreezerTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var freezer = anchor.Freeze(1234567);
        freezer.Dispose();
        ProceedToNextStep();
        ProceedToNextStep();
        AssertExpectedFinalResults();
    }
    
    /// <summary>
    /// Tests <see cref="ThreadValueFreezer{TValue}"/> (the thread-based freezer implementation).
    /// </summary>
    [Test]
    public void FreezeIntThreadTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        using var freezer = new ThreadValueFreezer<int>(anchor, 1234567);
        ProceedToNextStep();
        Thread.Sleep(100); // Make sure the freezer has time to write the value to make the test consistent
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputInt, "1234567");
    }
    
    /// <summary>
    /// Tests the Dispose method of <see cref="ThreadValueFreezer{TValue}"/> (the thread-based freezer implementation).
    /// </summary>
    [Test]
    public void FreezeIntThreadAndDisposeTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var freezer = new ThreadValueFreezer<int>(anchor, 1234567);
        freezer.Dispose();
        ProceedToNextStep();
        ProceedToNextStep();
        AssertExpectedFinalResults();
    }

    /// <summary>
    /// Freezes an unreadable value, and check that the freezer raises the
    /// <see cref="TimerValueFreezer{TValue}.FreezeFailed"/> event.
    /// </summary>
    [Test]
    public void FreezeFailureTest()
    {
        var anchor = TestProcessMemory!.GetAnchor<int>(1);
        using var freezer = anchor.Freeze(1234567);

        List<Failure> failures = [];
        freezer.FreezeFailed += (_, args) =>
        {
            failures.Add(args.Failure);
        };
        Thread.Sleep(3000);
        Assert.That(failures, Has.Count.GreaterThan(0));
    }
    
    #endregion
    
    #region Byte array anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetByteArrayAnchor(UIntPtr,int)"/> with the static address of the output byte
    /// array in the target app and reads the value. The result should be the initial byte array.
    /// </summary>
    [Test]
    public void ReadBytesAtStaticAddressTest()
    {
        var address = TestProcessMemory!.EvaluateMemoryAddress(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray)).Value;
        var anchor = TestProcessMemory.GetByteArrayAnchor(address, InitialByteArrayValue.Length);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialByteArrayValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetByteArrayAnchor(PointerPath,int)"/> with a pointer path to the output byte
    /// array in the target app and reads the value. The result should be the initial byte array.
    /// </summary>
    [Test]
    public void ReadBytesAtPointerPathTest()
    {
        var anchor = TestProcessMemory!.GetByteArrayAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray), InitialByteArrayValue.Length);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialByteArrayValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetByteArrayAnchor(PointerPath,int)"/> with a pointer path to the output byte
    /// array in the target app. Before the app outputs values, writes a new value. The output should contain the new
    /// value.
    /// </summary>
    [Test]
    public void WriteBytesTest()
    {
        ProceedToNextStep();
        var newValue = new byte[] { 14, 24, 34, 44 };
        var anchor = TestProcessMemory!.GetByteArrayAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray), InitialByteArrayValue.Length);
        var writeResult = anchor.Write(newValue);
        Assert.That(writeResult.IsSuccess, Is.True);
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputByteArray, "14,24,34,44");
    }
    
    #endregion
    
    #region String anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetStringPointerAnchor(UIntPtr,StringSettings)"/> with the static address of the
    /// output string pointer in the target app and reads the value. The result should be the initial string.
    /// </summary>
    [Test]
    public void ReadStringPointerAtStaticAddressTest()
    {
        var anchor = TestProcessMemory!.GetStringPointerAnchor(
            GetAddressForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialStringValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetStringPointerAnchor(PointerPath,StringSettings)"/> with a pointer path to
    /// the output string pointer in the target app and reads the value. The result should be the initial string.
    /// </summary>
    [Test]
    public void ReadStringPointerAtPointerPathTest()
    {
        var anchor = TestProcessMemory!.GetStringPointerAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialStringValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.GetStringPointerAnchor(UIntPtr,StringSettings)"/> with the static address of the
    /// output string pointer in the target app. Before the app outputs values, writes a new value. The operation should
    /// fail because it is not supported.
    /// </summary>
    [Test]
    public void WriteStringPointerTest()
    {
        ProceedToNextStep();
        var anchor = TestProcessMemory!.GetStringPointerAnchor(
            GetAddressForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var writeResult = anchor.Write("Hello, world!");
        Assert.That(writeResult.IsSuccess, Is.False);
    }
    
    #endregion
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryAnchorTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAnchorTestX86 : ProcessMemoryAnchorTest
{
    /// <summary>Gets a value indicating if the process is 64-bit.</summary>
    protected override bool Is64Bit => false;
}