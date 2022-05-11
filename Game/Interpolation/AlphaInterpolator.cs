using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class AlphaInterpolator : Interpolator<Color> {
    public AlphaInterpolator(Color initial) : base(initial) { }

    protected override void Interpolate(ref Color current) {
        current.A = (byte) InterpolationModes.Scale(Final.A, Initial.A, Mode(CurrentTime / FinalTime));
    }
}