using NUnit.Framework;

namespace MindControl.Test.SearchTests;

/// <summary>
/// Tests the <see cref="ByteSearchPattern"/> class.
/// </summary>
public class ByteSearchPatternTest
{
    public readonly record struct PatternExpectedResult(byte[] ByteArray, byte[] Mask);

    public readonly record struct PatternTestCase(string Pattern, PatternExpectedResult? Expected);

    private static readonly PatternTestCase[] TestCases =
    {
        // Nominal case: only full bytes
        new("1F 49 1A 03", new PatternExpectedResult(
            [0x1F, 0x49, 0x1A, 0x03],
            [0xFF, 0xFF, 0xFF, 0xFF])),
        
        // Full bytes and full wildcards
        new("1F ?? 1A 03", new PatternExpectedResult(
            [0x1F, 0x00, 0x1A, 0x03],
            [0xFF, 0x00, 0xFF, 0xFF])),
        
        // Partial wildcard (left)
        new("1F ?9 1A 03", new PatternExpectedResult(
            [0x1F, 0x09, 0x1A, 0x03],
            [0xFF, 0x0F, 0xFF, 0xFF])),
        
        // Partial wildcard (right)
        new("1F 4? 1A 03", new PatternExpectedResult(
            [0x1F, 0x40, 0x1A, 0x03],
            [0xFF, 0xF0, 0xFF, 0xFF])),
        
        // Mixed (all cases and odd spaces)
        new("1F4? ?A??", new PatternExpectedResult(
            [0x1F, 0x40, 0x0A, 0x00],
            [0xFF, 0xF0, 0x0F, 0x00])),
        
        // Error case: odd number of characters
        new("1F 49 1A 0", null),
        
        // Error case: invalid character
        new("1F 49 1A 0G", null),
        
        // Error case: only wildcards
        new("?? ?? ?? ??", null),
        
        // Error case: empty string
        new("   ", null)
    };
    
    /// <summary>
    /// Tests the <see cref="ByteSearchPattern.TryParse"/> method.
    /// </summary>
    [TestCaseSource(nameof(TestCases))]
    public void TryParseTest(PatternTestCase testCase)
    {
        var result = ByteSearchPattern.TryParse(testCase.Pattern);
        if (testCase.Expected == null)
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Failure.Message, Is.Not.Empty);
        }
        else
        {
            Assert.That(result.IsSuccess, Is.True);
            var pattern = result.Value;
            Assert.That(pattern.ToString(), Is.EqualTo(testCase.Pattern));
            Assert.That(pattern.ByteArray, Is.EqualTo(testCase.Expected.Value.ByteArray));
            Assert.That(pattern.Mask, Is.EqualTo(testCase.Expected.Value.Mask));
        }
    }
    
    /// <summary>
    /// Tests the <see cref="ByteSearchPattern"/> constructor.
    /// </summary>
    [TestCaseSource(nameof(TestCases))]
    public void ConstructorTest(PatternTestCase testCase)
    {
        ByteSearchPattern pattern;
        try
        {
            pattern = new ByteSearchPattern(testCase.Pattern);
        }
        catch (Exception e)
        {
            if (testCase.Expected != null)
                Assert.Fail($"Unexpected exception: {e}");

            return;
        }
        
        if (testCase.Expected == null)
            Assert.Fail("Expected exception not thrown.");
        
        Assert.That(pattern.ToString(), Is.EqualTo(testCase.Pattern));
        Assert.That(pattern.ByteArray, Is.EqualTo(testCase.Expected!.Value.ByteArray));
        Assert.That(pattern.Mask, Is.EqualTo(testCase.Expected.Value.Mask));
    }
}