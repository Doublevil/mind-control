using MindControl.State;
using NUnit.Framework;

namespace MindControl.Test.StateTests;

/// <summary>
/// Tests the <see cref="StateWatcher{T}"/> class.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class StateWatcherTest
{
    private class TestTimer : IStateTimer
    {
        private bool _isRunning;
        public bool IsRunning => _isRunning;
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(42);
        public event EventHandler? Tick;
        public void Start() => _isRunning = true;
        public void Stop() => _isRunning = false;
        public void ForceTick() => Tick?.Invoke(this, EventArgs.Empty);
    }
    
    private class TestStateWatcher : StateWatcher<string>
    {
        /// <summary>Set to true to raise an exception in the ReadState method.</summary>
        public bool SimulateException { get; set; }
        
        /// <summary>Set to true to wait before returning a value in the ReadState method.</summary>
        public bool SimulateLongLastingUpdate { get; set; }
        
        private int _i;
        public TestStateWatcher(IStateTimer stateTimer) : base(stateTimer) { }

        protected override string ReadState()
        {
            if (SimulateLongLastingUpdate)
                Thread.Sleep(200);

            if (SimulateException)
                throw new Exception("This exception was raised for test purposes.");
            
            return $"State{_i++}";
        }
    }

    private TestTimer? _timer;
    private TestStateWatcher? _testedInstance;
    private int _updateCount;
    private int _skippedCount;
    private int _exceptionCount;
    
    /// <summary>
    /// Sets up common components for the tests.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _timer = new TestTimer();
        _testedInstance = new TestStateWatcher(_timer);
        _testedInstance.StateUpdated += (_, _) => _updateCount++;
        _testedInstance.StateUpdateFailed += (_, _) => _exceptionCount++;
        _testedInstance.StateUpdateSkipped += (_, _) => _skippedCount++;
    }

    /// <summary>
    /// Verifies that the initial state of the tested instance is null.
    /// Also verifies all initial state properties.
    /// </summary>
    [Test]
    public void InitialStateIsNullTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_testedInstance!.LatestState, Is.Null);
            Assert.That(_testedInstance!.LatestUpdateTime, Is.Null);
            Assert.That(_testedInstance!.LatestException, Is.Null);
            Assert.That(_testedInstance!.LatestExceptionTime, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="StateWatcher{T}.Start"/> starts the timer.
    /// </summary>
    [Test]
    public void StartStartsTimerTest()
    {
        Assert.That(_timer!.IsRunning, Is.False);
        _testedInstance!.Start();
        Assert.That(_timer.IsRunning, Is.True);
    }

    /// <summary>
    /// Verifies that calling <see cref="StateWatcher{T}.Stop"/> stops the timer.
    /// </summary>
    [Test]
    public void StopStopsTimerTest()
    {
        _testedInstance!.Start();
        Assert.That(_timer!.IsRunning, Is.True);
        _testedInstance.Stop();
        Assert.That(_timer.IsRunning, Is.False);
    }
    
    /// <summary>
    /// Force the timer to tick. Verify that the state is updated every time the timer ticks.
    /// </summary>
    [Test]
    public void TicksUpdateStateTest()
    {
        Assert.That(_testedInstance!.LatestState, Is.Null);
        
        _timer!.ForceTick();
        Assert.Multiple(() =>
        {
            Assert.That(_updateCount, Is.EqualTo(1));
            Assert.That(_skippedCount, Is.Zero);
            Assert.That(_exceptionCount, Is.Zero);
            Assert.That(_testedInstance.LatestState, Is.EqualTo("State0"));
            Assert.That(_testedInstance.LatestUpdateTime, Is.Not.Null);
            Assert.That(_testedInstance.LatestException, Is.Null);
            Assert.That(_testedInstance.LatestExceptionTime, Is.Null);
        });
        var latestUpdateTime = _testedInstance.LatestUpdateTime;
        
        _timer!.ForceTick();
        Assert.Multiple(() =>
        {
            Assert.That(_updateCount, Is.EqualTo(2));
            Assert.That(_skippedCount, Is.Zero);
            Assert.That(_exceptionCount, Is.Zero);
            Assert.That(_testedInstance.LatestState, Is.EqualTo("State1"));
            Assert.That(_testedInstance.LatestUpdateTime, Is.GreaterThan(latestUpdateTime));
            Assert.That(_testedInstance.LatestException, Is.Null);
            Assert.That(_testedInstance.LatestExceptionTime, Is.Null);
        });
    }

    /// <summary>
    /// Sets the state watcher to simulate a long lasting state read.
    /// Tick twice in a row and verify that one of the updates has been skipped.
    /// Wait long enough for the long lasting update to finish and verify that it does succeed.
    /// </summary>
    [Test]
    public void SimultaneousTicksAreSkippedTest()
    {
        _testedInstance!.SimulateLongLastingUpdate = true;
        
        // Start the first update in the background, wait a bit for it to start, and start a second update that should
        // be instantly skipped.
        Task.Run(() => _timer!.ForceTick());
        Thread.Sleep(50);
        _timer!.ForceTick();
        
        // For now, the 1st update still has not completed, but the 2nd one has been skipped.
        Assert.Multiple(() =>
        {
            Assert.That(_updateCount, Is.Zero);
            Assert.That(_skippedCount, Is.EqualTo(1));
        });
        
        // Wait a bit and then the first one should have completed.
        Thread.Sleep(500);
        Assert.That(_updateCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Sets the state watcher to throw an exception during state read.
    /// Force the timer to tick and verify that the right even is called and that the right properties are set.
    /// </summary>
    [Test]
    public void ExceptionsDuringStateReadingAreHandledTest()
    {
        _testedInstance!.SimulateException = true;
        _timer!.ForceTick();
        Assert.Multiple(() =>
        {
            Assert.That(_updateCount, Is.Zero);
            Assert.That(_skippedCount, Is.Zero);
            Assert.That(_exceptionCount, Is.EqualTo(1));
            Assert.That(_testedInstance.LatestState, Is.Null);
            Assert.That(_testedInstance.LatestUpdateTime, Is.Null);
            Assert.That(_testedInstance.LatestException, Is.Not.Null);
            Assert.That(_testedInstance.LatestExceptionTime, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies that the interval of the state watcher is initialized with the timer interval and stays
    /// synchronized when whichever interval changes.
    /// </summary>
    [Test]
    public void IntervalIsSynchronizedWithTimerTest()
    {
        Assert.That(_testedInstance!.Interval, Is.EqualTo(TimeSpan.FromSeconds(42)));
        var secondValue = TimeSpan.FromMilliseconds(428);
        _testedInstance.Interval = secondValue;
        Assert.That(_timer!.Interval, Is.EqualTo(secondValue));

        var thirdValue = TimeSpan.FromHours(4);
        _timer.Interval = thirdValue;
        Assert.That(_testedInstance!.Interval, Is.EqualTo(thirdValue));
    }
}