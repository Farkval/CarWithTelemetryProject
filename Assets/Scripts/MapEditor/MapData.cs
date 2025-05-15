using System;
using System.Collections.Generic;

namespace Assets.Scripts.MapEditor
{
    [Serializable]
    public class MapData
    {
        public int mapMeters;
        public float[] heights; 
        public List<ElementInstanceData> instances = new();

        public MapData(List<PlacedObject> objects, int meters)
        {
            mapMeters = meters;
            foreach (var po in objects) 
                instances.Add(new ElementInstanceData(po));
        }
    }
}
