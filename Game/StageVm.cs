using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Game.Interpolation;
using Game.States;
using Game.Util;
using ImperishableNight;
using ImperishableNight.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class StageVm {
    public readonly Gameplay Owner;
    public readonly StageFile Stage;
    public readonly AnmVm Anm;
    public readonly Dictionary<int, int> Interrupts = new Dictionary<int, int>();

    public Vector3 Position;
    public Vector3 Facing;
    public Vector3 Up;
    public float Fov;
    public Vector3 FogColor => FogColorInternal;
    public float FogNear => FogNearInternal;
    public float FogFar => FogFarInternal;
    public Color ClearColor = Color.CornflowerBlue;
    public readonly Vector3Interpolator PositionInterpolator = new Vector3Interpolator(Vector3.Zero);
    public readonly Vector3Interpolator FacingInterpolator = new Vector3Interpolator(Vector3.Forward);
    public readonly Vector3Interpolator UpInterpolator = new Vector3Interpolator(Vector3.Down);
    public readonly FloatInterpolator FovInterpolator = new FloatInterpolator(30 * (MathF.PI / 180));
    public readonly Vector3Interpolator FogInterpolator = new Vector3Interpolator(Color.Aqua.ToVector3());
    public readonly FloatInterpolator FogNearInterpolator = new FloatInterpolator(0.1f);
    public readonly FloatInterpolator FogFarInterpolator = new FloatInterpolator(1000f);
    private float FogFarInternal;
    private float FogNearInternal;
    private Vector3 FogColorInternal;

    public int Ip;
    public int Time;
    public bool Stopped;

    public StageVm(Gameplay owner, StageFile stage, AnimationFile.AnimationInfo animations) {
        Owner = owner;
        Stage = stage;
        Anm = new AnmVm(animations);
        foreach (StageFile.Quad quad in stage.Instances.SelectMany(stageInstance => stage.Objects[stageInstance.ObjectId].Quads)) {
            AnmVm.Sprite sprite = new AnmVm.Sprite(Anm, animations.Animations[animations.Scripts[quad.ScriptIndex].AnmIndex], quad);
            switch (quad.QuadExtra) {
                case StageFile.RectQuadExtra rect: {
                    sprite.PositionInterpolator.Final = sprite.PositionInterpolator.Initial = rect.Position.ToXna();
                    sprite.Size = rect.Size.ToXna();
                    Console.WriteLine($"Rect quad: {sprite.Position} {sprite.Size}");
                    break;
                }
            }
            Anm.AddSprite(sprite);
        }

        ThGame.Instance.CurrentEffect.FogEnabled = true;
        ThGame.Instance.CurrentEffect.FogColor = Vector3.Zero;
        ThGame.Instance.CurrentEffect.FogStart = 0;
        ThGame.Instance.CurrentEffect.FogEnd = 1000;
    }

    public void Draw() {
        Anm.Draw();
    }

    public static uint RotateLeft(uint value, int count) {
        return (value << count) | (value >> (32 - count));
    }

    public void Update() {
        while (Ip < Stage.Instructions.Count && !Stopped) {
            StageFile.StdInstruction inst = Stage.Instructions[Ip];
            SpanBuffer buffer = inst.Buffer;
            if (inst.Time > Time) break;
            Ip++;

            switch (inst.Type) {
                case StageFile.Opcode.PosKeyframe: {
                    StageFile.StdInstruction? keyframe = Stage.Instructions.FirstOrDefault(inst2 => inst2.Time <= inst.Time);
                    if (keyframe.HasValue) {
                        PositionInterpolator.Initial = buffer.Read<Vector3>();
                        PositionInterpolator.Final = keyframe.Value.Buffer.Read<Vector3>();
                        PositionInterpolator.ResetTime(keyframe.Value.Time);
                    }

                    break;
                }
                case StageFile.Opcode.Fog: {
                    uint color = BinaryPrimitives.ReverseEndianness(RotateLeft(buffer.ReadU32(), 8));
                    FogInterpolator.Initial = FogInterpolator.Final = new Color(color).ToVector3();
                    FogNearInterpolator.Initial = FogNearInterpolator.Final = buffer.ReadF32();
                    FogFarInterpolator.Initial = FogFarInterpolator.Final = buffer.ReadF32();
                    break;
                }
                case StageFile.Opcode.FogTime: {
                    int time = buffer.ReadI32();
                    FogInterpolator.ResetTime(time);
                    FogNearInterpolator.ResetTime(time);
                    FogFarInterpolator.ResetTime(time);
                    break;
                }
                case StageFile.Opcode.Stop:
                    Stopped = true;
                    break;
                case StageFile.Opcode.Jump:
                    Ip = buffer.ReadI32();
                    Time = buffer.ReadI32();
                    break;
                case StageFile.Opcode.Pos:
                    (PositionInterpolator.Initial, PositionInterpolator.Final) = (PositionInterpolator.Final, buffer.Read<Vector3>());
                    break;
                case StageFile.Opcode.PosTime:
                    PositionInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    break;
                case StageFile.Opcode.Facing: {
                    Vector3 vec3 = buffer.Read<Vector3>();
                    (FacingInterpolator.Initial, FacingInterpolator.Final) = (FacingInterpolator.Final, vec3);
                    break;
                }
                case StageFile.Opcode.FacingTime:
                    FacingInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    break;
                case StageFile.Opcode.Up:
                    (UpInterpolator.Initial, UpInterpolator.Final) = (UpInterpolator.Final, buffer.Read<Vector3>());
                    break;
                case StageFile.Opcode.UpTime:
                    UpInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    break;
                case StageFile.Opcode.Fov:
                    (FovInterpolator.Initial, FovInterpolator.Final) = (FovInterpolator.Final, buffer.ReadF32());
                    break;
                case StageFile.Opcode.FovTime:
                    FovInterpolator.ResetTime(buffer.ReadI32(), InterpolationModes.Modes[buffer.ReadI32()]);
                    break;
                case StageFile.Opcode.ClearColor:
                    Console.WriteLine("clear color set");
                    ClearColor = new Color(BinaryPrimitives.ReverseEndianness(RotateLeft(buffer.ReadU32(), 8)));
                    break;
                case StageFile.Opcode.PosInitial:
                    PositionInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.PosFinal:
                    PositionInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.PosInitialDerivative:
                    PositionInterpolator.InitialDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.PosFinalDerivative:
                    PositionInterpolator.FinalDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.PosBezier:
                    PositionInterpolator.Bezier(buffer.ReadI32());
                    break;
                case StageFile.Opcode.FacingInitial:
                    FacingInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.FacingFinal:
                    FacingInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.FacingInitialDerivative:
                    FacingInterpolator.InitialDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.FacingFinalDerivative:
                    FacingInterpolator.FinalDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.FacingBezier:
                    FacingInterpolator.Bezier(buffer.ReadI32());
                    break;
                case StageFile.Opcode.UpInitial:
                    UpInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.UpFinal:
                    UpInterpolator.Initial = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.UpInitialDerivative:
                    UpInterpolator.InitialDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.UpFinalDerivative:
                    UpInterpolator.FinalDerivative = buffer.Read<Vector3>();
                    break;
                case StageFile.Opcode.UpBezier:
                    UpInterpolator.Bezier(buffer.ReadI32());
                    break;
                case StageFile.Opcode.SpriteA:
                // TODO: Find out what this does
                case StageFile.Opcode.SpriteB:
                // TODO: Find out what this does
                case StageFile.Opcode.InterruptLabel:
                case StageFile.Opcode.RockVector:
                // TODO: Implement rocking
                case StageFile.Opcode.RockMode:
                // TODO: Implement rocking
                case StageFile.Opcode.SpriteC:
                    // TODO: Find out what this does
                    break;
                default:
                    throw new NotImplementedException($"what {inst.Type}");
            }
        }
        
        Time++;

        PositionInterpolator.Update(ref Position);
        FacingInterpolator.Update(ref Facing);
        UpInterpolator.Update(ref Up);
        FovInterpolator.Update(ref Fov);
        FogInterpolator.Update(ref FogColorInternal);
        FogNearInterpolator.Update(ref FogNearInternal);
        FogFarInterpolator.Update(ref FogFarInternal);
        ThGame.Instance.CurrentEffect.FogColor = FogColorInternal;
        ThGame.Instance.CurrentEffect.FogStart = FogNearInternal;
        ThGame.Instance.CurrentEffect.FogEnd = FogFarInternal;

        Console.WriteLine($"{Position} {Facing} {Up} {Fov}");
        Anm.Update();
    }
}