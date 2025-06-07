using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using MindControl.Code;
using static Iced.Intel.AssemblerRegisters;
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
    /// <summary>Holds arguments for the GetCurrentDirectoryW Win32 API function.</summary>
    /// <remarks>We use the attribute to control how the structure is arranged in memory. The Sequential layout prevents
    /// the .net runtime from reordering fields, and the "Pack = 1" prevents padding in-between fields (otherwise, the
    /// buffer address might start at byte 8 instead of the expected 4, for alignment/performance reasons).</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct GetCurrentDirectoryWArgs
    {
        /// <summary>Size of the destination buffer.</summary>
        public uint BufferSize;
        /// <summary>Address of the destination buffer.</summary>
        public ulong BufferAddress;
    }
    
    /// <summary>
    /// Assembles instructions to call the GetCurrentDirectoryW kernel32.dll function.
    /// </summary>
    /// <param name="functionAddress">Address of the GetCurrentDirectoryW function.</param>
    /// <returns>An assembler object containing the assembled instructions.</returns>
    protected virtual Assembler AssembleTrampolineForGetCurrentDirectoryW(UIntPtr functionAddress)
    {
        var assembler = new Assembler(64);
        // In x64, the function uses the fastcall calling convention, i.e. RCX and RDX are used for the two arguments.
        // When the thread is created, the thread parameter is in RCX. In this case, our parameter is going to be the
        // address of a GetCurrentDirectoryWArgs struct holding the parameters we want.
        assembler.mov(rax, rcx); // Move the address of the GetCurrentDirectoryWArgs struct to RAX, to free up RCX
        assembler.mov(ecx, __dword_ptr[rax]); // Move the buffer size (first argument) to ECX/RCX
        assembler.mov(rdx, __[rax+4]); // Move the buffer address (second argument) to RDX
        assembler.call(functionAddress.ToUInt64()); // Call GetCurrentDirectoryW
        assembler.ret();
        return assembler;
    }
    
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
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Failure.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
        Assert.That(waitResult.IsSuccess, Is.True, () => waitResult.Failure.ToString());
        Assert.That(waitResult.Value, Is.Zero); // The exit code should be 0
        Assert.That(HasProcessExited, Is.True); // The process should have exited as a result of the function call
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(PointerPath,System.Nullable{System.UIntPtr})"/>.
    /// Equivalent of <see cref="RunThreadAndWaitTest"/>, but with a pointer path.
    /// </summary>
    [Test]
    public void RunThreadWithPointerPathTest()
    {
        var kernel32Module = TestProcessMemory!.GetModule("kernel32.dll");
        var functionAddress = kernel32Module!.ReadExportTable().Value["ExitProcess"];
        var threadResult = TestProcessMemory!.RunThread(functionAddress.ToString("X"));
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Failure.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
        Assert.That(waitResult.IsSuccess, Is.True, () => waitResult.Failure.ToString());
        Assert.That(waitResult.Value, Is.Zero);
        Assert.That(HasProcessExited, Is.True);
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(UIntPtr,System.Nullable{System.UIntPtr})"/>.
    /// Writes a simple assembly code that calls the GetCurrentDirectoryW kernel32.dll function, runs that code in a
    /// thread with a struct holding the function arguments as a parameter, and waits for the resulting thread to end.
    /// We should be able to get the current directory of the target process.
    /// </summary>
    [Test]
    public void RunThreadWithGetCurrentDirectoryWTrampolineTest()
    {
        var bufferReservation = TestProcessMemory!.Reserve(2048, false).Value;
        var argsReservation = TestProcessMemory.Store(new GetCurrentDirectoryWArgs
        {
            BufferSize = 2048,
            BufferAddress = bufferReservation.Address
        }).Value;
        var kernel32Module = TestProcessMemory.GetModule("kernel32.dll");
        var functionAddress = kernel32Module!.ReadExportTable().Value["GetCurrentDirectoryW"];
        
        // We cannot call GetCurrentDirectoryW directly because of its parameters. We need to write a trampoline
        // function that prepares the parameters as they are expected by the function before calling it.
        var assembler = AssembleTrampolineForGetCurrentDirectoryW(functionAddress);
        var codeReservation = TestProcessMemory
            .StoreCode(assembler, nearAddress: kernel32Module.GetRange().Start).Value;
        
        // Run the thread
        var threadResult = TestProcessMemory!.RunThread(codeReservation.Address, argsReservation.Address);
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Failure.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
        Assert.That(waitResult.IsSuccess, Is.True, () => waitResult.Failure.ToString());
        
        // Read the resulting string from the allocated buffer
        var resultingString = TestProcessMemory.ReadRawString(bufferReservation.Address, Encoding.Unicode, 512).Value;
        Assert.That(resultingString, Is.EqualTo(Environment.CurrentDirectory));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/>.
    /// Runs the Sleep kernel32.dll function in a thread in the target process.
    /// Waits for the resulting thread, but with a timeout that does not leave enough time for the function to complete.
    /// Check that the wait operation returns a <see cref="ThreadWaitTimeoutFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadSleepTimeoutTest()
    {
        // Start a Sleep thread that will run for 5 seconds, but wait only for 500ms
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "Sleep", 5000);
        Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Failure.ToString());
        var waitResult = threadResult.Value.WaitForCompletion(TimeSpan.FromMilliseconds(500));
        Assert.That(waitResult.IsSuccess, Is.False);
        Assert.That(waitResult.Failure, Is.TypeOf<ThreadWaitTimeoutFailure>());
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
        var tasks = new List<Task<Result<uint>>>();
        for (int i = 0; i < 10; i++)
        {
            // Each thread executes Sleep for 500ms
            var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "Sleep", 500);
            Assert.That(threadResult.IsSuccess, Is.True, () => threadResult.Failure.ToString());
            tasks.Add(threadResult.Value.WaitForCompletionAsync(TimeSpan.FromSeconds(10)));
        }
        
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        foreach (var task in tasks)
        {
            Assert.That(task.Result.IsSuccess, Is.True, () => task.Result.Failure.ToString());
            Assert.That(task.Result.Value, Is.Zero);
        }
        
        // We check that threads run concurrently by checking that waiting for all of them takes less than n times the
        // expected completion time of a single thread.
        // Make sure to keep some leeway for the test to pass consistently, even in environments with scarce resources.
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(PointerPath,System.Nullable{System.UIntPtr})"/> with an invalid
    /// pointer path.
    /// Expects a failure.
    /// </summary>
    [Test]
    public void RunThreadWithInvalidPointerPathTest()
    {
        var threadResult = TestProcessMemory!.RunThread("invalid pointer path");
        Assert.That(threadResult.IsSuccess, Is.False);
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(UIntPtr,System.Nullable{System.UIntPtr})"/> with a zero address.
    /// Expects a <see cref="InvalidArgumentFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithZeroPointerTest()
    {
        var threadResult = TestProcessMemory!.RunThread(UIntPtr.Zero);
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<InvalidArgumentFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/> with a module name
    /// that does not match any module loaded in the process.
    /// Expects a <see cref="FunctionNotFoundFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithInvalidModuleTest()
    {
        var threadResult = TestProcessMemory!.RunThread("invalid module", "ExitProcess");
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<FunctionNotFoundFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/> with a valid module
    /// name but a function name that does not match any exported function in the module.
    /// Expects a <see cref="FunctionNotFoundFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithInvalidFunctionTest()
    {
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "invalid function");
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<FunctionNotFoundFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(UIntPtr,System.Nullable{System.UIntPtr})"/> with a detached process.
    /// Expects a <see cref="DetachedProcessFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithAddressOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var threadResult = TestProcessMemory!.RunThread(0x1234);
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<DetachedProcessFailure>());
    }
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(PointerPath,System.Nullable{System.UIntPtr})"/> with a detached
    /// process. Expects a <see cref="DetachedProcessFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithPointerPathOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var threadResult = TestProcessMemory!.RunThread("1234");
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<DetachedProcessFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/> with a detached
    /// process. Expects a <see cref="DetachedProcessFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithExportedFunctionOnDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "Sleep", 2000);
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<DetachedProcessFailure>());
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

    /// <summary>
    /// Assembles instructions to call the GetCurrentDirectoryW kernel32.dll function.
    /// </summary>
    /// <param name="functionAddress">Address of the GetCurrentDirectoryW function.</param>
    /// <returns>An assembler object containing the assembled instructions.</returns>
    protected override Assembler AssembleTrampolineForGetCurrentDirectoryW(UIntPtr functionAddress)
    {
        var assembler = new Assembler(32);
        // In x86, this function uses the stdcall calling convention, i.e. the arguments are expected to be in the stack
        // and pop in the right order (meaning they must be pushed in reverse order).
        // When the thread is created, the thread parameter is in EBX. In this case, our parameter is going to be the
        // address of a GetCurrentDirectoryWArgs struct holding the parameters of the function to call.
        assembler.mov(eax, __[ebx + 4]);
        assembler.push(eax); // Push the buffer address (second argument)
        assembler.mov(eax, __[ebx]);
        assembler.push(eax); // Push the buffer size (first argument)
        assembler.call(functionAddress.ToUInt32()); // Call GetCurrentDirectoryW
        assembler.add(esp, 8); // Clean up the stack (as per stdcall convention)
        assembler.ret();
        return assembler;
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(UIntPtr,System.Nullable{System.UIntPtr})"/> with an address that is
    /// beyond the scope of a 32-bit process.
    /// Expect a <see cref="IncompatibleBitnessPointerFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithIncompatibleAddressTest()
    {
        UIntPtr maxAddress = uint.MaxValue;
        var threadResult = TestProcessMemory!.RunThread(maxAddress + 1);
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<IncompatibleBitnessPointerFailure>());
    }

    /// <summary>
    /// Tests <see cref="ProcessMemory.RunThread(string,string,System.Nullable{System.UIntPtr})"/> with a parameter
    /// value that is beyond the scope of a 32-bit process.
    /// Expect a <see cref="IncompatibleBitnessPointerFailure"/> error.
    /// </summary>
    [Test]
    public void RunThreadWithIncompatibleParameterTest()
    {
        UIntPtr maxValue = uint.MaxValue;
        var threadResult = TestProcessMemory!.RunThread("kernel32.dll", "ExitProcess", maxValue + 1);
        Assert.That(threadResult.IsSuccess, Is.False);
        Assert.That(threadResult.Failure, Is.TypeOf<IncompatibleBitnessPointerFailure>());
    }
}