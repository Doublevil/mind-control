using NUnit.Framework;

namespace MindControl.Test.AddressingTests;

/// <summary>
/// Tests the <see cref="PointerExtensions"/> class.
/// </summary>
public class PointerExtensionsTest
{
    /// <summary>
    /// Tests the <see cref="PointerExtensions.DistanceTo"/> method.
    /// </summary>
    [TestCase((ulong)0x1000, (ulong)0x2000, ExpectedResult = (ulong)0x1000)]
    [TestCase((ulong)0x2000, (ulong)0x1000, ExpectedResult = (ulong)0x1000)]
    [TestCase((ulong)0x1000, (ulong)0x1000, ExpectedResult = (ulong)0)]
    [TestCase((ulong)0, ulong.MaxValue, ExpectedResult = ulong.MaxValue,
        TestName = "DistanceToTest(0,ulong.MaxValue)")] // Specifying the name fixes a test duplication bug
    [TestCase(ulong.MaxValue, (ulong)0, ExpectedResult = ulong.MaxValue,
        TestName = "DistanceToTest(ulong.MaxValue,0)")] // Specifying the name fixes a test duplication bug
    public ulong DistanceToTest(ulong value1, ulong value2)
    {
        var ptr1 = new UIntPtr(value1);
        var ptr2 = new UIntPtr(value2);
        return ptr1.DistanceTo(ptr2);
    }
    
    /// <summary>Test cases for <see cref="PointerExtensionsTest.GetRangeAroundTest"/>.</summary>
    public record GetRangeAroundTestCase(ulong Address, ulong Size, ulong ExpectedStart, ulong ExpectedEnd);

    private static GetRangeAroundTestCase[] _getRangeAroundTestCases =
    {
        new(1, 2, 0, 2),
        new(0x1000, 0x2000, 0, 0x2000),
        new(1, 4, 0, 3),
        new(2, 3, 1, 3),
        new(ulong.MaxValue, 8, ulong.MaxValue - 4, ulong.MaxValue)
    };
    
    /// <summary>
    /// Tests the <see cref="PointerExtensions.GetRangeAround"/> method.
    /// </summary>
    /// <param name="testCase">Test case to run.</param>
    [TestCaseSource(nameof(_getRangeAroundTestCases))]
    public void GetRangeAroundTest(GetRangeAroundTestCase testCase)
    {
        var address = new UIntPtr(testCase.Address);
        var range = address.GetRangeAround(testCase.Size);
        Assert.That(range.Start.ToUInt64(), Is.EqualTo(testCase.ExpectedStart));
        Assert.That(range.End.ToUInt64(), Is.EqualTo(testCase.ExpectedEnd));
    }
    
    /// <summary>
    /// Tests the <see cref="PointerExtensions.GetRangeAround"/> method with a size of zero.
    /// Expects an <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void GetRangeAroundWithZeroSizeTest()
        => Assert.Throws<ArgumentException>(() => ((UIntPtr)0x1000).GetRangeAround(0));
    
    /// <summary>
    /// Tests the <see cref="PointerExtensions.GetRangeAround"/> method with a size of one.
    /// Expects an <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void GetRangeAroundWithOneSizeTest()
        => Assert.Throws<ArgumentException>(() => ((UIntPtr)0x1000).GetRangeAround(1));
}