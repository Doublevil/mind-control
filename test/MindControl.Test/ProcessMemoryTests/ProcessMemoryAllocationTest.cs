using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to memory allocation.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAllocationTest : ProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method.
    /// Performs a simple allocation and verifies that it is writable and the range is in the list of allocated ranges.
    /// </summary>
    [Test]
    public void AllocateTest()
    {
        var range = TestProcessMemory!.Allocate(0x1000, false);
        
        // To check that the memory is writable, we will write a byte array to the allocated range.
        // We will use the WriteBytes method rather than Store, because Store is built on top of Allocate and we only
        // want to test the allocation here.
        TestProcessMemory.WriteBytes(range.Range.Start, new byte[0x1000]);
        
        Assert.That(range, Is.Not.Null);
        Assert.That(range.Range.GetSize(), Is.AtLeast(0x1000));
        Assert.That(range.IsExecutable, Is.False);
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Member(range));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method.
    /// Performs an allocation for executable code and verifies that the resulting range is executable.
    /// </summary>
    [Test]
    public void AllocateExecutableTest()
    {
        var range = TestProcessMemory!.Allocate(0x1000, true);
        Assert.That(range, Is.Not.Null);
        Assert.That(range.Range.GetSize(), Is.AtLeast(0x1000));
        Assert.That(range.IsExecutable, Is.True);
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Member(range));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method with a zero size.
    /// This should throw an exception.
    /// </summary>
    [Test]
    public void AllocateZeroTest()
        => Assert.Throws<ArgumentException>(() => TestProcessMemory!.Allocate(0, false));
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],AllocatedRange)"/> method.
    /// Stores a byte array in an allocated range and verifies that the value has been stored properly.
    /// </summary>
    [Test]
    public void StoreWithRangeTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };
        var range = TestProcessMemory!.Allocate(0x1000, false);
        
        var result = TestProcessMemory.Store(value, range);
        var read = TestProcessMemory.ReadBytes(result.Range.Start, value.Length);
        
        Assert.That(result.IsReserved);
        // The resulting range should be a range reserved from our original range.
        Assert.That(result.ParentRange, Is.EqualTo(range));
        // When we read over the range, we should get the same value we stored.
        Assert.That(read, Is.EqualTo(value));
        Assert.That(TestProcessMemory.AllocatedRanges, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],bool)"/> method.
    /// Stores a byte array without specifying a range and verifies that the value has been stored properly.
    /// </summary>
    [Test]
    public void StoreWithoutRangeTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };
        
        var result = TestProcessMemory!.Store(value);
        var read = TestProcessMemory.ReadBytes(result.Range.Start, value.Length);
        
        // The store method should have allocated a new range.
        Assert.That(TestProcessMemory.AllocatedRanges, Has.Count.EqualTo(1));
        Assert.That(result.IsReserved);
        // When we read over the range, we should get the same value we stored.
        Assert.That(read, Is.EqualTo(value));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],bool)"/> method.
    /// Stores a small byte array multiple times without specifying a range, and verifies that only one allocation has
    /// been made, with multiple reservations on it.
    /// </summary>
    [Test]
    public void StoreWithoutRangeWithMultipleSmallValuesTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };

        var results = Enumerable.Range(0, 4).Select(_ => TestProcessMemory!.Store(value)).ToList();
        var readBackValues = results.Select(r => TestProcessMemory!.ReadBytes(r.Range.Start, value.Length));
        
        // The store method should have allocated only one range that's big enough to accomodate all the values.
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Count.EqualTo(1));
        
        // All the results should be reserved ranges from the same parent range.
        Assert.That(results, Has.All.Matches<AllocatedRange>(r => r.IsReserved));
        Assert.That(results, Has.All.Matches<AllocatedRange>(r => r.ParentRange == results.First().ParentRange));
        
        // When we read over the ranges, we should get the same value we stored.
        Assert.That(readBackValues, Is.All.EqualTo(value));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],bool)"/> method.
    /// Allocates a chunk of memory, stores a big value that just fits in the range, and then stores the same value
    /// another time.
    /// Verifies that the second store operation has allocated a new range. 
    /// </summary>
    [Test]
    public void StoreWithMultipleOverflowingValuesTest()
    {
        var range = TestProcessMemory!.Allocate(0x1000, false);
        var value = new byte[range.Range.GetSize()];

        TestProcessMemory!.Store(value);
        
        // So far, we should have only one allocated range.
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Count.EqualTo(1));
        
        // Now we store the same value again, which should overflow the range.
        TestProcessMemory!.Store(value);
        
        // We should have two allocated ranges now, because there is no room left in the first range.
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Count.EqualTo(2));
    }
}