using System.Runtime.CompilerServices;
using ImperishableNight.Buffers;
using OpenTK.Mathematics;

namespace ImperishableNight;

public class StageFile {
    public string StageName;
    public Song[] Songs;
    public Dictionary<int, ObjectData> Objects;
    public List<Instance> Instances;
    public List<StdInstruction> Instructions;
    private StageFile() { }

    public static StageFile ReadStd(Span<byte> data) {
        SpanBuffer buffer = new SpanBuffer(data);
        (ushort numObjects, ushort numInstances) = (buffer.ReadU16(), buffer.ReadU16());
        (int instancesOffset, int scriptOffset, _) = (buffer.ReadI32(), buffer.ReadI32(), buffer.BookmarkLocation(sizeof(int)));
        string stageName = buffer.ReadStringNull(128);
        (string song1Name, string song2Name, string song3Name, string song4Name)
            = (buffer.ReadStringNull(128), buffer.ReadStringNull(128), buffer.ReadStringNull(128), buffer.ReadStringNull(128));
        (string song1Path, string song2Path, string song3Path, string song4Path)
            = (buffer.ReadStringNull(128), buffer.ReadStringNull(128), buffer.ReadStringNull(128), buffer.ReadStringNull(128));
        Dictionary<int, ObjectData> objects = new Dictionary<int, ObjectData>();
        List<Instance> instances = new List<Instance>();
        List<StdInstruction> instructions = new List<StdInstruction>();

        {
            Span<int> offsets = buffer.Slice<int>(numObjects);
            foreach (int objectOffset in offsets) {
                buffer.Offset = objectOffset;
                ObjectInfo objectInfo = buffer.ReadStructure<ObjectInfo>();
                List<Quad> quads = new List<Quad>();

                while (true) {
                    Quad quad = buffer.ReadStructure<Quad>();
                    if (quad.StructSize == 4) break;
                    quads.Add(quad);
                }

                objects.Add(objectInfo.Id, new ObjectData(objectInfo, quads));
            }
        }
        {
            buffer.Offset = instancesOffset;
            for (int i = 0; i < numInstances; i++)
                instances.Add(buffer.ReadStructure<Instance>());
        }
        {
            buffer.Offset = scriptOffset;
            while (true) {
                InstructionHeader header = buffer.ReadStructure<InstructionHeader>();
                if (header.Size == 0xFFFF)
                    break;
                instructions.Add(new StdInstruction {
                    Time = header.Time,
                    Type = header.Type,
                    Size = header.Size,
                    Data = buffer.ReadBytes(header.Size).ToArray()
                });
            }
        }

        return new StageFile {
            StageName = stageName,
            Songs = new[] {
                new Song(song1Name, song1Path),
                new Song(song2Name, song2Path),
                new Song(song3Name, song3Path),
                new Song(song4Name, song4Path),
            },
            Objects = objects,
            Instances = instances,
            Instructions = instructions
        };
    }

    public record struct Song(string Name, string Path);

    public record struct ObjectData(ObjectInfo Info, List<Quad> Quads);

    public record struct ObjectInfo {
        public ushort Id;
#pragma warning disable CS0169
        private ushort padding;
#pragma warning restore CS0169
        public Vector3 Position;
        public Vector3 Size;
    }

    public record struct Quad {
#pragma warning disable CS0169
        private ushort unk;
#pragma warning restore CS0169
        public ushort StructSize;
        public ushort ScriptIndex;
#pragma warning disable CS0169
        private ushort padding;
#pragma warning restore CS0169
        public Vector3 Position;
        public Vector2 Size;
    }

    public record struct Instance {
        public ushort ObjectId;
#pragma warning disable CS0169
        private ushort padding;
#pragma warning restore CS0169
        public Vector3 Position;
    }

    public struct StdInstruction {
        public int Time;
        public ushort Type;
        public ushort Size;
        public byte[] Data;
    }

    private struct InstructionHeader {
        public int Time;
        public ushort Type;
        public ushort Size;
    }
}