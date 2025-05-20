using Assets.Scripts.Robot.Python;
using UnityEngine;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    [PythonStubExport("Датчик GPS")]
    public interface IGps 
    { 
        Vector3 Position { get; } 
    }
}
