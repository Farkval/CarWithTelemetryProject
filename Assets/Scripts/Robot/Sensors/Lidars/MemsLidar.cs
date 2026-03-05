using Assets.Scripts.Garage.Attributes;
using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Sensors.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    public class MemsLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        public float maxDistance = 80f;
        public float horizontalFOV = 60f;
        public float verticalFOV = 20f;

        [Tooltip("Количество строк (вертикальное разрешение).")]
        public int verticalLines = 8;
        [Tooltip("Количество точек на каждую строку (горизонтальное разрешение).")]
        public int horizontalPointsPerLine = 32;

        [Tooltip("Частота сканирования (сколько раз в секунду мы обходим все строки).")]
        public float scanFrequency = 10f;

        [Tooltip("Активность элемента")]
        [DisplayName("Активность")]
        public bool isEnabled = true;

        [Tooltip("Слой для рейкаста.")]
        public LayerMask layerMask = ~0;

        public event Action<List<ILidarPoint>> OnScanComplete;

        private List<ILidarPoint> _pointCloud = new List<ILidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        private float _scanTimer = 0f;

        private int _currentLineIndex = 0;

        public List<ILidarPoint> PointCloud => _pointCloud;
        public float Nearest => _pointCloud.Min(p => p.Distance);

        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _scanTimer = 0f;
            _currentLineIndex = 0;
        }

        private void Awake()
        {
            enabled = isEnabled;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!isEnabled)
                return;

            _scanTimer += Time.deltaTime;
            float timePerFrame = 1f / (scanFrequency * verticalLines);
            if (_scanTimer >= timePerFrame)
            {
                _scanTimer = 0f;
                ScanSingleLine(_currentLineIndex);
                _currentLineIndex++;

                if (_currentLineIndex >= verticalLines)
                {
                    _currentLineIndex = 0;
                }
            }
        }

        private void ScanSingleLine(int lineIndex)
        {
            float vPercent = (float)lineIndex / (verticalLines - 1);
            float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);

            if (lineIndex == 0)
            {
                _pointCloud.Clear();
                _nearestDistance = Mathf.Infinity;
            }

            for (int h = 0; h < horizontalPointsPerLine; h++)
            {
                float hPercent = (float)h / (horizontalPointsPerLine - 1);
                float hAngle = Mathf.Lerp(-horizontalFOV / 2f, horizontalFOV / 2f, hPercent);

                Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0f);
                Vector3 direction = transform.rotation * rotation * Vector3.forward;

                Vector3 origin = transform.position;
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

            OnScanComplete?.Invoke(_pointCloud);
        }

        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            for (int i = 0; i < verticalLines; i++)
            {
                ScanSingleLine(i);
            }
        }

        public void ApplySettings()
        {
            enabled = isEnabled;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
}
