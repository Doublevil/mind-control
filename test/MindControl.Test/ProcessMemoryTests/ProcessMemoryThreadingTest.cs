using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using MindControl.Results;
using MindControl.Threading;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to threads.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryThreadingTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/>.
    /// Runs the ExitProcess kernel32.dll function in a thread in the target process, and waits for the resulting
    /// thread to end.
    /// Check that the function has completed successfully and has triggered the process to exit.
    /// </summary>
    [Test]
    public void RunThreadAndWaitTest()
    {
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "ExitProcess");
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Error.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
        Assert.That(waitResult.IsSuccess, Is.True, () => waitResult.Error.ToString());
        Assert.That(waitResult.Value, Is.Zero); // The exit code should be 0
        Assert.That(HasProcessExited, Is.True); // The process should have exited as a result of the function call
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(UIntPtr,System.Nullable{System.UIntPtr})"/>.
    /// Writes a simple assembly code that calls the GetCurrentDirectoryW kernel32.dll function, runs that code in a
    /// thread, and waits for the resulting thread to end.
    /// We should be able to get the current directory of the target process.
    /// </summary>
    [Test]
    public void RunThreadWithMultipleParametersTest()
    {
        var bufferReservation = TestProcessMemory!.Reserve(2048, false).Value;
        var kernel32Module = TestProcessMemory.GetModule("kernel32.dll");
        var functionAddress = kernel32Module!.ReadExportTable().Value["GetCurrentDirectoryW"];
        
        // We cannot call GetCurrentDirectoryW directly because of its parameters. We need to write a trampoline
        // function that arranges the parameters in the right registers and then calls the target function.
        var assembler = new Assembler(64);
        var rcx = new AssemblerRegister64(Register.RCX);
        var rdx = new AssemblerRegister64(Register.RDX);
        // Use the fastcall calling convention, i.e. RCX and RDX are used for the first two arguments
        assembler.mov(rcx, 2048); // Write the buffer size
        assembler.mov(rdx, bufferReservation.Address.ToUInt64()); // Write the buffer address
        assembler.call(functionAddress.ToUInt64()); // Call GetCurrentDirectoryW
        assembler.ret();

        // Write the code to the target process
        var codeReservation = TestProcessMemory.Reserve(256, true, nearAddress: kernel32Module.GetRange().Start).Value;
        var bytes = assembler.AssembleToBytes(codeReservation.Address).Value;
        TestProcessMemory.Write(codeReservation.Address, bytes);
        
        // Run the thread
        var threadResult = TestProcessMemory!.RunThread(codeReservation.Address);
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Error.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
        Assert.That(waitResult.IsSuccess, Is.True, () => waitResult.Error.ToString());
        
        // Read the resulting string from the allocated buffer
        var resultingString = TestProcessMemory.ReadRawString(bufferReservation.Address, Encoding.Unicode, 512).Value;
        Assert.That(resultingString, Is.EqualTo(Environment.CurrentDirectory));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/>.
    /// Runs the Sleep kernel32.dll function in a thread in the target process.
    /// Waits for the resulting thread, but with a timeout that does not leave enough time for the function to complete.
    /// Check that the wait operation returns a <see cref="ThreadFailureOnWaitTimeout"/> error.
    /// </summary>
    [Test]
    public void RunThreadSleepTimeoutTest()
    {
        // Start a Sleep thread that will run for 5 seconds, but wait only for 500ms
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "Sleep", 5000);
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Error.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromMilliseconds(500));
        Assert.That(waitResult.IsSuccess, Is.False);
        Assert.That(waitResult.Error, Is.TypeOf<ThreadFailureOnWaitTimeout>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/>
    /// and <see cref="RemoteThread.WaitForCompletionAsync"/>.
    /// Runs the Sleep kernel32.dll function in a thread in the target process a bunch of times.
    /// Waits for all resulting threads asynchronously.
    /// Checks that all threads complete successfully in a timely manner.
    /// </summary>
    [Test]
    public async Task RunThreadSleepWaitAsyncTest()
    {
        var tasks = new List<Task<Result<uint, ThreadFailure>>>();
        for (int i = 0; i < 10; i++)
        {
            // Each thread executes Sleep for 500ms
            var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "Sleep", 500);
            Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Error.ToString());
            tasks.Add(threadResult.Value.WaitForCompletionAsync(TimeSpan.FromSeconds(10)));
        }
        
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        foreach (var task in tasks)
        {
            Assert.That(task.Result.IsSuccess, Is.True, () => task.Result.Error.ToString());
            Assert.That(task.Result.Value, Is.Zero);
        }
        
        // We check that threads run concurrently by checking that waiting for all of them takes less than n times the
        // expected completion time of a single thread.
        // Make sure to keep some leeway for the test to pass consistently, even in environments with scarce resources.
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryThreadingTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryThreadingTestX86 : ProcessMemoryThreadingTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
}