using Assets.Scripts.Robot.Cars;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Game.Map
{
    public class OdometryVisualizer : MonoBehaviour
    {
        [SerializeField] FourWheelsCarController robotAPI;
        [SerializeField] TMP_Text text;



        private void Update()
        {
            text.text = $@"
Энкодеры: {string.Join(", ", robotAPI.WheelRPM)}
GPS: {robotAPI.Position.x};{robotAPI.Position.z}
Compas: {robotAPI.YawDeg}
";
        }
    }
}
