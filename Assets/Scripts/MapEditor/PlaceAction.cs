using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class PlaceAction : IUndoableAction
    {
        private readonly GameObject _obj;
        private readonly Transform _parent;

        public PlaceAction(GameObject obj, Transform parent)
        {
            _obj = obj;
            _parent = parent;
        }

        public void Undo()
        {
            if (_obj) 
                _obj.SetActive(false);
        }

        public void Redo()
        {
            if (_obj) 
                _obj.SetActive(true);
        }
    }
}
