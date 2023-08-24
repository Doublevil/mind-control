using MindControl.State;
using NUnit.Framework;

namespace MindControl.Test.StateTests;

/// <summary>
/// Base testing class for <see cref="IStateTimer"/> implementations.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable]
public abstract class TimerTest
{
    /// <summary>
    /// Builds the timer that will be tested.
    /// </summary>
    protected abstract IStateTimer BuildTimer(TimeSpan defaultInterval);
    
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);
    private IStateTimer? _testedInstance;
    private int _tickCount;
    
    /// <summary>
    /// Sets up the tested instance and testing framework for this test suite.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _testedInstance = BuildTimer(DefaultInterval);
        _testedInstance.Tick += (_, _) => _tickCount++;
    }
    
    /// <summary>
    /// Tests that the timer is not running after building it, and that it does not tick in this state. 
    /// </summary>
    [Test]
    public void NeverTickWhenNotStartedTest()
    {
        Thread.Sleep(500);
        Assert.That(_tickCount, Is.Zero);
    }
    
    /// <summary>
    /// Tests that the timer starts running after calling the Start method, and ticks in that state. 
    /// </summary>
    [Test]
    public void TickWhenStartedTest()
    {
        _testedInstance!.Start();
        Thread.Sleep(500);
        
        // Do not rely on precise times or tick counts, because test runners might be very slow.
        // So even though it should tick about 5 times, test that it at least has ticked 3 times and no more than 6.
        Assert.That(_tickCount, Is.InRange(3, 6));
    }
    
    /// <summary>
    /// Tests that calling Start and then Stop puts the timer in a state that prevents it from ticking. 
    /// </summary>
    [Test]
    public void DoNotTickWhenStoppedTest()
    {
        _testedInstance!.Start();
        _testedInstance.Stop();
        Thread.Sleep(500);
        Assert.That(_tickCount, Is.Zero);
    }
    
    /// <summary>
    /// Tests that calling ForceTick will make the timer tick even when it is not running. 
    /// </summary>
    [Test]
    public void ForceTickTicksWhenStoppedTest()
    {
        _testedInstance!.ForceTick();
        Assert.That(_tickCount, Is.EqualTo(1));
    }
    
    /// <summary>
    /// Tests that calling ForceTick will not reset the timer. 
    /// </summary>
    [Test]
    public void ForceTickDoesNotResetTimerTest()
    {
        _testedInstance!.Start();
        for (int i = 0; i < 10; i++)
        {
            _testedInstance.ForceTick();
            Thread.Sleep(50);
        }
        
        // Do not rely on precise times or tick counts, because test runners might be very slow.
        // So, even though it should have ticked around 15 times (10 forced + 5 auto), test that it has ticked
        // at least 13 times. 
        Assert.That(_tickCount, Is.AtLeast(13));
    }
    
    /// <summary>
    /// Tests that changing the interval alters the number of ticks in the same interval. 
    /// </summary>
    [Test]
    public void ChangeIntervalAndStartTest()
    {
        _testedInstance!.Interval = TimeSpan.FromMilliseconds(300);
        _testedInstance.Start();
        Thread.Sleep(500);
        Assert.That(_tickCount, Is.EqualTo(1));
    }
    
    /// <summary>
    /// Tests that changing the interval after starting the timer alters the number of ticks in the same interval.
    /// We intentionally leave some leeway to allow timer implementations to take in account the interval change only
    /// after the next tick if that's more practical.
    /// </summary>
    [Test]
    public void ChangeIntervalAfterStartTest()
    {
        _testedInstance!.Start();
        Thread.Sleep(50);
        _testedInstance.Interval = TimeSpan.FromMilliseconds(30);
        Thread.Sleep(500);
        
        // Do not rely on precise times or tick counts, because test runners might be very slow.
        // And also leave some leeway for implementation details.
        Assert.That(_tickCount, Is.AtLeast(6));
    }
}