using System;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class ReadIntByAddressBenchmark
{
    private BenchmarkMemorySetup _setup;

    [GlobalSetup]
    public void GlobalSetup() => _setup = BenchmarkMemorySetup.Setup();

    [GlobalCleanup]
    public void GlobalCleanup() => _setup.Dispose();

    [Benchmark(Description = "MindControl Read<int>")]
    public int MindControlReadGenericType()
    {
        var result = _setup.MindControlProcessMemory.Read<int>(_setup.OuterClassPointer + 0x38).Value;
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "MindControl 100xRead<int>")]
    public void MindControlMassReadGenericType()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _setup.MindControlProcessMemory.Read<int>(_setup.OuterClassPointer + 0x38).Value;
            if (result != -7651)
                throw new Exception("Unexpected result");
        }
    }
    
    [Benchmark(Description = "MindControl Read(Type)")]
    public int MindControlReadObject()
    {
        var result = (int)_setup.MindControlProcessMemory.Read(typeof(int), _setup.OuterClassPointer + 0x38).Value;
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadInt", Baseline = true)]
    public int MemoryReadInt()
    {
        UIntPtr address = _setup.OuterClassPointer + 0x38;
        int result = _setup.MemoryDllMem.ReadInt(address.ToString("X"));
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll ReadMemory<int>")]
    public int MemoryReadGeneric()
    {
        UIntPtr address = _setup.OuterClassPointer + 0x38;
        var result = _setup.MemoryDllMem.ReadMemory<int>(address.ToString("X"));
        if (result != -7651)
            throw new Exception("Unexpected result");
        return result;
    }
    
    [Benchmark(Description = "Memory.dll 100xReadMemory<int>")]
    public void MemoryMassReadGeneric()
    {
        for (int i = 0; i < 100; i++)
        {
            UIntPtr address = _setup.OuterClassPointer + 0x38;
            var result = _setup.MemoryDllMem.ReadMemory<int>(address.ToString("X"));
            if (result != -7651)
                throw new Exception("Unexpected result");
        }
    }
}