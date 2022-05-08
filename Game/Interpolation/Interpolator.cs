namespace Game.Interpolation;

public abstract class Interpolator<T> {
    protected InterpolationModes.Function Mode { get; private set; }
    public bool Active;
    public int EndTime { get; private set; }
    protected float CurrentTime { get; set; }
    protected T Start { get; private set; } = default!;
    protected T End { get; private set; } = default!;

    protected Interpolator() {
        EndTime = 0;
        Mode = InterpolationModes.Linear;
    }

    public void Set(T start, T end, int time, InterpolationModes.Function mode) {
        Active = true;
        Start = start;
        End = end;
        EndTime = time;
        CurrentTime = 0;
        Mode = mode;
    }

    public void Update(ref T current) {
        if (!Active)
            return;
        Interpolate(ref current);
        CurrentTime += 1.0f / CurrentTime;
        
        if (CurrentTime >= EndTime) {
            Active = false;
        }
    }

    protected abstract void Interpolate(ref T current);
}