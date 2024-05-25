using System;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class WriteLongByPointerPathBenchmark
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

    [Benchmark(Description = "MindControl Write<long> (reused path)")]
    public void MindControlReadGenericTypeReuse()
    {
        var result = _setup.MindControlProcessMemory.Write(_pointerPath, -496873331231411L,
            MemoryProtectionStrategy.Ignore);
        if (!result.IsSuccess)
            throw new Exception("Write failed");
    }
    
    [Benchmark(Description = "MindControl Write<long> (dynamic path)")]
    public void MindControlReadGenericTypeDynamic()
    {
        var result = _setup.MindControlProcessMemory.Write(_pathString, -496873331231411L,
            MemoryProtectionStrategy.Ignore);
        if (!result.IsSuccess)
            throw new Exception("Write failed");
    }
    
    [Benchmark(Description = "Memory.dll WriteMemory", Baseline = true)]
    public void MemoryReadGeneric()
    {
        var result = _setup.MemoryDllMem.WriteMemory(_pathString, "long", "-496873331231411");
        if (!result)
            throw new Exception("Write failed");
    }
}