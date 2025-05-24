using Assets.Scripts.Robot.Api.Interfaces;

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
    }
}
