using NUnit.Framework;

namespace MindControl.Test.AddressingTests;

/// <summary>
/// Tests the <see cref="MemoryRange"/> class.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MemoryRangeTest
{
    #region Constructor
    
    /// <summary>
    /// Tests the constructor of the <see cref="MemoryRange"/> class with valid input.
    /// It should set the start and end properties correctly.
    /// </summary>
    [Test]
    public void ConstructorWithValidInputTest()
    {
        var range = new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF));
        Assert.Multiple(() =>
        {
            Assert.That(range.Start, Is.EqualTo(new UIntPtr(0x1000)));
            Assert.That(range.End, Is.EqualTo(new UIntPtr(0x1FFF)));
        });
    }
    
    /// <summary>
    /// Tests the constructor of the <see cref="MemoryRange"/> class with a start address greater than the end address.
    /// It should throw an <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void ConstructorWithInvalidInputTest()
        => Assert.That(() => new MemoryRange(new UIntPtr(0x2000), new UIntPtr(0x1000)), Throws.ArgumentException);
    
    #endregion
    
    #region FromStartAndSize
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.FromStartAndSize"/> method with valid input.
    /// It should return a <see cref="MemoryRange"/> with the correct start and end addresses.
    /// </summary>
    [Test]
    public void FromStartAndSizeWithValidInputTest()
    {
        var range = MemoryRange.FromStartAndSize(new UIntPtr(0x1000), 0x1000);
        Assert.Multiple(() =>
        {
            Assert.That(range.Start, Is.EqualTo(new UIntPtr(0x1000)));
            Assert.That(range.End, Is.EqualTo(new UIntPtr(0x1FFF)));
        });
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.FromStartAndSize"/> method with a zero size.
    /// It should throw an <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void FromStartAndSizeWithZeroSizeTest()
        => Assert.That(() => MemoryRange.FromStartAndSize(new UIntPtr(0x1000), 0), Throws.ArgumentException);
    
    #endregion
    
    #region GetSize
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.GetSize"/> method.
    /// It should return the size of the range.
    /// </summary>
    [Test]
    public void GetSizeTest()
    {
        var range = new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF));
        Assert.That(range.GetSize(), Is.EqualTo(0x1000));
    }
    
    #endregion
    
    #region IsInRange

    /// <summary>
    /// Tests the <see cref="MemoryRange.IsInRange"/> method with specified addresses and ranges.
    /// It should return the specified expected value.
    /// </summary>
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0FFF, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x2000, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1000, (ulong)0x1000, ExpectedResult = true)]
    public bool IsInRangeTest(ulong start, ulong end, ulong address)
    {
        var range = new MemoryRange(new UIntPtr(start), new UIntPtr(end));
        return range.IsInRange(new UIntPtr(address));
    }
    
    #endregion
    
    #region Contains
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.Contains"/> method with specified ranges.
    /// It should return the specified expected value.
    /// </summary>
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1001, (ulong)0x1FFE, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x1FFE, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1001, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0FFF, (ulong)0x1FFF, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x2000, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x2000, (ulong)0x2FFF, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0000, (ulong)0x0FFF, ExpectedResult = false)]
    public bool ContainsTest(ulong start, ulong end, ulong otherStart, ulong otherEnd)
    {
        var range = new MemoryRange(new UIntPtr(start), new UIntPtr(end));
        var otherRange = new MemoryRange(new UIntPtr(otherStart), new UIntPtr(otherEnd));
        return range.Contains(otherRange);
    }
    
    #endregion
    
    #region Overlaps
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.Overlaps"/> method with specified ranges.
    /// It should return the specified expected value.
    /// </summary>
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1001, (ulong)0x1FFE, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x1FFE, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1001, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0FFF, (ulong)0x1FFF, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x1000, (ulong)0x2000, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0FFF, (ulong)0x2000, ExpectedResult = true)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x2000, (ulong)0x2FFF, ExpectedResult = false)]
    [TestCase((ulong)0x1000, (ulong)0x1FFF, (ulong)0x0000, (ulong)0x0FFF, ExpectedResult = false)]
    public bool OverlapsTest(ulong start, ulong end, ulong otherStart, ulong otherEnd)
    {
        var range = new MemoryRange(new UIntPtr(start), new UIntPtr(end));
        var otherRange = new MemoryRange(new UIntPtr(otherStart), new UIntPtr(otherEnd));
        
        // Overlaps is commutative, so we check both ways
        return range.Overlaps(otherRange) && otherRange.Overlaps(range);
    }
    
    #endregion
    
    #region Intersect
    
    /// <summary>
    /// Describes a test case for the <see cref="MemoryRange.Intersect"/> method.
    /// </summary>
    public record struct IntersectTestCase(ulong Start, ulong End, ulong OtherStart, ulong OtherEnd,
        MemoryRange? ExpectedResult);

    private static IntersectTestCase[] _intersectTestCases =
    {
        new(0x1000, 0x1FFF, 0x1001, 0x1FFE, new MemoryRange(new UIntPtr(0x1001), new UIntPtr(0x1FFE))),
        new(0x1000, 0x1FFF, 0x1000, 0x1FFF, new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF))),
        new(0x0FFF, 0x1000, 0x1000, 0x1FFF, new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1000))),
        new(0x1FFF, 0x2000, 0x1000, 0x1FFF, new MemoryRange(new UIntPtr(0x1FFF), new UIntPtr(0x1FFF))),
        new(0x1000, 0x1FFF, 0x2000, 0x2FFF, null),
        new(0x1000, 0x1FFF, 0x0000, 0x0FFF, null)
    };
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.Intersect"/> method with specified ranges.
    /// It should return the specified expected value.
    /// </summary>
    [TestCaseSource(nameof(_intersectTestCases))]
    public void IntersectTest(IntersectTestCase testCase)
    {
        var range = new MemoryRange(new UIntPtr(testCase.Start), new UIntPtr(testCase.End));
        var otherRange = new MemoryRange(new UIntPtr(testCase.OtherStart), new UIntPtr(testCase.OtherEnd));
        
        // We check both ways because Intersect is commutative
        var result = range.Intersect(otherRange);
        var result2 = otherRange.Intersect(range);
        Assert.That(result, Is.EqualTo(testCase.ExpectedResult));
        Assert.That(result2, Is.EqualTo(testCase.ExpectedResult));
    }
    
    #endregion
    
    #region Exclude

    /// <summary>
    /// Describes a test case for the <see cref="MemoryRange.Exclude"/> method.
    /// </summary>
    public record struct ExcludeTestCase(ulong Start, ulong End,
        ulong OtherStart, ulong OtherEnd, MemoryRange[] ExpectedRanges);
    
    private static ExcludeTestCase[] _excludeTestCases = {
        new(0x1000, 0x1FFF, 0x1100, 0x11FF, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x10FF)),
            new MemoryRange(new UIntPtr(0x1200), new UIntPtr(0x1FFF))}),
        
        new(0x1000, 0x1FFF, 0x1000, 0x11FF, new[] {
            new MemoryRange(new UIntPtr(0x1200), new UIntPtr(0x1FFF))}),
        
        new(0x1000, 0x1FFF, 0x0F00, 0x11FF, new[] {
            new MemoryRange(new UIntPtr(0x1200), new UIntPtr(0x1FFF))}),
        
        new(0x1000, 0x1FFF, 0x1F00, 0x1FFF, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1EFF))}),
        
        new(0x1000, 0x1FFF, 0x1F00, 0x2100, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1EFF))}),
        
        new(0x1000, 0x1FFF, 0x1F00, 0x1FFF, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1EFF))}),
        
        new(0x1000, 0x1FFF, 0x0100, 0x0200, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF))}),
        
        new(0x1000, 0x1FFF, 0x2100, 0x2200, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF))}),
        
        new(0x1000, 0x1FFF, 0x0100, 0x0200, new[] {
            new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF))}),
            
        new(0x1000, 0x1FFF, 0x1000, 0x1FFF, Array.Empty<MemoryRange>()),
        
        new(0x1000, 0x1FFF, 0x0100, 0x2200, Array.Empty<MemoryRange>()),
    };
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.Exclude"/> method with specified ranges.
    /// It should return the specified expected value.
    /// </summary>
    [TestCaseSource(nameof(_excludeTestCases))]
    public void ExcludeTest(ExcludeTestCase testCase)
    {
        var range = new MemoryRange(new UIntPtr(testCase.Start), new UIntPtr(testCase.End));
        var otherRange = new MemoryRange(new UIntPtr(testCase.OtherStart), new UIntPtr(testCase.OtherEnd));
        var results = range.Exclude(otherRange).ToArray();
        Assert.That(results, Is.EquivalentTo(testCase.ExpectedRanges));
    }
    
    #endregion
    
    #region AlignedTo
    
    /// <summary>
    /// Describes a test case for the <see cref="MemoryRange.AlignedTo"/> method.
    /// </summary>
    public record struct AlignedToTestCase(ulong Start, ulong End, uint Alignment,
        RangeAlignmentMode AlignmentMode, MemoryRange? ExpectedResult);
    
    private static AlignedToTestCase[] _alignedToTestCases = {
        new(0x1000, 0x1FFF, 4, RangeAlignmentMode.AlignBlock, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        new(0x1000, 0x1FFF, 4, RangeAlignmentMode.AlignStart, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        new(0x1000, 0x1FFF, 4, RangeAlignmentMode.None, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        
        new(0x1000, 0x1FFF, 8, RangeAlignmentMode.AlignBlock, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        new(0x1000, 0x1FFF, 8, RangeAlignmentMode.AlignStart, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        new(0x1000, 0x1FFF, 8, RangeAlignmentMode.None, new MemoryRange((UIntPtr)0x1000, (UIntPtr)0x1FFF)),
        
        new(0x1001, 0x1FFE, 4, RangeAlignmentMode.AlignBlock, new MemoryRange((UIntPtr)0x1004, (UIntPtr)0x1FFB)),
        new(0x1001, 0x1FFE, 4, RangeAlignmentMode.AlignStart, new MemoryRange((UIntPtr)0x1004, (UIntPtr)0x1FFE)),
        new(0x1001, 0x1FFE, 4, RangeAlignmentMode.None, new MemoryRange((UIntPtr)0x1001, (UIntPtr)0x1FFE)),
        
        new(0x1001, 0x1FFE, 8, RangeAlignmentMode.AlignBlock, new MemoryRange((UIntPtr)0x1008, (UIntPtr)0x1FF7)),
        new(0x1001, 0x1FFE, 8, RangeAlignmentMode.AlignStart, new MemoryRange((UIntPtr)0x1008, (UIntPtr)0x1FFE)),
        new(0x1001, 0x1FFE, 8, RangeAlignmentMode.None, new MemoryRange((UIntPtr)0x1001, (UIntPtr)0x1FFE)),
        
        new(0x1000, 0x1FFF, 0x2000, RangeAlignmentMode.AlignBlock, null)
    };
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.AlignedTo"/> method with specified alignment.
    /// It should return the expected range.
    /// </summary>
    [TestCaseSource(nameof(_alignedToTestCases))]
    public void AlignedToTest(AlignedToTestCase testCase)
    {
        var range = new MemoryRange(new UIntPtr(testCase.Start), new UIntPtr(testCase.End));
        var alignedRange = range.AlignedTo(testCase.Alignment, testCase.AlignmentMode);
        Assert.That(alignedRange, Is.EqualTo(testCase.ExpectedResult));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryRange.AlignedTo"/> method with zero alignment.
    /// It should throw an <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void AlignedToWithZeroAlignmentTest()
        => Assert.That(() => new MemoryRange(new UIntPtr(0x1000), new UIntPtr(0x1FFF)).AlignedTo(0),
            Throws.ArgumentException);
    
    #endregion
}