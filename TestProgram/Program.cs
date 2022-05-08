using ByteSizeLib;
using ImperishableNight;
using Newtonsoft.Json;
using TestProgram;

public static class Program {
    private const string BaseFolder = "thfiles";

    public static void Extract(string path) {
        Archive data = Archive.ReadData(File.ReadAllBytes(path));

        Console.WriteLine($"Extracting {path}");
        foreach (Archive.Th8Entry entry in data) {
            string filePath = Path.Combine(BaseFolder, entry.Filename);
            string basePath = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            Console.WriteLine($"Saving {filePath} (size: {ByteSize.FromBytes(entry.MagicFreeSize):0.00})");
            File.WriteAllBytes(filePath, data.ReadEntry(entry).Item1);
        }
    }

    public static void AnmToJson(string path) {
        Console.WriteLine($"Reading {path}");
        Span<byte> data = File.ReadAllBytes(path);
        File.WriteAllText(
            "anm.json",
            JsonConvert.SerializeObject(
                AnimationFile.ReadAnm(data),
                Formatting.Indented,
                new Vector2iConverter(),
                new Vector2Converter()
            )
        );
        Console.WriteLine("Wrote anm json representation to anm.json");
    }

    public static void StdToJson(string path) {
        Console.WriteLine($"Reading {path}");
        Span<byte> data = File.ReadAllBytes(path);
        File.WriteAllText(
            "std.json",
            JsonConvert.SerializeObject(
                StageFile.ReadStd(data),
                Formatting.Indented,
                new Vector2iConverter(),
                new Vector2Converter(),
                new Vector3Converter()
            )
        );
        Console.WriteLine("Wrote std json representation to std.json");
    }

    public static int Main(string[] args) {
        if (args.Length < 2) {
            Console.Error.WriteLine("[subcommand] [path to file]");
            return 1;
        }
        switch (args[0]) {
            case "anm":
                AnmToJson(args[1]);
                return 0;
            case "std":
                StdToJson(args[1]);
                return 0;
            case "extract":
                Extract(args[1]);
                return 0;
            default:
                return -1;
        }
    }
}