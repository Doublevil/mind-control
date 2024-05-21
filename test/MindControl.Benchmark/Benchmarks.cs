using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Memory;

namespace MindControl.Benchmark;

[MemoryDiagnoser]
public class Benchmarks
{
    private Process _targetProcess;
    private ProcessMemory _processMemory;
    private UIntPtr _outerClassPointer;
    private Mem _mem;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _targetProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "./MindControl.Test.TargetApp.exe",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
        _targetProcess.Start();
        
        string line = _targetProcess.StandardOutput.ReadLine();
        if (!UIntPtr.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var outerClassPointer))
            throw new Exception($"Could not read the outer class pointer output by the app: \"{line}\".");
        
        _outerClassPointer = outerClassPointer;
        _processMemory = ProcessMemory.OpenProcessById(_targetProcess.Id);
        _mem = new Mem();
        _mem.OpenProcess(_targetProcess.Id);
    }
    
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _processMemory.Dispose();
        _targetProcess.Kill();
        
        // Make sure the process is exited before going on 
        Thread.Sleep(250);
    }

    [Benchmark(Description = "MindControl Read<int>")]
    public int MindControlReadGenericType()
    {
        var result = _processMemory.Read<int>(_outerClassPointer + 0x38).Value;
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl Read(Type)")]
    public int MindControlReadObject()
    {
        var result = (int)_processMemory.Read(typeof(int), _outerClassPointer + 0x38).Value;
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadInt")]
    public int MemoryReadInt()
    {
        var address = _outerClassPointer + 0x38;
        var result = _mem.ReadInt(address.ToString("X"));
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadMemory<int>")]
    public int MemoryReadGeneric()
    {
        var address = _outerClassPointer + 0x38;
        var result = _mem.ReadMemory<int>(address.ToString("X"));
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
}