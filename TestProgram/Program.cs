using ByteSizeLib;
using ImperishableNight;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TestProgram;

public static class Program {
    private const string BaseFolder = "thfiles";
    private const string BaseInfoFolder = "info";

    public static void Extract(Span<byte> bytes) {
        Archive data = Archive.ReadData(bytes.ToArray());
        
        if (!Directory.Exists(BaseInfoFolder))
            Directory.CreateDirectory(BaseInfoFolder);

        foreach (Archive.Th8Entry entry in data) {
            string filePath = Path.Combine(BaseFolder, entry.Filename);
            string basePath = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            Console.WriteLine($"Saving {filePath} (size: {ByteSize.FromBytes(entry.MagicFreeSize):0.00})");
            if (entry.Filename.EndsWith(".anm"))
                AnmToJson(entry.Filename, data.ReadEntry(entry).Item1);
            else if (entry.Filename.EndsWith(".std"))
                StdToJson(entry.Filename, data.ReadEntry(entry).Item1);
            else File.WriteAllBytes(filePath, data.ReadEntry(entry).Item1);
        }
    }

    public static void AnmToJson(string name, Span<byte> data) {
        AnimationFile.AnimationInfo anms = AnimationFile.ReadAnm(data);
        File.WriteAllText(
            $"{BaseInfoFolder}/{name}.json",
            JsonConvert.SerializeObject(
                anms,
                Formatting.Indented,
                new Vector2iConverter(),
                new Vector2Converter()
            )
        );
        foreach (AnimationFile anm in anms.Animations) {
            if (anm.Texture != null) {
                Image<Rgba32> img = Image.LoadPixelData<Rgba32>(anm.Texture.ConvertToRgba(), anm.Texture.Header.X, anm.Texture.Header.Y);
                string folder = Path.GetDirectoryName(anm.Path)!;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                Console.WriteLine($"\tSaving {anm.Path}");
                img.SaveAsPng(anm.Path);
            }
        }
        Console.WriteLine("Wrote anm json representation to anm.json");
    }

    public static void StdToJson(string name, Span<byte> data) {
        File.WriteAllText(
            $"{BaseInfoFolder}/{name}.json",
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
                Console.WriteLine($"Reading {args[1]}");
                AnmToJson(Path.GetFileName(args[1]), File.ReadAllBytes(args[1]));
                return 0;
            case "std":
                Console.WriteLine($"Reading {args[1]}");
                StdToJson(Path.GetFileName(args[1]), File.ReadAllBytes(args[1]));
                return 0;
            case "extract":
                Console.WriteLine($"Extracting {args[1]}");
                Extract(File.ReadAllBytes(args[1]));
                return 0;
            default:
                return -1;
        }
    }
}