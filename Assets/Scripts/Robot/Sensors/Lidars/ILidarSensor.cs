using Assets.Scripts.Robot.Api.Interfaces;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    public interface ILidarSensor : ILidar
    {
        void Initialize();
        void PerformScan();

        event Action<List<ILidarPoint>> OnScanComplete;
    }
}
