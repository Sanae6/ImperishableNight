namespace Game.Interpolation;

public abstract class Interpolator<T> {
    protected InterpolationModes.Function Mode = InterpolationModes.Linear;
    public bool Active => CurrentTime < FinalTime && FinalTime != 0;
    public int FinalTime;
    public float CurrentTime;
    public T Initial;
    public T Final;

    protected Interpolator(T initial) {
        Final = Initial = initial;
    }

    public virtual void ResetTime(int time, InterpolationModes.Function? mode = null) {
        FinalTime = time;
        CurrentTime = 0;
        if (mode != null) Mode = mode;
    }

    public void Update(ref T current) {
        if (!Active) {
            current = Final;
            return;
        }

        Interpolate(ref current);
        CurrentTime++;
    }

    protected abstract void Interpolate(ref T current);
}