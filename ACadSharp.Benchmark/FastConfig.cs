using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace ACadSharp.Benchmark;
public class FastConfig : ManualConfig
{
    public FastConfig()
    {
        Add(DefaultConfig.Instance);
        AddJob(Job.Default
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithIterationCount(1)
        );
    }
}