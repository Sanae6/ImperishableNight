using System.Collections.Immutable;
using Game.Interpolation;
using Game.Util;
using ImperishableNight;
using ImperishableNight.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class AnmVm {
    public readonly AnimationFile.AnimationInfo File;
    public List<Sprite> Sprites = new List<Sprite>();
    public Dictionary<AnimationFile, Texture2D> Textures = new Dictionary<AnimationFile, Texture2D>();
    public Dictionary<int, AnimationFile.ScriptInfo> Scripts;

    public AnmVm(AnimationFile.AnimationInfo anms) {
        File = anms;
        foreach (AnimationFile anm in File.Animations) {
            if (anm.Texture == null) continue;
            Texture2D tex = new Texture2D(ThGame.Instance.GraphicsDevice, anm.Size.X, anm.Size.Y, false, SurfaceFormat.Color);
            tex.SetData(anm.Texture!.ConvertToRgba());
            Textures.Add(anm, tex);
        }

        Scripts = File.Animations.SelectMany(x => x.Scripts).ToDictionary(x => x.Key, y => y.Value);
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
        public Vector2 Scale;
        public Vector2 ScaleGrowth;
        public Vector2 Uv;
        public Vector2 UvScroll;
        public Vector2 Size;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color CurrentColor => !ColorSwapped ? PrimaryColor : SecondaryColor;
        public BlendMode Blend;
        public bool AnchoredTopLeft = false; // https://exphp.github.io/thpages/#/anm/ins?g=08#ins-22
        public bool Active = true;
        public bool Visible = true;
        public bool SetRelativePosition = false;
        public bool ZWriteDisabled = false;
        public bool ColorSwapped = false; // untested LoL

        public readonly Vector3Interpolator PositionInterpolator = new Vector3Interpolator(Vector3.Zero);
        public readonly Vector3Interpolator RotationInterpolator = new Vector3Interpolator(Vector3.Zero);
        public readonly Vector2Interpolator ScaleInterpolator = new Vector2Interpolator(Vector2.One);
        public readonly ColorInterpolator ColorInterpolator = new ColorInterpolator(Color.Cornsilk);
        public readonly ColorInterpolator Color2Interpolator = new ColorInterpolator(Color.Violet);
        public readonly AlphaInterpolator AlphaInterpolator = new AlphaInterpolator(Color.White);
        public readonly AlphaInterpolator Alpha2Interpolator = new AlphaInterpolator(Color.White);
        public readonly SpriteType Type;
        public readonly StageFile.Quad Quad;

        public readonly AnimationFile.ScriptInfo Script;
        public readonly Dictionary<int, int> Interrupts = new Dictionary<int, int>();
        public int I0, I1, I2, I3, I4, I5;
        public float F0, F1, F2, F3;
        public int Ip;
        public int Time;
        public int WaitTime;
        public int ReturnIp;
        public int ReturnTime;

        private VertexBuffer VertexBuffer;

        public Sprite(AnmVm owner, AnimationFile file, AnimationFile.Sprite? sprite, int script, SpriteType spriteType) {
            Owner = owner;
            File = file;
            Type = spriteType;
            if (sprite.HasValue) {
                SetSprite(sprite.Value);
            }

            Script = owner.File.Scripts[script];
            VertexBuffer = new VertexBuffer(
                ThGame.Instance.GraphicsDevice,
                VertexPositionColorTexture.VertexDeclaration,
                4,
                BufferUsage.WriteOnly
            );

            foreach (AnimationFile.AnmInstruction inst in Script.Instructions.Where(x => x.Type == AnimationFile.Opcode.InterruptLabel))
                Interrupts.Add(inst.Buffer.ReadI32(), inst.Index);
        }

        public Sprite(AnmVm owner, AnimationFile file, StageFile.Quad quad)
            : this(owner, file, null, quad.ScriptIndex, quad.Type switch {
                StageFile.QuadType.Rectangle => SpriteType.StageRect,
                StageFile.QuadType.Strip => SpriteType.StageStrip,
                StageFile.QuadType.End or _ => throw new ArgumentOutOfRangeException(nameof(quad))
            }) {
            Owner = owner;
            Quad = quad;
        }

        public void SetSprite(int index) {
            SetSprite(File.Sprites[index]);
        }

        public void SetSprite(AnimationFile.Sprite sprite) {
            TextureInfo = sprite;
            Uv = sprite.Uv.ToXna();
            Size = sprite.Size.ToXna();
        }

        public void Draw() {
            switch (Type) {
                case SpriteType.StageRect: {
                    // https://projects.govanify.com/govanify/touhou/-/blob/master/src/th06/anm0_vm.rs#L90
                    // eosd anm is older than pcb anm, that link isn't right, and i'm not sure how pcb+ does this
        
                    // Matrix mat = new Matrix(
                    //     -0.5f, 0.5f, 0.5f, -0.5f,
                    //     -0.5f, -0.5f, 0.5f, 0.5f,
                    //     0f, 0f, 0f, 0f,
                    //     1f, 1f, 1f, 1f
                    // );
                    Matrix mat = Matrix.Identity;
                    ((float u, float v), (float tw, float th)) = (Uv, Size);
                    (float sx, float sy) = Scale;
                    mat *= Matrix.CreateScale(tw * sx, th * sy, 1f);
                    mat *= Matrix.CreateRotationX(Rotation.X);
                    mat *= Matrix.CreateRotationY(Rotation.Y);
                    mat *= Matrix.CreateRotationZ(Rotation.Z);
                    mat *= Matrix.CreateTranslation(Position);
                    // Console.WriteLine(mat.Translation);
                    // if (AnchoredTopLeft) {
                    //     mat *= Matrix.CreateTranslation(-sx / 2f, -sy / 2f, 1f);
                    // }

                    Texture2D tex = ThGame.Instance.CurrentEffect.Texture = Owner.Textures[File];
                    (float w, float h) = (1f / tex.Width, 1f / tex.Height);

                    float left = u * w;
                    float right = (u + tw) * w;
                    float bottom = v * h;
                    float top = (v + th) * h;

                    VertexPositionColorTexture[] vertices = {
                        // new VertexPositionColorTexture(new Vector3(mat[0, 0], mat[0, 1], mat[0, 2]), Color.White, new Vector2(left, bottom)),
                        // new VertexPositionColorTexture(new Vector3(mat[1, 0], mat[1, 1], mat[1, 2]), Color.White, new Vector2(right, bottom)),
                        // new VertexPositionColorTexture(new Vector3(mat[2, 0], mat[2, 1], mat[2, 2]), Color.White, new Vector2(right, top)),
                        // new VertexPositionColorTexture(new Vector3(mat[3, 0], mat[3, 1], mat[3, 2]), Color.White, new Vector2(left, top))
                        new VertexPositionColorTexture(Position, Color.White, new Vector2(left, bottom)),
                        new VertexPositionColorTexture(Position + new Vector3(Vector2.UnitX * Scale * Size, Position.Z), Color.White, new Vector2(right, bottom)),
                        new VertexPositionColorTexture(Position + new Vector3(Scale * Size, Position.Z), Color.White, new Vector2(right, top)),
                        new VertexPositionColorTexture(Position + new Vector3(Vector2.UnitY * Scale * Size, Position.Z), Color.White, new Vector2(left, top))
                    };
                    // Console.WriteLine($"A {vertices[0].Position} {vertices[1].Position} {vertices[2].Position} {vertices[3].Position}");
                    int[] indices = {
                        1, 2, 3,
                        0, 1, 3,
                    };

                    foreach (EffectPass pass in ThGame.Instance.CurrentEffect.CurrentTechnique.Passes) {
                        pass.Apply();
                        ThGame.Instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Update() {
            while (Active && WaitTime == 0 && Ip < Script.Instructions.Count) {
                AnimationFile.AnmInstruction inst = Script.Instructions[Ip];
                if (Time < inst.Time) break;
                Ip++;
                if (Ip >= Script.Instructions.Count) Active = false;

                if (Time == inst.Time) RunInstruction(inst);
            }

            if (WaitTime > 0) WaitTime--;
            else if (Active) Time++;

            PositionInterpolator.Update(ref Position);
            RotationInterpolator.Update(ref Rotation);
            ScaleInterpolator.Update(ref Scale);
            ColorInterpolator.Update(ref PrimaryColor);
            Color2Interpolator.Update(ref SecondaryColor);
            AlphaInterpolator.Update(ref SecondaryColor);
            Alpha2Interpolator.Update(ref SecondaryColor);
            Uv += UvScroll;
        }

        public void Interrupt(int interrupt) {
            if (!Interrupts.TryGetValue(interrupt, out int ip)) {
                Console.Error.WriteLine($"Interrupt {interrupt} not found");
                return;
            }

            Visible = true;
            Active = true;
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
                    Position = buffer.Read<Vector3>();
                    break;
                case AnimationFile.Opcode.Scale:
                    Scale = buffer.Read<Vector2>();
                    break;
                case AnimationFile.Opcode.Alpha:
                    PrimaryColor = new Color(PrimaryColor, buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Color: {
                    (PrimaryColor, byte a) = (new Color(buffer.Read<Vector3>()), PrimaryColor.A);
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
                    Rotation = buffer.Read<Vector3>();
                    break;
                case AnimationFile.Opcode.AngleVel:
                    AngularVelocity = buffer.Read<Vector3>();
                    break;
                case AnimationFile.Opcode.ScaleGrowth:
                    ScaleGrowth = buffer.Read<Vector2>();
                    break;
                case AnimationFile.Opcode.AlphaTimeLinear:
                    AlphaInterpolator.Initial = AlphaInterpolator.Final;
                    AlphaInterpolator.Final = new Color(Color.White, buffer.ReadI32());
                    AlphaInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.BlendAdditiveSet:
                    Blend = buffer.ReadI32() > 0 ? BlendMode.Add : BlendMode.Normal;
                    break;
                case AnimationFile.Opcode.PosTimeLinear:
                    PositionInterpolator.Initial = PositionInterpolator.Final;
                    PositionInterpolator.Final = buffer.Read<Vector3>();
                    PositionInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.PosTimeEaseOutSquare:
                    PositionInterpolator.Initial = PositionInterpolator.Final;
                    PositionInterpolator.Final = buffer.Read<Vector3>();
                    PositionInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.EaseOutSquare);
                    break;
                case AnimationFile.Opcode.PosTimeEaseOutQuad:
                    PositionInterpolator.Initial = PositionInterpolator.Final;
                    PositionInterpolator.Final = buffer.Read<Vector3>();
                    PositionInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.EaseOutQuad);
                    break;
                case AnimationFile.Opcode.Stop:
                    // Stopped = true;
                    WaitTime = -1;
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
                    ScaleInterpolator.Initial = ScaleInterpolator.Final;
                    ScaleInterpolator.Final = buffer.Read<Vector2>();
                    ScaleInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Linear);
                    break;
                case AnimationFile.Opcode.DisableZWrite:
                    ZWriteDisabled = buffer.ReadI32() > 0;
                    break;
                case AnimationFile.Opcode.Ins31:
                    // TODO: What should this do???
                    break;
                case AnimationFile.Opcode.PosTime: {
                    PositionInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    PositionInterpolator.Initial = PositionInterpolator.Final;
                    PositionInterpolator.Final = buffer.Read<Vector3>();
                    break;
                }
                case AnimationFile.Opcode.ColorTime: {
                    ColorInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    ColorInterpolator.Initial = ColorInterpolator.Final;
                    ColorInterpolator.Final = new Color(buffer.Read<Vector3>());
                    break;
                }
                case AnimationFile.Opcode.AlphaTime: {
                    AlphaInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    AlphaInterpolator.Initial = AlphaInterpolator.Final;
                    AlphaInterpolator.Final = new Color(Color.White, buffer.ReadF32());
                    break;
                }
                case AnimationFile.Opcode.RotateTime: {
                    RotationInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    RotationInterpolator.Initial = RotationInterpolator.Final;
                    RotationInterpolator.Final = buffer.Read<Vector3>();
                    break;
                }
                case AnimationFile.Opcode.ScaleTime: {
                    ScaleInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    ScaleInterpolator.Initial = ScaleInterpolator.Final;
                    ScaleInterpolator.Final = buffer.Read<Vector2>();
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
                    // TODO: Implement seeded random number generation
                    GetIntVar(buffer.ReadI32()) = Random.Shared.Next();
                    break;
                case AnimationFile.Opcode.FloatSetRand:
                    // TODO: Implement seeded random number generation
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
                    if (Math.Abs(buffer.ReadF32() - buffer.ReadF32()) < 0.001) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.IntNotEquals:
                    if (buffer.ReadI32() != buffer.ReadI32()) goto case AnimationFile.Opcode.Jump;
                    break;
                case AnimationFile.Opcode.FloatNotEquals:
                    if (Math.Abs(buffer.ReadF32() - buffer.ReadF32()) > 0.001) goto case AnimationFile.Opcode.Jump;
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
                case AnimationFile.Opcode.PlayerHitType:
                    // TODO: Find out what this does
                    break;
                case AnimationFile.Opcode.Color2: {
                    (SecondaryColor, byte a) = (new Color(buffer.Read<Vector3>()), SecondaryColor.A);
                    SecondaryColor.A = a;
                    break;
                }
                case AnimationFile.Opcode.Alpha2:
                    SecondaryColor = new Color(SecondaryColor, buffer.ReadF32());
                    break;
                case AnimationFile.Opcode.Color2Time: {
                    Color2Interpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    Color2Interpolator.Initial = Color2Interpolator.Final;
                    Color2Interpolator.Final = new Color(buffer.Read<Vector3>());
                    break;
                }
                case AnimationFile.Opcode.Alpha2Time: {
                    Alpha2Interpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    Alpha2Interpolator.Initial = Alpha2Interpolator.Final;
                    Alpha2Interpolator.Final = new Color(Color.White, buffer.ReadF32());
                    break;
                }
                case AnimationFile.Opcode.ColorSwap:
                    ColorSwapped = (buffer.ReadI32() & 2) > 0;
                    break;
                case AnimationFile.Opcode.CaseReturn:
                    (Ip, Time, ReturnIp, ReturnTime) = (ReturnIp, (ushort) ReturnTime, -1, -1);
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

        public enum SpriteType {
            Enemy, // TODO: Implement enemies
            Ui, // TODO: Implement UI
            StageRect,
            StageStrip
        }
    }
}