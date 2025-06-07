using System;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class ReadStringPointerBenchmark
{
    private BenchmarkMemorySetup _setup;
    private string _pathString;
    private PointerPath _pointerPath;
    private UIntPtr _address;
    private StringSettings _settings;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _setup = BenchmarkMemorySetup.Setup();
        _settings = new StringSettings(Encoding.Unicode, true, new StringLengthPrefix(4, StringLengthUnit.Characters),
            new byte[8]);
        _address = _setup.OuterClassPointer + 8;
        _pathString = $"{_setup.OuterClassPointer:X}+8";
        _pointerPath = _pathString;
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _setup.Dispose();
    
    [Benchmark(Description = "MindControl ReadRawString (address)")]
    public string MindControlReadRawStringAddress()
    {
        var result = _setup.MindControlProcessMemory.ReadStringPointer(_address, _settings).Value;
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl ReadRawString (reused path)")]
    public string MindControlReadRawStringReusedPath()
    {
        var result = _setup.MindControlProcessMemory.ReadStringPointer(_pointerPath, _settings).Value;
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl ReadRawString (dynamic path)")]
    public string MindControlReadRawStringDynamicPath()
    {
        var result = _setup.MindControlProcessMemory.ReadStringPointer(_pathString, _settings).Value;
        if (result != "ThisIsÄString")
            throw new Exception("Unexpected result");
        return result;
    }
}