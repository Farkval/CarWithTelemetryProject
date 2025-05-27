using Assets.Scripts.Robot.Api.Attributes;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ICompass
    {
        [RobotApi]
        float YawDeg { get; }
    }
}
