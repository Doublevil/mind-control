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
    [TestCase(true, IndexOfOutputOuterBool, "True")]
    [TestCase((byte)94, IndexOfOutputOuterByte, "94")]
    [TestCase((int)-447712345, IndexOfOutputOuterInt, "-447712345")]
    [TestCase((uint)74753312, IndexOfOutputOuterUint, "74753312")]
    [TestCase((long)-858884523, IndexOfOutputOuterLong, "-858884523")]
    [TestCase((ulong)755443121891, IndexOfOutputOuterUlong, "755443121891")]
    [TestCase((long)51356, IndexOfOutputInnerLong, "51356")]
    [TestCase((short)-2421, IndexOfOutputOuterShort, "-2421")]
    [TestCase((ushort)2594, IndexOfOutputOuterUshort, "2594")]
    [TestCase((float)4474.783, IndexOfOutputOuterFloat, "4474.783")]
    [TestCase((double)54234423.3147, IndexOfOutputOuterDouble, "54234423.3147")]
    [TestCase(new byte[] { 0x8, 0x6, 0x4, 0xA }, IndexOfOutputOuterByteArray, "8,6,4,10")]
    public void WriteValueTest(object value, int finalResultsIndex, string expectedValue)
    {
        var result = TestProcessMemory.Read<UIntPtr>(OuterClassPointer + (UIntPtr)0x10).Value;
        var pointerPath = GetPointerPathForValueAtIndex(finalResultsIndex);
        ProceedToNextStep();
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
        var pointerAddress = GetPointerPathForValueAtIndex(IndexOfOutputOuterString);
        var stringSettings = TestProcessMemory!.FindStringSettings(pointerAddress, "ThisIsÄString").Value;
        ProceedToNextStep(); // Make sure we make the test program change the string pointer before we overwrite it
        
        var newString = "This String Is Completely New And Also Longer Than The Original One";
        var newStringReservation = TestProcessMemory.StoreString(newString, stringSettings).Value;
        TestProcessMemory.Write(pointerAddress, newStringReservation.Address);
        
        ProceedToNextStep(); // Makes the test program output the final results
        
        // Test that the program actually used (wrote to the console) the string that we hacked in
        AssertFinalResults(IndexOfOutputOuterString, newString);
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryWriteTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryWriteTestX86 : ProcessMemoryWriteTest { protected override bool Is64Bit => false; }