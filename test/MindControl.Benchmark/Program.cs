using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = DefaultConfig.Instance
    // We have to disable the optimizations validator because Memory.dll is not optimized
    // But we should always run benchmarks in Release in any case.
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
