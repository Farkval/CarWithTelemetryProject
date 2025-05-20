using Assets.Scripts.Robot.Python;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Датчик компаса")]
    public interface ICompass
    {
        [PythonStubExport("Ориентация робота")]
        float YawDeg { get; }
    }
}
