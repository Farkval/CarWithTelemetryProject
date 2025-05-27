using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;
using System;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    /// <summary>
    /// Интерфейс базового лидара:
    /// - Инициализация (если нужно)
    /// - Выполнение сканирования/обновления
    /// - Получение ближайшей дистанции
    /// - Получение облака точек
    /// </summary>
    public interface ILidarSensor : ILidar
    {
        void Initialize();
        void PerformScan();

        event Action<List<ILidarPoint>> OnScanComplete;
    }
}
