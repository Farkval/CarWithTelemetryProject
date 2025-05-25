using Assets.Scripts.Sensors.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Lidars
{
    /// <summary>
    /// 1) "Механический" лидар, имитирующий вращение вокруг вертикальной оси.
    ///    Можно настраивать скорость вращения и разрешение сканирования по вертикали/горизонтали.
    /// </summary>
    public class MechanicalLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        [Tooltip("Максимальная дальность, на которой лидар регистрирует объекты.")]
        public float maxDistance = 100f;

        [Tooltip("Вертикальный угол обзора (сколько \"лучей\" будет формироваться по вертикали).")]
        public float verticalFOV = 30f;

        [Tooltip("Число \"линий\" сканирования по вертикали. Например, 16, 32 и т.д.")]
        public int verticalResolution = 16;

        [Tooltip("Частота вращения лидара (градусов в секунду).")]
        public float rotationSpeed = 30f;

        [Tooltip("Частота сканирования (обновление за секунду). При слишком высокой нужно оптимизировать код.")]
        public float scanFrequency = 10f;

        [Tooltip("Слой, по которому стреляют лучи. Лучше выделить отдельные слои для объектов окружения.")]
        public LayerMask layerMask = ~0; // По умолчанию все слои

        public event Action<List<LidarPoint>> OnScanComplete;

        // Вспомогательные поля
        private float _currentRotationAngle = 0f;
        private float _scanTimer = 0f;
        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;

        public List<LidarPoint> PointCloud => _pointCloud;

        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _currentRotationAngle = 0f;
            _scanTimer = 0f;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Обновляем вращение и периодически запускаем скан.
        /// </summary>
        private void Update()
        {
            // Плавно вращаем лидар
            _currentRotationAngle += rotationSpeed * Time.deltaTime;
            // Чтобы угол не рос бесконечно
            if (_currentRotationAngle >= 360f) _currentRotationAngle -= 360f;

            // Задаём таймер для сканирования
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 1f / scanFrequency)
            {
                _scanTimer = 0f;
                PerformScan();
            }
        }

        /// <summary>
        /// Выполнение сканирования:
        ///  - берем текущий угол поворота (горизонтальный),
        ///  - по вертикали делаем несколько лучей,
        ///  - сохраняем результат.
        /// </summary>
        public void PerformScan()
        {
            // Очищаем облако перед новым сканом
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            // Горизонтальный вектор направления
            Quaternion baseRotation = Quaternion.Euler(0f, _currentRotationAngle, 0f);

            for (int i = 0; i < verticalResolution; i++)
            {
                // Пробегаем от -verticalFOV/2 до +verticalFOV/2
                float vPercent = (float)i / (verticalResolution - 1);
                float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);

                // Поворот по вертикали
                Quaternion verticalRot = Quaternion.Euler(vAngle, 0f, 0f);
                // Итоговая ориентация луча
                Quaternion rayRotation = baseRotation * verticalRot;

                Vector3 direction = rayRotation * Vector3.forward;
                Vector3 origin = transform.position;

                // Рейкаст
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                {
                    float dist = hit.distance;
                    // Заполняем структуру
                    LidarPoint pt = new LidarPoint(hit.point, dist);
                    _pointCloud.Add(pt);

                    // Обновляем ближайшую дистанцию
                    if (dist < _nearestDistance)
                    {
                        _nearestDistance = dist;
                    }
                }
                else
                {
                    // Если луч не встретил объект — для облака точек можно не добавлять ничего
                    // но вы можете добавить "точку на макс. дистанции", если нужно
                }
            }

            OnScanComplete?.Invoke(_pointCloud);
        }

        // Для визуализации в Editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
}
