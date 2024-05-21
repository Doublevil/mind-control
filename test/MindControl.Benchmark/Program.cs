using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using MindControl.Benchmark;

var config = DefaultConfig.Instance
    // We have to disable the optimizations validator because Memory.dll is not optimized
    // But we should always run benchmarks in Release in any case.
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
BenchmarkRunner.Run<Benchmarks>(config, args);
