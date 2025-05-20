using Assets.Scripts.Robot.Python;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Управление роботом")]
    public interface IRobotAPI : IMotion, IEncoders, IGps, ICompass
    {
        /// <summary>
        /// Список лидаров
        /// </summary>
        [PythonStubExport("Датчики лидаров")]
        List<ILidar> Lidars { get; }
        /// <summary>
        /// Ручное управление
        /// </summary>
        [PythonStubExport("Ручное управление")]
        bool ManualControl { get; set; }
    }
}
