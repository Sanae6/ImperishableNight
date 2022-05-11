using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class ColorInterpolator : Interpolator<Color> {
    public ColorInterpolator(Color initial) : base(initial) { }

    protected override void Interpolate(ref Color current) {
        current.R = (byte) InterpolationModes.Scale(Final.R, Initial.R, Mode(CurrentTime / FinalTime));
        current.G = (byte) InterpolationModes.Scale(Final.G, Initial.G, Mode(CurrentTime / FinalTime));
        current.B = (byte) InterpolationModes.Scale(Final.B, Initial.B, Mode(CurrentTime / FinalTime));
    }
}