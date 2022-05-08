using Microsoft.Xna.Framework;

using TkVector3 = OpenTK.Mathematics.Vector3;
using TkVector2 = OpenTK.Mathematics.Vector2;

namespace Game.Util;

public static class TypeConversionExtensions {
    public static Vector3 ToXna(this TkVector3 tk) => new Vector3(tk.X, tk.Y, tk.Z);
    public static Vector2 ToXna(this TkVector2 tk) => new Vector2(tk.X, tk.Y);
}