using System;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark.Benchmarks;

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
    
    [Benchmark(Description = "MindControl 100xRead<long>")]
    public void MindControl100xReadGenericTypeReuse()
    {
        for (int i = 0; i < 100; i++)
        {
            long result = _setup.MindControlProcessMemory.Read<long>(_pointerPath).Value;
            if (result != -1)
                throw new Exception("Unexpected result");
        }
    }
    
    [Benchmark(Description = "MindControl 100xRead<long> (module path)")]
    public void MindControl100xReadGenericModulePath()
    {
        var modulePath = new PointerPath("MindControl.Test.TargetApp.dll+8");
        for (int i = 0; i < 100; i++)
        {
            long result = _setup.MindControlProcessMemory.Read<long>(modulePath).Value;
            if (result == 0)
                throw new Exception("Unexpected result");
        }
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
    
    [Benchmark(Description = "Memory.dll 100xReadMemory<long>")]
    public void Memory100xReadGeneric()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _setup.MemoryDllMem.ReadMemory<long>(_pathString);
            if (result != -1)
                throw new Exception("Unexpected result");
        }
    }
    
    [Benchmark(Description = "Memory.dll 100xReadMemory<long> (module path)")]
    public void Memory100xReadGenericWithModule()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _setup.MemoryDllMem.ReadMemory<long>("MindControl.Test.TargetApp.dll+8");
            if (result != -1)
                throw new Exception("Unexpected result");
        }
    }
}