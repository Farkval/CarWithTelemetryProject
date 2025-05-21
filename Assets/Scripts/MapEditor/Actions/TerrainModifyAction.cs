using Assets.Scripts.MapEditor.Controllers;

namespace Assets.Scripts.MapEditor.Actions
{
    public class TerrainModifyAction : IUndoableAction
    {
        private readonly MapTerrain _terrain;
        private readonly float[,] _before;
        private readonly float[,] _after;

        public TerrainModifyAction(MapTerrain terrain, float[,] before, float[,] after)
        {
            _terrain = terrain;
            _before = before;
            _after = after;
        }

        public object Undo()
        {
            _terrain.SetHeights(_before);
            return null;
        }

        public object Redo()
        {
            _terrain.SetHeights(_after);
            return null;
        }
    }
}
