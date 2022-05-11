using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImperishableNight.Buffers;
using OpenTK.Mathematics;

namespace ImperishableNight;

public class StageFile {
    public string StageName;
    public Song[] Songs;
    public Dictionary<int, ObjectData> Objects;
    public List<Instance> Instances;
    public List<StdInstruction> Instructions = new List<StdInstruction>();

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
                ObjectInfo objectInfo = buffer.Read<ObjectInfo>();
                List<Quad> quads = new List<Quad>();
                int i = 0;

                while (true) {
                    Quad quad = buffer.ReadDynamic<Quad>();
                    if (quad.Type == QuadType.End) break;
                    quads.Add(quad);
                }

                objects.Add(objectInfo.Id, new ObjectData(objectInfo, quads));
            }
        }
        {
            buffer.Offset = instancesOffset;
            for (int i = 0; i < numInstances; i++)
                instances.Add(buffer.Read<Instance>());
        }
        {
            buffer.Offset = scriptOffset;
            int offset = 0,
                i = 0;
            while (true) {
                {
                    Bookmark headerStart = buffer.BookmarkLocation(6);
                    if (buffer.ReadU16() == 0xFFFF)
                        break;
                    headerStart.Jump(ref buffer);
                }
                StdInstruction header = buffer.ReadDynamic<StdInstruction>();
                header.Index = i;
                instructions.Add(header);
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
        private ushort Padding;
#pragma warning restore CS0169
        public Vector3 Position;
        public Vector3 Size;
    }

    public record struct Quad : IDynamicStructure {
        public QuadType Type;
        public ushort ScriptIndex;
        public IQuadExtra QuadExtra;

        public void Load(ref SpanBuffer slice) {
            Type = slice.Read<QuadType>();
            int size = slice.ReadU16();
            ScriptIndex = slice.ReadU16();
            slice.Offset += 2;
            switch (Type, size) {
                case (QuadType.Rectangle, 0x1c):
                    QuadExtra = slice.Read<RectQuadExtra>();
                    break;
                case (QuadType.Strip, 0x24):
                    QuadExtra = slice.Read<StripQuadExtra>();
                    break;
                case (QuadType.End, 4):
                    break;
                default:
                    throw new InvalidDataException($"Invalid quad header (type: {Type}, size: {size})");
            }
        }

        public void Save(ref SpanBuffer slice) {
            throw new NotImplementedException();
        }
    }

    public interface IQuadExtra { }

    public struct RectQuadExtra : IQuadExtra {
        public Vector3 Position;
        public Vector2 Size;
    }

    public struct StripQuadExtra : IQuadExtra {
        public Vector3 Start;
        public Vector3 End;
        public float Width;
    }

    public enum QuadType : ushort {
        Rectangle,
        Strip,
        End = ushort.MaxValue
    }

    public record struct Instance {
        public ushort ObjectId;
#pragma warning disable CS0169
        private ushort Padding;
#pragma warning restore CS0169
        public Vector3 Position;
    }

    public struct StdInstruction : IDynamicStructure {
        public const int HeaderSize = 8;
        public int Time;
        public Opcode Type;
        public int Index;
        public ushort Size;
        public byte[] Data;
        public SpanBuffer Buffer => new SpanBuffer(Data);

        public void Load(ref SpanBuffer slice) {
            Time = slice.ReadI32();
            Type = (Opcode) slice.ReadU16();
            Size = slice.ReadU16();
            Data = slice.ReadBytes(Size).ToArray();
        }

        public void Save(ref SpanBuffer slice) {
            throw new NotImplementedException();
        }
    }

    public enum Opcode : ushort {
        PosKeyframe,
        Fog,
        FogTime,
        Stop,
        Jump,
        Pos,
        PosTime,
        Facing,
        FacingTime,
        Up,
        UpTime,
        Fov,
        FovTime,
        ClearColor,
        PosInitial,
        PosFinal,
        PosInitialDerivative,
        PosFinalDerivative,
        PosBezier,
        FacingInitial,
        FacingFinal,
        FacingInitialDerivative,
        FacingFinalDerivative,
        FacingBezier,
        UpInitial,
        UpFinal,
        UpInitialDerivative,
        UpFinalDerivative,
        UpBezier,
        SpriteA, // https://exphp.github.io/thpages/#/std/ins?g=08#ins-29
        SpriteB, // https://exphp.github.io/thpages/#/std/ins?g=08#ins-30
        InterruptLabel,
        RockVector,
        RockMode,
        SpriteC // https://exphp.github.io/thpages/#/std/ins?g=08#ins-34
    }
}