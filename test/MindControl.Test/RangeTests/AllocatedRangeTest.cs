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
    
}
