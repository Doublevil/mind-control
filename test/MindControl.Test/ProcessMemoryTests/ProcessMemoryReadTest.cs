using System.Text;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the memory reading methods of <see cref="ProcessMemory"/>.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryReadTest : BaseProcessMemoryTest
{
    #region Bytes reading
    
    #region ReadBytes
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(PointerPath,ulong)"/>.
    /// Reads a known byte array from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadBytesTest()
    {
        PointerPath pointerPath = GetPointerPathForValueAtIndex(IndexOfOutputByteArray);
        Assert.That(TestProcessMemory!.ReadBytes(pointerPath, 4).GetValueOrDefault(),
            Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 0x44 }));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.ReadBytes(pointerPath, 4).GetValueOrDefault(),
            Is.EqualTo(new byte[] { 0x55, 0x66, 0x77, 0x88 }));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(UIntPtr,ulong)"/> with a zero pointer.
    /// Expect the result to be a <see cref="ReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadBytesWithZeroPointerTest()
    {
        var result = TestProcessMemory!.ReadBytes(UIntPtr.Zero, 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(UIntPtr,ulong)"/> with an unreadable address.
    /// Expect the result to be a <see cref="ReadFailureOnSystemRead"/>.
    /// </summary>
    [Test]
    public void ReadBytesWithUnreadableAddressTest()
    {
        var result = TestProcessMemory!.ReadBytes(GetMaxPointerValue(), 1);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
        var systemError = ((ReadFailureOnSystemRead)result.Error).Details;
        Assert.That(systemError, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(PointerPath,ulong)"/> with a bad pointer path.
    /// Expect the result to be a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadBytesWithInvalidPathTest()
    {
        var result = TestProcessMemory!.ReadBytes("bad pointer path", 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
        var pathError = ((ReadFailureOnPointerPathEvaluation)result.Error).Details;
        Assert.That(pathError, Is.Not.Null);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(PointerPath,ulong)"/> with a zero length.
    /// Expect the result to be an empty array of bytes.
    /// </summary>
    [Test]
    public void ReadBytesWithZeroLengthTest()
    {
        var result = TestProcessMemory!.ReadBytes(OuterClassPointer, 0);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(UIntPtr,ulong)"/> with a negative length.
    /// Expect the result to be a <see cref="ReadFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReadBytesWithNegativeLengthTest()
    {
        var result = TestProcessMemory!.ReadBytes(OuterClassPointer, -4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnInvalidArguments)));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(UIntPtr,ulong)"/> with an address and length that would make the read
    /// start on a readable address but end on an unreadable one.
    /// Expect the result to be a <see cref="ReadFailureOnSystemRead"/>. 
    /// </summary>
    /// <remarks>This is what <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> is for.</remarks>
    [Test]
    public void ReadBytesOnPartiallyUnreadableRangeTest()
    {
        // Prepare a segment of memory that is isolated from other memory regions, and get an address near the edge.
        var allocatedMemory = TestProcessMemory!.Allocate(0x1000, false).Value;
        var targetAddress = allocatedMemory.Range.End - 4;

        // Read 8 bytes, which should result in reading 4 bytes from the readable region and 4 bytes from the unreadable
        // one.
        var result = TestProcessMemory.ReadBytes(targetAddress, 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(UIntPtr,ulong)"/> with a detached process.
    /// Expect the result to be a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadBytesOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadBytes(OuterClassPointer, 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(PointerPath,ulong)"/> with a detached process.
    /// Expect the result to be a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadBytesWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadBytes(OuterClassPointer.ToString("X"), 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    #endregion
    
    #region ReadBytesPartial
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(PointerPath,byte[],ulong)"/>.
    /// Reads a known byte array from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadBytesPartialTest()
    {
        PointerPath pointerPath = GetPointerPathForValueAtIndex(IndexOfOutputByteArray);
        var firstBuffer = new byte[4];
        var firstResult = TestProcessMemory!.ReadBytesPartial(pointerPath, firstBuffer, 4);
        ProceedToNextStep();
        var secondBuffer = new byte[4];
        var secondResult = TestProcessMemory.ReadBytesPartial(pointerPath, secondBuffer, 4);

        Assert.That(firstResult.IsSuccess);
        Assert.That(firstResult.Value, Is.EqualTo(4));
        Assert.That(firstBuffer, Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 0x44 }));
        
        Assert.That(secondResult.IsSuccess);
        Assert.That(secondResult.Value, Is.EqualTo(4));
        Assert.That(secondBuffer, Is.EqualTo(new byte[] { 0x55, 0x66, 0x77, 0x88 }));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> with a zero pointer.
    /// Expect the result to be a <see cref="ReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithZeroPointerTest()
    {
        var result = TestProcessMemory!.ReadBytesPartial(UIntPtr.Zero, new byte[4], 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> with an unreadable address.
    /// Expect the result to be a <see cref="ReadFailureOnSystemRead"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithUnreadableAddressTest()
    {
        var result = TestProcessMemory!.ReadBytesPartial(GetMaxPointerValue(), new byte[1], 1);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
        var systemError = ((ReadFailureOnSystemRead)result.Error).Details;
        Assert.That(systemError, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(PointerPath,byte[],ulong)"/> with a bad pointer path.
    /// Expect the result to be a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithInvalidPathTest()
    {
        var result = TestProcessMemory!.ReadBytesPartial("bad pointer path", new byte[4], 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
        var pathError = ((ReadFailureOnPointerPathEvaluation)result.Error).Details;
        Assert.That(pathError, Is.Not.Null);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(PointerPath,byte[],ulong)"/> with a zero length.
    /// Expect the result to be an empty array of bytes.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithZeroLengthTest()
    {
        var result = TestProcessMemory!.ReadBytesPartial(OuterClassPointer, [], 0);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Zero);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> with a length exceeding the buffer
    /// capacity.
    /// Expect the result to be a <see cref="ReadFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithExcessiveLengthTest()
    {
        var result = TestProcessMemory!.ReadBytesPartial(OuterClassPointer, new byte[4], 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnInvalidArguments)));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> with an address and length that would
    /// make the read start on a readable address but end on an unreadable one.
    /// Expect the result to be 4 (as in 4 bytes read) and the buffer to contain the 4 bytes that were readable. 
    /// </summary>
    [Test]
    public void ReadBytesPartialOnPartiallyUnreadableRangeTest()
    {
        // Prepare a segment of memory that is isolated from other memory regions, and has a known sequence of bytes
        // at the end.
        var bytesAtTheEnd = new byte[] { 0x1, 0x2, 0x3, 0x4 };
        var allocatedMemory = TestProcessMemory!.Allocate(0x1000, false).Value;
        var targetAddress = allocatedMemory.Range.End - 4;
        var writeResult = TestProcessMemory.WriteBytes(targetAddress, bytesAtTheEnd, MemoryProtectionStrategy.Ignore);
        Assert.That(writeResult.IsSuccess, Is.True);

        // Read 8 bytes, which should result in reading 4 bytes from the readable region and 4 bytes from the unreadable
        // one.
        var buffer = new byte[8];
        var result = TestProcessMemory.ReadBytesPartial(targetAddress, buffer, 8);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0x1, 0x2, 0x3, 0x4, 0, 0, 0, 0 }));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(UIntPtr,byte[],ulong)"/> with a detached process.
    /// Expect the result to be a <see cref="ReadFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadBytesPartial(OuterClassPointer, new byte[4], 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytesPartial(PointerPath,byte[],ulong)"/> with a detached process.
    /// Expect the result to be a <see cref="ReadFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReadBytesPartialWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadBytesPartial(OuterClassPointer.ToString("X"), new byte[4], 4);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    #endregion
    
    #endregion
    
    #region Primitive type reading
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read(Type,UIntPtr)"/>.
    /// Reads a first time after initialization, and then a second time after value are modified.
    /// The values are expected to match the specified ones.
    /// </summary>
    /// <param name="targetType">Type of data to read.</param>
    /// <param name="valueIndexInOutput">Index of the value to read, by order of output in the target app.</param>
    /// <param name="expectedResultBeforeBreak">Expected value on the first read (after initialization).</param>
    /// <param name="expectedResultAfterBreak">Expected value on the second read (after modification).</param>
    [TestCase(typeof(bool), IndexOfOutputBool, InitialBoolValue, ExpectedFinalBoolValue)]
    [TestCase(typeof(byte), IndexOfOutputByte, InitialByteValue, ExpectedFinalByteValue)]
    [TestCase(typeof(short), IndexOfOutputShort, InitialShortValue, ExpectedFinalShortValue)]
    [TestCase(typeof(ushort), IndexOfOutputUShort, InitialUShortValue, ExpectedFinalUShortValue)]
    [TestCase(typeof(int), IndexOfOutputInt, InitialIntValue, ExpectedFinalIntValue)]
    [TestCase(typeof(uint), IndexOfOutputUInt, InitialUIntValue, ExpectedFinalUIntValue)]
    [TestCase(typeof(long), IndexOfOutputLong, InitialLongValue, ExpectedFinalLongValue)]
    [TestCase(typeof(ulong), IndexOfOutputULong, InitialULongValue, ExpectedFinalULongValue)]
    [TestCase(typeof(float), IndexOfOutputFloat, InitialFloatValue, ExpectedFinalFloatValue)]
    [TestCase(typeof(double), IndexOfOutputDouble, InitialDoubleValue, ExpectedFinalDoubleValue)]
    public void ReadTwoStepGenericTest(Type targetType, int valueIndexInOutput, object? expectedResultBeforeBreak,
        object? expectedResultAfterBreak)
    {
        UIntPtr targetIntAddress = GetAddressForValueAtIndex(valueIndexInOutput);
        object resultBefore = TestProcessMemory!.Read(targetType, targetIntAddress).Value;
        Assert.That(resultBefore, Is.EqualTo(expectedResultBeforeBreak));
        ProceedToNextStep();
        object resultAfter = TestProcessMemory.Read(targetType, targetIntAddress).Value;
        Assert.That(resultAfter, Is.EqualTo(expectedResultAfterBreak));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known boolean from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadBoolTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputBool);
        Assert.That(TestProcessMemory!.Read<bool>(address).GetValueOrDefault(), Is.EqualTo(InitialBoolValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<bool>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalBoolValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known byte from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadByteTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputByte);
        Assert.That(TestProcessMemory!.Read<byte>(address).GetValueOrDefault(), Is.EqualTo(InitialByteValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<byte>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalByteValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known short from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadShortTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputShort);
        Assert.That(TestProcessMemory!.Read<short>(address).GetValueOrDefault(), Is.EqualTo(InitialShortValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<short>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalShortValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned short from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadUShortTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputUShort);
        Assert.That(TestProcessMemory!.Read<ushort>(address).GetValueOrDefault(), Is.EqualTo(InitialUShortValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<ushort>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalUShortValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known integer from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadIntTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputInt);
        Assert.That(TestProcessMemory!.Read<int>(address).GetValueOrDefault(), Is.EqualTo(InitialIntValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<int>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalIntValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned integer from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadUIntTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputUInt);
        Assert.That(TestProcessMemory!.Read<uint>(address).GetValueOrDefault(), Is.EqualTo(InitialUIntValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<uint>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalUIntValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known long from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadLongTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputLong);
        Assert.That(TestProcessMemory!.Read<long>(address).GetValueOrDefault(), Is.EqualTo(InitialLongValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<long>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalLongValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned long from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadULongTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputULong);
        Assert.That(TestProcessMemory!.Read<ulong>(address).GetValueOrDefault(), Is.EqualTo(InitialULongValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<ulong>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalULongValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known float from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadFloatTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputFloat);
        Assert.That(TestProcessMemory!.Read<float>(address).GetValueOrDefault(), Is.EqualTo(InitialFloatValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<float>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalFloatValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known double from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadDoubleTest()
    {
        var address = GetAddressForValueAtIndex(IndexOfOutputDouble);
        Assert.That(TestProcessMemory!.Read<double>(address).GetValueOrDefault(), Is.EqualTo(InitialDoubleValue));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<double>(address).GetValueOrDefault(), Is.EqualTo(ExpectedFinalDoubleValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(PointerPath)"/>.
    /// Reads a known long from the target process before and after the process changes their value.
    /// This method uses a <see cref="PointerPath"/> and reads a value that is nested in the known instance.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadNestedLongTest() => Assert.That(
        TestProcessMemory!.Read<long>(GetPointerPathForValueAtIndex(IndexOfOutputInnerLong)).GetValueOrDefault(),
        Is.EqualTo(InitialInnerLongValue));

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(PointerPath)"/>.
    /// Reads an ulong with the max value from the target process before and after the process changes their value.
    /// This method uses a <see cref="PointerPath"/> and reads a value that is nested in the known instance.
    /// Reading this value as a pointer should yield a valid pointer. This test validates that there is no arithmetic
    /// overflow caused by signed pointer usage.
    /// </summary>
    [Test]
    public void ReadUIntPtrMaxValueTest()
    {
        var ptr = TestProcessMemory!.Read<UIntPtr>(GetPathToPointerToMaxAddress()).GetValueOrDefault();
        Assert.That(ptr, Is.EqualTo(GetMaxPointerValue()));
    }

    /// <summary>
    /// Tests an edge case where the pointer path given to a read operation points to the last possible byte in memory
    /// (the maximum value of a UIntPtr).
    /// The read operation is expected to fail with a <see cref="ReadFailureOnSystemRead"/> (this memory region is not
    /// readable).
    /// </summary>
    [Test]
    public void ReadAtMaxPointerValueTest()
    {
        var result = TestProcessMemory!.Read<byte>(GetMaxPointerValue());
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
        var systemError = ((ReadFailureOnSystemRead)error).Details;
        Assert.That(systemError, Is.TypeOf(typeof(OperatingSystemCallFailure)));
        var osError = (OperatingSystemCallFailure)systemError;
        Assert.That(osError.ErrorCode, Is.GreaterThan(0));
        Assert.That(osError.ErrorMessage, Is.Not.Empty);
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read(Type, UIntPtr)"/> method with a reference type.
    /// It should fail with a <see cref="ReadFailureOnUnsupportedType"/>.
    /// </summary>
    [Test]
    public void ReadIncompatibleTypeTest()
    {
        var result = TestProcessMemory!.Read(typeof(string), OuterClassPointer + 0x38);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnUnsupportedType)));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read{T}(PointerPath)"/> method with a pointer path that fails to evaluate.
    /// It should fail with a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.Read<long>("bad pointer path");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read{T}(UIntPtr)"/> method with a zero pointer.
    /// It should fail with a <see cref="ReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadWithZeroPointerTest()
    {
        var result = TestProcessMemory!.Read<long>(UIntPtr.Zero);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read{T}(UIntPtr)"/> method with a detached process.
    /// It should fail with a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Read<long>(OuterClassPointer);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read{T}(UIntPtr)"/> method with a detached process.
    /// It should fail with a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Read<long>(OuterClassPointer.ToString("X"));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read(Type,PointerPath)"/> method with a pointer path that fails to evaluate.
    /// It should fail with a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadObjectWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.Read(typeof(long), "bad pointer path");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read(Type,UIntPtr)"/> method with a zero pointer.
    /// It should fail with a <see cref="ReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadObjectWithZeroPointerTest()
    {
        var result = TestProcessMemory!.Read(typeof(long), UIntPtr.Zero);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read(Type,UIntPtr)"/> method with a detached process.
    /// It should fail with a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadObjectOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Read(typeof(long), OuterClassPointer);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read(Type,UIntPtr)"/> method with a detached process.
    /// It should fail with a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadObjectWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Read(typeof(long), OuterClassPointer.ToString("X"));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }

    #endregion
    
    #region Structure reading
    
    /// <summary>A struct with a couple fields, to test reading structs.</summary>
    public record struct TestStruct(long A, ulong B);
    
    /// <summary>Structure used to test reading a structure with references.</summary>
    private record struct TestStructWithReferences(int A, string B);
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(PointerPath)"/>.
    /// Reads a known struct from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadStructureTest()
    {
        var pointerPath = GetPointerPathForValueAtIndex(IndexOfOutputLong);
        Assert.That(TestProcessMemory!.Read<TestStruct>(pointerPath).Value,
            Is.EqualTo(new TestStruct(InitialLongValue, InitialULongValue)));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<TestStruct>(pointerPath).Value,
            Is.EqualTo(new TestStruct(ExpectedFinalLongValue, ExpectedFinalULongValue)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read(Type, PointerPath)"/>.
    /// Reads a known struct from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadStructureAsObjectTest()
    {
        var pointerPath = GetPointerPathForValueAtIndex(IndexOfOutputLong);
        Assert.That(TestProcessMemory!.Read(typeof(TestStruct), pointerPath).Value,
            Is.EqualTo(new TestStruct(InitialLongValue, InitialULongValue)));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read(typeof(TestStruct), pointerPath).Value,
            Is.EqualTo(new TestStruct(ExpectedFinalLongValue, ExpectedFinalULongValue)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Read{T}(UIntPtr)"/> method with a structure type that contains reference
    /// types.
    /// It should fail with a <see cref="ReadFailureOnConversionFailure"/>.
    /// </summary>
    [Test]
    public void ReadStructureWithReferencesTest()
    {
        var result = TestProcessMemory!.Read<TestStructWithReferences>(OuterClassPointer + 0x38);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnConversionFailure)));
    }
    
    #endregion
    
    #region String reading
    
    /// <summary>Test case for tests using string settings.</summary>
    public record StringSettingsTestCase(Encoding Encoding, string String, bool IsNullTerminated,
        StringLengthPrefix? LengthPrefix, byte[]? TypePrefix);

    private static readonly byte[] ExampleTypePrefix = [0x11, 0x22, 0x33, 0x44];
    
    private static readonly StringSettingsTestCase[] StringSettingsTestCases = {
        new(Encoding.Unicode, "Simple string", true, null, null),
        new(Encoding.UTF8, "Simple string", true, null, null),
        new(Encoding.UTF8, "String with diàcrîtìçs", true, null, null),
        new(Encoding.Latin1, "String with diàcrîtìçs", true, null, null),
        new(Encoding.Latin1, "é", true, null, null),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", true, null, null),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", true, new StringLengthPrefix(4, StringLengthUnit.Bytes), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, new StringLengthPrefix(4, StringLengthUnit.Bytes), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, new StringLengthPrefix(4, StringLengthUnit.Characters), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, new StringLengthPrefix(2, StringLengthUnit.Characters), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, new StringLengthPrefix(1, StringLengthUnit.Characters), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", false, new StringLengthPrefix(4, StringLengthUnit.Bytes), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", false, new StringLengthPrefix(4, StringLengthUnit.Characters), null),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, new StringLengthPrefix(4, StringLengthUnit.Bytes), ExampleTypePrefix),
        new(Encoding.UTF8, "Mµl十ÿहि鬱", true, null, ExampleTypePrefix),
    };
    
    #region FindStringSettings
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> using predefined test cases.
    /// The methodology is to allocate some memory, write a string to it using the parameters defined in the test case,
    /// and then call the tested method on it.
    /// The result is expected to match the test case settings.
    /// </summary>
    /// <remarks>This test depends on allocation methods and writing methods. If all test cases fail, check the tests
    /// for these features first.</remarks>
    [TestCaseSource(nameof(StringSettingsTestCases))]
    public void FindStringSettingsTest(StringSettingsTestCase testCase)
    {
        var settings = new StringSettings(testCase.Encoding)
        {
            IsNullTerminated = testCase.IsNullTerminated,
            LengthPrefix = testCase.LengthPrefix,
            TypePrefix = testCase.TypePrefix
        };

        // Allocate some memory to write the string.
        var allocatedSpace = TestProcessMemory!.Allocate(1024, false).Value.ReserveRange(1024).Value;
        byte[] bytes = settings.GetBytes(testCase.String)
            ?? throw new ArgumentException("The test case is invalid: the length prefix is too short for the string.");
        
        // Fill the allocated space with FF bytes to prevent unexpected null termination results
        TestProcessMemory.WriteBytes(allocatedSpace.Address, Enumerable.Repeat((byte)255, 1024).ToArray());
        
        // Write the string to the allocated space. Because the FindStringSettings expects the address of a pointer
        // to the string, we write the string with an offset of 8, and then we write the first 8 bytes to point to
        // the address where we wrote the string.
        TestProcessMemory.Write(allocatedSpace.Address + 8, bytes);
        TestProcessMemory.Write(allocatedSpace.Address, allocatedSpace.Address + 8);
        
        // Call the tested method on the string pointer (the one we wrote last)
        var findSettingsResult = TestProcessMemory.FindStringSettings(allocatedSpace.Address, testCase.String);
        Assert.That(findSettingsResult.IsSuccess, Is.True);
        
        // Check that the settings match the test case, i.e. that the determined settings are the same settings we
        // used to write the string in memory.
        var foundSettings = findSettingsResult.Value;
        Assert.Multiple(() =>
        {
            Assert.That(foundSettings.Encoding, Is.EqualTo(testCase.Encoding));
            Assert.That(foundSettings.IsNullTerminated, Is.EqualTo(testCase.IsNullTerminated));
            Assert.That(foundSettings.LengthPrefix?.Size, Is.EqualTo(testCase.LengthPrefix?.Size));
            Assert.That(foundSettings.TypePrefix, Is.EqualTo(testCase.TypePrefix));
        });
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(PointerPath,string)"/> on a known string in the target
    /// process.
    /// </summary>
    [Test]
    public void FindStringSettingsOnKnownPathTest()
    {
        var dotNetStringSettings = GetDotNetStringSettings();
        var result = TestProcessMemory!.FindStringSettings(GetPointerPathForValueAtIndex(IndexOfOutputString),
            InitialStringValue);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Encoding, Is.EqualTo(dotNetStringSettings.Encoding));
        Assert.That(result.Value.IsNullTerminated, Is.EqualTo(dotNetStringSettings.IsNullTerminated));
        Assert.That(result.Value.LengthPrefix?.Size,
            Is.EqualTo(dotNetStringSettings.LengthPrefix?.Size));
        Assert.That(result.Value.LengthPrefix?.Unit, Is.EqualTo(dotNetStringSettings.LengthPrefix?.Unit));
        // For the type prefix, we only check the length, because the actual value is dynamic.
        Assert.That(result.Value.TypePrefix?.Length, Is.EqualTo(dotNetStringSettings.TypePrefix?.Length));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> with a valid address but the wrong
    /// expected string.
    /// The method should return a <see cref="FindStringSettingsFailureOnNoSettingsFound"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsWithWrongStringTest()
    {
        var result = TestProcessMemory!.FindStringSettings(GetStringPointerAddress(), "Wrong string");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnNoSettingsFound)));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> with a valid address but an empty
    /// expected string.
    /// The method should return a <see cref="FindStringSettingsFailureOnNoSettingsFound"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsWithEmptyStringTest()
    {
        var result = TestProcessMemory!.FindStringSettings(GetStringPointerAddress(), "");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnNoSettingsFound)));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> on a zero pointer address.
    /// The method should return a <see cref="FindStringSettingsFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsOnZeroPointerTest()
    {
        // We do not have a known zero pointer address, so we are going to allocate some memory and point to it.
        var allocatedSpace = TestProcessMemory!.Allocate(8, false).Value.ReserveRange(8).Value;
        var result = TestProcessMemory!.FindStringSettings(allocatedSpace.Address, "Whatever");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnZeroPointer)));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> with a pointer at the maximum possible
    /// address, which is invalid.
    /// The method should return a <see cref="FindStringSettingsFailureOnPointerReadFailure"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsOnInvalidPointerAddressTest()
    {
        var result = TestProcessMemory!.FindStringSettings(GetMaxPointerValue(), "Whatever");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnPointerReadFailure)));
        var failure = (FindStringSettingsFailureOnPointerReadFailure)result.Error;
        Assert.That(failure.Details, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(PointerPath,string)"/> with a pointer to the maximum possible
    /// address, which is invalid.
    /// The method should return a <see cref="FindStringSettingsFailureOnStringReadFailure"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsOnInvalidStringAddressTest()
    {
        // Use a known path that should cause the string address to be 0xFFFFFFFFFFFFFFFF (x64) or 0xFFFFFFFF (x86).
        PointerPath pointerPath = GetPathToPointerToMaxAddress();
        var result = TestProcessMemory!.FindStringSettings(pointerPath, "Whatever");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnStringReadFailure)));
        var failure = (FindStringSettingsFailureOnStringReadFailure)result.Error;
        Assert.That(failure.Details, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(PointerPath,string)"/> with a pointer path that cannot be
    /// evaluated.
    /// The method should return a <see cref="FindStringSettingsFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsOnInvalidPointerPathTest()
    {
        var result = TestProcessMemory!.FindStringSettings("bad pointer path", "Whatever");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnPointerPathEvaluation)));
        var failure = (FindStringSettingsFailureOnPointerPathEvaluation)result.Error;
        Assert.That(failure.Details, Is.Not.Null);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(UIntPtr,string)"/> with a detached process.
    /// The method should return a <see cref="FindStringSettingsFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.FindStringSettings(0x1234, InitialStringValue);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.FindStringSettings(PointerPath,string)"/> with a detached process.
    /// The method should return a <see cref="FindStringSettingsFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void FindStringSettingsWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.FindStringSettings("1234", InitialStringValue);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(FindStringSettingsFailureOnDetachedProcess)));
    }
    
    #endregion

    #region ReadRawString

    /// <summary>Test case for <see cref="ProcessMemoryReadTest.ReadRawStringTest"/>.</summary>
    public record ReadRawStringTestCase(Encoding Encoding, string String, bool IsNullTerminated, int MaxLength,
        string? ExpectedStringIfDifferent = null);
    
    private static readonly ReadRawStringTestCase[] ReadRawStringTestCases = {
        new(Encoding.Unicode, "Simple string", true, 100),
        new(Encoding.Unicode, "Simple string", true, 0, string.Empty),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", true, 10),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", false, 10, "Mµl十ÿहि鬱\0\0"),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", true, 4, "Mµl十"),
        new(Encoding.Unicode, "Mµl十ÿहि鬱", false, 4, "Mµl十"),
        new(Encoding.UTF8, "Simple string", true, 100),
        new(Encoding.UTF8, "String with diàcrîtìçs", true, 100),
        new(Encoding.Latin1, "String with diàcrîtìçs", true, 100),
    };
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(UIntPtr,Encoding,System.Nullable{int},bool)"/>.
    /// Writes a string to memory using a specific encoding, then reads it back with the tested method using the same
    /// encoding and other various parameters.
    /// The result must match the expectation of the test case.
    /// </summary>
    /// <param name="testCase">Test case defining the parameters to test.</param>
    [TestCaseSource(nameof(ReadRawStringTestCases))]
    public void ReadRawStringTest(ReadRawStringTestCase testCase)
    {
        var reservedMemory = TestProcessMemory!.Allocate(0x1000, false).Value.ReserveRange(0x1000).Value;
        byte[] bytes = testCase.Encoding.GetBytes(testCase.String);
        TestProcessMemory.Write(reservedMemory.Address, bytes);
        
        var result = TestProcessMemory.ReadRawString(reservedMemory.Address, testCase.Encoding,
            testCase.MaxLength, testCase.IsNullTerminated);
        Assert.That(result.IsSuccess, Is.True);
        var resultString = result.Value;
        
        var expectedString = testCase.ExpectedStringIfDifferent ?? testCase.String;
        Assert.That(resultString, Is.EqualTo(expectedString));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(PointerPath,Encoding,System.Nullable{int},bool)"/>.
    /// Calls the method with a pointer path that goes through a known string pointer, before and after the string
    /// pointer is modified.
    /// Expect the result to match the known strings in both cases.
    /// </summary>
    [Test]
    public void ReadRawStringWithKnownStringTest()
    {
        var path = GetPathToRawStringBytes();
        var firstResult = TestProcessMemory!.ReadRawString(path, Encoding.Unicode);
        ProceedToNextStep();
        var secondResult = TestProcessMemory.ReadRawString(path, Encoding.Unicode);
        
        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value, Is.EqualTo(InitialStringValue));
        
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.Value, Is.EqualTo(ExpectedFinalStringValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(UIntPtr,Encoding,System.Nullable{int},bool)"/>.
    /// Call the method with a zero pointer as the address.
    /// Expect the result to be a <see cref="ReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithZeroPointerTest()
    {
        var result = TestProcessMemory!.ReadRawString(UIntPtr.Zero, Encoding.Unicode);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(UIntPtr,Encoding,System.Nullable{int},bool)"/>.
    /// Call the method with an unreadable address.
    /// Expect the result to be a <see cref="ReadFailureOnSystemRead"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithUnreadableAddressTest()
    {
        var result = TestProcessMemory!.ReadRawString(GetMaxPointerValue(), Encoding.Unicode);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
        var systemError = ((ReadFailureOnSystemRead)result.Error).Details;
        Assert.That(systemError, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(PointerPath,Encoding,System.Nullable{int},bool)"/>.
    /// Call the method with a negative max length.
    /// Expect the result to be a <see cref="ReadFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithNegativeMaxLengthTest()
    {
        var result = TestProcessMemory!.ReadRawString(GetPathToRawStringBytes(), Encoding.Unicode, -1);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnInvalidArguments)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(PointerPath,Encoding,System.Nullable{int},bool)"/>.
    /// Call the method with a pointer path that cannot be evaluated.
    /// Expect the result to be a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.ReadRawString("bad pointer path", Encoding.Unicode);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
        var pathError = ((ReadFailureOnPointerPathEvaluation)result.Error).Details;
        Assert.That(pathError, Is.Not.Null);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(UIntPtr,Encoding,System.Nullable{int},bool)"/> with a detached
    /// process. Expect the result to be a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadRawString(0x1234, Encoding.Unicode);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadRawString(PointerPath,Encoding,System.Nullable{int},bool)"/> with a detached
    /// process. Expect the result to be a <see cref="ReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadRawStringWithPointerPathWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadRawString("1234", Encoding.Unicode);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(ReadFailureOnDetachedProcess)));
    }
    
    #endregion
    
    #region ReadStringPointer
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> using predefined test cases.
    /// The methodology is to allocate some memory, write a string to it using the parameters defined in the test case,
    /// and then call the tested method on it.
    /// The result is expected to match the test case settings.
    /// </summary>
    /// <remarks>This test depends on allocation methods and writing methods. If all test cases fail, check the tests
    /// for these features first.</remarks>
    [TestCaseSource(nameof(StringSettingsTestCases))]
    public void ReadStringPointerTest(StringSettingsTestCase testCase)
    {
        var settings = new StringSettings(testCase.Encoding)
        {
            IsNullTerminated = testCase.IsNullTerminated,
            LengthPrefix = testCase.LengthPrefix,
            TypePrefix = testCase.TypePrefix
        };

        // Allocate some memory to write the string.
        var allocatedSpace = TestProcessMemory!.Allocate(1024, false).Value.ReserveRange(1024).Value;
        byte[] bytes = settings.GetBytes(testCase.String)
            ?? throw new ArgumentException("The test case is invalid: the length prefix is too short for the string.");
        
        // Fill the allocated space with FF bytes to prevent unexpected null termination results
        TestProcessMemory.WriteBytes(allocatedSpace.Address, Enumerable.Repeat((byte)255, 1024).ToArray());
        
        // Write the string to the allocated space. Because ReadStringPointer expects the address of a pointer to the
        // string, we write the string with an offset of 8, and then we write the first 8 bytes to point to the address
        // where we wrote the string.
        TestProcessMemory.Write(allocatedSpace.Address + 8, bytes);
        TestProcessMemory.Write(allocatedSpace.Address, allocatedSpace.Address + 8);
        
        // Call the tested method on the string pointer (the one we wrote last) and check that we got the same string
        // that we wrote previously.
        var result = TestProcessMemory.ReadStringPointer(allocatedSpace.Address, settings);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(testCase.String));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.ReadStringPointer(PointerPath,StringSettings)"/> method with a pointer path
    /// that points to a known string in the target process.
    /// The method is called before and after the string pointer is modified.
    /// The result is expected to match the known strings in both cases.
    /// </summary>
    [Test]
    public void ReadStringPointerOnKnownStringTest()
    {
        var path = GetPointerPathForValueAtIndex(IndexOfOutputString);
        var firstResult = TestProcessMemory!.ReadStringPointer(path, GetDotNetStringSettings());
        ProceedToNextStep();
        var secondResult = TestProcessMemory.ReadStringPointer(path, GetDotNetStringSettings());
        
        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value, Is.EqualTo(InitialStringValue));
        
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.Value, Is.EqualTo(ExpectedFinalStringValue));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> with a zero pointer address.
    /// Expect the result to be a <see cref="StringReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithZeroPointerTest()
    {
        var result = TestProcessMemory!.ReadStringPointer(UIntPtr.Zero, GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> with a pointer that reads 0.
    /// Expect the result to be a <see cref="StringReadFailureOnZeroPointer"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithPointerToZeroPointerTest()
    {
        // Arrange a usable space in memory that contains only zeroes so that we can get a zero pointer.
        var allocatedSpace = TestProcessMemory!.Allocate(8, false).Value.ReserveRange(8).Value;
        
        var result = TestProcessMemory!.ReadStringPointer(allocatedSpace.Address, GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnZeroPointer)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> with a zero pointer address.
    /// Expect the result to be a <see cref="StringReadFailureOnPointerReadFailure"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithUnreadablePointerTest()
    {
        var result = TestProcessMemory!.ReadStringPointer(GetMaxPointerValue(), GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnPointerReadFailure)));
        var failure = (StringReadFailureOnPointerReadFailure)result.Error;
        Assert.That(failure.Details, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(PointerPath,StringSettings)"/> with an address that points to
    /// an unreadable memory region.
    /// Expect the result to be a <see cref="StringReadFailureOnStringBytesReadFailure"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithPointerToUnreadableMemoryTest()
    {
        var result = TestProcessMemory!.ReadStringPointer(GetPathToPointerToMaxAddress(), GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnStringBytesReadFailure)));
        var failure = (StringReadFailureOnStringBytesReadFailure)result.Error;
        Assert.That(failure.Details, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> with invalid string settings.
    /// Expect the result to be a <see cref="StringReadFailureOnInvalidSettings"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithInvalidSettingsTest()
    {
        var settings = new StringSettings(Encoding.Unicode,
            isNullTerminated: false,
            lengthPrefix: null);
        var result = TestProcessMemory!.ReadStringPointer(GetAddressForValueAtIndex(IndexOfOutputString), settings);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnInvalidSettings)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(PointerPath,StringSettings)"/> with a bad pointer path.
    /// Expect the result to be a <see cref="StringReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithBadPointerPathTest()
    {
        var result = TestProcessMemory!.ReadStringPointer("bad pointer path", GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnPointerPathEvaluation)));
        var failure = (StringReadFailureOnPointerPathEvaluation)result.Error;
        Assert.That(failure.Details, Is.Not.Null);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/>.
    /// The settings specify a length prefix in bytes. The pointer points to a string with a length prefix in bytes that
    /// has a value of 10.
    /// Performs 2 reads: one with a max length of 10 bytes, and one with a max length of 9 bytes.
    /// Expect the first read to return the full string, and the second read to fail with a
    /// <see cref="StringReadFailureOnStringTooLong"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithTooLongStringWithBytesPrefixTest()
    {
        var allocatedSpace = TestProcessMemory!.Allocate(0x1000, false).Value.ReserveRange(0x1000).Value;
        var settings = new StringSettings(Encoding.UTF8, false, new StringLengthPrefix(4, StringLengthUnit.Bytes));
        var stringContent = "0123456789"; // Length of 10 characters, and 10 bytes in UTF-8.
        var bytes = settings.GetBytes(stringContent)!;
        
        // Write the string to the allocated space. Because ReadStringPointer expects the address of a pointer to the
        // string, we write the string with an offset of 8, and then we write the first 8 bytes to point to the address
        // where we wrote the string.
        TestProcessMemory.Write(allocatedSpace.Address + 8, bytes);
        TestProcessMemory.Write(allocatedSpace.Address, allocatedSpace.Address + 8);
        
        // Read the string with a max length of 10 bytes.
        settings.MaxLength = 10;
        var firstResult = TestProcessMemory!.ReadStringPointer(allocatedSpace.Address, settings);
        
        // Read the string with a max length of 9 bytes.
        settings.MaxLength = 9;
        var secondResult = TestProcessMemory.ReadStringPointer(allocatedSpace.Address, settings);
        
        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value, Is.EqualTo(stringContent));
        
        Assert.That(secondResult.IsSuccess, Is.False);
        Assert.That(secondResult.Error, Is.TypeOf(typeof(StringReadFailureOnStringTooLong)));
        var failure = (StringReadFailureOnStringTooLong)secondResult.Error;
        Assert.That(failure.LengthPrefixValue, Is.EqualTo(10));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/>.
    /// The settings specify a length prefix in bytes. The pointer points to a string with a length prefix in characters
    /// that has a value of 10.
    /// Performs 2 reads: one with a max length of 10 characters, and one with a max length of 9 characters.
    /// Expect the first read to return the full string, and the second read to fail with a
    /// <see cref="StringReadFailureOnStringTooLong"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithTooLongStringWithCharactersPrefixTest()
    {
        var allocatedSpace = TestProcessMemory!.Allocate(0x1000, false).Value.ReserveRange(0x1000).Value;
        var settings = new StringSettings(Encoding.Unicode, false,
            new StringLengthPrefix(4, StringLengthUnit.Characters));
        var stringContent = "0123456789"; // Length of 10 characters, but 16 bytes in UTF-16.
        var bytes = settings.GetBytes(stringContent)!;
        
        // Write the string to the allocated space. Because ReadStringPointer expects the address of a pointer to the
        // string, we write the string with an offset of 8, and then we write the first 8 bytes to point to the address
        // where we wrote the string.
        TestProcessMemory.Write(allocatedSpace.Address + 8, bytes);
        TestProcessMemory.Write(allocatedSpace.Address, allocatedSpace.Address + 8);
        
        // Read the string with a max length of 10 characters.
        settings.MaxLength = 10;
        var firstResult = TestProcessMemory!.ReadStringPointer(allocatedSpace.Address, settings);
        
        // Read the string with a max length of 9 characters.
        settings.MaxLength = 9;
        var secondResult = TestProcessMemory.ReadStringPointer(allocatedSpace.Address, settings);
        
        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value, Is.EqualTo(stringContent));
        
        Assert.That(secondResult.IsSuccess, Is.False);
        Assert.That(secondResult.Error, Is.TypeOf(typeof(StringReadFailureOnStringTooLong)));
        var failure = (StringReadFailureOnStringTooLong)secondResult.Error;
        Assert.That(failure.LengthPrefixValue, Is.EqualTo(10));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/>.
    /// The settings specify no length prefix, but a null terminator. The pointer points to a string with 10 characters.
    /// Performs 2 reads: one with a max length of 10 characters, and one with a max length of 9 characters.
    /// Expect the first read to return the full string, and the second read to fail with a
    /// <see cref="StringReadFailureOnStringTooLong"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithTooLongStringWithoutPrefixTest()
    {
        var allocatedSpace = TestProcessMemory!.Allocate(0x1000, false).Value.ReserveRange(0x1000).Value;
        var settings = new StringSettings(Encoding.Unicode, true, null);
        var stringContent = "0123456789"; // Length of 10 characters
        var bytes = settings.GetBytes(stringContent)!;
        
        // Write the string to the allocated space. Because ReadStringPointer expects the address of a pointer to the
        // string, we write the string with an offset of 8, and then we write the first 8 bytes to point to the address
        // where we wrote the string.
        TestProcessMemory.Write(allocatedSpace.Address + 8, bytes);
        TestProcessMemory.Write(allocatedSpace.Address, allocatedSpace.Address + 8);
        
        // Read the string with a max length of 10 characters.
        settings.MaxLength = 10;
        var firstResult = TestProcessMemory!.ReadStringPointer(allocatedSpace.Address, settings);
        
        // Read the string with a max length of 9 characters.
        settings.MaxLength = 9;
        var secondResult = TestProcessMemory.ReadStringPointer(allocatedSpace.Address, settings);
        
        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value, Is.EqualTo(stringContent));
        
        Assert.That(secondResult.IsSuccess, Is.False);
        Assert.That(secondResult.Error, Is.TypeOf(typeof(StringReadFailureOnStringTooLong)));
        var failure = (StringReadFailureOnStringTooLong)secondResult.Error;
        Assert.That(failure.LengthPrefixValue, Is.Null); // No prefix, so the value should be null.
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(UIntPtr,StringSettings)"/> with a detached process.
    /// Expect the result to be a <see cref="StringReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadStringPointer(0x1234, GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnDetachedProcess)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadStringPointer(PointerPath,StringSettings)"/> with a detached process.
    /// Expect the result to be a <see cref="StringReadFailureOnDetachedProcess"/>.
    /// </summary>
    [Test]
    public void ReadStringPointerWithPointerPathWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.ReadStringPointer("1234", GetDotNetStringSettings());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf(typeof(StringReadFailureOnDetachedProcess)));
    }
    
    #endregion
    
    #endregion
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryReadTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryReadTestX86 : ProcessMemoryReadTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
}