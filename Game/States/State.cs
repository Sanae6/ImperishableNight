namespace Game.States;

public abstract class State {
    protected ThGame Game { get; } = ThGame.Instance;
    public bool Active { get; set; }
    public bool Visible { get; set; }
    
    public abstract void Update();
    public abstract void Draw();
}