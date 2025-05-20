using System;

namespace Assets.Scripts.Robot.Python
{
    [AttributeUsage(AttributeTargets.Interface |
                AttributeTargets.Struct |
                AttributeTargets.Property |
                AttributeTargets.Method,
                Inherited = false, AllowMultiple = false)]
    public sealed class PythonStubExportAttribute : Attribute
    {
        /// <summary>
        /// Если false, этот тип/член не попадёт в stub.
        /// По умолчанию — true.
        /// </summary>
        public bool Include { get; }

        /// <summary>
        /// Текст комментария, который будет вставлен в stub.
        /// </summary>
        public string Doc { get; }

        public PythonStubExportAttribute(string doc = null, bool include = true)
        {
            Doc = doc;
            Include = include;
        }
    }
}
