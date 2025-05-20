using Assets.Scripts.Robot.Python;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Датчики энкодеров")]
    public interface IEncoders 
    {
        [PythonStubExport("Список значений энкодеров колес")]
        float[] WheelRPM { get; } 
    }
}
