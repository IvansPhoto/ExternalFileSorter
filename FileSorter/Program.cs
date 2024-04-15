using FileSorter;

var sorter = new TextLineSorter((int)Math.Pow(2, 20));

Console.WriteLine("Folder with file to sort");
var folder = Console.ReadLine() ?? throw new Exception("Cannot be null");

Console.WriteLine("Filename to sort");
var fileName = Console.ReadLine() ?? throw new Exception("Cannot be null");

await sorter.SortFile(folder, fileName);

// await sorter.SortFile(@"C:\Users\guteh\RiderProjects\TestFiles", "__50_000_000.txt", TextLineSorter.SplittingMethod.ArrayPool);