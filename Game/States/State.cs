namespace Game.States;

public abstract class State {
    protected static ThGame Game => ThGame.Instance;
    public bool Active { get; set; } = true;
    public bool Visible { get; set; } = true;
    
    public abstract void Update();
    public abstract void Draw();
}