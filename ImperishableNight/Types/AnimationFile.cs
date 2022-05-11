using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImperishableNight.Buffers;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace ImperishableNight;

public class AnimationFile {
    public AnmVersion Version;
    public string Path;
    public Vector2i Size;
    public Vector2i Position;
    public int Format;
    public int ColorKey;
    public Dictionary<int, Sprite> Sprites;
    public Dictionary<int, ScriptInfo> Scripts;
    public TextureInfo? Texture;

    private AnimationFile() { }

    public record AnimationInfo(List<AnimationFile> Animations, Dictionary<int, ScriptInfo> Scripts);

    public static AnimationInfo ReadAnm(Span<byte> data) {
        SpanBuffer buffer = new SpanBuffer(data);
        List<AnimationFile> entries = new List<AnimationFile>();
        Dictionary<int, ScriptInfo> allScripts = new Dictionary<int, ScriptInfo>();
        EosdHeader header;
        int currentCount = 0;
        do {
            //Console.WriteLine($"Reading animation #{currentCount++}");
            Bookmark curOffset = buffer.BookmarkLocation();
            header = buffer.Read<EosdHeader>();
            string path;
            {
                Bookmark bm = buffer.BookmarkLocation();
                curOffset.Jump(ref buffer, header.NameOffset);
                path = buffer.ReadStringNull();
                bm.Jump(ref buffer);
            }
            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            Dictionary<int, ScriptInfo> scripts = new Dictionary<int, ScriptInfo>();

            {
                int spriteOffsetsSize = sizeof(int) * header.SpriteCount;
                Bookmark spritesEnd = buffer.GetBookmark(SeekOrigin.Current, spriteOffsetsSize);
                Span<int> offsets = MemoryMarshal.Cast<byte, int>(buffer.Slice(spriteOffsetsSize));
                for (int i = 0; i < header.SpriteCount; i++) {
                    curOffset.Jump(ref buffer, offsets[i]);
                    Sprite sprite = buffer.Read<Sprite>();
                    sprites.Add(sprite.Id, sprite);
                }

                spritesEnd.Jump(ref buffer);
            }
            {
                int scriptOffsetsSize = Unsafe.SizeOf<ScriptHeader>() * header.ScriptCount;
                Span<ScriptHeader> scriptHeaders = MemoryMarshal.Cast<byte, ScriptHeader>(buffer.Slice(scriptOffsetsSize));
                for (int i = 0; i < header.ScriptCount; i++) {
                    curOffset.Jump(ref buffer, scriptHeaders[i].Offset);
                    int offset = 0,
                        j = 0;
                    ScriptInfo info = new ScriptInfo {
                        AnmIndex = currentCount,
                        Id = scriptHeaders[i].Id
                    };
                    while (true) {
                        AnmInstruction inst = buffer.ReadDynamic<AnmInstruction>();
                        if (inst.Type == Opcode.ParsingEnd) break;
                        inst.Offset = offset;
                        inst.Index = j;
                        offset += inst.Data.Length + AnmInstruction.HeaderSize;
                        info.Instructions.Add(inst);
                        info.OffsetIndexMap.Add(inst.Offset, j++);
                    }

                    allScripts[info.Id] = info;
                    scripts.Add(info.Id, info);
                }
            }

            TextureInfo? textureInfo = null;
            if (header.HasData != 0) {
                curOffset.Jump(ref buffer, header.ThtxOffset);
                TextureHeader texHeader = buffer.Read<TextureHeader>();
                textureInfo = new TextureInfo(texHeader, buffer.ReadBytes(texHeader.Size).ToArray());
            }

            entries.Add(new AnimationFile {
                Path = path,
                Position = header.Position,
                Size = header.Size,
                ColorKey = header.ColorKey,
                Format = header.Format,
                Version = header.Version,
                Sprites = sprites,
                Scripts = scripts,
                Texture = textureInfo
            });

            currentCount++;
            curOffset.Jump(ref buffer, header.NextOffset);
        } while (header.NextOffset > 0);

        return new AnimationInfo(entries, allScripts);
    }

    public struct Sprite {
        public int Id;
        public Vector2 Uv;
        public Vector2 Size;
    }

    public class ScriptInfo {
        public int Id;
        public int AnmIndex;
        public List<AnmInstruction> Instructions = new List<AnmInstruction>();
        public Dictionary<int, int> OffsetIndexMap = new Dictionary<int, int>();
    }

    private struct ScriptHeader {
        public int Id;
        public int Offset;
    }

    public struct AnmInstruction : IDynamicStructure {
        public const int HeaderSize = 8;
        public Opcode Type;
        public ushort Time;
        public int Offset;
        public int Index;
        public ushort ParamMask;
        public byte[] Data;
        public SpanBuffer Buffer => new SpanBuffer(Data);

        public void Load(ref SpanBuffer slice) {
            Type = (Opcode) slice.ReadU16();
            if (Type == Opcode.ParsingEnd)
                return;
            int length = slice.ReadU16();
            Time = slice.ReadU16();
            ParamMask = slice.ReadU16();
            Data = slice.ReadBytes(length - HeaderSize).ToArray();
        }

        public void Save(ref SpanBuffer slice) {
            throw new NotImplementedException();
        }
    }

