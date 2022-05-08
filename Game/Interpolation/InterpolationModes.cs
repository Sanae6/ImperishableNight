using System.Runtime.CompilerServices;

namespace Game.Interpolation;

public class InterpolationModes {
    public delegate float Function(float x);

    public static readonly Function[] Modes = {
        Linear,
        EaseInSquare,
        EaseInCube,
        EaseInQuad,
        Flip(EaseInSquare),
        Flip(EaseInCube),
        Flip(EaseInQuad)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Function Flip(Function f) => x => 1 - f(x - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Scale(float max, float min, float x) => min + x * (max - min);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Linear(float x) => x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInSquare(float x) => x * x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInCube(float x) => x * x * x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInQuad(float x) => x * x * x * x;
    public static Function EaseOutSquare { get; } = Flip(EaseInSquare);
    public static Function EaseOutCube { get; } = Flip(EaseInCube);
    public static Function EaseOutQuad { get; } = Flip(EaseInQuad);
}