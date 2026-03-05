using System;
using System.Collections.Generic;

namespace Assets.Scripts.Garage.Models
{
    [Serializable]
    class VehicleSaveData
    {
        public string prefabName;
        public List<ComponentSaveData> components;
    }
}
