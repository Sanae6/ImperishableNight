using Game;
using Game.States;
using ImperishableNight;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

ThGame.Instance.LoadGame(args[0]);
ThGame.Instance.Run();

public class ThGame : Microsoft.Xna.Framework.Game {
    public static readonly ThGame Instance = new ThGame();
    public GraphicsDeviceManager Graphics;
    public SpriteBatch Batch;
    public Archive Archive;
    public Stack<State> StateStack = new Stack<State>();

    public ThGame() {
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        
        StateStack.Push(new Gameplay());
    }
    
    protected override void Initialize() {
        Graphics.PreferredBackBufferWidth = 384;
        Graphics.PreferredBackBufferHeight = 448;
        Graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent() {
        Batch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        foreach (State state in StateStack.Where(state => state.Visible)) {
            state.Update();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        foreach (State state in StateStack.Where(state => state.Visible)) {
            state.Draw();
        }

        base.Draw(gameTime);
    }

    public void LoadGame(string gameLocation) {
        if (!File.Exists(gameLocation))
            throw new FileNotFoundException("Provide a path to Imperishable Night's data archive. (th08.dat)");
        Archive = new ExtractedArchive(Archive.ReadData(File.ReadAllBytes(gameLocation)));
    }

    public void LoadAudio(string audioLocation) {
        
    }
}