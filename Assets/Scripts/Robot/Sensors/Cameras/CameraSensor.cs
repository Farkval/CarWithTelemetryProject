using Assets.Scripts.Garage.Interfaces;
using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Cameras
{
    /// <summary>
    /// Камерный сенсор для мобильного робота.
    /// Возвращает список объектов с тегом "Element", находящихся в поле зрения и не перекрытых.
    /// </summary>
    public class CameraSensor : MonoBehaviour, IApplySettings, ICameraSensor
    {
        [SerializeField][Range(1, 180)] public float fieldOfView = 60f;

        [Header("Общие параметры")]
        [Tooltip("Максимальная дальность детекции в метрах")]
        public float maxDistance = 50f;

        [Tooltip("Частота обновления, Гц (0 = каждый кадр)")]
        public float updateRateHz = 10f;

        [Tooltip("Точки выборки на объект (1 = центр Bounds)")]
        [Range(1, 9)]
        public int samplePointsPerAxis = 1;

        [Header("Слои, которые сенсор \"видит\"")]
        public LayerMask visibleLayers;

        [Header("Кэширование")]
        [Tooltip("Как часто обновлять кэш объектов, сек. (0 = не кешировать)")]
        public float refreshElementsEvery = 5f;

        public bool isEnabled;

        // --- Внутренние поля ---
        private Camera _cam;
        private float _timeSinceLastScan;
        private float _timeSinceElementsRefresh;
        private readonly List<GameObject> _elements = new();
        private readonly List<IDetectedObjectInfo> _detected = new();
        private Plane[] _frustumPlanes;

        /// <summary> Публично доступный список результатов последнего сканирования. </summary>
        public IReadOnlyList<IDetectedObjectInfo> DetectedObjects => _detected;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null)
            {
                Debug.LogError("CameraSensor требует компонент Camera");
                enabled = false;
                return;
            }

            enabled = isEnabled;

            if (refreshElementsEvery <= 0f)
                FindElements();
        }

        private void Update()
        {
            // Ограничиваем частоту
            if (updateRateHz > 0f && _timeSinceLastScan < 1f / updateRateHz)
            {
                _timeSinceLastScan += Time.deltaTime;
                return;
            }
            _timeSinceLastScan = 0f;

            // Обновляем кэш при необходимости
            if (refreshElementsEvery > 0f)
            {
                _timeSinceElementsRefresh += Time.deltaTime;
                if (_timeSinceElementsRefresh >= refreshElementsEvery)
                {
                    FindElements();
                    _timeSinceElementsRefresh = 0f;
                }
            }

            Scan();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_cam == null)
                _cam = GetComponent<Camera>();
            ApplySettings();
        }
#endif
        public void ApplySettings()
        {
            _cam.enabled = isEnabled;
            _cam.fieldOfView = fieldOfView;
        }

        /// <summary> Находит все объекты с тегом "Element" в сцене. </summary>
        private void FindElements()
        {
            _elements.Clear();
            _elements.AddRange(GameObject.FindGameObjectsWithTag("Element"));
        }

        /// <summary> Основная логика сканирования. </summary>
        private void Scan()
        {
            _detected.Clear();
            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_cam);

            foreach (var obj in _elements)
            {
                if (obj == null || !obj.activeInHierarchy)
                    continue;

                var col = obj.GetComponent<Collider>();
                if (col == null)
                    continue;

                // Быстрый тест попадания объекта во фрустум камеры
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, col.bounds))
                    continue;

                // Отбрасываем по расстоянию
                var toObj = col.bounds.center - _cam.transform.position;
                float sqrDist = toObj.sqrMagnitude;
                if (sqrDist > maxDistance * maxDistance)
                    continue;

                // Подробная проверка лучами
                if (IsVisible(col, out var viziblePercent))
                {
                    _detected.Add(new DetectedObjectInfo
                    {
                        Name = obj.name.Replace("(Clone)", ""),
                        position = col.bounds.center,
                        distance = Mathf.Sqrt(sqrDist),
                        viziblePercent = viziblePercent
                    });
                }
            }
        }

        /// <summary>
        /// Проверяет, нет ли преград между камерой и объектом.
        /// Для крупных объектов берём сетку samplePointsPerAxis^3.
        /// </summary>
        private bool IsVisible(Collider col, out float viziblePercent)
        {
            Vector3 min = col.bounds.min;
            Vector3 max = col.bounds.max;

            int n = samplePointsPerAxis;
            float step = n > 1 ? 1f / (n - 1) : 0f;
            int totalSamples = n * n * n;
            int visibleCount = 0;

            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    for (int z = 0; z < n; z++)
                    {
                        Vector3 sample = n == 1
                            ? col.bounds.center
                            : new Vector3(
                                Mathf.Lerp(min.x, max.x, x * step),
                                Mathf.Lerp(min.y, max.y, y * step),
                                Mathf.Lerp(min.z, max.z, z * step));

                        Vector3 dir = sample - _cam.transform.position;
                        if (Physics.Raycast(_cam.transform.position, dir.normalized,
                                            out RaycastHit hit, maxDistance,
                                            visibleLayers, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.collider == col)
                                visibleCount++;
                        }
                    }

            viziblePercent = (visibleCount / totalSamples) * 100;
            return visibleCount > 0;   // или >= totalSamples * 0.2f для порога 20 %
        }
    }
}
