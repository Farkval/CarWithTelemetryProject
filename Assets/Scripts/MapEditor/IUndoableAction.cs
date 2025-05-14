namespace Assets.Scripts.MapEditor
{
    public interface IUndoableAction
    {
        void Undo();
        void Redo();
    }
}
