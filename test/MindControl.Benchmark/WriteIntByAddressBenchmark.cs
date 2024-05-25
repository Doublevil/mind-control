using System;
using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class WriteIntByAddressBenchmark
{
    private BenchmarkMemorySetup _setup;

    [GlobalSetup]
    public void GlobalSetup() => _setup = BenchmarkMemorySetup.Setup();

    [GlobalCleanup]
    public void GlobalCleanup() => _setup.Dispose();

    [Benchmark(Description = "MindControl Write<int>")]
    public void MindControlReadGenericType()
    {
        var result = _setup.MindControlProcessMemory.Write(_setup.OuterClassPointer + 0x38, -7651,
            MemoryProtectionStrategy.Ignore);
        if (!result.IsSuccess)
            throw new Exception("Write failed");
    }
    
    [Benchmark(Description = "Memory.dll WriteMemory", Baseline = true)]
    public void MemoryReadInt()
    {
        UIntPtr address = _setup.OuterClassPointer + 0x38;
        bool result = _setup.MemoryDllMem.WriteMemory(address.ToString("X"), "int", "-7651");
        if (!result)
            throw new Exception("Write failed");
    }
}