using System.Diagnostics;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the <see cref="ProcessTracker"/> class.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessTrackerTest
{
    private const string TargetAppProcessName = "MindControl.Test.TargetApp";
    private ProcessTracker? _tracker;
    private readonly List<Process> _targetProcesses = new();
    private int _attachedEventCount;
    private int _detachedEventCount;
    
    /// <summary>
    /// Initializes the necessary instances for the tests.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _tracker = new ProcessTracker(TargetAppProcessName);
        _tracker.Attached += (_, _) => { Interlocked.Increment(ref _attachedEventCount); };
        _tracker.Detached += (_, _) => { Interlocked.Increment(ref _detachedEventCount); };
    }
    
    /// <summary>
    /// Disposes everything and kills the target app processes at the end of the test.
    /// </summary>
    [TearDown]
    public void CleanUp()
    {
        foreach (var targetProcess in _targetProcesses)
        {
            targetProcess.Kill();
            targetProcess.Dispose();
            // Make sure the process is exited before going on, otherwise it could cause other tests to fail. 
            Thread.Sleep(250);
        }
        
        _tracker!.Dispose();
    }

    /// <summary>
    /// Starts an instance of the target process app and returns the resulting process.
    /// </summary>
    private Process StartTargetAppProcess()
    {
        var process = ProcessMemoryTest.StartTargetAppProcess();
        _targetProcesses.Add(process);
        Thread.Sleep(500); // Wait a bit to make sure the process is ready.
        return process;
    }
    
    /// <summary>
    /// Sends input to the target app process in order to make it continue to the end.
    /// </summary>
    private static void ProceedUntilProcessEnds(Process targetAppProcess)
    {
        // Write lines into the standard input of the target app to make it advance to the next step twice and thus end. 
        targetAppProcess.StandardInput.WriteLine();
        targetAppProcess.StandardInput.WriteLine();
        targetAppProcess.StandardInput.Flush();
        
        // Wait a bit to make sure the process has time to respond and exit before the method ends.
        Thread.Sleep(1000);
    }

    /// <summary>
    /// Calls <see cref="ProcessTracker.GetProcessMemory"/> when no instance of the target process has been started.
    /// The expected result is null, because no process with the target name is running.
    /// </summary>
    [Test]
    public void GetProcessMemoryWithoutStartingTest() => Assert.That(_tracker!.GetProcessMemory(), Is.Null);

    /// <summary>
    /// Starts a target process and calls <see cref="ProcessTracker.GetProcessMemory"/>.
    /// The resulting <see cref="ProcessMemory"/> instance must be attached.
    /// </summary>
    [Test]
    public void GetProcessMemoryAfterStartingTest()
    {
        StartTargetAppProcess();
        var result = _tracker!.GetProcessMemory();
        Assert.Multiple(() =>
        {
            Assert.That(result?.IsAttached, Is.True);
            Assert.That(_attachedEventCount, Is.EqualTo(1));
            Assert.That(_detachedEventCount, Is.Zero);
        });
    }
    
    /// <summary>
    /// Starts a target process and calls <see cref="ProcessTracker.GetProcessMemory"/>.
    /// The resulting <see cref="ProcessMemory"/> instance must be attached.
    /// After that, makes the process run until it exits, and calls <see cref="ProcessTracker.GetProcessMemory"/> a
    /// second time. This time, the result should be null because no process with the target name is running.
    /// </summary>
    [Test]
    public void GetProcessMemoryAfterProcessExitTest()
    {
        var targetProcess = StartTargetAppProcess();
        var result = _tracker!.GetProcessMemory();
        Assert.That(result?.IsAttached, Is.True);
        ProceedUntilProcessEnds(targetProcess);
        var secondResult = _tracker.GetProcessMemory();
        Assert.Multiple(() =>
        {
            Assert.That(secondResult, Is.Null);
            Assert.That(_attachedEventCount, Is.EqualTo(1));
            Assert.That(_detachedEventCount, Is.EqualTo(1));
        });
    }
    
    /// <summary>
    /// After starting, running until the end, and then restarting the target process, calling
    /// <see cref="ProcessTracker.GetProcessMemory"/> at each of these steps, the last result should be a non-null,
    /// attached <see cref="ProcessMemory"/> instance.
    /// </summary>
    [Test]
    public void GetProcessMemoryAfterProcessRestartTest()
    {
        var targetProcess = StartTargetAppProcess();
        var result = _tracker!.GetProcessMemory();
        Assert.That(result?.IsAttached, Is.True);
        ProceedUntilProcessEnds(targetProcess);
        var secondResult = _tracker.GetProcessMemory();
        Assert.That(secondResult, Is.Null);
        StartTargetAppProcess();
        var thirdResult = _tracker.GetProcessMemory();
        Assert.Multiple(() =>
        {
            Assert.That(thirdResult?.IsAttached, Is.True);
            Assert.That(_attachedEventCount, Is.EqualTo(2));
            Assert.That(_detachedEventCount, Is.EqualTo(1));
        });
    }
    
    /// <summary>
    /// Starts 2 instances of the target process, and calls <see cref="ProcessTracker.GetProcessMemory"/>.
    /// The resulting <see cref="ProcessMemory"/> instance should be attached properly.
    /// Which of the two processes is attached is left undetermined, as the tracker only uses a process name by design. 
    /// </summary>
    [Test]
    public void GetProcessMemoryWithMultipleTargetsTest()
    {
        StartTargetAppProcess();
        StartTargetAppProcess();
        var result = _tracker!.GetProcessMemory();
        Assert.Multiple(() =>
        {
            Assert.That(result?.IsAttached, Is.True);
            Assert.That(_attachedEventCount, Is.EqualTo(1));
            Assert.That(_detachedEventCount, Is.Zero);
        });
    }
    
    /// <summary>
    /// Starts a target process and calls <see cref="ProcessTracker.GetProcessMemory"/> two times in a row.
    /// Verifies that the <see cref="ProcessTracker.Attached"/> event is fired only once.
    /// </summary>
    [Test]
    public void GetProcessMemoryOnMultipleCallsAfterStartingTest()
    {
        StartTargetAppProcess();
        _tracker!.GetProcessMemory();
        _tracker!.GetProcessMemory();
        Assert.Multiple(() =>
        {
            Assert.That(_attachedEventCount, Is.EqualTo(1));
            Assert.That(_detachedEventCount, Is.Zero);
        });
    }
    
    /// <summary>
    /// Starts a target process and calls <see cref="ProcessTracker.GetProcessMemory"/> asynchronously a large number
    /// of times simultaneously.
    /// Verifies that the <see cref="ProcessTracker.Attached"/> event is fired only once. This test checks that the
    /// method is thread-safe.
    /// </summary>
    [Test]
    public async Task GetProcessMemoryWithMultipleSimultaneousCallsTest()
    {
        var random = new Random();
        StartTargetAppProcess();
        const int runCount = 1000;
        var tasks = new List<Task>(runCount);
        for (var i = 0; i < runCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                Thread.Sleep(random.Next(1, 50));
                _tracker!.GetProcessMemory();
            }));
        }

        await Task.WhenAll(tasks);
        Assert.Multiple(() =>
        {
            Assert.That(_attachedEventCount, Is.EqualTo(1));
            Assert.That(_detachedEventCount, Is.Zero);
        });
    }
}