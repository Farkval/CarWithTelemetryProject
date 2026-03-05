using UnityEngine;

namespace Assets.Scripts.Robot.Cameras
{
    [AddComponentMenu("Camera-Control/Third Person Camera")]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target to follow")]
        public Transform target;         

        [Header("Distance and height")]
        public float distance = 5.0f;    
        public float height = 1.5f;      

        [Header("Mouse sensitivity")]
        public float xSpeed = 70.0f;     
        public float ySpeed = 70.0f;     

        [Header("Vertical angle limits")]
        public float yMinLimit = -20f;   
        public float yMaxLimit = 80f;    

        private float x = 0.0f;          
        private float y = 0.0f;          

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            if (GetComponent<Rigidbody>() != null)
                GetComponent<Rigidbody>().freezeRotation = true;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 targetOffset = new Vector3(0, height, 0);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance)
                               + target.position + targetOffset;

            transform.rotation = rotation;
            transform.position = position;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
