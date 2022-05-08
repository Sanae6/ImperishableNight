using System.Runtime.CompilerServices;
using Game.Util;
using ImperishableNight;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class StageVm {
    private List<Instance> Instances = new List<Instance>();
    public AnmVm Anm;
    public BasicEffect BgEffect = new BasicEffect(ThGame.Instance.GraphicsDevice);

    public StageVm(StageFile stage, List<AnimationFile> animations) {
        Anm = new AnmVm(animations);
        foreach (StageFile.Instance stageInstance in stage.Instances) {
            Instance instance = new Instance();
            Instances.Add(instance);
            foreach (StageFile.Quad quad in stage.Objects[stageInstance.ObjectId].Quads) {
                AnmVm.Sprite sprite = new AnmVm.Sprite(Anm, animations[stageInstance.ObjectId], null, quad.ScriptIndex, quad.Size.ToXna());
                Anm.AddSprite(sprite);
                instance.Sprites.Add(sprite);
            }
        }
        
        BgEffect.FogEnabled = true;
        BgEffect.FogColor = Vector3.Zero;
        BgEffect.FogStart = 0;
        BgEffect.FogEnd = 1000; 
    }

    public void Draw() {
        
    }

    private class Instance {
        public List<AnmVm.Sprite> Sprites = new List<AnmVm.Sprite>();
    }
}