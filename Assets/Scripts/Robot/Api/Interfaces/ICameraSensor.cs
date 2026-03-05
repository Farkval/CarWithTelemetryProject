using System.Collections.Generic;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    public interface ICameraSensor
    {
        public IReadOnlyList<IDetectedObjectInfo> DetectedObjects { get; }
    }
}
