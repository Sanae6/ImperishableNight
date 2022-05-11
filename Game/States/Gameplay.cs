using ImperishableNight;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.States;

public class Gameplay : State {
    // public readonly BasicEffect World;
    public StageVm Stage;

    public Gameplay() {
        // World = new BasicEffect(Game.GraphicsDevice);
        // Game.CurrentEffect = World;
        Stage = new StageVm(this, StageFile.ReadStd(Game.Archive["stage1.std"]),
            AnimationFile.ReadAnm(Game.Archive["stg1bg.anm"]));
    }

    public override void Draw() {
        ThGame.Instance.GraphicsDevice.Clear(Stage.ClearColor);
        Game.CurrentEffect.VertexColorEnabled = true;
        Game.CurrentEffect.TextureEnabled = true;
        Game.CurrentEffect.Projection = Matrix.CreatePerspectiveFieldOfView(Stage.Fov, ThGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);
        Game.CurrentEffect.View = Matrix.CreateLookAt(Stage.Position, Stage.Facing, Stage.Up);
        // Game.CurrentEffect.World = Matrix.Identity * Matrix.CreateTranslation(-Stage.Position);
        Stage.Draw();
    }

    public override void Update() {
        Stage.Update();
        ThGame.Instance.Window.Title = $"Pointer: {Stage.Ip}, Time: {Stage.Time}";
    }
}