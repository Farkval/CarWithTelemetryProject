using Assets.Scripts.Robot.Python;
using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Датчик лидар")]
    public interface ILidar
    {
        [PythonStubExport("Облако точек")]
        List<LidarPoint> PointCloud { get; } 
    }
}
