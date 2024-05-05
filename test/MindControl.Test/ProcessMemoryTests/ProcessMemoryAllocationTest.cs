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
    /// Performs a simple allocation and verifies that it is writable and that it is added to the list of managed
    /// allocations.
    /// </summary>
    [Test]
    public void AllocateTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, false);
        
        // To check that the memory is writable, we will write a byte array to the allocated range.
        // We will use the WriteBytes method rather than Store, because Store is built on top of Allocate, and we only
        // want to test the allocation here.
        TestProcessMemory.WriteBytes(allocation.Range.Start, new byte[0x1000]);
        
        Assert.That(allocation, Is.Not.Null);
        Assert.That(allocation.IsDisposed, Is.False);
        Assert.That(allocation.Range.GetSize(), Is.AtLeast(0x1000));
        Assert.That(allocation.IsExecutable, Is.False);
        Assert.That(TestProcessMemory!.Allocations, Has.Member(allocation));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method.
    /// Performs an allocation for executable code and verifies that the resulting allocation is executable.
    /// </summary>
    [Test]
    public void AllocateExecutableTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, true);
        Assert.That(allocation, Is.Not.Null);
        Assert.That(allocation.IsDisposed, Is.False);
        Assert.That(allocation.Range.GetSize(), Is.AtLeast(0x1000));
        Assert.That(allocation.IsExecutable, Is.True);
        Assert.That(TestProcessMemory!.Allocations, Has.Member(allocation));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method with a zero size.
    /// This should throw an exception.
    /// </summary>
    [Test]
    public void AllocateZeroTest()
        => Assert.Throws<ArgumentException>(() => TestProcessMemory!.Allocate(0, false));
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],MemoryAllocation)"/> method.
    /// Stores a byte array in an allocated range and verifies that the value has been stored properly.
    /// </summary>
    [Test]
    public void StoreWithRangeTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };
        var allocation = TestProcessMemory!.Allocate(0x1000, false);
        
        var reservation = TestProcessMemory.Store(value, allocation);
        var read = TestProcessMemory.ReadBytes(reservation.Range.Start, value.Length);
        
        Assert.That(reservation.IsDisposed, Is.False);
        // The resulting range should be a range reserved from our original range.
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation));
        // When we read over the range, we should get the same value we stored.
        Assert.That(read, Is.EqualTo(value));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],bool)"/> method.
    /// Stores a byte array without specifying a range and verifies that the value has been stored properly.
    /// </summary>
    [Test]
    public void StoreWithoutPreAllocationTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };
        
        var reservation = TestProcessMemory!.Store(value);
        var read = TestProcessMemory.ReadBytes(reservation.Range.Start, value.Length);
        
        // The store method should have allocated a new range.
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
        Assert.That(reservation.IsDisposed, Is.False);
        // When we read over the range, we should get the same value we stored.
        Assert.That(read, Is.EqualTo(value));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],bool)"/> method.
    /// Stores a small byte array multiple times without specifying an allocation, and verifies that only one
    /// allocation has been made, with multiple reservations on it.
    /// </summary>
    [Test]
    public void StoreWithoutPreAllocationWithMultipleSmallValuesTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };

        var reservations = Enumerable.Range(0, 4).Select(_ => TestProcessMemory!.Store(value)).ToList();
        var readBackValues = reservations.Select(r => TestProcessMemory!.ReadBytes(r.Range.Start, value.Length));
        
        // The store method should have allocated only one range that's big enough to accomodate all the values.
        Assert.That(TestProcessMemory!.Allocations, Has.Count.EqualTo(1));
        var allocation = TestProcessMemory.Allocations.Single();
        
        // All the results should be reserved ranges from the same parent allocation.
        Assert.That(reservations, Has.All.Matches<MemoryReservation>(r => r.ParentAllocation == allocation));
        
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
        var allocation = TestProcessMemory!.Allocate(0x1000, false);
        var value = new byte[allocation.Range.GetSize()];

        TestProcessMemory!.Store(value);
        
        // So far, we should have only one allocated range.
        Assert.That(TestProcessMemory!.Allocations, Has.Count.EqualTo(1));
        
        // Now we store the same value again, which should overflow the range.
        TestProcessMemory!.Store(value);
        
        // We should have two allocated ranges now, because there is no room left in the first range.
        Assert.That(TestProcessMemory!.Allocations, Has.Count.EqualTo(2));
    }
}