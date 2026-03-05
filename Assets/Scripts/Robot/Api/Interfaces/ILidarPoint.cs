using UnityEngine;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ILidarPoint
    {
        Vector3 WorldPosition { get; set; }
        float Distance { get; set; }
    }
}
