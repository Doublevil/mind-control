using System.Text;
using MindControl.Results;
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
        var allocationResult = TestProcessMemory!.Allocate(0x1000, false);
        
        Assert.That(allocationResult.IsSuccess, Is.True);
        var allocation = allocationResult.Value;
        
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
        var allocationResult = TestProcessMemory!.Allocate(0x1000, true);
        
        Assert.That(allocationResult.IsSuccess, Is.True);
        var allocation = allocationResult.Value;
        Assert.That(allocation, Is.Not.Null);
        Assert.That(allocation.IsDisposed, Is.False);
        Assert.That(allocation.Range.GetSize(), Is.AtLeast(0x1000));
        Assert.That(allocation.IsExecutable, Is.True);
        Assert.That(TestProcessMemory!.Allocations, Has.Member(allocation));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method.
    /// Performs two allocations: one with a range and one without a range.
    /// The range starts after the expected range of the rangeless allocation.
    /// Expect the rangeless allocation to be outside of the range, and the ranged allocation to be within the range.
    /// </summary>
    [Test]
    public void AllocateWithinRangeTest()
    {
        var range = new MemoryRange(new UIntPtr(0x120000), UIntPtr.MaxValue);
        var allocationWithoutRangeResult = TestProcessMemory!.Allocate(0x1000, false);
        var allocationWithRangeResult = TestProcessMemory!.Allocate(0x1000, false, range);
        Assert.That(allocationWithoutRangeResult.IsSuccess, Is.True);
        Assert.That(allocationWithRangeResult.IsSuccess, Is.True);
        Assert.That(range.Contains(allocationWithoutRangeResult.Value.Range), Is.False);
        Assert.That(range.Contains(allocationWithRangeResult.Value.Range), Is.True);
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method.
    /// Performs two allocations: one with a specific "nearAddress", the other one with default parameters.
    /// The one with a specific "nearAddress" should be allocated closer to the specified address than the other one.
    /// </summary>
    [Test]
    public void AllocateNearAddressTest()
    {
        var nearAddress = new UIntPtr(0x400000000000);
        var allocationWithNearAddressResult = TestProcessMemory!.Allocate(0x1000, false, nearAddress: nearAddress);
        var allocationWithoutNearAddressResult = TestProcessMemory!.Allocate(0x1000, false);
        Assert.That(allocationWithoutNearAddressResult.IsSuccess, Is.True);
        Assert.That(allocationWithNearAddressResult.IsSuccess, Is.True);
        
        Assert.That(allocationWithNearAddressResult.Value.Range.DistanceTo(nearAddress),
            Is.LessThan(allocationWithoutNearAddressResult.Value.Range.DistanceTo(nearAddress)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Allocate"/> method with a zero size.
    /// This should return an <see cref="AllocationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void AllocateZeroTest()
    {
        var allocateResult = TestProcessMemory!.Allocate(0, false);
        Assert.That(allocateResult.IsSuccess, Is.False);
        Assert.That(allocateResult.Error, Is.InstanceOf<AllocationFailureOnInvalidArguments>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs an allocation, and then calls the tested method with a size that fits the previously allocated range.
    /// This should perform a reservation on the existing allocation.
    /// </summary>
    [Test]
    public void ReserveWithAvailableAllocationTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        var reservationResult = TestProcessMemory.Reserve(0x1000, false);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.Range.GetSize(), Is.EqualTo(0x1000));
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Calls the tested method without any existing allocation.
    /// This should perform a new allocation and a reservation on it.
    /// </summary>
    [Test]
    public void ReserveWithoutAvailableAllocationTest()
    {
        var reservationResult = TestProcessMemory!.Reserve(0x1000, false);
        Assert.That(reservationResult.IsSuccess, Is.True);
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
        Assert.That(reservationResult.Value.Range.GetSize(), Is.EqualTo(0x1000));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs a code allocation, and then calls the tested method with a size that fits the previously allocated
    /// range, but for data, not code.
    /// This should still perform a reservation on the existing allocation, because code allocations can be used for
    /// data.
    /// </summary>
    [Test]
    public void ReserveForDataWithAvailableCodeAllocationTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, true).Value;
        var reservationResult = TestProcessMemory.Reserve(0x1000, false);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs a data allocation, and then calls the tested method with a size that fits the previously allocated
    /// range, but for executable code, not data.
    /// This should perform a new allocation with executable permissions (because data allocations cannot be used for
    /// code), and a reservation on it.
    /// </summary>
    [Test]
    public void ReserveForCodeWithAvailableDataAllocationTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        var reservationResult = TestProcessMemory.Reserve(0x1000, true);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.Not.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(2));
        Assert.That(reservation.ParentAllocation.IsExecutable, Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs a code allocation, and then calls the tested method with a size that fits the previously allocated
    /// range, and for code.
    /// This should perform a reservation on the existing allocation.
    /// </summary>
    [Test]
    public void ReserveForCodeWithAvailableCodeAllocationTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, true).Value;
        var reservationResult = TestProcessMemory.Reserve(0x1000, true);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs an allocation of 0x1000 bytes, and then calls the tested method with a size of 0x2000 bytes.
    /// This should perform a new allocation and a reservation on it.
    /// </summary>
    [Test]
    public void ReserveTooLargeForAvailableAllocationsTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, true).Value;
        var reservationResult = TestProcessMemory.Reserve(0x2000, true);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.Not.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs an allocation, and then calls the tested method with a size that fits the previously allocated range,
    /// but with a limit range that is outside of the existing allocated range.
    /// This should perform a new allocation and a reservation on it.
    /// </summary>
    [Test]
    public void ReserveWithLimitRangeTest()
    {
        var range = new MemoryRange(unchecked((UIntPtr)0x400000000000), UIntPtr.MaxValue); 
        var allocation = TestProcessMemory!.Allocate(0x1000, true).Value;
        var reservationResult = TestProcessMemory.Reserve(0x1000, true, range);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.Not.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(2));
        Assert.That(range.Contains(reservation.Address));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method.
    /// Performs 3 allocations at various ranges, and then calls the tested method with a size that fits all previously
    /// allocated ranges, but with a near address that is closer to the second allocation.
    /// This should perform a reservation on the second allocation.
    /// </summary>
    [Test]
    public void ReserveWithNearAddressTest()
    {
        TestProcessMemory!.Allocate(0x1000, true,
            new MemoryRange(unchecked((UIntPtr)0x400000000000), UIntPtr.MaxValue));
        var allocation2 = TestProcessMemory!.Allocate(0x1000, true,
            new MemoryRange(unchecked((UIntPtr)0x200000000000), UIntPtr.MaxValue)).Value;
        TestProcessMemory!.Allocate(0x1000, true,
            new MemoryRange(unchecked((UIntPtr)0x4B0000000000), UIntPtr.MaxValue));
        
        var reservationResult = TestProcessMemory.Reserve(0x1000, true, nearAddress:unchecked((UIntPtr)0x2000051C0000));
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation2));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(3));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.Reserve"/> method with a size of zero.
    /// Expects the result to be an <see cref="AllocationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void ReserveZeroTest()
    {
        var reserveResult = TestProcessMemory!.Reserve(0, false);
        Assert.That(reserveResult.IsSuccess, Is.False);
        Assert.That(reserveResult.Error, Is.InstanceOf<AllocationFailureOnInvalidArguments>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.Store(byte[],MemoryAllocation)"/> method.
    /// Stores a byte array in an allocated range and verifies that the value has been stored properly.
    /// </summary>
    [Test]
    public void StoreWithAllocationTest()
    {
        var value = new byte[] { 1, 2, 3, 4 };
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        
        var reservationResult = TestProcessMemory.Store(value, allocation);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        byte[] read = TestProcessMemory.ReadBytes(reservation.Range.Start, value.Length).Value;
        
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
        
        var reservationResult = TestProcessMemory!.Store(value);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        var read = TestProcessMemory.ReadBytes(reservation.Range.Start, value.Length).Value;
        
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

        var reservationResults = Enumerable.Range(0, 4).Select(_ => TestProcessMemory!.Store(value)).ToList();
        foreach (var result in reservationResults)
            Assert.That(result.IsSuccess, Is.True);
        
        var reservations = reservationResults.Select(r => r.Value).ToList();
        var readBackValues = reservations.Select(r => TestProcessMemory!.ReadBytes(r.Range.Start, value.Length)
            .GetValueOrDefault());
        
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
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        var value = new byte[allocation.Range.GetSize()];

        var firstStoreResult = TestProcessMemory!.Store(value);
        
        // So far, we should have only one allocated range.
        Assert.That(firstStoreResult.IsSuccess, Is.True);
        Assert.That(TestProcessMemory!.Allocations, Has.Count.EqualTo(1));
        
        // Now we store the same value again, which should overflow the range.
        var secondStoreResult = TestProcessMemory!.Store(value);
        
        // We should have two allocated ranges now, because there is no room left in the first range.
        Assert.That(secondStoreResult.IsSuccess, Is.True);
        Assert.That(TestProcessMemory!.Allocations, Has.Count.EqualTo(2));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings)"/> method.
    /// Stores a string with specific settings and read them back using the same settings.
    /// When read back, the string should be the same as the original string.
    /// </summary>
    [Test]
    public void StoreStringWithoutAllocationTest()
    {
        var stringToStore = "Hello 世界!";
        var reservationResult = TestProcessMemory!.StoreString(stringToStore, DotNetStringSettings);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;

        var bytesReadBack = TestProcessMemory.ReadBytes(reservation.Address, reservation.Range.GetSize()).Value;
        var stringReadBack = DotNetStringSettings.GetString(bytesReadBack);
        
        // The store method should have allocated a new range.
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
        Assert.That(reservation.IsDisposed, Is.False);
        
        // When we read back our string, we should get the same value we stored.
        Assert.That(stringReadBack, Is.EqualTo(stringToStore));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings,MemoryAllocation)"/> method.
    /// Stores a string in a pre-allocated range with specific settings and read them back using the same settings.
    /// When read back, the string should be the same as the original string.
    /// </summary>
    [Test]
    public void StoreStringWithAllocationTest()
    {
        var stringToStore = "Hello 世界!";
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        
        var reservationResult = TestProcessMemory.StoreString(stringToStore, DotNetStringSettings, allocation);
        Assert.That(reservationResult.IsSuccess, Is.True);
        var reservation = reservationResult.Value;
        
        var bytesReadBack = TestProcessMemory.ReadBytes(reservation.Address, reservation.Range.GetSize()).Value;
        var stringReadBack = DotNetStringSettings.GetString(bytesReadBack);
        
        Assert.That(reservation.IsDisposed, Is.False);
        // The resulting range should be a range reserved from our original range.
        Assert.That(reservation.ParentAllocation, Is.EqualTo(allocation));
        Assert.That(TestProcessMemory.Allocations, Has.Count.EqualTo(1));
        // When we read over the range, we should get the same value we stored.
        Assert.That(stringReadBack, Is.EqualTo(stringToStore));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings)"/> method.
    /// Specify invalid settings. The result is expected to be an <see cref="AllocationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void StoreStringWithoutAllocationWithInvalidSettingsTest()
    {
        var invalidSettings = new StringSettings(Encoding.UTF8, isNullTerminated: false, lengthPrefix: null);
        var result = TestProcessMemory!.StoreString("Hello world", invalidSettings);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.InstanceOf<AllocationFailureOnInvalidArguments>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings)"/> method.
    /// Specify valid settings, but with a length prefix that is too short to store the provided string. The result is
    /// expected to be an <see cref="AllocationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void StoreStringWithoutAllocationWithIncompatibleSettingsTest()
    {
        var settingsThatCanOnlyStoreUpTo255Chars = new StringSettings(Encoding.UTF8, isNullTerminated: false,
            new StringLengthPrefix(1, StringLengthUnit.Characters));
        var result = TestProcessMemory!.StoreString(new string('a', 256), settingsThatCanOnlyStoreUpTo255Chars);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.InstanceOf<AllocationFailureOnInvalidArguments>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings,MemoryAllocation)"/> method.
    /// Specify invalid settings. The result is expected to be an <see cref="ReservationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void StoreStringWithAllocationWithInvalidSettingsTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        var invalidSettings = new StringSettings(Encoding.UTF8, isNullTerminated: false, lengthPrefix: null);
        var result = TestProcessMemory!.StoreString("Hello world", invalidSettings, allocation);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.InstanceOf<ReservationFailureOnInvalidArguments>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.StoreString(string,StringSettings,MemoryAllocation)"/> method.
    /// Specify valid settings, but with a length prefix that is too short to store the provided string. The result is
    /// expected to be an <see cref="ReservationFailureOnInvalidArguments"/>.
    /// </summary>
    [Test]
    public void StoreStringWithAllocationWithIncompatibleSettingsTest()
    {
        var allocation = TestProcessMemory!.Allocate(0x1000, false).Value;
        var settingsThatCanOnlyStoreUpTo255Chars = new StringSettings(Encoding.UTF8, isNullTerminated: false,
            new StringLengthPrefix(1, StringLengthUnit.Characters));
        var result = TestProcessMemory!.StoreString(new string('a', 256), settingsThatCanOnlyStoreUpTo255Chars,
            allocation);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.InstanceOf<ReservationFailureOnInvalidArguments>());
    }
}