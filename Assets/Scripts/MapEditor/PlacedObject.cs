using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class PlacedObject
    {
        public GameObject instance;
        public ElementData data;

        public PlacedObject(GameObject go, ElementData d)
        {
            instance = go;
            data = d;
        }
    }
}
