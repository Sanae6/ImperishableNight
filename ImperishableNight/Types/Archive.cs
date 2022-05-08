using System.Collections;
using ImperishableNight.Buffers;
using Newtonsoft.Json;

namespace ImperishableNight;

public class Archive : IEnumerable<Archive.Th8Entry> {
    private static readonly CryptParams[] crypto = {
        new CryptParams('M', 0x1b, 0x37,   0x40, 0x2000),
        new CryptParams('T', 0x51, 0xe9,   0x40, 0x3000),
        new CryptParams('A', 0xc1, 0x51, 0x1400, 0x2000),
        new CryptParams('J', 0x03, 0x19, 0x1400, 0x7800),
        new CryptParams('E', 0xab, 0xcd,  0x200, 0x1000),
        new CryptParams('W', 0x12, 0x34,  0x400, 0x2800),
        new CryptParams('-', 0x35, 0x97,   0x80, 0x2800),
        new CryptParams('*', 0x99, 0x37,  0x400, 0x1000),
    };
    public readonly byte[] FileData;
    protected readonly Th8Entry[] entries;
    protected readonly Dictionary<string, byte[]> fileEntryCache = new Dictionary<string, byte[]>();

    protected Archive(byte[] data, Th8Entry[] fileEntries) {
        FileData = data;
        entries = fileEntries;
    }

    public virtual Span<byte> this[string index] => ReadEntry(entries.First(x => x.Filename == index)).Item1;

    public int Count => entries.Length;

    public (byte[], bool) ReadEntry(Th8Entry entry) {
        if (fileEntryCache.TryGetValue(entry.Filename, out byte[]? value))
            return (value, false);

        SpanBuffer buffer = new SpanBuffer(FileData.AsSpan()[entry.Offset..(entry.Offset + entry.ZSize)]);
        buffer = buffer.Decompress(entry.Size);
        if (buffer.ReadString(3) != "edz") {
            throw new InvalidDataException("Magic is not edz");
        }

        byte type = buffer.ReadU8();
        (_, byte key, byte step, int block, int limit) = crypto.First(x => x.Type == type);
        buffer.Decrypt(entry.MagicFreeSize, key, step, block, limit);
        byte[] array = buffer.Slice(entry.MagicFreeSize).ToArray();
        fileEntryCache.Add(entry.Filename, array);
        return (array, true);
    }

    public IEnumerator<Th8Entry> GetEnumerator() {
        for (int i = 0; i < Count; i++) {
            yield return entries[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public static Archive ReadData(byte[] data) {
        SpanBuffer buffer = new SpanBuffer(data);

        Th8Header header = buffer.ReadDynamicStructure<Th8Header>();
        buffer.Offset = header.Offset;
        int zSize = buffer.Size - header.Offset;

        SpanBuffer zData = new SpanBuffer(buffer.Slice(zSize), buffer.BigEndian);
        zData.Decrypt(zSize, 0x3E, 0x9B, 0x80, 0x400);
        SpanBuffer uncompressedEntries = zData.Decompress(header.ArcSize);
        Th8Entry[] entries = new Th8Entry[header.Count];
        for (int i = 0; i < header.Count; i++) {
            Th8Entry entry = new Th8Entry {
                Filename = uncompressedEntries.ReadStringNull(),
                Offset = uncompressedEntries.ReadI32(),
                Size = uncompressedEntries.ReadI32(),
                Extra = uncompressedEntries.ReadI32()
            };
            entries[i] = entry;
        }

        if (header.Count > 0) {
            for (int i = 1; i < entries.Length; i++) {
                entries[i - 1].ZSize = entries[i].Offset - entries[i - 1].Offset;
            }

            entries[^1].ZSize = (buffer.Size - zSize) - entries[^1].Offset;
        }

        return new Archive(data, entries);
    }

    private record CryptParams(char Type, byte Key, byte Step, int Block, int Limit);

    public struct Th8Entry {
        public string Filename;
        public int Offset;
        public int Size;
        public int MagicFreeSize => Size - 4;
        public int ZSize;
        public int Extra;
    }

    public struct Th8Header : IDynamicStructure {
        public const string Magic = "PBGZ";
        public int Count = 0;
        public int Offset = 0;
        public int ArcSize = 0;

        public Th8Header() { }

        public void Load(ref SpanBuffer slice) {
            if (slice.ReadString(4) != Magic) {
                throw new InvalidDataException($"Magic is not {Magic}");
            }

            slice.Decrypt(12, 0x1B, 0x37, 12, 0x400);
            Count = slice.ReadI32() - 123456;
            Offset = slice.ReadI32() - 345678;
            ArcSize = slice.ReadI32() - 567891;
        }

        public void Save(ref SpanBuffer slice) {
            throw new NotImplementedException();
        }
    }
}