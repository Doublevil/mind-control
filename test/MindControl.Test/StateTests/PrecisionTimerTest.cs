using MindControl.State;
using NUnit.Framework;

namespace MindControl.Test.StateTests;

/// <summary>
/// Tests the <see cref="PrecisionTimer"/> class.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable]
public class PrecisionTimerTest : TimerTest
{
    /// <summary>
    /// Builds the timer that will be tested.
    /// </summary>
    protected override IStateTimer BuildTimer(TimeSpan defaultInterval) => new PrecisionTimer(defaultInterval);
}