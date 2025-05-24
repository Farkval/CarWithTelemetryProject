using System;
using UnityEngine;

namespace Assets.Scripts.Garage.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string Name { get; }
        public DisplayNameAttribute(string name) => Name = name;
    }
}
