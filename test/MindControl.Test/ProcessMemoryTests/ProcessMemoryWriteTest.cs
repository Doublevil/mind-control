using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the memory writing methods of <see cref="ProcessMemory"/>.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryWriteTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Write a value in the target app memory, and assert that the output of the app for that specific value (and no
    /// other value) reflects the change.
    /// </summary>
    /// <param name="value">Value to write.</param>
    /// <param name="finalResultsIndex">Index of the value to assert in the array representing the final output from the
    /// target app.</param>
    /// <param name="expectedValue">Expected value of the final output from the target app.</param>
    [TestCase(true, IndexOfOutputBool, "True")]
    [TestCase((byte)94, IndexOfOutputByte, "94")]
    [TestCase((int)-447712345, IndexOfOutputInt, "-447712345")]
    [TestCase((uint)74753312, IndexOfOutputUInt, "74753312")]
    [TestCase((long)-858884523, IndexOfOutputLong, "-858884523")]
    [TestCase((ulong)755443121891, IndexOfOutputULong, "755443121891")]
    [TestCase((long)51356, IndexOfOutputInnerLong, "51356")]
    [TestCase((short)-2421, IndexOfOutputShort, "-2421")]
    [TestCase((ushort)2594, IndexOfOutputUShort, "2594")]
    [TestCase((float)4474.783, IndexOfOutputFloat, "4474.783")]
    [TestCase((double)54234423.3147, IndexOfOutputDouble, "54234423.3147")]
    [TestCase(new byte[] { 0x8, 0x6, 0x4, 0xA }, IndexOfOutputByteArray, "8,6,4,10")]
    public void WriteValueTest(object value, int finalResultsIndex, string expectedValue)
    {
        var pointerPath = GetPointerPathForValueAtIndex(finalResultsIndex);
        ProceedToNextStep(); // Let the app overwrite the values once before writing
        TestProcessMemory!.Write(pointerPath, value);
        ProceedToNextStep();
        AssertFinalResults(finalResultsIndex, expectedValue);
    }

    /// <summary>A struct with a couple fields, to test writing structs in the target app memory. </summary>
    public record struct TestStruct(int A, long B);
    
    /// <summary>
    /// Write a struct instance in the target app memory, and attempt to read it back.
    /// </summary>
    [Test]
    public void WriteStructTest()
    {
        var structInstance = new TestStruct(123, -456789);
        ProceedToNextStep();
        var pointerPath = new PointerPath($"{OuterClassPointer:X}+50");
        TestProcessMemory!.Write(pointerPath, structInstance);
        
        var readBackResult = TestProcessMemory.Read<TestStruct>(pointerPath);
        
        Assert.That(readBackResult.IsSuccess, Is.True);
        Assert.That(readBackResult.Value, Is.EqualTo(structInstance));
    }

    /// <summary>
    /// This tests a combination of methods that can be used to overwrite a string pointer in the target app memory.
    /// The protocol is: read the string settings from the target string pointer (because we know the initial value),
    /// store the new string in memory, and then replace the pointer value with the address of the new string.
    /// We then assert that the final output of the target app reflects the change.
    /// </summary>
    /// <remarks>There is no shortcut method that does all of this at once, by design, to make sure users understand
    /// that they are performing an allocation or reservation operation when they write a string.</remarks>
    [Test]
    public void WriteStringPointerTest()
    {
        var pointerAddress = GetPointerPathForValueAtIndex(IndexOfOutputString);
        var stringSettings = TestProcessMemory!.FindStringSettings(pointerAddress, InitialStringValue).Value;
        ProceedToNextStep(); // Make sure we make the test program change the string pointer before we overwrite it
        
        var newString = "This String Is Completely New And Also Longer Than The Original One";
        var newStringReservation = TestProcessMemory.StoreString(newString, stringSettings).Value;
        TestProcessMemory.Write(pointerAddress, newStringReservation.Address);
        
        ProceedToNextStep(); // Makes the test program output the final results
        
        // Test that the program actually used (wrote to the console) the string that we hacked in
        AssertFinalResults(IndexOfOutputString, newString);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> with a zero pointer.
    /// Expect a <see cref="ZeroPointerFailure"/> error.
    /// </summary>
    [Test]
    public void WriteAtZeroPointerTest()
    {
        var result = TestProcessMemory!.Write(UIntPtr.Zero, 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<ZeroPointerFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> with a null value.
    /// Expect a <see cref="InvalidArgumentFailure"/> error.
    /// </summary>
    [Test]
    public void WriteNullValueTest()
    {
        var result = TestProcessMemory!.Write(OuterClassPointer, (int?)null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<InvalidArgumentFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> with an unsupported
    /// type. Expect a <see cref="UnsupportedTypeWriteFailure"/> error.
    /// </summary>
    [Test]
    public void WriteUnsupportedTypeTest()
    {
        var result = TestProcessMemory!.Write(OuterClassPointer, new object());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<UnsupportedTypeWriteFailure>());
    }

    #pragma warning disable 0649
    /// <summary>Defines a structure that is expected to be incompatible with writing methods.</summary>
    private struct IncompatibleStruct { public long A; public byte[] B; } // The byte[] makes it incompatible
    #pragma warning restore 0649
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> with an incompatible
    /// struct. Expect a <see cref="ConversionWriteFailure"/> error.
    /// </summary>
    /// <remarks>
    /// This test has been disabled because it triggers a System.AccessViolationException. This exception type used to
    /// be impossible to catch by default in .net, and will still crash the NUnit test runner. It may be re-enabled in
    /// the future if a solution is found. 
    /// </remarks>
    public void WriteIncompatibleStructTest()
    {
        var result = TestProcessMemory!.Write(OuterClassPointer, new IncompatibleStruct());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<ConversionWriteFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> with a detached
    /// process. Expect a <see cref="DetachedProcessFailure"/> error.
    /// </summary>
    [Test]
    public void WriteWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Write(OuterClassPointer, 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<DetachedProcessFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(PointerPath,T,Nullable{MemoryProtectionStrategy})"/> with a detached
    /// process. Expect a <see cref="DetachedProcessFailure"/> error.
    /// </summary>
    [Test]
    public void WriteAtPointerPathWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.Write(OuterClassPointer.ToString("X"), 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<DetachedProcessFailure>());
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryWriteTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryWriteTestX86 : ProcessMemoryWriteTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;

    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> on a 32-bit target app
    /// with a 64-bit address. Expect a <see cref="IncompatibleBitnessPointerFailure"/> error.
    /// </summary>
    [Test]
    public void WriteGenericOnX86WithX64AddressTest()
    {
        var address = (ulong)uint.MaxValue + 1;
        var result = TestProcessMemory!.Write((UIntPtr)address, 8);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<IncompatibleBitnessPointerFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.WriteBytes(UIntPtr,Span{byte},Nullable{MemoryProtectionStrategy})"/> on a 32-bit
    /// target app with a 64-bit address. Expect a <see cref="IncompatibleBitnessPointerFailure"/> error.
    /// </summary>
    [Test]
    public void WriteBytesOnX86WithX64AddressTest()
    {
        var address = (ulong)uint.MaxValue + 1;
        var result = TestProcessMemory!.WriteBytes((UIntPtr)address, new byte[8]);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<IncompatibleBitnessPointerFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.Write{T}(UIntPtr,T,Nullable{MemoryProtectionStrategy})"/> on a 32-bit target app
    /// with a reachable address, but a value with a pointer type that goes beyond the 32-bit address space.
    /// Expect a <see cref="IncompatibleBitnessPointerFailure"/> error.
    /// </summary>
    [Test]
    public void WriteX64PointerOnX86Test()
    {
        var address = (ulong)uint.MaxValue + 1;
        var result = TestProcessMemory!.Write(OuterClassPointer, (UIntPtr)address);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<IncompatibleBitnessPointerFailure>());
    }
}