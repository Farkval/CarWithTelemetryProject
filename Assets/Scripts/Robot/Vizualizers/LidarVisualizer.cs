using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Sensors.Lidars;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Vizualizers
{
    public class LidarVisualizer : MonoBehaviour
    {
        [Header("Настройки визуализации")]
        [SerializeField] GameObject rayPrefab;
        [SerializeField] float rayDuration = 0.1f;

        class SensorContext
        {
            public ILidarSensor Sensor;
            public List<LineRenderer> Pool = new List<LineRenderer>();
        }

        readonly List<SensorContext> _contexts = new List<SensorContext>();

        void Awake()
        {
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
            foreach (var ctx in _contexts)
                ctx.Sensor.OnScanComplete -= points => OnScan(ctx, points);
        }

        void OnDisable()
        {
            foreach (var ctx in _contexts)
                Clear(ctx);
        }

        void OnScan(SensorContext ctx, List<ILidarPoint> cloud)
        {
            if (!enabled) 
                return;

            int needed = cloud.Count;
            while (ctx.Pool.Count < needed)
            {
                var go = Instantiate(rayPrefab, (ctx.Sensor as MonoBehaviour).transform);
                var lr = go.GetComponent<LineRenderer>();
                go.SetActive(false);
                ctx.Pool.Add(lr);
            }

            var origin = (ctx.Sensor as MonoBehaviour).transform.position;

            for (int i = 0; i < needed; i++)
            {
                var pt = cloud[i];
                var lr = ctx.Pool[i];

                lr.positionCount = 2;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, pt.WorldPosition);

                // градиент по дистанции:
                // float t = Mathf.Clamp01(pt.Distance / maxDistance);
                // Color c = Color.Lerp(Color.red, Color.blue, t);
                // lr.startColor = lr.endColor = c;

                lr.gameObject.SetActive(true);
            }

            for (int i = needed; i < ctx.Pool.Count; i++)
                ctx.Pool[i].gameObject.SetActive(false);

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
