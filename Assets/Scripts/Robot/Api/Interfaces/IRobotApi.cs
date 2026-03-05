using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IRobotAPI : IMotion, IEncoders, IGps, ICompass
    {
        List<ILidar> Lidars { get; }
        List<ICameraSensor> Cameras { get; }
        bool ManualControl { get; set; }
    }
}
