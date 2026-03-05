using Assets.Scripts.MapEditor.Models.Enums;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.MapEditor.Models
{
    [Serializable]
    public class MapData
    {
        public TimeOfDay timeOfDay;
        public MapSize mapSize;
        public int heightRes;
        public int surfaceRes;
        public float[] heights;
        public byte[] surfaces;
        public List<ElementInstanceData> instances = new();

        public MapData(List<PlacedObject> objects, MapSize ms, TimeOfDay tod)
        {
            mapSize = ms;
            timeOfDay = tod;
            foreach (var po in objects) 
                instances.Add(new ElementInstanceData(po));
        }
    }
}
