using Assets.Scripts.Robot.Api.Attributes;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IRobotAPI : IMotion, IEncoders, IGps, ICompass
    {
        /// <summary>
        /// Список лидаров
        /// </summary>
        [RobotApi]
        List<ILidar> Lidars { get; }
        /// <summary>
        /// Список камер
        /// </summary>
        [RobotApi]
        List<ICameraSensor> Cameras { get; }
        /// <summary>
        /// Ручное управление
        /// </summary>
        bool ManualControl { get; set; }
    }
}
