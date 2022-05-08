using ImperishableNight;
using Microsoft.Xna.Framework.Graphics;

namespace Game.States;

public class Gameplay : State {
    private BasicEffect World;
    public StageVm Stage;

    public Gameplay() {
        Stage = new StageVm(StageFile.ReadStd(Game.Archive["stg7bg.anm"]), AnimationFile.ReadAnm(Game.Archive["stg7bg.anm"]));
        World = new BasicEffect(Game.GraphicsDevice);
    }
    public override void Update() {
    }

    public override void Draw() {
        foreach (EffectPass pass in World.CurrentTechnique.Passes) {
            
            pass.Apply();
        }
    }
}