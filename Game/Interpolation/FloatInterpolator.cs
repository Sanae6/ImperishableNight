namespace Game.Interpolation;

public class FloatInterpolator : Interpolator<float> {
    protected override void Interpolate(ref float current) {
        current = InterpolationModes.Scale(End, Start, Mode(current));
    }
}