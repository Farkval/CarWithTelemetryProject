using Assets.Scripts.Sensors.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    /// <summary>
    /// 3) MEMS-lidar Ч скан \"построчно\" (или \"секторами\"), без механического вращени€.
    ///    ƒопустим, мы сканируем построчно по горизонтали. «а 1 скан проходим все строки.
    ///    ѕримерно напоминает принцип работы некоторых твердотельных лидаров.
    /// </summary>
    public class MemsLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        public float maxDistance = 80f;
        public float horizontalFOV = 60f;
        public float verticalFOV = 20f;

        [Tooltip(" оличество строк (вертикальное разрешение).")]
        public int verticalLines = 8;
        [Tooltip(" оличество точек на каждую строку (горизонтальное разрешение).")]
        public int horizontalPointsPerLine = 32;

        [Tooltip("„астота сканировани€ (сколько раз в секунду мы обходим все строки).")]
        public float scanFrequency = 10f;

        [Tooltip("—лой дл€ рейкаста.")]
        public LayerMask layerMask = ~0;

        public event Action<List<LidarPoint>> OnScanComplete;

        // “екущее состо€ние
        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        private float _scanTimer = 0f;

        // »ндекс текущей строки (сканируем построчно в Update, пока не пройдЄм все строки)
        private int _currentLineIndex = 0;

        public List<LidarPoint> PointCloud => _pointCloud;

        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _scanTimer = 0f;
            _currentLineIndex = 0;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            _scanTimer += Time.deltaTime;
            float timePerFrame = 1f / (scanFrequency * verticalLines);
            //  аждые timePerFrame секунд сканируем очередную строку
            if (_scanTimer >= timePerFrame)
            {
                _scanTimer = 0f;
                ScanSingleLine(_currentLineIndex);
                _currentLineIndex++;

                // ≈сли дошли до конца Ч начинаем заново
                if (_currentLineIndex >= verticalLines)
                {
                    _currentLineIndex = 0;
                }
            }
        }

        /// <summary>
        /// —канирование одной строки (угол по вертикали зафиксирован дл€ данной строки).
        /// </summary>
        private void ScanSingleLine(int lineIndex)
        {
            // ”гол по вертикали дл€ этой строки
            float vPercent = (float)lineIndex / (verticalLines - 1);
            float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);

            // ѕри каждом полном проходе всех строк можно очистить / обновить облако.
            // Ќо если хотим, чтобы облако копилось непрерывно, то не очищаем.
            // ƒл€ примера Ч перезаполним весь список заново, если дошли до первой строки:
            if (lineIndex == 0)
            {
                _pointCloud.Clear();
                _nearestDistance = Mathf.Infinity;
            }

            for (int h = 0; h < horizontalPointsPerLine; h++)
            {
                float hPercent = (float)h / (horizontalPointsPerLine - 1);
                float hAngle = Mathf.Lerp(-horizontalFOV / 2f, horizontalFOV / 2f, hPercent);

                // »тоговый поворот луча
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

        /// <summary>
        /// ћожно €вно вызвать полный скан: пройтись по всем строкам подр€д.
        /// </summary>
        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            for (int i = 0; i < verticalLines; i++)
            {
                ScanSingleLine(i);
            }
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
