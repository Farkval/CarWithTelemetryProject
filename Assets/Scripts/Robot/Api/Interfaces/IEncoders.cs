using Assets.Scripts.Robot.Api.Attributes;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IEncoders
    {
        [RobotApi]
        float[] WheelRPM { get; } 
    }
}
