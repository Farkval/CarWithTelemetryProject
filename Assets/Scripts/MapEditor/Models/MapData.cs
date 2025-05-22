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
        public int heightRes;                  // реальное (_heRes) = size * heightSubDiv
        public int surfaceRes;                 // реальное (_suRes) = size * surfaceSubDiv
        public float[] heights;                // длина = (heightRes+1)^2
        public byte[] surfaces;               // длина =  surfaceRes  * surfaceRes
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
