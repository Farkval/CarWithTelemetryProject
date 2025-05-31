using UnityEngine;

namespace Assets.Scripts.Robot.Cameras
{
    [AddComponentMenu("Camera-Control/Third Person Camera")]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target to follow")]
        public Transform target;          // Ссылка на игрока

        [Header("Distance and height")]
        public float distance = 5.0f;     // Расстояние от камеры до игрока
        public float height = 1.5f;       // Вертикальный сдвиг камеры относительно точки слежения

        [Header("Mouse sensitivity")]
        public float xSpeed = 70.0f;     // Чувствительность вращения по горизонтали
        public float ySpeed = 70.0f;     // Чувствительность вращения по вертикали

        [Header("Vertical angle limits")]
        public float yMinLimit = -20f;    // Минимальный угол (в градусах)
        public float yMaxLimit = 80f;     // Максимальный угол (в градусах)

        private float x = 0.0f;           // Текущий угол по горизонтали
        private float y = 0.0f;           // Текущий угол по вертикали

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

            // Вычисляем новую ориентацию и позицию камеры
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
