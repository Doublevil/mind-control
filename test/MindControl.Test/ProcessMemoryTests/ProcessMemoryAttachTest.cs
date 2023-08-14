using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to attaching to a process.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryAttachTest : ProcessMemoryTest
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