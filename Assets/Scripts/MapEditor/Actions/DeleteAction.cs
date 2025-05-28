using Assets.Scripts.MapEditor.Models;

namespace Assets.Scripts.MapEditor.Actions
{
    public class DeleteAction : IUndoableAction
    {

        private readonly PlacedObject _placedObject;

        public DeleteAction(PlacedObject placedObject)
        {
            _placedObject = placedObject;
        }

        public object Undo()
        {
            if (_placedObject.instance)
                _placedObject.instance.SetActive(true);
            return _placedObject;
        }

        public object Redo()
        {
            if (_placedObject.instance)
                _placedObject.instance.SetActive(false);
            return _placedObject;
        }
    }
}
