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
    public BasicEffect DefaultEffect;
    public BasicEffect CurrentEffect;
    public List<State> States = new List<State>();
    private VertexPositionColorTexture[] Vertices;
    private int[] Indices;
    private Texture2D Texture;

    public ThGame() {
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }
    
    protected override void Initialize() {
        Graphics.PreferredBackBufferWidth = 384;
        Graphics.PreferredBackBufferHeight = 448;
        Graphics.SynchronizeWithVerticalRetrace = true;
        Graphics.ApplyChanges();

        DefaultEffect = CurrentEffect = new BasicEffect(GraphicsDevice);
        // CurrentEffect.Projection *= Matrix.CreateScale(2/384f, 1/448f, 1f);
        CurrentEffect.View = Matrix.CreateLookAt(new Vector3(384f / 2f, 448f / 2f, 0f), new Vector3(384f / 2f, 448f / 2f, 10f), Vector3.Up);
        Vertices = new VertexPositionColorTexture[4];
        Vertices[0] = new VertexPositionColorTexture(new Vector3(-1f, -1f, 0), Color.White, Vector2.One);
        Vertices[1] = new VertexPositionColorTexture(new Vector3(1f, -1f, 0), Color.White, Vector2.UnitY);
        Vertices[2] = new VertexPositionColorTexture(new Vector3(-1f, 1f, 0), Color.White, Vector2.UnitX);
        Vertices[3] = new VertexPositionColorTexture(new Vector3(1f, 1f, 0), Color.WhiteSmoke, Vector2.Zero);
        Indices = new[] {
            0, 2, 1,
            1, 2, 3
        };
        base.Initialize();
    }

    protected override void LoadContent() {
        Batch = new SpriteBatch(GraphicsDevice);
        Texture = Content.Load<Texture2D>("trolley");
        Console.WriteLine($"{Texture.Bounds}");

        States.Add(new Gameplay());
    }

    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        foreach (State state in States.Where(state => state.Active)) {
            state.Update();
        }

        foreach (State state in States.Where(state => state.Visible)) {
            state.Draw();
        }

        // Batch.Begin();
        // Batch.Draw(Texture, Vector2.Zero, Color.Fuchsia);
        // Batch.End();


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