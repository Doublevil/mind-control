using System.Globalization;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>Tests the methods of <see cref="ProcessMemory"/> related to value anchors.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAnchorTest : BaseProcessMemoryTest
{
    #region Primitive type anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterAnchor{T}(UIntPtr)"/> with the static address of the int value in the
    /// target app and reads the value. The result should be the initial int value.
    /// </summary>
    [Test]
    public void ReadIntAtStaticAddressTest()
    {
        using var anchor = TestProcessMemory!.RegisterAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialIntValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterAnchor{T}(PointerPath)"/> with a pointer path to the int value in the
    /// target app and reads the value. The result should be the initial int value.
    /// </summary>
    [Test]
    public void ReadIntAtPointerPathTest()
    {
        using var anchor = TestProcessMemory!.RegisterAnchor<int>(GetPointerPathForValueAtIndex(IndexOfOutputInt));
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialIntValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterAnchor{T}(UIntPtr)"/> with the static address of the int value in the
    /// target app. Before the app outputs values, writes a new int value. The output should contain the value written.
    /// </summary>
    [Test]
    public void WriteIntTest()
    {
        ProceedToNextStep();
        int newValue = 1234567;
        using var anchor = TestProcessMemory!.RegisterAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
        var writeResult = anchor.Write(newValue);
        Assert.That(writeResult.IsSuccess, Is.True);
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputInt, newValue.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterAnchor{T}(UIntPtr)"/> with an address of 1, which is not readable.
    /// When trying to read the value, the result should be a <see cref="ReadFailureOnSystemRead"/>.
    /// </summary>
    [Test]
    public void ReadIntWithOutOfRangeAddressTest()
    {
        using var anchor = TestProcessMemory!.RegisterAnchor<int>(1);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.False);
        Assert.That(readResult.Error, Is.InstanceOf<ReadFailureOnSystemRead>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterAnchor{T}(UIntPtr)"/> with an address of 1, which is not writeable.
    /// When trying to write the value, the result should be a <see cref="WriteFailureOnSystemWrite"/>.
    /// </summary>
    [Test]
    public void WriteIntWithOutOfRangeAddressTest()
    {
        using var anchor = TestProcessMemory!.RegisterAnchor<int>(1);
        var writeResult = anchor.Write(1234567);
        Assert.That(writeResult.IsSuccess, Is.False);
        Assert.That(writeResult.Error, Is.InstanceOf<WriteFailureOnSystemWrite>());
    }
    
    #endregion
    
    #region Byte array anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterByteArrayAnchor(UIntPtr,int)"/> with the static address of the output
    /// byte array in the target app and reads the value. The result should be the initial byte array.
    /// </summary>
    [Test]
    public void ReadBytesAtStaticAddressTest()
    {
        var address = TestProcessMemory!.EvaluateMemoryAddress(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray)).Value;
        using var anchor = TestProcessMemory.RegisterByteArrayAnchor(address, InitialByteArrayValue.Length);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialByteArrayValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterByteArrayAnchor(PointerPath,int)"/> with a pointer path to the output
    /// byte array in the target app and reads the value. The result should be the initial byte array.
    /// </summary>
    [Test]
    public void ReadBytesAtPointerPathTest()
    {
        using var anchor = TestProcessMemory!.RegisterByteArrayAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray), InitialByteArrayValue.Length);
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialByteArrayValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterByteArrayAnchor(PointerPath,int)"/> with a pointer path to the output
    /// byte array in the target app. Before the app outputs values, writes a new value. The output should contain the
    /// new value.
    /// </summary>
    [Test]
    public void WriteBytesTest()
    {
        ProceedToNextStep();
        var newValue = new byte[] { 14, 24, 34, 44 };
        using var anchor = TestProcessMemory!.RegisterByteArrayAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputByteArray), InitialByteArrayValue.Length);
        var writeResult = anchor.Write(newValue);
        Assert.That(writeResult.IsSuccess, Is.True);
        ProceedToNextStep();
        AssertFinalResults(IndexOfOutputByteArray, "14,24,34,44");
    }
    
    #endregion
    
    #region String anchors
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterStringPointerAnchor(UIntPtr,StringSettings)"/> with the static address of
    /// the output string pointer in the target app and reads the value. The result should be the initial string.
    /// </summary>
    [Test]
    public void ReadStringPointerAtStaticAddressTest()
    {
        using var anchor = TestProcessMemory!.RegisterStringPointerAnchor(
            GetAddressForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialStringValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterStringPointerAnchor(PointerPath,StringSettings)"/> with a pointer path to
    /// the output string pointer in the target app and reads the value. The result should be the initial string.
    /// </summary>
    [Test]
    public void ReadStringPointerAtPointerPathTest()
    {
        using var anchor = TestProcessMemory!.RegisterStringPointerAnchor(
            GetPointerPathForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var readResult = anchor.Read();
        Assert.That(readResult.IsSuccess, Is.True);
        Assert.That(readResult.Value, Is.EqualTo(InitialStringValue));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RegisterStringPointerAnchor(UIntPtr,StringSettings)"/> with the static address of
    /// the output string pointer in the target app. Before the app outputs values, writes a new value. The operation
    /// should fail because it is not supported.
    /// </summary>
    [Test]
    public void WriteStringPointerTest()
    {
        ProceedToNextStep();
        var newValue = "Hello, world!";
        using var anchor = TestProcessMemory!.RegisterStringPointerAnchor(
            GetAddressForValueAtIndex(IndexOfOutputString), GetDotNetStringSettings());
        var writeResult = anchor.Write(newValue);
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