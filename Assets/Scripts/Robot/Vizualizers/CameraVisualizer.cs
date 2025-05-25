using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Vizualizers
{
    [RequireComponent(typeof(Camera))]
    public class CameraVisualizer : MonoBehaviour
    {
        [Header("Настройки визуализации")]
        [SerializeField] GameObject rayPrefab;     // LidarRay или аналогичный префаб с LineRenderer
        [SerializeField] float rayDuration = 0.1f; // Необязательно: можно оставить 0, если хотим непрерывную отрисовку

        Camera _cam;
        List<LineRenderer> _pool = new List<LineRenderer>(4);
        float _lastDrawTime;

        void Awake()
        {
            _cam = GetComponent<Camera>();

            // Создаем пул из 4 LineRenderer'ов
            for (int i = 0; i < 4; i++)
            {
                var go = Instantiate(rayPrefab, transform);
                var lr = go.GetComponent<LineRenderer>();
                go.SetActive(false);
                _pool.Add(lr);
            }
        }

        void LateUpdate()
        {
            if (!enabled) 
                return;

            DrawFrustumRays();
            _lastDrawTime = Time.time;
        }

        void Update()
        {
            // опционально: очищаем через rayDuration
            if (rayDuration > 0 && Time.time - _lastDrawTime > rayDuration)
                ClearRays();
        }

        void OnDisable()
        {
            ClearRays();
        }

        void DrawFrustumRays()
        {
            // вычисляем 4 угла фрустума на дальней плоскости
            Vector3[] farCorners = new Vector3[4];
            _cam.CalculateFrustumCorners(
                new Rect(0, 0, 1, 1),
                _cam.farClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                farCorners);

            // переводим в мировой
            for (int i = 0; i < 4; i++)
                farCorners[i] = transform.TransformPoint(farCorners[i]);

            Vector3 origin = _cam.transform.position;

            // для каждого из 4-х ребер: рисуем луч из origin в corner
            for (int i = 0; i < 4; i++)
            {
                var lr = _pool[i];
                lr.positionCount = 2;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, farCorners[i]);
                lr.gameObject.SetActive(true);
            }
        }

        void ClearRays()
        {
            foreach (var lr in _pool)
                lr.gameObject.SetActive(false);
        }
    }
}
