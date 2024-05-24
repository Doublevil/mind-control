using System.Text;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the memory reading methods of <see cref="ProcessMemory"/>.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryReadTest : ProcessMemoryTest
{
    /// <summary>A struct with a couple fields, to test reading structs.</summary>
    public record struct TestStruct(double A, int B);
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read(Type,UIntPtr)"/>.
    /// Reads a first time after initialization, and then a second time after value are modified.
    /// The values are expected to match the specified ones.
    /// </summary>
    /// <param name="targetType">Type of data to read.</param>
    /// <param name="pointerOffset">Offset to add up with the outer class pointer to get the address of the value to
    /// read.</param>
    /// <param name="expectedResultBeforeBreak">Expected value on the first read (after initialization).</param>
    /// <param name="expectedResultAfterBreak">Expected value on the second read (after modification).</param>
    [TestCase(typeof(bool), 0x48, true, false)]
    [TestCase(typeof(byte), 0x49, 0xAC, 0xDC)]
    [TestCase(typeof(short), 0x44, -7777, -8888)]
    [TestCase(typeof(ushort), 0x46, 8888, 9999)]
    [TestCase(typeof(int), 0x38, -7651, 987411)]
    [TestCase(typeof(uint), 0x3C, 6781631, 444763)]
    [TestCase(typeof(long), 0x20, -65746876815103L, -777654646516513L)]
    [TestCase(typeof(ulong), 0x28, 76354111324644L, 34411111111164L)]
    [TestCase(typeof(float), 0x40, 3456765.323f, -123444.147f)]
    [TestCase(typeof(double), 0x30, 79879131651.33345, -99879416311.4478)]
    public void ReadTwoStepGenericTest(Type targetType, int pointerOffset, object? expectedResultBeforeBreak,
        object? expectedResultAfterBreak)
    {
        UIntPtr targetIntAddress = OuterClassPointer + pointerOffset;
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
        var address = OuterClassPointer + 0x48;
        Assert.That(TestProcessMemory!.Read<bool>(address).GetValueOrDefault(), Is.EqualTo(true));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<bool>(address).GetValueOrDefault(), Is.EqualTo(false));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known byte from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadByteTest()
    {
        var address = OuterClassPointer + 0x49;
        Assert.That(TestProcessMemory!.Read<byte>(address).GetValueOrDefault(), Is.EqualTo(0xAC));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<byte>(address).GetValueOrDefault(), Is.EqualTo(0xDC));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known short from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadShortTest()
    {
        var address = OuterClassPointer + 0x44;
        Assert.That(TestProcessMemory!.Read<short>(address).GetValueOrDefault(), Is.EqualTo(-7777));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<short>(address).GetValueOrDefault(), Is.EqualTo(-8888));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned short from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadUShortTest()
    {
        var address = OuterClassPointer + 0x46;
        Assert.That(TestProcessMemory!.Read<ushort>(address).GetValueOrDefault(), Is.EqualTo(8888));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<ushort>(address).GetValueOrDefault(), Is.EqualTo(9999));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known integer from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadIntTest()
    {
        var address = OuterClassPointer + 0x38;
        Assert.That(TestProcessMemory!.Read<int>(address).GetValueOrDefault(), Is.EqualTo(-7651));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<int>(address).GetValueOrDefault(), Is.EqualTo(987411));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned integer from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadUIntTest()
    {
        var address = OuterClassPointer + 0x3C;
        Assert.That(TestProcessMemory!.Read<uint>(address).GetValueOrDefault(), Is.EqualTo(6781631));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<uint>(address).GetValueOrDefault(), Is.EqualTo(444763));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known long from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadLongTest()
    {
        var address = OuterClassPointer + 0x20;
        Assert.That(TestProcessMemory!.Read<long>(address).GetValueOrDefault(), Is.EqualTo(-65746876815103L));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<long>(address).GetValueOrDefault(), Is.EqualTo(-777654646516513L));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known unsigned long from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadULongTest()
    {
        var address = OuterClassPointer + 0x28;
        Assert.That(TestProcessMemory!.Read<ulong>(address).GetValueOrDefault(), Is.EqualTo(76354111324644L));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<ulong>(address).GetValueOrDefault(), Is.EqualTo(34411111111164L));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known float from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadFloatTest()
    {
        var address = OuterClassPointer + 0x40;
        Assert.That(TestProcessMemory!.Read<float>(address).GetValueOrDefault(), Is.EqualTo(3456765.323f));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<float>(address).GetValueOrDefault(), Is.EqualTo(-123444.147f));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(UIntPtr)"/>.
    /// Reads a known double from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadDoubleTest()
    {
        var address = OuterClassPointer + 0x30;
        Assert.That(TestProcessMemory!.Read<double>(address).GetValueOrDefault(), Is.EqualTo(79879131651.33345));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<double>(address).GetValueOrDefault(), Is.EqualTo(-99879416311.4478));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(PointerPath)"/>.
    /// Reads a known struct from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadStructureTest()
    {
        var pointerPath = new PointerPath($"{OuterClassPointer:X}+30");
        Assert.That(TestProcessMemory!.Read<TestStruct>(pointerPath).Value,
            Is.EqualTo(new TestStruct(79879131651.33345, -7651)));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read<TestStruct>(pointerPath).Value,
            Is.EqualTo(new TestStruct(-99879416311.4478, 987411)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Read(Type, PointerPath)"/>.
    /// Reads a known struct from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadStructureAsObjectTest()
    {
        var pointerPath = new PointerPath($"{OuterClassPointer:X}+30");
        Assert.That(TestProcessMemory!.Read(typeof(TestStruct), pointerPath).Value,
            Is.EqualTo(new TestStruct(79879131651.33345, -7651)));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.Read(typeof(TestStruct), pointerPath).Value,
            Is.EqualTo(new TestStruct(-99879416311.4478, 987411)));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadBytes(PointerPath,ulong)"/>.
    /// Reads a known byte array from the target process before and after the process changes their value.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadBytesTest()
    {
        PointerPath pointerPath = $"{OuterClassPointer:X}+18,10";
        Assert.That(TestProcessMemory!.ReadBytes(pointerPath, 4).GetValueOrDefault(),
            Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 0x44 }));
        ProceedToNextStep();
        Assert.That(TestProcessMemory.ReadBytes(pointerPath, 4).GetValueOrDefault(),
            Is.EqualTo(new byte[] { 0x55, 0x66, 0x77, 0x88 }));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Read{T}(PointerPath)"/>.
    /// Reads a known long from the target process before and after the process changes their value.
    /// This method uses a <see cref="PointerPath"/> and reads a value that is nested in the known instance.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadNestedLongTest() => Assert.That(
        TestProcessMemory!.Read<long>($"{OuterClassPointer:X}+10,8").GetValueOrDefault(), Is.EqualTo(999999999999L));

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
        var ptr = TestProcessMemory!.Read<UIntPtr>($"{OuterClassPointer:X}+10,10").GetValueOrDefault();
        Assert.That(ptr.ToUInt64(), Is.EqualTo(ulong.MaxValue));
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
        var result = TestProcessMemory!.Read<byte>($"{OuterClassPointer:X}+10,10,0");
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf(typeof(ReadFailureOnSystemRead)));
        var systemError = ((ReadFailureOnSystemRead)error).SystemReadFailure;
        Assert.That(systemError, Is.TypeOf(typeof(OperatingSystemCallFailure)));
        var osError = (OperatingSystemCallFailure)systemError;
        Assert.That(osError.ErrorCode, Is.GreaterThan(0));
        Assert.That(osError.ErrorMessage, Is.Not.Empty);
    }
    
    /// <summary>
    /// Tests an edge case where the pointer path given to a read operation points to a value located after the last
    /// possible byte in memory (the maximum value of a UIntPtr + 1).
    /// The read operation is expected to fail with a <see cref="ReadFailureOnPointerPathEvaluation"/>.
    /// </summary>
    [Test]
    public void ReadOverMaxPointerValueTest()
    {
        var result = TestProcessMemory!.Read<byte>($"{OuterClassPointer:X}+10,10,1");
        Assert.That(result.IsSuccess, Is.False);
        var error = result.Error;
        Assert.That(error, Is.TypeOf(typeof(ReadFailureOnPointerPathEvaluation)));
        var pathError = ((ReadFailureOnPointerPathEvaluation)error).PathEvaluationFailure;
        Assert.That(pathError, Is.TypeOf(typeof(PathEvaluationFailureOnPointerOutOfRange)));
        var outOfRangeError = (PathEvaluationFailureOnPointerOutOfRange)pathError;
        Assert.That(outOfRangeError.Offset, Is.EqualTo(new PointerOffset(1, false)));
        Assert.That(outOfRangeError.PreviousAddress, Is.EqualTo(UIntPtr.MaxValue));
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

    /// <summary>Structure used to test reading a structure with references.</summary>
    private record struct TestStructWithReferences(int A, string B);
    
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
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, without using any parameter beyond the
    /// pointer path.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position.
    /// It should be equal to its known values before and after modification.
    /// </summary>
    [Test]
    public void ReadStringWithNoParametersTest()
    {
        var pointerPath = new PointerPath($"{OuterClassPointer:X}+8,8");
        Assert.That(TestProcessMemory!.ReadString(pointerPath).GetValueOrDefault(), Is.EqualTo("ThisIsÄString"));
        ProceedToNextStep();
        Assert.That(TestProcessMemory!.ReadString(pointerPath).GetValueOrDefault(),
            Is.EqualTo("ThisIsALongerStrîngWith文字化けチェック"));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a max length of 10 bytes.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position.
    /// It should be equal to the first 5 characters of its known value, because in memory, the string is stored as in
    /// UTF-16, so it uses 2 bytes per ASCII-friendly character, so 10 bytes would be 5 characters.
    /// Despite the length prefix being read correctly as more than 10, only the 10 first bytes should be read.
    /// </summary>
    [Test]
    public void ReadStringWithLimitedLengthTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,8", 10).GetValueOrDefault(),
            Is.EqualTo("ThisI"));
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance similar to the
    /// .net preset but with no length prefix.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position, after its length prefix.
    /// It should be equal to its full known value. Despite not being able to know the length of the string because we
    /// specify that there is no length prefix, we still use a setting that specifies a null terminator, and the string
    /// is indeed null-terminated, so it should properly cut after the last character.
    /// </summary>
    [Test]
    public void ReadStringWithoutLengthPrefixTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,C",
            stringSettings: new StringSettings(Encoding.Unicode, true, null)).GetValueOrDefault(),
            Is.EqualTo("ThisIsÄString"));
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance similar to the
    /// .net preset but not null-terminated.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position.
    /// It should be equal to its full known value. Despite not being able to identify a null terminator as the end of
    /// the string, we do use a setting that specifies a correct length prefix, so only the right number of bytes should
    /// be read.
    /// </summary>
    [Test]
    public void ReadStringWithLengthPrefixWithoutNullTerminatorTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,8",
            stringSettings: new StringSettings(Encoding.Unicode, false, new StringLengthPrefixSettings(4, 2)))
                .GetValueOrDefault(),
            Is.EqualTo("ThisIsÄString"));

    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance specifying no
    /// null terminator and no length prefix. We use a byte count of 64.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position, after the length prefix.
    /// It should be equal to its known value, followed by a bunch of garbage characters. Because we specified no length
    /// prefix and no null terminator, all 64 characters will be read as a string, despite the actual string being
    /// shorter than that.
    /// To clarify, even though the result looks wrong, this is the expected output. The input parameters are wrong.
    /// </summary>
    [Test]
    public void ReadStringWithoutLengthPrefixOrNullTerminatorTest()
    {
        string? result = TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,C", 64,
            new StringSettings(Encoding.Unicode, false, null)).GetValueOrDefault();
        
        // We should have a string that starts with the full known string, and has at least one more character.
        // We cannot test an exact string or length because the memory region after the string is not guaranteed to
        // always be the same.
        Assert.That(result, Does.StartWith("ThisIsÄString"));
        Assert.That(result, Has.Length.AtLeast("ThisIsÄString".Length + 1));
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance that has a
    /// length prefix unit set to null instead of 2, and no null terminator.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position.
    /// It should be equal to its full known value. The unit being set to null should trigger it to automatically
    /// determine what the unit should be. It should determine that it is 2 and read the string correctly.
    /// </summary>
    [Test]
    public void ReadStringWithUnspecifiedLengthPrefixUnitTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,8",
                stringSettings: new StringSettings(Encoding.Unicode, false, new StringLengthPrefixSettings(4)))
                .GetValueOrDefault(),
            Is.EqualTo("ThisIsÄString"));
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance that has a
    /// length prefix unit with a size of 2 instead of the correct 4.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position, 2 bytes into the length
    /// prefix.
    /// The result should be an empty string, because the length prefix should be read as 0.
    /// </summary>
    [Test]
    public void ReadStringWithZeroLengthPrefixUnitTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,A",
                stringSettings: new StringSettings(Encoding.Unicode, true, new StringLengthPrefixSettings(2, 2)))
                .GetValueOrDefault(),
            Is.EqualTo(string.Empty));
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.ReadString(PointerPath, int, StringSettings?)"/>.
    /// Reads a known string from the target process after initialization, with a StringSettings instance that has a
    /// UTF-8 encoding instead of the correct UTF-16.
    /// This method uses a <see cref="PointerPath"/> to read the string at the right position.
    /// The result should be only the first character of the know value, because the null terminator is hit on the
    /// second UTF-16 byte that is supposed to be part of the first character.
    /// To explain a little bit more, a UTF-16 string has a minimum of 2 bytes per character. In this case, the first
    /// two characters are "Th", which are held in memory as 54 00 68 00. But UTF-8 would write the same "Th" as 54 68.
    /// The encoding will interpret the second byte (00) as a null terminator that signals the end of the string, and
    /// read it only as "T" (54), discarding everything after that.
    /// </summary>
    [Test]
    public void ReadStringWithWrongEncodingTest()
        => Assert.That(TestProcessMemory!.ReadString($"{OuterClassPointer:X}+8,8",
                stringSettings: new StringSettings(Encoding.UTF8, true, new StringLengthPrefixSettings(4, 2)))
                .GetValueOrDefault(),
            Is.EqualTo("T"));
}