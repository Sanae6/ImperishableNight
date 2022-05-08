using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class Vector3Interpolator : Interpolator<Vector3> {
    protected override void Interpolate(ref Vector3 current) {
        current = new Vector3(
            InterpolationModes.Scale(End.X, Start.X, Mode(current.X)),
            InterpolationModes.Scale(End.Y, Start.Y, Mode(current.Y)),
            InterpolationModes.Scale(End.Z, Start.Z, Mode(current.Z))
        );
    }
}