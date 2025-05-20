using Assets.Scripts.Robot.Python;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Управление роботом")]
    public interface IMotion
    {
        [PythonStubExport("Движение")]
        void SetMotorPower(float left, float right);
        [PythonStubExport("Тормоз")]
        void Brake(float power = 1);
    }
}
