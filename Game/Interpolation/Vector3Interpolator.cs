using Microsoft.Xna.Framework;

namespace Game.Interpolation;

public class Vector3Interpolator : Interpolator<Vector3> {
    public Vector3 InitialDerivative;
    public Vector3 FinalDerivative;
    private bool IsBezier;
    public Vector3Interpolator(Vector3 initial) : base(initial) { }

    protected override void Interpolate(ref Vector3 current) {
        // if (IsBezier) {
        //     // TODO: Implement Bezier curves
        //     
        //     current = new Vector3(
        //         
        //     );
        // }
        
        current = new Vector3(
            InterpolationModes.Scale(Final.X, Initial.X, Mode(CurrentTime / FinalTime)),
            InterpolationModes.Scale(Final.Y, Initial.Y, Mode(CurrentTime / FinalTime)),
            InterpolationModes.Scale(Final.Z, Initial.Z, Mode(CurrentTime / FinalTime))
        );
    }

    // public override void ResetTime(int time, InterpolationModes.Function? mode = null) {
    //     if (mode != null) IsBezier = false;
    //     base.ResetTime(time, mode);
    // }

    public void Bezier(int time) {
        ResetTime(time);
        // IsBezier = true;
    }
}