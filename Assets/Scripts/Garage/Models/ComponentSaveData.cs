using System;
using System.Collections.Generic;

namespace Assets.Scripts.Garage.Models
{
    [Serializable]
    class ComponentSaveData
    {
        public string assemblyQualifiedName;
        public List<FieldSaveData> fields;
    }
}
