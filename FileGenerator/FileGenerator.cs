namespace FileGenerator;

public static class FileGenerator
{
    private static readonly string[] Fruits =
    [
        "Apple", "Banana", "Orange", "Strawberry", "Kiwi",
        "Pineapple", "Grape", "Watermelon", "Mango", "Peach",
        "Cherry", "Pear", "Plum", "Lemon", "Blueberry",
        "Raspberry", "Blackberry", "Cantaloupe", "Pomegranate",
        "Apricot", "Coconut", "Fig", "Grapefruit", "Lime",
        "Nectarine", "Papaya", "Passionfruit", "Persimmon", "Dragonfruit"
    ];

    private static readonly string[] Colours =
    [
        "Red", "Blue", "Green", "Yellow", "Purple",
        "Orange", "Pink", "Turquoise", "Brown", "Black",
        "White", "Gray", "Magenta", "Cyan", "Lavender",
        "Teal", "Maroon", "Navy", "Indigo", "Gold"
    ];

    public static void GenerateFile(int lineNumber, string filePath)
    {
        var text = $"{Fruits[Random.Shared.Next(Fruits.Length)]} {Colours[Random.Shared.Next(Colours.Length)]}";
        using var outputFile = new StreamWriter(
            Path.Combine(filePath, $"{lineNumber:###_###_###_###_000}.txt")
        );

        for (var i = 0; i < lineNumber; i++)
        {
            if (i % 11 == 0)
            {
                outputFile.WriteLine($"{Random.Shared.Next(10000)}. {text}");
                continue;
            }

            if (i % 9 == 0)
            {
                outputFile.WriteLine($"{Random.Shared.Next(10000)}. {text} {Colours[Random.Shared.Next(Colours.Length)]}");
                continue;
            }

            text = $"{Fruits[Random.Shared.Next(Fruits.Length)]} {Colours[Random.Shared.Next(Colours.Length)]}";

            outputFile.WriteLine($"{Random.Shared.Next(10000)}. {text}");
        }
    }

    public static void GenerateFile()
    {
        try
        {
            int number;
            while (true)
            {
                Console.WriteLine("Line numbers");
                if (!int.TryParse(Console.ReadLine(), out number))
                    Console.WriteLine("Number is in a wrong format");
                else
                    break;
            }

            Console.WriteLine("File path:");
            var filePath = Console.ReadLine() ?? Directory.GetCurrentDirectory();

            GenerateFile(number, filePath);

            Console.WriteLine("File generated successfully");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"File generated with errors: {exception}");
        }
    }
}