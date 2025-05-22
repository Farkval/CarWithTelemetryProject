using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models.Enums;

namespace Assets.Scripts.MapEditor.Actions
{
    public class SurfaceModifyAction : IUndoableAction
    {
        private readonly MapTerrain _terrain;
        private readonly SurfaceType[,] _before;
        private readonly SurfaceType[,] _after;

        public SurfaceModifyAction(MapTerrain t, SurfaceType[,] b, SurfaceType[,] a)
        {
            _terrain = t;
            _before = b;
            _after = a;
        }

        public object Undo()
        {
            _terrain.SetSurfaces(_before);
            return null;
        }

        public object Redo()
        {
            _terrain.SetSurfaces(_after);
            return null;
        }
    }
}
