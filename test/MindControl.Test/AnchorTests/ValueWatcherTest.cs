using MindControl.Anchors;
using MindControl.Test.ProcessMemoryTests;
using NUnit.Framework;

namespace MindControl.Test.AnchorTests;

/// <summary>Tests <see cref="ValueWatcher{TValue}"/>.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ValueWatcherTest : BaseProcessMemoryTest
{
    /// <summary>Builds and returns an anchor instance that can be used to build a value watcher.</summary>
    private ValueAnchor<int> GetAnchorOnOutputInt()
        => TestProcessMemory!.GetAnchor<int>(GetAddressForValueAtIndex(IndexOfOutputInt));
    
    /// <summary>Builds and returns a value watcher instance that can be used to test the class.</summary>
    private ValueWatcher<int> GetWatcherOnOutputInt(TimeSpan? refreshInterval = null)
        => GetAnchorOnOutputInt().Watch(refreshInterval ?? DefaultRefreshInterval);

    /// <summary>Default interval for refreshing the value in the watcher.</summary>
    private static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}"/> constructor.
    /// The constructor should set the watcher instance to an initial state by reading the value without raising events.
    /// </summary>
    [Test]
    public void ReadableInitialStateTest()
    {
        var dtAtStart = DateTime.Now;
        var watcher = GetWatcherOnOutputInt();
        watcher.ValueChanged += (_, _) => Assert.Fail("ValueChanged event should not be raised.");
        watcher.ValueLost += (_, _) => Assert.Fail("ValueLost event should not be raised.");
        watcher.ValueReacquired += (_, _) => Assert.Fail("ValueReacquired event should not be raised.");
        
        // Even if we update the state here, it should be the same as the initial state, so no events should be raised
        watcher.UpdateState();
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(InitialIntValue));
        Assert.That(watcher.LastChangeTime, Is.EqualTo(dtAtStart).Within(TimeSpan.FromSeconds(10)));
    }
    
    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}"/> constructor.
    /// The watcher is set to an initial state where the value is unreadable.
    /// </summary>
    [Test]
    public void UnreadableInitialStateTest()
    {
        var reservation = TestProcessMemory!.Reserve(16, false).Value;
        var pointerPath = new PointerPath($"{reservation.Address:X},0");
        var watcher = TestProcessMemory.GetAnchor<int>(pointerPath).Watch(DefaultRefreshInterval);
        watcher.ValueChanged += (_, _) => Assert.Fail("ValueChanged event should not be raised.");
        watcher.ValueLost += (_, _) => Assert.Fail("ValueLost event should not be raised.");
        watcher.ValueReacquired += (_, _) => Assert.Fail("ValueReacquired event should not be raised.");
        
        watcher.UpdateState();
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.False);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(0)); // Default value for int
    }
    
    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}.ValueChanged"/> event.
    /// Build a value watcher, let the process change the value, force an update, and check if the event is raised.
    /// </summary>
    [Test]
    public void ValueChangedTest()
    {
        bool valueChangedCalled = false;
        var watcher = GetWatcherOnOutputInt();
        watcher.ValueChanged += (_, args) =>
        {
            if (valueChangedCalled)
                Assert.Fail("ValueChanged event should not be raised more than once.");
            
            valueChangedCalled = true;
            Assert.That(args.PreviousValue, Is.EqualTo(InitialIntValue));
            Assert.That(args.NewValue, Is.EqualTo(ExpectedFinalIntValue));
        };
        watcher.ValueLost += (_, _) => Assert.Fail("ValueLost event should not be raised.");
        watcher.ValueReacquired += (_, _) => Assert.Fail("ValueReacquired event should not be raised.");
        
        ProceedToNextStep(); // The target process will change the value here
        watcher.UpdateState();
        watcher.UpdateState(); // Update once more to make sure events are not raised multiple times
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(valueChangedCalled, Is.True);
    }

    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}.ValueLost"/> event.
    /// Build a value watcher with a pointer path, change a pointer in the path so that the address no longer resolves,
    /// and check that the event gets raised after a state update.
    /// </summary>
    [Test]
    public void ValueLostTest()
    {
        using var reservation = TestProcessMemory!.Reserve(16, false).Value;
        TestProcessMemory.Write(reservation.Address, reservation.Address + 8).ThrowOnFailure();
        int targetValue = 46;
        TestProcessMemory.Write(reservation.Address + 8, targetValue).ThrowOnFailure();
        var pointerPath = new PointerPath($"{reservation.Address:X},0");
        
        var watcher = TestProcessMemory.GetAnchor<int>(pointerPath).Watch(DefaultRefreshInterval);
        bool valueLostCalled = false;
        watcher.ValueChanged += (_, _) => Assert.Fail("ValueChanged event should not be raised.");
        watcher.ValueLost += (_, args) =>
        {
            if (valueLostCalled)
                Assert.Fail("ValueLost event should not be raised more than once.");
            
            valueLostCalled = true;
            Assert.That(args.LastKnownValue, Is.EqualTo(targetValue));
        };
        watcher.ValueReacquired += (_, _) => Assert.Fail("ValueReacquired event should not be raised.");
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(targetValue));
        
        TestProcessMemory.Write(reservation.Address, 0).ThrowOnFailure(); // Sabotage the pointer path
        
        watcher.UpdateState();
        watcher.UpdateState(); // Update once more to make sure events are not raised multiple times
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.False);
        Assert.That(valueLostCalled, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(targetValue));
    }
    
    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}.ValueReacquired"/> event.
    /// Build a value watcher with a pointer path, change a pointer in the path so that the address no longer resolves,
    /// update the state so that the value gets lost, and then repair the pointer path and update the state again.
    /// The tested event should be raised exactly once.
    /// </summary>
    [Test]
    public void ValueReacquiredTest()
    {
        using var reservation = TestProcessMemory!.Reserve(16, false).Value;
        TestProcessMemory.Write(reservation.Address, reservation.Address + 8).ThrowOnFailure();
        int targetValue = 46;
        TestProcessMemory.Write(reservation.Address + 8, targetValue).ThrowOnFailure();
        var pointerPath = new PointerPath($"{reservation.Address:X},0");
        
        var watcher = TestProcessMemory.GetAnchor<int>(pointerPath).Watch(DefaultRefreshInterval);
        bool valueReacquiredCalled = false;
        watcher.ValueChanged += (_, _) => Assert.Fail("ValueChanged event should not be raised.");
        watcher.ValueReacquired += (_, args) =>
        {
            if (valueReacquiredCalled)
                Assert.Fail("ValueReacquired event should not be raised more than once.");
            
            valueReacquiredCalled = true;
            Assert.That(args.NewValue, Is.EqualTo(targetValue));
        };
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(targetValue));
        
        TestProcessMemory.Write(reservation.Address, 0).ThrowOnFailure(); // Sabotage the pointer path
        watcher.UpdateState(); // This should raise a ValueLost event
        
        TestProcessMemory.Write(reservation.Address, reservation.Address + 8).ThrowOnFailure(); // Repair the pointer path
        watcher.UpdateState(); // This should raise a ValueReacquired event
        watcher.UpdateState(); // Update once more to make sure events are not raised multiple times
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(valueReacquiredCalled, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(targetValue));
    }
    
    /// <summary>
    /// Tests the <see cref="ValueWatcher{TValue}.ValueReacquired"/> and <see cref="ValueWatcher{TValue}.ValueChanged"/>
    /// events.
    /// Same setup as <see cref="ValueReacquiredTest"/>, except we make the pointer path resolve to a different value.
    /// We expect the ValueReacquired event to be raised first, and then the ValueChanged event.
    /// </summary>
    [Test]
    public void ValueReacquiredAndChangedTest()
    {
        using var reservation = TestProcessMemory!.Reserve(16, false).Value;
        TestProcessMemory.Write(reservation.Address, reservation.Address + 8).ThrowOnFailure();
        int targetValue = 46;
        TestProcessMemory.Write(reservation.Address + 8, targetValue).ThrowOnFailure();
        var pointerPath = new PointerPath($"{reservation.Address:X},0");
        
        var watcher = TestProcessMemory.GetAnchor<int>(pointerPath).Watch(DefaultRefreshInterval);
        bool valueReacquiredCalled = false;
        bool valueChangedCalled = false;
        watcher.ValueChanged += (_, args) =>
        {
            if (valueChangedCalled)
                Assert.Fail("ValueChanged event should not be raised more than once.");
            
            valueChangedCalled = true;
            Assert.That(args.PreviousValue, Is.EqualTo(targetValue));
            Assert.That(args.NewValue, Is.EqualTo(0));
        };
        watcher.ValueReacquired += (_, args) =>
        {
            if (valueReacquiredCalled)
                Assert.Fail("ValueReacquired event should not be raised more than once.");
            if (valueChangedCalled)
                Assert.Fail("ValueReacquired event should be raised before ValueChanged event.");
            
            valueReacquiredCalled = true;
            Assert.That(args.NewValue, Is.EqualTo(0));
        };
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(targetValue));
        
        TestProcessMemory.Write(reservation.Address, 0).ThrowOnFailure(); // Sabotage the pointer path
        watcher.UpdateState(); // This should raise a ValueLost event
        
        // Make the pointer path resolve to an area with a 0 value
        TestProcessMemory.Write(reservation.Address, reservation.Address + 12).ThrowOnFailure();
        watcher.UpdateState(); // This should raise a ValueReacquired event and then a ValueChanged event
        watcher.Dispose();
        
        Assert.That(watcher.IsValueReadable, Is.True);
        Assert.That(valueReacquiredCalled, Is.True);
        Assert.That(valueChangedCalled, Is.True);
        Assert.That(watcher.LastKnownValue, Is.EqualTo(0));
    }
}

/// <summary>
/// Runs the tests from <see cref="ValueWatcherTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ValueWatcherTestX86 : ValueWatcherTest
{
    /// <summary>Gets a value indicating if the process is 64-bit.</summary>
    protected override bool Is64Bit => false;
}