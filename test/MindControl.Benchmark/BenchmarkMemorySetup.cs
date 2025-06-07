using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Memory;

namespace MindControl.Benchmark;

public class BenchmarkMemorySetup : IDisposable
{
    /// <summary>
    /// Gets the process that runs the target project.
    /// </summary>
    public Process TargetProcess { get; }
    
    /// <summary>
    /// Gets the address of the outer class instance in the target project.
    /// </summary>
    public UIntPtr OuterClassPointer { get; }
    
    /// <summary>
    /// Gets the MindControl process memory instance attached to the target project.
    /// </summary>
    public ProcessMemory MindControlProcessMemory { get; }
    
    /// <summary>
    /// Gets the Memory.dll process memory instance attached to the target project.
    /// </summary>
    public Mem MemoryDllMem { get; }
    
    private BenchmarkMemorySetup(Process targetProcess, UIntPtr outerClassPointer,
        ProcessMemory mindControlProcessMemory, Mem memoryDllMem)
    {
        TargetProcess = targetProcess;
        OuterClassPointer = outerClassPointer;
        MindControlProcessMemory = mindControlProcessMemory;
        MemoryDllMem = memoryDllMem;
    }
    
    /// <summary>
    /// Sets up the benchmark memory setup.
    /// </summary>
    /// <returns>The benchmark memory setup.</returns>
    public static BenchmarkMemorySetup Setup()
    {
        var targetProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "./MindControl.Test.TargetApp.exe",
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            }
        };
        targetProcess.Start();
        
        string line = targetProcess.StandardOutput.ReadLine();
        if (!UIntPtr.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var outerClassPointer))
            throw new Exception($"Could not read the outer class pointer output by the app: \"{line}\".");
        
        var mindControlProcessMemory = ProcessMemory.OpenProcessById(targetProcess.Id).Value;
        var memoryDllMem = new Mem();
        memoryDllMem.OpenProcess(targetProcess.Id);
        
        return new BenchmarkMemorySetup(targetProcess, outerClassPointer, mindControlProcessMemory, memoryDllMem);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        MemoryDllMem.CloseProcess();
        TargetProcess.Kill();
        TargetProcess.Dispose();
        MindControlProcessMemory.Dispose();
        
        // Make sure the process is exited before going on 
        Thread.Sleep(250);
    }
}