using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class ColorInterpolator : Interpolator<Color> {
    
    protected override void Interpolate(ref Color current) {
        current.R = (byte) InterpolationModes.Scale(End.R, Start.R, Mode(current.R));
        current.G = (byte) InterpolationModes.Scale(End.G, Start.G, Mode(current.G));
        current.B = (byte) InterpolationModes.Scale(End.B, Start.B, Mode(current.B));
    }
}