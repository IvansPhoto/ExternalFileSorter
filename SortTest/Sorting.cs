using FileSorter;

namespace SortTest;

public class Tests
{
    private const string Folder = @"C:\Users\guteh\RiderProjects\TestFiles";
    private const string DefaultSortedFileName = "SortedFile.txt";

    [Test]
    [TestCase(TextLineSorter.SplittingMethod.ArrayPool, "__10_000_000.txt")]
    [TestCase(TextLineSorter.SplittingMethod.Channel, "__10_000_000.txt")]
    public async Task SortedFile_Is_SortCorrectly(TextLineSorter.SplittingMethod splittingMethod, string sourceFile)
    {
        //Arrange
        await new TextLineSorter((int)Math.Pow(2, 20)).SortFile(Folder, sourceFile, splittingMethod);

        //Act, Assert
        string? previous = null;
        foreach (var line in File.ReadLines($"{Folder}/{DefaultSortedFileName}"))
        {
            if (previous is null)
            {
                previous = line;
                continue;
            }

            var compare = Runner.Comparison(previous, line);
            Assert.That(compare, Is.LessThanOrEqualTo(0), () => $"x: {previous}. y: {line}");
            previous = line;
        }
    }

    [Test]
    public void Default_SortedFile_Is_SortCorrectly()
    {
        string? previous = null;
        foreach (var line in File.ReadLines($"{Folder}/{DefaultSortedFileName}"))
        {
            if (previous is null)
            {
                previous = line;
                continue;
            }

            var compare = Runner.Comparison(previous, line);
            Assert.That(compare, Is.LessThanOrEqualTo(0), () => $"x: {previous}. y: {line}");
            previous = line;
        }
    }
}