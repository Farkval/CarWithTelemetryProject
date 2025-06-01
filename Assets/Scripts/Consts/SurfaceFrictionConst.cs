using Assets.Scripts.MapEditor.Models.Enums;
using System.Collections.Generic;

namespace Assets.Scripts.Consts
{
    public class SurfaceFrictionConst
    {
        public static readonly Dictionary<SurfaceType, (float forward, float sideways)> SurfaceFriction =
            new()
            {
                { SurfaceType.Grass,   (1.00f, 1.00f) },
                { SurfaceType.Mud,     (0.55f, 0.60f) },
                { SurfaceType.Gravel,  (0.80f, 0.80f) },
                { SurfaceType.Ice,     (0.00f, 0.00f) },
                { SurfaceType.Water,   (0.40f, 0.50f) },
            };
    }
}
