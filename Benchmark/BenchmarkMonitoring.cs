using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FileSorter;

namespace Benchmark;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 1, iterationCount: 1, invocationCount: 1)]
[MemoryDiagnoser]
public class BenchmarkMonitoring
{
    private readonly TextLineSorter _sorterChannel;
    private readonly int _fileSize = (int)Math.Pow(2, 20);
    private const string CTestfiles = @"C:\Users\guteh\RiderProjects\TestFiles";

    public BenchmarkMonitoring()
    {
        _sorterChannel = new TextLineSorter(_fileSize);
    }
    
    [Benchmark]
    public async Task ArrayPool()
    {
        await _sorterChannel.SortFile(CTestfiles, "__50_000_000.txt", TextLineSorter.SplittingMethod.ArrayPool);
    }

    [Benchmark]
    public async Task Channel() => await _sorterChannel.SortFile(@"C:\Users\guteh\RiderProjects\TestFiles", "__50_000_000.txt", TextLineSorter.SplittingMethod.Channel);
}