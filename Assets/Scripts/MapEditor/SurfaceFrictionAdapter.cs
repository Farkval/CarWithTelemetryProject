using Assets.Scripts.Robot;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    [RequireComponent(typeof(FourWheelsCarController))]
    public class SurfaceFrictionAdapter : MonoBehaviour
    {
        public float grassFric = 1f;
        public float mudFric = 0.6f;
        public float gravelFric = 0.8f;
        public float waterFric = 0.5f;
        public float iceFric = 0.2f;

        private MapTerrain _terrain;
        private FourWheelsCarController _car;

        void Awake()
        {
            _terrain = FindFirstObjectByType<MapTerrain>();
            _car = GetComponent<FourWheelsCarController>();
        }

        void FixedUpdate()
        {
            // проверяем каждое колесо
            Adjust(_car.frontLeftWheel);
            Adjust(_car.frontRightWheel);
            Adjust(_car.rearLeftWheel);
            Adjust(_car.rearRightWheel);
        }

        void Adjust(WheelCollider wc)
        {
            Vector3 pos; Quaternion rot;
            wc.GetWorldPose(out pos, out rot);

            float mul = wc.forwardFriction.extremumValue; // default
            switch (_terrain.SurfaceAt(pos))
            {
                case SurfaceType.Grass: 
                    mul = grassFric; 
                    break;

                case SurfaceType.Mud: 
                    mul = mudFric; 
                    break;

                case SurfaceType.Gravel: 
                    mul = gravelFric; 
                    break;

                case SurfaceType.Water: 
                    mul = waterFric; 
                    break;

                case SurfaceType.Ice: 
                    mul = iceFric; 
                    break;
            }
            //_car.SetSurfaceFriction(mul, mul, mul);
        }
    }
}
