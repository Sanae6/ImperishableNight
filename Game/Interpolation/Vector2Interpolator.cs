using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class Vector2Interpolator : Interpolator<Vector2> {
    protected override void Interpolate(ref Vector2 current) {
        current = new Vector2(
            InterpolationModes.Scale(End.X, Start.X, Mode(current.X)),
            InterpolationModes.Scale(End.Y, Start.Y, Mode(current.Y))
        );
    }
}