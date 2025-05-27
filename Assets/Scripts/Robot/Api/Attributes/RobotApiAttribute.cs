using System;

namespace Assets.Scripts.Robot.Api.Attributes
{
    /// <summary>
    /// Помечает метод/свойство, которое должно попасть в robot.py-stub.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class RobotApiAttribute : Attribute { }
}
