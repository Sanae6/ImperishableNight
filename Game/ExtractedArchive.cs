using ImperishableNight;

namespace Game;

public class ExtractedArchive : Archive {
    private const string BaseFolder = "Cache";

    public ExtractedArchive(Archive child) : base(child.FileData, child.ToArray()) {
        Directory.CreateDirectory(BaseFolder);
        foreach (Th8Entry entry in this) {
            string path = Path.Combine(BaseFolder, entry.Filename);
            if (File.Exists(path))
                FileEntryCache.Add(entry.Filename, File.ReadAllBytes(path));
        }
    }

    public override Span<byte> this[string index] {
        get {
            Th8Entry entry = Entries.First(x => x.Filename == index);
            (byte[]? data, bool added) = ReadEntry(entry);
            if (added) File.WriteAllBytes(Path.Combine(BaseFolder, entry.Filename), data);
            return data.AsSpan();
        }
    }
}