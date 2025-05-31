using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.MapEditor.Controllers
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Speeds")]
        [SerializeField] float rotateSpeed = 6f;
        [SerializeField] float panSpeed = .3f;
        [SerializeField] float zoomSpeed = 60f;
        [Header("Limits")]
        [SerializeField] float minDist = 5f, maxDist = 400f;
        [Header("Invert")]
        [SerializeField] bool invertX, invertY;

        Vector3 pivot = Vector3.zero;
        float distance = 60f, yaw = 45, pitch = 30;

        public void Frame(float mapSize)
        {
            pivot = Vector3.zero;
            distance = mapSize * 1.2f;
            yaw = 45; pitch = 30;
            Apply();
        }

        /// <summary>
        /// Устанавливает камеру в заданную позицию и ориентацию, пересчитывая параметры поворота и дистанции.
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;

            // Пересчитываем угол поворота в pitch/yaw
            Vector3 euler = rotation.eulerAngles;
            pitch = euler.x;
            yaw = euler.y;

            // Вычисляем дистанцию
            Vector3 offset = rotation * new Vector3(0, 0, -1);
            Ray ray = new Ray(position, offset);
            // pivot будет на расстоянии "distance" от позиции камеры вдоль forward
            pivot = position - offset * distance;

            // Пересчитываем distance по реальному положению:
            distance = Vector3.Distance(position, pivot);
            distance = Mathf.Clamp(distance, minDist, maxDist);
        }

        public void SetInvertX(bool v) => invertX = v;

        public void SetInvertY(bool v) => invertY = v;

        private void Update()
        {
            var m = Mouse.current;
            if (m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue() * rotateSpeed * Time.deltaTime;
                yaw += invertX ? -d.x : d.x;
                pitch += invertY ? d.y : -d.y;
                pitch = Mathf.Clamp(pitch, 5, 85);
            }
            if (m.middleButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue() * panSpeed * Time.deltaTime;
                pivot -= transform.right * d.x + transform.up * d.y;
            }
            float sc = m.scroll.ReadValue().y;
            if (Mathf.Abs(sc) > 0.01f)
            {
                distance *= 1f - sc * zoomSpeed * 0.001f;
                distance = Mathf.Clamp(distance, minDist, maxDist);
            }

            Apply();
        }

        private void Apply()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0);
            transform.position = pivot + rot * new Vector3(0, 0, -distance);
            transform.rotation = rot;
        }
    }
}
