using System.ComponentModel;
using MindControl.Test.ProcessMemoryTests;
using NUnit.Framework;

namespace MindControl.Test.RangeTests;

/// <summary>
/// Tests the <see cref="AllocatedRange"/> class.
/// Because this class is strongly bound to a ProcessMemory, we have to use the <see cref="ProcessMemory.Allocate"/>
/// method to create instances, so the tests below will use an actual instance of <see cref="ProcessMemory"/> and
/// depend on that method.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class AllocatedRangeTest : ProcessMemoryTest
{
    private AllocatedRange _range;

    /// <summary>
    /// Initializes the test by allocating a range of memory.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Allocate a range of memory for the tests
        _range = TestProcessMemory!.Allocate(0x1000, false);
    }

    /// <summary>
    /// Tests the <see cref="AllocatedRange.Dispose"/> method.
    /// Expects the range to be removed from the list of allocated ranges and the memory to be released.
    /// </summary>
    [Test]
    public void DisposeTest()
    {
        var address = _range.Range.Start;
        _range.Dispose();
        
        // Check that the range is now unusable
        Assert.That(_range.IsReserved, Is.False);
        Assert.Throws<ObjectDisposedException>(() => _range.ReserveRange(0x10));
        
        // Check that the range has been removed from the list of allocated ranges
        Assert.That(TestProcessMemory!.AllocatedRanges, Is.Empty);
        
        // Check that the memory has been released (we should not be able to write to it)
        Assert.Throws<Win32Exception>(() => TestProcessMemory.Write(address, 0, MemoryProtectionStrategy.Ignore));
    }

    /// <summary>
    /// Tests the <see cref="AllocatedRange.Dispose"/> method on a reservation.
    /// Expects the range to be removed from the list of the parent allocated range.
    /// </summary>
    [Test]
    public void DisposeReservationTest()
    {
        var reservation = _range.ReserveRange(0x10);
        reservation.Dispose();
        Assert.That(reservation.IsReserved, Is.False);
        Assert.That(_range.ReservedRanges, Is.Empty);
        Assert.That(_range.GetRemainingSpace(), Is.EqualTo(_range.Range.GetSize()));
    }
    
    /// <summary>
    /// Tests the <see cref="AllocatedRange.ReserveRange"/> method.
    /// Reserve a single 0x10 portion of memory in the range.
    /// </summary>
    [Test]
    public void ReserveTest()
    {
        var reservedRange = _range.ReserveRange(0x10);
        
        // Check the reserved range
        Assert.That(reservedRange, Is.Not.Null);
        Assert.That(reservedRange.Range.GetSize(), Is.EqualTo(0x10));
        Assert.That(reservedRange.IsReserved, Is.True);
        Assert.That(reservedRange.ParentRange, Is.EqualTo(_range));
        Assert.That(reservedRange.ReservedRanges, Is.Empty);
        Assert.That(reservedRange.GetRemainingSpace(), Is.EqualTo(0x10));
        Assert.That(reservedRange.GetTotalReservedSpace(), Is.Zero);
        Assert.That(reservedRange.GetLargestReservableSpace()?.GetSize(), Is.EqualTo(0x10));
        
        // Check the effect on the parent range
        Assert.That(_range.ReservedRanges, Has.Member(reservedRange));
        Assert.That(_range.ReservedRanges, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="AllocatedRange.GetRemainingSpace"/> method, before and after reservations.
    /// </summary>
    [Test]
    public void GetRemainingSpaceTest()
    {
        Assert.That(_range.GetRemainingSpace(), Is.EqualTo(_range.Range.GetSize()));
        _range.ReserveRange(0x10);
        var b = _range.ReserveRange(0x10);
        _range.ReserveRange(0x10);
        Assert.That(_range.GetRemainingSpace(), Is.EqualTo(_range.Range.GetSize() - 0x30));

        b.Dispose();
        Assert.That(_range.GetRemainingSpace(), Is.EqualTo(_range.Range.GetSize() - 0x20));
    }
}
