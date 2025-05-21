namespace Assets.Scripts.MapEditor.Actions
{
    public interface IUndoableAction
    {
        object Undo();
        object Redo();
    }
}
