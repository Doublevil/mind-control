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
        Assert.That(TestProcessMemory!.AllocatedRanges, Has.Member(range));
    }
}