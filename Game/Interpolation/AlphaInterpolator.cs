using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class AlphaInterpolator : Interpolator<Color> {
    protected override void Interpolate(ref Color current) {
        current.A = (byte) InterpolationModes.Scale(End.A, Start.A, Mode(current.A));
    }
}