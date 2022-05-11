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
        Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        Game.GraphicsDevice.DepthStencilState = DepthStencilState.None; // TODO: tie this to zWriteDisable
        Game.CurrentEffect.VertexColorEnabled = true;
        Game.CurrentEffect.FogEnabled = false;
        Game.CurrentEffect.TextureEnabled = true;
        Game.CurrentEffect.Projection = Matrix.CreatePerspectiveFieldOfView(Stage.Fov, ThGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000f);
        Game.CurrentEffect.View = Matrix.CreateLookAt(Stage.Position - (Vector3.UnitZ * 1000), Stage.Facing, Stage.Up);
        // Game.CurrentEffect.World = Matrix.Identity * Matrix.CreateTranslation(-Stage.Position);
        Stage.Draw();
    }

    public override void Update() {
        Stage.Update();
        ThGame.Instance.Window.Title = $"Pointer: {Stage.Ip}, Time: {Stage.Time}";
    }
}