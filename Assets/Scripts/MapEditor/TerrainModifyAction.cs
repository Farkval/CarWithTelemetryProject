using System.Collections.Generic;

namespace Assets.Scripts.MapEditor
{
    public class TerrainModifyAction : IUndoableAction
    {
        readonly MapTerrain terrain;
        readonly Dictionary<(int, int), float> before, after;

        public TerrainModifyAction(MapTerrain t,
            Dictionary<(int, int), float> b, Dictionary<(int, int), float> a)
        { terrain = t; before = b; after = a; }

        public void Undo() { terrain.RestoreHeights(before); }
        public void Redo() { terrain.RestoreHeights(after); }
    }
}
