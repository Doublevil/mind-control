using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the memory writing methods of <see cref="ProcessMemory"/>.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryWriteTest : ProcessMemoryTest
{
    #region Common

    /// <summary>
    /// Stores the line-by-line expected output of the target app.
    /// </summary>
    private static string[] ExpectedFinalValues =
    {
        "False",
        "220",
        "987411",
        "444763",
        "ThisIsALongerStrîngWith文字化けチェック",
        "-777654646516513",
        "34411111111164",
        "173",
        "64646321",
        "7777777777777",
        "-8888",
        "9999",
        "-123444.15",
        "-99879416311.4478",
        "85,102,119,136"
    };
    
    /// <summary>
    /// Asserts that among the final results output by the target app, the one at the given index matches the
    /// expected value, and all the other results are the known, untouched values. 
    /// </summary>
    /// <param name="index">Index of the final result to check against the <paramref name="expectedValue"/>.</param>
    /// <param name="expectedValue">Expected value of the final result at the specified index.</param>
    private void AssertFinalResults(int index, string expectedValue)
    {
        for (int i = 0; i < ExpectedFinalValues.Length; i++)
        {
            string expectedValueAtIndex = i == index ? expectedValue : ExpectedFinalValues[i];
            Assert.That(FinalResults.ElementAtOrDefault(i), Is.EqualTo(expectedValueAtIndex));
        }
    }
    
    #endregion

    /// <summary>
    /// Write a value in the target app memory, and assert that the output of the app for that specific value (and no
    /// other value) reflects the change.
    /// </summary>
    /// <param name="pointerPathSuffix">Pointer path fragment to insert after the address of the target object instance
    /// in order to reach the address where we want to write the value.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="finalResultsIndex">Index of the value to assert in the array representing the final output from the
    /// target app.</param>
    /// <param name="expectedValue">Expected value of the final output from the target app.</param>
    [TestCase("+48", true, 0, "True")]
    [TestCase("+49", (byte)94, 1, "94")]
    [TestCase("+38", (int)-447712345, 2, "-447712345")]
    [TestCase("+3C", (uint)74753312, 3, "74753312")]
    [TestCase("+20", (long)-858884523, 5, "-858884523")]
    [TestCase("+28", (ulong)755443121891, 6, "755443121891")]
    [TestCase("+10,8", (long)51356, 9, "51356")]
    [TestCase("+44", (short)-2421, 10, "-2421")]
    [TestCase("+46", (ushort)2594, 11, "2594")]
    [TestCase("+40", (float)4474.783, 12, "4474.783")]
    [TestCase("+30", (double)54234423.3147, 13, "54234423.3147")]
    [TestCase("+18,10", new byte[] { 0x8, 0x6, 0x4, 0xA }, 14, "8,6,4,10")]
    public void WriteValueTest(string pointerPathSuffix, object value, int finalResultsIndex, string expectedValue)
    {
        ProceedToNextStep();
        TestProcessMemory!.Write($"{OuterClassPointer:X}{pointerPathSuffix}", value);
        ProceedToNextStep();
        AssertFinalResults(finalResultsIndex, expectedValue);
    }
}