    public enum Opcode {
        Nop,
        Delete,
        Static,
        Sprite,
        Jump,
        JumpDecrement,
        Pos,
        Scale,
        Alpha,
        Color,
        FlipX,
        FlipY,
        Rotate,
        AngleVel,
        ScaleGrowth,
        AlphaTimeLinear,
        BlendAdditiveSet,
        PosTimeLinear,
        PosTimeEaseOutSquare,
        PosTimeEaseOutQuad,
        Stop,
        InterruptLabel,
        AnchorTopLeft,
        StopHide,
        PosMode,
        Type, // unknown? https://exphp.github.io/thpages/#/anm/ins?g=08#ins-25
        ScrollNowX,
        ScrollNowY,
        Visible,
        ScaleTimeLinear,
        DisableZWrite,
        Ins31, // cool docs https://exphp.github.io/thpages/#/anm/ins?g=08#ins-31
        PosTime,
        ColorTime,
        AlphaTime,
        RotateTime,
        ScaleTime,
        IntSet,
        FloatSet,
        IntAdd,
        FloatAdd,
        IntSub,
        FloatSub,
        IntMul,
        FloatMul,
        IntDiv,
        FloatDiv,
        IntMod,
        FloatMod,
        IntSetAdd,
        FloatSetAdd,
        IntSetSub,
        FloatSetSub,
        IntSetMul,
        FloatSetMul,
        IntSetDiv,
        FloatSetDiv,
        IntSetMod,
        FloatSetMod,
        IntSetRand,
        FloatSetRand,
        Sin,
        Cos,
        Tan,
        Acos,
        Atan,
        SetValidAngle,
        IntEquals,
        FloatEquals,
        IntNotEquals,
        FloatNotEquals,
        IntLess,
        FloatLess,
        IntLessEquals,
        FloatLessEquals,
        IntGreater,
        FloatGreater,
        IntGreaterEquals,
        FloatGreaterEquals,
        Wait,
        ScrollX,
        ScrollY,
        BlendMode,
        PlayerHitType, // https://exphp.github.io/thpages/#/anm/ins?g=08#ins-83
        Color2,
        Alpha2,
        Color2Time,
        Alpha2Time,
        ColorSwap, // Color flag, https://exphp.github.io/thpages/#/anm/ins?g=08#ins-88
        CaseReturn,
        ParsingEnd = 0xFFFF
    }

    public record TextureInfo(TextureHeader Header, byte[] Data) {
        private byte[]? CachedRgba;

        public byte[] ConvertToRgba() {
            if (CachedRgba != null) return CachedRgba;
            switch (Header.Format) {
                case TextureFormat.Bgra8888: {
                    SpanBuffer src = new SpanBuffer(Data);
                    SpanBuffer dst = new SpanBuffer(new byte[Data.Length]);
                    while (src.HasLeft) {
                        (byte b, byte g, byte r, byte a) = (src.ReadU8(), src.ReadU8(), src.ReadU8(), src.ReadU8());
                        dst.WriteU8(r);
                        dst.WriteU8(g);
                        dst.WriteU8(b);
                        dst.WriteU8(a);
                    }

                    return CachedRgba = dst.Buffer.ToArray();
                }
                case TextureFormat.Rgb565: {
                    SpanBuffer src = new SpanBuffer(Data);
                    SpanBuffer dst = new SpanBuffer(new byte[Data.Length * 2]);
                    while (src.HasLeft) {
                        ushort rgb = src.ReadU16();
                        (byte r, byte g, byte b) = ((byte) ((rgb & 0b11111000_00000000) >> 8), (byte) ((rgb & 0b00000111_11100000) >> 3), (byte) ((rgb & 0b00000000_00011111) << 3));
                        dst.WriteU8(r);
                        dst.WriteU8(g);
                        dst.WriteU8(b);
                        dst.WriteU8(byte.MaxValue);
                    }

                    return CachedRgba = dst.Buffer.ToArray();
                }
                case TextureFormat.Argb4444: {
                    SpanBuffer src = new SpanBuffer(Data);
                    SpanBuffer dst = new SpanBuffer(new byte[Data.Length * 2]);
                    while (src.HasLeft) {
                        ushort argb = src.ReadU16();
                        dst.WriteU8(Scale((argb & 0x0F00) >> 8));
                        dst.WriteU8(Scale((argb & 0x00F0) >> 4));
                        dst.WriteU8(Scale((argb & 0x000F) >> 0));
                        dst.WriteU8(Scale((argb & 0xF000) >> 12));
                    }

                    return CachedRgba = dst.Buffer.ToArray();
                }
                case TextureFormat.Gray8: {
                    SpanBuffer src = new SpanBuffer(Data);
                    SpanBuffer dst = new SpanBuffer(new byte[Data.Length * 4]);

                    while (src.HasLeft) {
                        byte gray = src.ReadU8();
                        dst.WriteRepeatedU8(gray, 3);
                        dst.WriteU8(byte.MaxValue);
                    }

                    return CachedRgba = dst.Buffer.ToArray();
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Scale(int x) => (byte) (x * 16);
    }

    public struct TextureHeader {
        public int Magic;
        public ushort Zero;
        public TextureFormat Format;
        public ushort X, Y;
        public int Size;
    }

    public enum TextureFormat : ushort {
        Bgra8888 = 1,
        Rgb565 = 3,
        Argb4444 = 5,
        Gray8 = 7
    }

    public enum AnmVersion : uint {
        Eosd = 0,
        Pcb = 2,
        InPofv = 3,
        StbMof = 4,
        SaUfoDs = 7,
        TdAndUp = 8
    }

    private struct EosdHeader {
        public int SpriteCount;
        public int ScriptCount;
        public int TextureSlot;
        public Vector2i Size;
        public int Format;
        public int ColorKey;
        public int NameOffset;
        public Vector2i Position;
        public AnmVersion Version;
        public int MemoryPriority;
        public int ThtxOffset;
        public ushort HasData;
        public ushort LowResScale;
        public int NextOffset;
        public uint Zero3; // what
    }
}