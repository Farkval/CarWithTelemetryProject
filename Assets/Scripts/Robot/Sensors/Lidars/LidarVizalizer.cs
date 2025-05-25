using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    [RequireComponent(typeof(FlashLidar))]
    public class LidarVisualizer : MonoBehaviour
    {
        [Header("Настройки визуализации")]
        [SerializeField] GameObject rayPrefab;     // префаб c LineRenderer
        [SerializeField] float rayDuration = 0.1f; // время показа лучей

        FlashLidar _sensor;
        List<LineRenderer> _pool;
        int _poolSize;

        void Awake()
        {
            _sensor = GetComponent<FlashLidar>();
            _sensor.OnScanComplete += OnScan;
        }

        void OnDestroy()
        {
            _sensor.OnScanComplete -= OnScan;
        }

        private void OnDisable()
        {
            ClearRays();
        }

        void Start()
        {
            // Определяем максимальное кол-во лучей в одном скане
            _poolSize = _sensor.horizontalResolution * _sensor.verticalResolution;
            _pool = new List<LineRenderer>(_poolSize);

            // Создаём пул
            for (int i = 0; i < _poolSize; i++)
            {
                var go = Instantiate(rayPrefab, transform);
                var lr = go.GetComponent<LineRenderer>();
                go.SetActive(false);
                _pool.Add(lr);
            }
        }

        private void OnScan(List<LidarPoint> cloud)
        {
            if (!enabled)
                return;

            var origin = transform.position;
            float maxDist = _sensor.maxDistance;
            int useCount = Mathf.Min(cloud.Count, _poolSize);

            // Рисуем первые useCount лучей
            for (int i = 0; i < useCount; i++)
            {
                var pt = cloud[i];
                var lr = _pool[i];

                lr.positionCount = 2;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, pt.WorldPosition);

                float t = Mathf.Clamp01(pt.Distance / maxDist);
                Color c = Color.Lerp(Color.red, Color.blue, t);
                lr.startColor = c;
                lr.endColor = c;

                lr.gameObject.SetActive(true);
            }

            // Скрываем все остальные лучи (если предыдущий скан был больше)
            for (int i = useCount; i < _poolSize; i++)
                _pool[i].gameObject.SetActive(false);

            // Запланировать полное очищение через rayDuration
            CancelInvoke(nameof(ClearRays));
            Invoke(nameof(ClearRays), rayDuration);
        }

        private void ClearRays()
        {
            // Скрываем сразу все, чтобы старые не висели
            for (int i = 0; i < _poolSize; i++)
                _pool[i].gameObject.SetActive(false);
        }
    }
}
