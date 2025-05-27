using Assets.Scripts.Robot.Api.Attributes;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IMotion
    {
        [RobotApi]
        void SetMotorPower(float left, float right);
        [RobotApi]
        void Brake(float power = 1);
    }
}
