using NUnit.Framework;
using MindControl.Results;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to attaching to a process.
/// This class does not inherit from <see cref="BaseProcessMemoryTest"/> and thus does not start a target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryInstancelessAttachTest
{
    /// <summary>
    /// Tests <see cref="ProcessMemory.OpenProcessByName"/> when no process with the given name is found.
    /// Expects a <see cref="TargetProcessNotFoundFailure"/> result.
    /// </summary>
    [Test]
    public void OpenProcessByNameWithNoMatchTest()
    {
        var result = ProcessMemory.OpenProcessByName("ThisProcessDoesNotExist");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<TargetProcessNotFoundFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.OpenProcessById"/> with a PID that does not match any running process.
    /// Expects a <see cref="TargetProcessNotFoundFailure"/> result.
    /// </summary>
    [Test]
    public void OpenProcessByInvalidPidTest()
    {
        var result = ProcessMemory.OpenProcessById(-1);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<TargetProcessNotFoundFailure>());
    }
}

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to attaching to a process.
/// This class inherits from <see cref="BaseProcessMemoryTest"/> and thus does start a target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAttachTest : BaseProcessMemoryTest
{
    /// <summary>
    /// This test only ensures that the setup works, i.e. that opening a process as a
    /// <see cref="MindControl.ProcessMemory"/> instance won't throw an exception.
    /// </summary>
    [Test]
    public void OpenProcessTest() { }

    /// <summary>
    /// Tests that the Dispose method detaches from the process and raises the relevant event.
    /// </summary>
    [Test]
    public void DisposeTest()
    {
        var hasRaisedEvent = false;
        TestProcessMemory!.ProcessDetached += (_, _) => { hasRaisedEvent = true; }; 
        TestProcessMemory.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(hasRaisedEvent, Is.True);
            Assert.That(TestProcessMemory.IsAttached, Is.False);
        });
    }
    
    /// <summary>
    /// Tests that the <see cref="ProcessMemory.ProcessDetached"/> event is raised when the process exits.
    /// </summary>
    [Test]
    public void ProcessDetachedOnExitTest()
    {
        var hasRaisedEvent = false;
        TestProcessMemory!.ProcessDetached += (_, _) => { hasRaisedEvent = true; }; 
        
        // Go to the end of the process, then wait for a bit to make sure the process exits before we assert the results
        ProceedUntilProcessEnds();
        Thread.Sleep(1000);
        
        Assert.Multiple(() =>
        {
            Assert.That(hasRaisedEvent, Is.True);
            Assert.That(TestProcessMemory.IsAttached, Is.False);
        });
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryAttachTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAttachTestX86 : ProcessMemoryAttachTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
}