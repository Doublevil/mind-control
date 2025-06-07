using BenchmarkDotNet.Attributes;

namespace MindControl.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class PointerPathBenchmark
{
    [Benchmark(Description = "Minimal path")]
    public PointerPath MinimalPath() => new("0");
    
    [Benchmark(Description = "Base address with offsets")]
    public PointerPath BaseAddressWithOffsets() => "1F71CD5CD88+10,10,0";

    [Benchmark(Description = "Module and offsets")]
    public PointerPath Default() => "MyDefaultModule.dll+10,10,0";
}