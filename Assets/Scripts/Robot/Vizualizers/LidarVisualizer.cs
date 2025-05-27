using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Sensors.Lidars;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Vizualizers
{
    public class LidarVisualizer : MonoBehaviour
    {
        [Header("Настройки визуализации")]
        [SerializeField] GameObject rayPrefab;   // префаб с LineRenderer
        [SerializeField] float rayDuration = 0.1f;

        class SensorContext
        {
            public ILidarSensor Sensor;
            public List<LineRenderer> Pool = new List<LineRenderer>();
        }

        readonly List<SensorContext> _contexts = new List<SensorContext>();

        void Awake()
        {
            // Находим все ILidarSensor в потомках и подписываемся на их событие
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb is ILidarSensor sensor)
                {
                    var ctx = new SensorContext { Sensor = sensor };
                    sensor.OnScanComplete += points => OnScan(ctx, points);
                    _contexts.Add(ctx);
                }
            }
        }

        void OnDestroy()
        {
            // Отписка
            foreach (var ctx in _contexts)
                ctx.Sensor.OnScanComplete -= points => OnScan(ctx, points);
        }

        void OnDisable()
        {
            // Если визуализатор выключают — сразу чистим всё
            foreach (var ctx in _contexts)
                Clear(ctx);
        }

        void OnScan(SensorContext ctx, List<ILidarPoint> cloud)
        {
            if (!enabled) 
                return;

            int needed = cloud.Count;
            // Расширяем пул, если нужно
            while (ctx.Pool.Count < needed)
            {
                var go = Instantiate(rayPrefab, (ctx.Sensor as MonoBehaviour).transform);
                var lr = go.GetComponent<LineRenderer>();
                go.SetActive(false);
                ctx.Pool.Add(lr);
            }

            var origin = (ctx.Sensor as MonoBehaviour).transform.position;

            // Рисуем лучи
            for (int i = 0; i < needed; i++)
            {
                var pt = cloud[i];
                var lr = ctx.Pool[i];

                lr.positionCount = 2;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, pt.WorldPosition);

                // Если хотите градиент по дистанции:
                // float t = Mathf.Clamp01(pt.Distance / maxDistance);
                // Color c = Color.Lerp(Color.red, Color.blue, t);
                // lr.startColor = lr.endColor = c;

                lr.gameObject.SetActive(true);
            }

            // Скрываем лишние
            for (int i = needed; i < ctx.Pool.Count; i++)
                ctx.Pool[i].gameObject.SetActive(false);

            // Запланировать полное очищение через rayDuration
            CancelInvoke(nameof(ClearAll));
            Invoke(nameof(ClearAll), rayDuration);
        }

        void Clear(SensorContext ctx)
        {
            foreach (var lr in ctx.Pool)
                lr.gameObject.SetActive(false);
        }

        void ClearAll()
        {
            foreach (var ctx in _contexts)
                Clear(ctx);
        }
    }
}
