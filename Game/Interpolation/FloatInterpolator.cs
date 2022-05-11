namespace Game.Interpolation;

public class FloatInterpolator : Interpolator<float> {
    public FloatInterpolator(float initial) : base(initial) { }

    protected override void Interpolate(ref float current) {
        current = InterpolationModes.Scale(Final, Initial, Mode(CurrentTime / FinalTime));
    }
}