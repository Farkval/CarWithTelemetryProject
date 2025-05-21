namespace Assets.Scripts.MapEditor
{
    public interface IUndoableAction
    {
        object Undo();
        object Redo();
    }
}
