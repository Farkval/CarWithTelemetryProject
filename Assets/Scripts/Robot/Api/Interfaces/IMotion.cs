namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IMotion
    {
        void SetMotorPower(float left, float right);
        void Brake(float power = 1);
    }
}
