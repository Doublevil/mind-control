using System;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class ReadRawStringBenchmark
{
    private BenchmarkMemorySetup _setup;
    private string _pathString;
    private PointerPath _pointerPath;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _setup = BenchmarkMemorySetup.Setup();
        _pathString = $"{_setup.OuterClassPointer:X}+8,C";
        _pointerPath = _pathString;
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _setup.Dispose();
    
    [Benchmark(Description = "MindControl ReadRawString (reused path)")]
    public string MindControlReadRawStringReusedPath()
    {
        var result = _setup.MindControlProcessMemory.ReadRawString(_pointerPath, Encoding.Unicode).Value;
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl ReadRawString (dynamic path)")]
    public string MindControlReadRawStringDynamicPath()
    {
        var result = _setup.MindControlProcessMemory.ReadRawString(_pathString, Encoding.Unicode, 32, true).Value;
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadMemory<string>")]
    public string MemoryReadMemoryString()
    {
        var result = _setup.MemoryDllMem.ReadString(_pathString, length: 32, zeroTerminated: true,
            stringEncoding: Encoding.Unicode);
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
}