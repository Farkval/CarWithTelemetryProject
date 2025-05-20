using System;
using System.Collections.Generic;

namespace Assets.Scripts.MapEditor
{
    [Serializable]
    public class MapData
    {
        public MapSize size;
        public float[] heights; 
        public List<ElementInstanceData> instances = new();

        public MapData(List<PlacedObject> objects, MapSize mapSize)
        {
            size = mapSize;
            foreach (var po in objects) 
                instances.Add(new ElementInstanceData(po));
        }
    }
}
