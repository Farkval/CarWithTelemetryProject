using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;

namespace Assets.Scripts.Sensors.Intefaces
{
    /// <summary>
    /// Интерфейс базового лидара:
    /// - Инициализация (если нужно)
    /// - Выполнение сканирования/обновления
    /// - Получение ближайшей дистанции
    /// - Получение облака точек
    /// </summary>
    public interface ILidarSensor
    {
        void Initialize();
        void PerformScan();
        float GetNearestDistance();
        List<LidarPoint> GetPointCloud();
    }
}
