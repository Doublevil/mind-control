using System.ComponentModel;
using MindControl.Results;
using MindControl.Test.ProcessMemoryTests;
using NUnit.Framework;

namespace MindControl.Test.AllocationTests;

/// <summary>
/// Tests the <see cref="MemoryAllocation"/> class.
/// Because this class is strongly bound to a ProcessMemory, we have to use the <see cref="ProcessMemory.Allocate"/>
/// method to create instances, so the tests below will use an actual instance of <see cref="ProcessMemory"/> and
/// depend on that method.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MemoryAllocationTest : BaseProcessMemoryTest
{
    private MemoryAllocation _allocation;

    /// <summary>
    /// Initializes the test by allocating a range of memory.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Allocate a range of memory for the tests
        _allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.Dispose"/> method.
    /// Expects the allocation to be removed from the list of managed allocations, and the memory to be released.
    /// </summary>
    [Test]
    public void DisposeTest()
    {
        var address = _allocation.Range.Start;
        _allocation.Dispose();
        
        // Check that the allocation instance is now disposed and unusable
        Assert.That(_allocation.IsDisposed, Is.True);
        Assert.Throws<ObjectDisposedException>(() => _allocation.ReserveRange(0x10));
        
        // Check that the allocation has been removed from the list
        Assert.That(TestProcessMemory!.Allocations, Is.Empty);
        
        // Check that the memory has been released (we should not be able to write to it)
        Assert.That(TestProcessMemory.Write(address, 0, MemoryProtectionStrategy.Ignore).IsSuccess, Is.False);
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Reserve a single 0x10 portion of memory in the range and check the reservation and allocation properties.
    /// </summary>
    [Test]
    public void ReserveRangeTest()
    {
        var result = _allocation.ReserveRange(0x10);
        Assert.That(result.IsSuccess, Is.True);
        
        var reservedRange = result.Value;
        
        // Check the reserved range
        Assert.That(reservedRange, Is.Not.Null);
        Assert.That(reservedRange.Range.GetSize(), Is.EqualTo(0x10));
        Assert.That(reservedRange.IsDisposed, Is.False);
        
        // Check that the reservation is added to the list in the allocation instance
        Assert.That(_allocation.Reservations, Has.Member(reservedRange));
        Assert.That(_allocation.Reservations, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Reserve the whole allocation range in one time.
    /// </summary>
    [Test]
    public void ReserveRangeWithFullRangeTest()
    {
        var result = _allocation.ReserveRange(_allocation.Range.GetSize());
        Assert.That(result.IsSuccess, Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Attempt to reserve 0 bytes. This should throw.
    /// </summary>
    [Test]
    public void ReserveRangeWithZeroSizeTest()
        => Assert.Throws<ArgumentException>(() => _allocation.ReserveRange(0));

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Attempt to reserve 1 byte more than the size of the full reservation range. This should fail.
    /// </summary>
    [Test]
    public void ReserveRangeWithRangeLargerThanAllocationTest()
    {
        var result = _allocation.ReserveRange(0x1001);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<ReservationFailureOnNoSpaceAvailable>());
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Reserve 0x100 bytes, and then attempt to reserve 0x1000 bytes (full allocation range).
    /// The second reservation should fail.
    /// </summary>
    [Test]
    public void ReserveRangeWithMultipleReservationsTooLargeToFitTest()
    {
        _allocation.ReserveRange(0x100);
        var result = _allocation.ReserveRange(0x1000);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.TypeOf<ReservationFailureOnNoSpaceAvailable>());
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Reserve 5 bytes, two times in a row, with an alignment of 4.
    /// The resulting reservations should be [0,7] and [8,15], relative to the start of the allocated range.
    /// </summary>
    [Test]
    public void ReserveRangeWithRealignmentTest()
    {
        var a = _allocation.ReserveRange(5, byteAlignment: 4).Value;
        var b = _allocation.ReserveRange(5, byteAlignment: 4).Value;
        
        Assert.That(a.Range, Is.EqualTo(new MemoryRange(_allocation.Range.Start, _allocation.Range.Start + 7)));
        Assert.That(b.Range, Is.EqualTo(new MemoryRange(_allocation.Range.Start + 8, _allocation.Range.Start + 15)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ReserveRange"/> method.
    /// Reserve 5 bytes, two times in a row, with no alignment.
    /// The resulting reservations should be [0,4] and [5,9], relative to the start of the allocated range.
    /// </summary>
    [Test]
    public void ReserveRangeWithNoAlignmentTest()
    {
        var a = _allocation.ReserveRange(5, byteAlignment: null).Value;
        var b = _allocation.ReserveRange(5, byteAlignment: null).Value;
        
        Assert.That(a.Range, Is.EqualTo(new MemoryRange(_allocation.Range.Start, _allocation.Range.Start + 4)));
        Assert.That(b.Range, Is.EqualTo(new MemoryRange(_allocation.Range.Start + 5, _allocation.Range.Start + 9)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetRemainingSpace"/> method, before and after reservations.
    /// </summary>
    [Test]
    public void GetRemainingSpaceTest()
    {
        Assert.That(_allocation.GetRemainingSpace(), Is.EqualTo(_allocation.Range.GetSize()));
        
        // Make 3 0x10 allocations and check that the remaining space is total space - 0x30
        _allocation.ReserveRange(0x10);
        var b = _allocation.ReserveRange(0x10).Value;
        _allocation.ReserveRange(0x10);
        Assert.That(_allocation.GetRemainingSpace(), Is.EqualTo(_allocation.Range.GetSize() - 0x30));

        // Dispose the 2nd reservation and check that the remaining space is total space - 0x20
        b.Dispose();
        Assert.That(_allocation.GetRemainingSpace(), Is.EqualTo(_allocation.Range.GetSize() - 0x20));
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetTotalReservedSpace"/> method, before and after reservations.
    /// </summary>
    [Test]
    public void GetTotalReservedSpaceTest()
    {
        Assert.That(_allocation.GetTotalReservedSpace(), Is.Zero);
        
        // Make 3 0x10 allocations and check that the reserved space is 0x30
        _allocation.ReserveRange(0x10);
        var b = _allocation.ReserveRange(0x10).Value;
        _allocation.ReserveRange(0x10);
        Assert.That(_allocation.GetTotalReservedSpace(), Is.EqualTo(0x30));

        // Dispose the 2nd reservation and check that the reserved space is 0x20
        b.Dispose();
        Assert.That(_allocation.GetTotalReservedSpace(), Is.EqualTo(0x20));
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetLargestReservableSpace"/> method, before and after reservations.
    /// </summary>
    [Test]
    public void GetLargestReservableSpaceTest()
    {
        Assert.That(_allocation.GetLargestReservableSpace(), Is.EqualTo(_allocation.Range));
        
        // Make 3 0x500 allocations. The largest unreserved space should now be the last 0x100 bytes of the range.
        _allocation.ReserveRange(0x500);
        var b = _allocation.ReserveRange(0x500).Value;
        _allocation.ReserveRange(0x500);
        var expectedRange = new MemoryRange(_allocation.Range.End - 0xFF, _allocation.Range.End);
        Assert.That(_allocation.GetLargestReservableSpace(), Is.EqualTo(expectedRange));

        // Dispose the 2nd reservation. Its range should now be the largest unreserved space.
        b.Dispose();
        Assert.That(_allocation.GetLargestReservableSpace(), Is.EqualTo(b.Range));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetLargestReservableSpace"/> method, with no space remaining.
    /// </summary>
    [Test]
    public void GetLargestReservableSpaceWithNoSpaceRemainingTest()
    {
        _allocation.ReserveRange(_allocation.Range.GetSize());
        Assert.That(_allocation.GetLargestReservableSpace(), Is.Null);
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetNextRangeFittingSize"/> method, before and after reservations.
    /// </summary>
    [Test]
    public void GetNextRangeFittingSizeTest()
    {
        Assert.That(_allocation.GetNextRangeFittingSize(0x10),
            Is.EqualTo(new MemoryRange(_allocation.Range.Start, _allocation.Range.Start + 0xF)));
        
        // Make 3 0x10 allocations. The next available 0x10 space should be right after these three.
        _allocation.ReserveRange(0x10);
        var b = _allocation.ReserveRange(0x10).Value;
        _allocation.ReserveRange(0x10);
        Assert.That(_allocation.GetNextRangeFittingSize(0x10),
            Is.EqualTo(new MemoryRange(_allocation.Range.Start + 0x30, _allocation.Range.Start + 0x3F)));

        // Dispose the 2nd reservation. The next available 0x10 space should now be the original 2nd reservation range.
        b.Dispose();
        Assert.That(_allocation.GetNextRangeFittingSize(0x10), Is.EqualTo(b.Range));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetNextRangeFittingSize"/> method, with the size of the whole allocated
    /// range.
    /// The resulting range should be the same as the allocation range.
    /// </summary>
    [Test]
    public void GetNextRangeFittingSizeWithFullAllocationRangeTest()
    {
        Assert.That(_allocation.GetNextRangeFittingSize(_allocation.Range.GetSize()), Is.EqualTo(_allocation.Range));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetNextRangeFittingSize"/> method.
    /// Reserves the full allocated range, and then attempt to get the next range fitting 1 byte.
    /// The result should be null.
    /// </summary>
    [Test]
    public void GetNextRangeFittingSizeWithNoSpaceRemainingTest()
    {
        _allocation.ReserveRange(_allocation.Range.GetSize());
        Assert.That(_allocation.GetNextRangeFittingSize(1), Is.Null);
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetNextRangeFittingSize"/> method.
    /// Call the method with a size of 5 bytes, and an alignment of 4.
    /// The resulting range should be [0,7], relative to the start of the allocated range.
    /// </summary>
    [Test]
    public void GetNextRangeFittingSizeWithRealignmentTest()
    {
        Assert.That(_allocation.GetNextRangeFittingSize(5, byteAlignment: 4),
            Is.EqualTo(new MemoryRange(_allocation.Range.Start, _allocation.Range.Start + 7)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.GetNextRangeFittingSize"/> method.
    /// Call the method with a size of 5 bytes, and no alignment.
    /// The resulting range should be [0,4], relative to the start of the allocated range.
    /// </summary>
    [Test]
    public void GetNextRangeFittingSizeWithNoAlignmentTest()
    {
        Assert.That(_allocation.GetNextRangeFittingSize(5, byteAlignment: null),
            Is.EqualTo(new MemoryRange(_allocation.Range.Start, _allocation.Range.Start + 4)));
    }

    /// <summary>
    /// Tests the <see cref="MemoryAllocation.FreeRange"/> method.
    /// Reserve a range of 0x10 bytes, and then free the range of that reservation.
    /// After this operation, there should be no reservations in the allocation, and the reservation should be disposed.
    /// </summary>
    [Test]
    public void FreeRangeWithFullReservationRangeTest()
    {
        var reservation = _allocation.ReserveRange(0x10).Value;
        _allocation.FreeRange(reservation.Range);
        
        Assert.That(reservation.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Is.Empty);
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.FreeRange"/> method.
    /// Reserve a range of 0x10 bytes, and then free the range [-4,4], relative to the start of the allocated range.
    /// After this operation, the reservation should be disposed, and a new reservation with a range of [5,F] should
    /// be present in the allocation.
    /// </summary>
    [Test]
    public void FreeRangeWithRangeOverlappingStartOfExistingReservationTest()
    {
        var reservation = _allocation.ReserveRange(0x10).Value;
        _allocation.FreeRange(new MemoryRange(reservation.Range.Start - 4, reservation.Range.Start + 4));
        
        Assert.That(reservation.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Has.Count.EqualTo(1));
        Assert.That(_allocation.Reservations[0].Range,
            Is.EqualTo(new MemoryRange(reservation.Range.Start + 5, reservation.Range.End)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.FreeRange"/> method.
    /// Reserve a range of 0x10 bytes, and then free the range [4,F], relative to the start of the allocated range.
    /// After this operation, the reservation should be disposed, and a new reservation with a range of [0,3] should
    /// be present in the allocation.
    /// </summary>
    [Test]
    public void FreeRangeWithRangeOverlappingEndOfExistingReservationTest()
    {
        var reservation = _allocation.ReserveRange(0x10).Value;
        _allocation.FreeRange(new MemoryRange(reservation.Range.Start + 4, reservation.Range.Start + 0xF));
        
        Assert.That(reservation.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Has.Count.EqualTo(1));
        Assert.That(_allocation.Reservations[0].Range,
            Is.EqualTo(new MemoryRange(reservation.Range.Start, reservation.Range.Start + 3)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.FreeRange"/> method.
    /// Reserve a range of 0x10 bytes, and then free the range [4,6], relative to the start of the allocated range.
    /// After this operation, the reservation should be disposed, and two new reservations, with ranges of [0,3] and
    /// [7,F], should be present in the allocation.
    /// </summary>
    [Test]
    public void FreeRangeWithRangeInsideExistingReservationTest()
    {
        var reservation = _allocation.ReserveRange(0x10).Value;
        _allocation.FreeRange(new MemoryRange(reservation.Range.Start + 4, reservation.Range.Start + 6));
        
        Assert.That(reservation.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Has.Count.EqualTo(2));
        Assert.That(_allocation.Reservations[0].Range,
            Is.EqualTo(new MemoryRange(reservation.Range.Start, reservation.Range.Start + 3)));
        Assert.That(_allocation.Reservations[1].Range,
            Is.EqualTo(new MemoryRange(reservation.Range.Start + 7, reservation.Range.End)));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.FreeRange"/> method.
    /// Reserve 3 ranges of 0x10 bytes, and then free the range [0x8,0x1F], relative to the start of the allocated
    /// range.
    /// After this operation, the 2 first reservations should be disposed, and the allocation should hold 2 reservations
    /// with the original third reservation, and a new one with a range of [0,7].
    /// </summary>
    [Test]
    public void FreeRangeWithRangeOverlappingMultipleExistingReservationsTest()
    {
        var a = _allocation.ReserveRange(0x10).Value;
        var b = _allocation.ReserveRange(0x10).Value;
        var c = _allocation.ReserveRange(0x10).Value;
        _allocation.FreeRange(new MemoryRange(a.Range.Start + 8, b.Range.End));
        
        Assert.That(a.IsDisposed, Is.True);
        Assert.That(b.IsDisposed, Is.True);
        Assert.That(c.IsDisposed, Is.False);
        Assert.That(_allocation.Reservations, Has.Count.EqualTo(2));
        Assert.That(_allocation.Reservations.Select(r => r.Range),
            Has.Member(new MemoryRange(a.Range.Start, a.Range.Start + 7)));
        Assert.That(_allocation.Reservations, Has.Member(c));
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryAllocation.ClearReservations"/> method.
    /// Reserve 3 ranges, and then clear the reservations. The reservations should be disposed, and the allocation
    /// should have no reservations.
    /// </summary>
    [Test]
    public void ClearReservationsTest()
    {
        var a = _allocation.ReserveRange(0x10).Value;
        var b = _allocation.ReserveRange(0x10).Value;
        var c = _allocation.ReserveRange(0x10).Value;
        _allocation.ClearReservations();
        
        Assert.That(a.IsDisposed, Is.True);
        Assert.That(b.IsDisposed, Is.True);
        Assert.That(c.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Is.Empty);
    }
}
