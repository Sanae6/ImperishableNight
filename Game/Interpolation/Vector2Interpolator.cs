using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class Vector2Interpolator : Interpolator<Vector2> {
    public Vector2Interpolator(Vector2 initial) : base(initial) { }

    protected override void Interpolate(ref Vector2 current) {
        current = new Vector2(
            InterpolationModes.Scale(Final.X, Initial.X, Mode(CurrentTime / FinalTime)),
            InterpolationModes.Scale(Final.Y, Initial.Y, Mode(CurrentTime / FinalTime))
        );
    }
}