using Assets.Scripts.Robot.Api.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Sensors.Models
{
    /// <summary>
    /// Точка результата сканирования лидаром.
    /// Хранит мировую позицию точки, дистанцию и (опционально) нормаль/интенсивность.
    /// </summary>
    public struct LidarPoint : ILidarPoint
    {
        public Vector3 WorldPosition { get; set; }
        public float Distance { get; set; }

        public LidarPoint(Vector3 pos, float dist)
        {
            WorldPosition = pos;
            Distance = dist;
        }
    }
}
