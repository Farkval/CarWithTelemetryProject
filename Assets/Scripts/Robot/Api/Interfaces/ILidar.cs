using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ILidar 
    { 
        List<LidarPoint> PointCloud { get; } 
    }
}
