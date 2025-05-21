namespace Assets.Scripts.MapEditor
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
            _terrain.SetSurface(_before);
            return null;
        }

        public object Redo()
        {
            _terrain.SetSurface(_after);
            return null;
        }
    }
}
