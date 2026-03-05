namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface IMotion
    {
        void SetMotorPower(float left, float right);
        public void SetSteerAngle(float steer);
        void Brake(float power = 1);
        float CurrentSpeed { get; }
        float CurrentSteerAngle { get; }
    }
}
