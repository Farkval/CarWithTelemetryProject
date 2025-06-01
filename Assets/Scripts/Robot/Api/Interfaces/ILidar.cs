using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ILidar
    {
        List<ILidarPoint> PointCloud { get; } 
    }
}
