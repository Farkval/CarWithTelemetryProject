using Assets.Scripts.Robot.Api.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Robot.Models
{
    public struct DetectedObjectInfo : IDetectedObjectInfo
    {
        public string Name { get; set; }
        public Vector3 position { get; set; }
        public float distance { get; set; }
        public float viziblePercent { get; set; }
    }
}
