using Assets.Scripts.Robot.Api.Attributes;
using UnityEngine;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ILidarPoint
    {
        [RobotApi]
        Vector3 WorldPosition { get; set; }
        [RobotApi]
        float Distance { get; set; }
    }
}
