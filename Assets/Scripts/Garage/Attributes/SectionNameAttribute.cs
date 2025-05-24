using System;

namespace Assets.Scripts.Garage.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SectionNameAttribute : Attribute
    {
        public string Name { get; }
        public SectionNameAttribute(string name) => Name = name;
    }
}
