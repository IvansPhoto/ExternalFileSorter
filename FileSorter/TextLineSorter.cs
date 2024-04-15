using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace FileSorter;

public sealed class TextLineSorter
{
    private readonly int _splitFileLength;
    private const string Postfix = ".txt";
    private const string TempFolder = "temp";
    private const int BufferSize = 65536;

    public TextLineSorter(int splitFileLength)
    {
        _splitFileLength = splitFileLength;
    }

    public async Task SortFile(string folder, string sourceFile, SplittingMethod splittingMethod = SplittingMethod.ArrayPool, string sortedFile = "SortedFile.txt")
    {
        Console.WriteLine($"Temp file length: {_splitFileLength:000_000_000}");
        var sw = Stopwatch.StartNew();

        var tempFolder = $"{folder}/{TempFolder}";

        CleanTempFolder(folder, tempFolder, sortedFile);

        switch (splittingMethod)
        {
            case SplittingMethod.ArrayPool:
                await SplitToSortedFilesOnArrayPool(folder, tempFolder, sourceFile);
                break;
            case SplittingMethod.Channel:
                await SplitToSortedFilesOnChannel(folder, tempFolder, sourceFile);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(splittingMethod), splittingMethod, null);
        }

        Console.WriteLine($"Splitting is finished in: {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)}");

        while (true)
        {
            var fileNames = Directory.GetFiles(tempFolder);
            if (fileNames.Length <= 1)
            {
                File.Move(fileNames[0], $"{folder}/{sortedFile}");
                break;
            }

            MergeFiles(fileNames, tempFolder);

            Console.WriteLine($"Merge iteration is finished in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)}");
        }

        Console.WriteLine($"Sorting is finished in: {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)}");
    }

    private static void CleanTempFolder(string folder, string tempFolder, string sortedFile)
    {
        if (Directory.Exists(tempFolder))
            foreach (var tempFile in Directory.GetFiles(tempFolder))
                File.Delete(tempFile);
        else
            Directory.CreateDirectory(tempFolder);

        if (File.Exists($"{folder}/{sortedFile}"))
            File.Delete($"{folder}/{sortedFile}");
    }

    private async Task SplitToSortedFilesOnChannel(string folder, string tempFolder, string originalFileName)
    {
        var channel = Channel.CreateBounded<string>(10_000_000);

        var runners = Enumerable
            .Range(0, Environment.ProcessorCount / 2)
            .Select(_ => new Runner(channel.Reader, _splitFileLength, tempFolder).RunIAsyncEnumerable())
            .ToArray();

        foreach (var line in File.ReadLines($"{folder}/{originalFileName}"))
            await channel.Writer.WriteAsync(line);

        channel.Writer.Complete();
        await Task.WhenAll(runners);

        Console.WriteLine($"{Directory.GetFiles($"{tempFolder}").Length} files created.");
    }

    private async Task SplitToSortedFilesOnArrayPool(string folder, string tempFolder, string originalFileName)
    {
        var task = new List<Task>(500);
        var storage = new List<string>(_splitFileLength);
        var arrayPool = ArrayPool<string>.Shared;
        foreach (var line in File.ReadLines($"{folder}/{originalFileName}"))
        {
            storage.Add(line);
            if (storage.Count >= _splitFileLength)
            {
                var copy = arrayPool.Rent(storage.Count);
                storage.CopyTo(copy);
                storage.Clear();
                task.Add(Task.Run(() =>
                {
                    try
                    {
                        var filePath = $"{tempFolder}/{Random.Shared.Next(10_000_000)}{Postfix}";
                        Array.Sort(copy, Runner.Comparison);
                        File.WriteAllLines(filePath, copy);
                        arrayPool.Return(copy, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Environment.Exit(-123);
                    }
                }));
            }
        }
        
        if (storage.Count > 0)
        {
            storage.Sort(Runner.Comparison);
            File.WriteAllLines($"{tempFolder}/{Random.Shared.Next(10_000_000)}{Postfix}", storage);
        }

        await Task.WhenAll(task);

        Console.WriteLine($"{Directory.GetFiles($"{tempFolder}").Length} files created.");
    }
    
    private void MergeFiles(string[] fileNames, string tempFolder)
    {
        var list = new List<CoupleFiles>(fileNames.Length / 2 + 1);
        using var enumerator = (fileNames as IEnumerable<string>).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var cp = new CoupleFiles(enumerator.Current, null);

            if (enumerator.MoveNext())
                cp.PathB = enumerator.Current;

            list.Add(cp);
        }

        Parallel.ForEach(list,
            files =>
            {
                var newPath = $"{tempFolder}/{Random.Shared.Next(10_000_000)}{Postfix}";

                if (files.PathA is null || files.PathB is null)
                    return;

                var fileA = new StreamReader(File.OpenRead(files.PathA), bufferSize: BufferSize);
                var fileB = new StreamReader(File.OpenRead(files.PathB), bufferSize: BufferSize);

                try
                {
                    using var openWrite = File.OpenWrite(newPath);
                    using var streamWriter = new StreamWriter(openWrite, Encoding.UTF8, BufferSize);

                    var lineA = fileA.ReadLine();
                    var lineB = fileB.ReadLine();

                    while (lineA is not null && lineB is not null)
                    {
                        if (Runner.Comparison(lineA, lineB) <= 0)
                        {
                            streamWriter.WriteLine(lineA);
                            lineA = fileA.ReadLine();
                        }
                        else
                        {
                            streamWriter.WriteLine(lineB);
                            lineB = fileB.ReadLine();
                        }
                    }

                    if (fileA.EndOfStream)
                    {
                        var text = fileB.ReadToEnd();
                        streamWriter.Write(text);
                    }

                    if (fileB.EndOfStream)
                    {
                        var text = fileA.ReadToEnd();
                        streamWriter.Write(text);
                    }
                }
                finally
                {
                    fileA.Close();
                    fileA.Dispose();
                    fileB.Close();
                    fileB.Dispose();
                }

                if (File.Exists(files.PathA)) File.Delete(files.PathA);
                if (File.Exists(files.PathB)) File.Delete(files.PathB);
            });
    }

    private record struct CoupleFiles(string? PathA, string? PathB);
    
    public enum SplittingMethod
    {
        ArrayPool,
        Channel
    }
}

public sealed class Runner(ChannelReader<string> reader, int fileStringCapacity, string tempFolder)
{
    private const string Postfix = ".txt";
    private readonly List<string> _storage = new(fileStringCapacity);

    public async Task RunIAsyncEnumerable()
    {
        await foreach (var line in reader.ReadAllAsync())
        {
            _storage.Add(line);

            if (_storage.Count >= fileStringCapacity)
                await CreateFile();
        }

        if (_storage.Count > 0)
            await CreateFile();
    }

    private async Task CreateFile()
    {
        _storage.Sort(Comparison);
        var filePath = $"{tempFolder}/{Random.Shared.Next(10_000_000)}{Postfix}";
        await File.WriteAllLinesAsync(filePath, _storage);
        _storage.Clear();
    }

    public static int Comparison(string? x, string? y)
    {
        if (x is null) return -1;
        if (y is null) return 1;

        var indexX = x.IndexOf('.');
        var textX = x.AsSpan()[(indexX + 2)..];

        var indexY = y.IndexOf('.');
        var textY = y.AsSpan()[(indexY + 2)..];

        var textComparison = textX.CompareTo(textY, StringComparison.Ordinal);
        if (textComparison != 0)
            return textComparison;

        var compareTo = int.Parse(x.AsSpan()[..indexX]).CompareTo(int.Parse(y.AsSpan()[..indexY]));
        return compareTo;
    }
}