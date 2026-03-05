using UnityEngine;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IDetectedObjectInfo
    {
        public string Name { get; set; }
        public Vector3 position { get; set; }
        public float distance { get; set; }
        public float viziblePercent { get; set; }
    }
}
