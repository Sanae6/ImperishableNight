using System.Collections.Immutable;
using Game.Interpolation;
using Game.Util;
using ImperishableNight;
using ImperishableNight.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class AnmVm {
    public readonly List<AnimationFile> Animations;
    public List<Sprite> Sprites = new List<Sprite>();
    public List<Texture2D> Textures = new List<Texture2D>();
    public Dictionary<int, AnimationFile.ScriptInfo> Scripts;

    public AnmVm(List<AnimationFile> anms) {
        Animations = anms;
        foreach (AnimationFile anm in Animations.Where(anm => anm.Texture != null))
            Textures.Add(new Texture2D(ThGame.Instance.GraphicsDevice, anm.Size.X, anm.Size.Y, false, anm.Texture!.Header.Format switch {
                AnimationFile.TextureFormat.Bgra8888 => SurfaceFormat.Bgra32,
                AnimationFile.TextureFormat.Gray8 => throw new NotImplementedException("Gray textures not yet supported"),
                AnimationFile.TextureFormat.Rgb565 => throw new NotImplementedException("Rgb565 textures not yet supported"),
                AnimationFile.TextureFormat.Argb4444 => throw new NotImplementedException("Argb4444 textures not yet supported"),
                _ => throw new ArgumentOutOfRangeException(nameof(anms))
            }));

        Scripts = Animations.SelectMany(x => x.Scripts).ToDictionary(x => x.Key, y => y.Value);
    }

    public void AddSprite(Sprite sprite) {
        Sprites.Add(sprite);
    }

    public void Draw() {
        foreach (Sprite sprite in Sprites.Where(x => x.Visible)) {
            sprite.Draw();
        }
    }

    public void Update() {
        foreach (Sprite sprite in Sprites.Where(x => x.Active)) {
            sprite.Update();
        }
    }

    public class Sprite {
        public readonly AnmVm Owner;
        public readonly AnimationFile File;
        public AnimationFile.Sprite? TextureInfo;
        public Vector3 Position;
        public Vector3 Offset;
        public Vector3 Rotation;
        public Vector3 AngularVelocity;
        public Vector2 Scale = Vector2.One;
        public Vector2 ScaleGrowth;
        public Vector2 Uv = Vector2.One;
        public Vector2 UvScroll;
        public Vector2 Size = Vector2.One;
        public Color PrimaryColor = Color.White;
        public Color SecondaryColor = Color.White;
        public BlendMode Blend;
        public bool AnchoredTopLeft = false; // https://exphp.github.io/thpages/#/anm/ins?g=08#ins-22
        public bool Active = true;
        public bool Visible = true;
        public bool SetRelativePosition = false;
        public bool ZWriteDisabled = false;
        public bool ColorSwapped = false; // untested LoL

        public readonly Vector3Interpolator PosInterpolator = new Vector3Interpolator();
        public readonly Vector3Interpolator RotationInterpolator = new Vector3Interpolator();
        public readonly Vector2Interpolator ScaleInterpolator = new Vector2Interpolator();
        public readonly ColorInterpolator ColorInterpolator = new ColorInterpolator();
        public readonly ColorInterpolator Color2Interpolator = new ColorInterpolator();
        public readonly AlphaInterpolator AlphaInterpolator = new AlphaInterpolator();
        public readonly AlphaInterpolator Alpha2Interpolator = new AlphaInterpolator();

        public readonly AnimationFile.ScriptInfo Script;
        public readonly Dictionary<int, int> Interrupts = new Dictionary<int, int>();
        public int I0, I1, I2, I3, I4, I5;
        public float F0, F1, F2, F3;
        public int Ip;
        public int Time;
        public bool Stopped;
        public int WaitTime;
        public int ReturnIp;
        public int ReturnTime;

        public Sprite(AnmVm owner, AnimationFile file, AnimationFile.Sprite? sprite, int script) {
            Owner = owner;
            File = file;
            if (sprite.HasValue) {
                SetSprite(sprite.Value);
            }

            Script = file.Scripts[script];

            foreach (AnimationFile.AnmInstruction inst in Script.Instructions.Where(x => x.Type == AnimationFile.Opcode.InterruptLabel))
                Interrupts.Add(inst.Buffer.ReadI32(), inst.Index);
        }

        public Sprite(AnmVm owner, AnimationFile file, AnimationFile.Sprite? sprite, int script, Vector2 size)
            : this(owner, file, sprite, script) {
            Owner = owner;
        }

        public void SetSprite(int index) {
            SetSprite(File.Sprites[index]);
        }

        public void SetSprite(AnimationFile.Sprite sprite) {
            TextureInfo = sprite;
            Uv = sprite.Uv.ToXna();
            Size = sprite.Size.ToXna();
        }

        public void Draw() { }

        public void Update() {
            if (!Active) return;

            while (Active && WaitTime != 0 && Ip < Script.Instructions.Count) {
                AnimationFile.AnmInstruction inst = Script.Instructions[Ip];
                if (Time > inst.Time) break;
                Ip++;

                if (Time == inst.Time) RunInstruction(inst);
            }

            if (WaitTime > 0) WaitTime--;
            else Time++;
        }

        public void Interrupt(int interrupt) {
            if (!Interrupts.TryGetValue(interrupt, out int ip)) {
                Console.Error.WriteLine($"Interrupt {interrupt} not found");
                return;
            }

            Visible = true;
            ReturnIp = Ip;
            ReturnTime = Time;
            Ip = ip;
            Time = Script.Instructions[ip].Time;
        }

        public ref int GetIntVar(int register) {
            switch (register) {
                case 10000: return ref I0;
                case 10001: return ref I1;
                case 10002: return ref I2;
                case 10003: return ref I3;
                case 10008: return ref I4;
                case 10009: return ref I5;
                default: throw new ArgumentOutOfRangeException(nameof(register));
            }
        }

        public ref float GetFloatVar(int register) {
            switch (register) {
                case 10004: return ref F0;
                case 10005: return ref F1;
                case 10006: return ref F2;
                case 10007: return ref F3;
                default: throw new ArgumentOutOfRangeException(nameof(register));
            }
        }

        public void RunInstruction(AnimationFile.AnmInstruction inst) {
            SpanBuffer buffer = inst.Buffer;
            switch (inst.Type) {
                case AnimationFile.Opcode.Delete:
                    Visible = false;
                    goto case AnimationFile.Opcode.Static;
                case AnimationFile.Opcode.Static:
                    Active = false;
                    break;
                case AnimationFile.Opcode.Sprite:
                    SetSprite(buffer.ReadI32());
                    break;
                case AnimationFile.Opcode.Jump:
                    Ip = Script.OffsetIndexMap[buffer.ReadI32()];
                    Time = (ushort) buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.JumpDecrement:
                    GetIntVar(buffer.ReadI32())++;
                    goto case AnimationFile.Opcode.Jump;
                case AnimationFile.Opcode.Pos:
                    Position = buffer.ReadStructure<Vector3>();
                    break;
                case AnimationFile.Opcode.Scale:
                    Scale = buffer.ReadStructure<Vector2>();
                    break;
                case AnimationFile.Opcode.Alpha:
                    PrimaryColor = new Color(PrimaryColor, buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Color: {
                    (PrimaryColor, byte a) = (new Color(buffer.ReadStructure<Vector3>()), PrimaryColor.A);
                    PrimaryColor.A = a;
                    break;
                }
                case AnimationFile.Opcode.FlipX:
                    Scale.X = -Scale.X;
                    break;
                case AnimationFile.Opcode.FlipY:
                    Scale.Y = -Scale.Y;
                    break;
                case AnimationFile.Opcode.Rotate:
                    Rotation = buffer.ReadStructure<Vector3>();
                    break;
                case AnimationFile.Opcode.AngleVel:
                    AngularVelocity = buffer.ReadStructure<Vector3>();
                    break;
                case AnimationFile.Opcode.ScaleGrowth:
                    ScaleGrowth = buffer.ReadStructure<Vector2>();
                    break;
                case AnimationFile.Opcode.AlphaTimeLinear:
                    AlphaInterpolator.Set(PrimaryColor, new Color(Color.White, buffer.ReadI32()), buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.BlendAdditiveSet:
                    Blend = buffer.ReadI32() > 0 ? BlendMode.Add : BlendMode.Normal;
                    break;
                case AnimationFile.Opcode.PosTimeLinear:
                    PosInterpolator.Set(Position, buffer.ReadStructure<Vector3>(), buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.PosTimeEaseOutSquare:
                    PosInterpolator.Set(Position, buffer.ReadStructure<Vector3>(), buffer.ReadI32(), InterpolationModes.EaseOutSquare);
                    break;
                case AnimationFile.Opcode.PosTimeEaseOutQuad:
                    PosInterpolator.Set(Position, buffer.ReadStructure<Vector3>(), buffer.ReadI32(), InterpolationModes.EaseOutQuad);
                    break;
                case AnimationFile.Opcode.Stop:
                    Stopped = true;
                    break;
                case AnimationFile.Opcode.AnchorTopLeft:
                    AnchoredTopLeft = true;
                    break;
                case AnimationFile.Opcode.StopHide:
                    Visible = false;
                    goto case AnimationFile.Opcode.Stop;
                case AnimationFile.Opcode.PosMode:
                    SetRelativePosition = buffer.ReadI32() > 0;
                    break;
                case AnimationFile.Opcode.Type:
                    // TODO: What???
                    break;
                case AnimationFile.Opcode.ScrollNowX:
                    Uv.X += 1 / Size.X;
                    break;
                case AnimationFile.Opcode.ScrollNowY:
                    Uv.Y += 1 / Size.X;
                    break;
                case AnimationFile.Opcode.Visible:
                    Visible = buffer.ReadI32() > 0;
                    break;
                case AnimationFile.Opcode.ScaleTimeLinear:
                    ScaleInterpolator.Set(Scale, buffer.ReadStructure<Vector2>(), buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.DisableZWrite:
                    ZWriteDisabled = buffer.ReadI32() > 0;
                    break;
                case AnimationFile.Opcode.Ins31:
                    throw new NotImplementedException("What should this do?");
                case AnimationFile.Opcode.PosTime: {
                    (int time, int mode, Vector3 goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadStructure<Vector3>());
                    PosInterpolator.Set(Position, goal, time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.ColorTime: {
                    (int time, int mode, Vector3 goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadStructure<Vector3>());
                    ColorInterpolator.Set(PrimaryColor, new Color(goal), time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.AlphaTime: {
                    (int time, int mode, float goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadF32());
                    AlphaInterpolator.Set(PrimaryColor, new Color(Color.White, goal), time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.RotateTime: {
                    (int time, int mode, Vector3 goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadStructure<Vector3>());
                    RotationInterpolator.Set(Rotation, goal, time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.ScaleTime: {
                    (int time, int mode, Vector2 goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadStructure<Vector2>());
                    ScaleInterpolator.Set(Scale, goal, time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.IntSet:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSet:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntAdd:
                    GetIntVar(buffer.ReadI32()) += buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatAdd:
                    GetFloatVar(buffer.ReadI32()) += buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSub:
                    GetIntVar(buffer.ReadI32()) -= buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSub:
                    GetFloatVar(buffer.ReadI32()) -= buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntMul:
                    GetIntVar(buffer.ReadI32()) *= buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatMul:
                    GetFloatVar(buffer.ReadI32()) *= buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntDiv:
                    GetIntVar(buffer.ReadI32()) /= buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatDiv:
                    GetFloatVar(buffer.ReadI32()) /= buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntMod:
                    GetIntVar(buffer.ReadI32()) %= buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatMod:
                    GetFloatVar(buffer.ReadI32()) %= buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetAdd:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32() + buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSetAdd:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32() + buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetSub:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32() - buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSetSub:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32() - buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetMul:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32() * buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSetMul:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32() * buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetDiv:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32() / buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSetDiv:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32() / buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetMod:
                    GetIntVar(buffer.ReadI32()) = buffer.ReadI32() % buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.FloatSetMod:
                    GetFloatVar(buffer.ReadI32()) = buffer.ReadF32() % buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.IntSetRand:
                    // TODO: PROPERLY IMPLEMENT SEEDED RNG
                    GetIntVar(buffer.ReadI32()) = Random.Shared.Next();
                    break;
                case AnimationFile.Opcode.FloatSetRand:
                    // TODO: PROPERLY IMPLEMENT SEEDED RNG
                    GetFloatVar(buffer.ReadI32()) = Random.Shared.NextSingle();
                    break;
                case AnimationFile.Opcode.Sin:
                    GetFloatVar(buffer.ReadI32()) = MathF.Sin(buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Cos:
                    GetFloatVar(buffer.ReadI32()) = MathF.Cos(buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Tan:
                    GetFloatVar(buffer.ReadI32()) = MathF.Tan(buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Acos:
                    GetFloatVar(buffer.ReadI32()) = MathF.Acos(buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Atan:
                    GetFloatVar(buffer.ReadI32()) = MathF.Atan(buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.SetValidAngle:
                    GetFloatVar(buffer.ReadI32()) = InterpolationModes.Scale(MathF.PI, -MathF.PI, buffer.ReadF32() % (2 * MathF.PI));
                    break;
                case AnimationFile.Opcode.IntEquals:
                    if (buffer.ReadI32() == buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatEquals:
                    if (buffer.ReadF32() == buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntNotEquals:
                    if (buffer.ReadI32() != buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatNotEquals:
                    if (buffer.ReadF32() != buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntLess:
                    if (buffer.ReadI32() < buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatLess:
                    if (buffer.ReadF32() < buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntLessEquals:
                    if (buffer.ReadI32() <= buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatLessEquals:
                    if (buffer.ReadF32() <= buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntGreater:
                    if (buffer.ReadI32() > buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatGreater:
                    if (buffer.ReadF32() > buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntGreaterEquals:
                    if (buffer.ReadI32() >= buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatGreaterEquals:
                    if (buffer.ReadF32() >= buffer.ReadF32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.Wait:
                    WaitTime = buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.ScrollX:
                    UvScroll.X = buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.ScrollY:
                    UvScroll.Y = buffer.ReadF32();
                    break;
                case AnimationFile.Opcode.BlendMode:
                    Blend = (BlendMode) buffer.ReadI32();
                    break;
                case AnimationFile.Opcode.Color2: {
                    (SecondaryColor, byte a) = (new Color(buffer.ReadStructure<Vector3>()), SecondaryColor.A);
                    SecondaryColor.A = a;
                    break;
                }
                case AnimationFile.Opcode.Alpha2:
                    SecondaryColor = new Color(SecondaryColor, buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Color2Time: {
                    (int time, int mode, Vector3 goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadStructure<Vector3>());
                    Color2Interpolator.Set(SecondaryColor, new Color(goal), time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.Alpha2Time: {
                    (int time, int mode, float goal) = (buffer.ReadI32(), buffer.ReadI32(), buffer.ReadF32());
                    Alpha2Interpolator.Set(SecondaryColor, new Color(Color.White, goal), time, InterpolationModes.Modes[mode]);
                    break;
                }
                case AnimationFile.Opcode.ColorSwap:
                    ColorSwapped = (buffer.ReadI32() & 2) > 0;
                    break;
                case AnimationFile.Opcode.CaseReturn:
                    Ip = ReturnIp;
                    Time = (ushort) ReturnTime;
                    break;
                case AnimationFile.Opcode.ParsingEnd:
                    throw new NotImplementedException("This should never happen.");
                case AnimationFile.Opcode.Nop:
                case AnimationFile.Opcode.InterruptLabel:
                default:
                    break;
            }
        }

        public enum BlendMode {
            Normal,
            Add,
            Subtract,
            Replace,
            Screen,
            Multiply,
            Uh, // i'm not giving this a name, see https://exphp.github.io/thpages/#/anm/ins?g=08#ins-82
            Behind,
            Darken,
            Lighten
        }
    }
}