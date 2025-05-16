using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors
{
    /// <summary>
    /// 2) Flash Lidar — одноимпульсный \"моментальный снимок\".
    ///    При каждом скане мы посылаем N x M лучей в заданном поле зрения.
    /// </summary>
    public class FlashLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        public float maxDistance = 50f;
        public float horizontalFOV = 60f;
        public float verticalFOV = 30f;

        [Tooltip("Горизонтальное разрешение (кол-во лучей по горизонтали).")]
        public int horizontalResolution = 32;
        [Tooltip("Вертикальное разрешение (кол-во лучей по вертикали).")]
        public int verticalResolution = 16;

        [Tooltip("Слой, по которому стреляют лучи.")]
        public LayerMask layerMask = ~0;

        [Tooltip("Частота кадров/сканов в секунду.")]
        public float scanFrequency = 5f;

        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        private float _scanTimer = 0f;

        public List<LidarPoint> PointCloud => _pointCloud;

        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _scanTimer = 0f;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 1f / scanFrequency)
            {
                _scanTimer = 0f;
                PerformScan();
            }
        }

        /// <summary>
        /// Одномоментный \"снимок\": заполняем всю сетку лучей по вертикали и горизонтали.
        /// </summary>
        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            Vector3 origin = transform.position;

            for (int h = 0; h < horizontalResolution; h++)
            {
                // процент по горизонтали от -hFOV/2 до +hFOV/2
                float hPercent = (float)h / (horizontalResolution - 1);
                float hAngle = Mathf.Lerp(-horizontalFOV / 2f, horizontalFOV / 2f, hPercent);

                for (int v = 0; v < verticalResolution; v++)
                {
                    float vPercent = (float)v / (verticalResolution - 1);
                    float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);

                    // Формируем итоговый поворот
                    Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0f);
                    Vector3 direction = transform.rotation * rotation * Vector3.forward;

                    if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                    {
                        float dist = hit.distance;
                        LidarPoint pt = new LidarPoint(hit.point, dist);
                        _pointCloud.Add(pt);

                        if (dist < _nearestDistance)
                        {
                            _nearestDistance = dist;
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
}
