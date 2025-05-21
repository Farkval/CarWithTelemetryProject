namespace Assets.Scripts.MapEditor
{
    public class PlaceAction : IUndoableAction
    {
        private readonly PlacedObject _placedObject;

        public PlaceAction(PlacedObject placedObject)
        {
            _placedObject = placedObject;
        }

        public object Undo()
        {
            if (_placedObject.instance) 
                _placedObject.instance.SetActive(false);
            return _placedObject;
        }

        public object Redo()
        {
            if (_placedObject.instance) 
                _placedObject.instance.SetActive(true);
            return _placedObject;
        }
    }
}
