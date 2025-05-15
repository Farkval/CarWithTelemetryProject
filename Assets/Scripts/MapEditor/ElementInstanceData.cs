using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    [Serializable]
    public class ElementInstanceData
    {
        public string elementPath;
        public Vector3 position;
        public Vector3 rotation;
        public float scale;
        public List<CustomProperty> customProperties = new();
        public Vector3 localScale;

        public ElementInstanceData(PlacedObject po)
        {
            elementPath = $"Elements/{po.data.name}";
            position = po.instance.transform.position;
            rotation = po.instance.transform.rotation.eulerAngles;
            localScale = po.instance.transform.localScale;   // не float
        }
    }
}
