using Assets.Scripts.Robot.Api.Attributes;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ILidar
    {
        [RobotApi]
        List<ILidarPoint> PointCloud { get; } 
    }
}
