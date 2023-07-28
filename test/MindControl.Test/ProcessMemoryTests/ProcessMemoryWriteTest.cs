using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

// /!\ WARNING /!\
// Some of these tests may not pass when run by JetBrains Rider.
// If you get a Win32Exception that does not make sense, check the tests with dotnet run instead.

/// <summary>
/// Tests the memory writing methods of <see cref="ProcessMemory"/>.
/// </summary>
public class ProcessMemoryWriteTest : ProcessMemoryTest
{
    /// <summary>
    /// Write bytes in the target app, overwriting the known byte array of the same length.
    /// The target app should output the values we wrote.
    /// </summary>
    [Test]
    public void WriteBytesWithArrayOfSameLengthTest()
    {
        ProceedToNextStep();
        TestProcessMemory!.WriteBytes($"{OuterClassPointer:X}+18,10", new byte[] { 0x8, 0x6, 0x4, 0xA });
        ProceedToNextStep();
        Assert.That(FinalResults.ElementAtOrDefault(14), Is.EqualTo("8,6,4,10"));
    }
}