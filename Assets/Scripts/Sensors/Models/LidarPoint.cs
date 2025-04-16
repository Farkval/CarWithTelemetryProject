using UnityEngine;

namespace Assets.Scripts.Sensors.Models
{
    /// <summary>
    /// Точка результата сканирования лидаром.
    /// Хранит мировую позицию точки, дистанцию и (опционально) нормаль/интенсивность.
    /// </summary>
    public struct LidarPoint
    {
        public Vector3 WorldPosition;
        public float Distance;
        // Можно добавить другие параметры: интенсивность, нормаль и т. д.

        public LidarPoint(Vector3 pos, float dist)
        {
            WorldPosition = pos;
            Distance = dist;
        }
    }
}
