using MindControl.Test.ProcessMemoryTests;
using NUnit.Framework;

namespace MindControl.Test.AllocationTests;

/// <summary>
/// Tests the <see cref="MemoryReservation"/> class.
/// Because this class is strongly bound to <see cref="MemoryAllocation"/>, which is itself bound to ProcessMemory,
/// we have to use the <see cref="ProcessMemory.Allocate"/> method to create instances, so the tests below will use an
/// actual instance of <see cref="ProcessMemory"/> and depend on that method.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MemoryReservationTest : ProcessMemoryTest
{
    private MemoryAllocation _allocation;
    private MemoryReservation _reservation;

    /// <summary>
    /// Initializes the test by allocating a range of memory and making a reservation.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Allocate a range of memory for the tests
        _allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        
        // Reserve a portion of the memory
        _reservation = _allocation.ReserveRange(0x10).Value;
    }
    
    /// <summary>
    /// Tests the <see cref="MemoryReservation.Dispose"/> method.
    /// Expects the instance to be disposed and unusable, and to be removed from the list of the parent allocation.
    /// </summary>
    [Test]
    public void DisposeReservationTest()
    {
        _reservation.Dispose();
        Assert.That(_reservation.IsDisposed, Is.True);
        Assert.That(_allocation.Reservations, Is.Empty);
    }
}