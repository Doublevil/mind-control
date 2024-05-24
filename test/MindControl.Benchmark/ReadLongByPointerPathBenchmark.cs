using System;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class ReadLongByPointerPathBenchmark
{
    private BenchmarkMemorySetup _setup;
    private string _pathString;
    private PointerPath _pointerPath;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _setup = BenchmarkMemorySetup.Setup();
        _pathString = $"{_setup.OuterClassPointer:X}+10,10";
        _pointerPath = _pathString;
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _setup.Dispose();

    [Benchmark(Description = "MindControl Read<long> (reused path)")]
    public long MindControlReadGenericTypeReuse()
    {
        long result = _setup.MindControlProcessMemory.Read<long>(_pointerPath).Value;
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl Read<long> (dynamic path)")]
    public long MindControlReadGenericTypeDynamic()
    {
        long result = _setup.MindControlProcessMemory.Read<long>(_pathString).Value;
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl Read(Type) (reused path)")]
    public long MindControlReadObjectReuse()
    {
        var result = (long)_setup.MindControlProcessMemory.Read(typeof(long), _pointerPath).Value;
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl Read(Type) (dynamic path)")]
    public long MindControlReadObjectDynamic()
    {
        var result = (long)_setup.MindControlProcessMemory.Read(typeof(long), _pathString).Value;
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadLong", Baseline = true)]
    public long MemoryReadLong()
    {
        long result = _setup.MemoryDllMem.ReadLong(_pathString);
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadMemory<long>")]
    public long MemoryReadGeneric()
    {
        var result = _setup.MemoryDllMem.ReadMemory<long>(_pathString);
        if (result != -1)
            throw new Exception("Unexpected result");
        return result;
    }
}