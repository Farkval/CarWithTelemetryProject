using Assets.Scripts.Garage.Attributes;
using UnityEngine;

namespace Assets.Scripts.Robot.Sensors.Cameras
{
    public class CameraSettings : MonoBehaviour
    {
        [Header("Поля камеры")]
        [DisplayName("Угол обзора (FOV)")]
        public float fieldOfView = 60f;

        [DisplayName("Разрешение по ширине")]
        public int renderWidth = 1280;

        [DisplayName("Разрешение по высоте")]
        public int renderHeight = 720;

        [DisplayName("HDR рендер")]
        public bool enableHDR = false;

        // Внутренние ссылки
        Camera _cam;
        RenderTexture _rt;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            ApplySettings();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            ApplySettings();
        }
#endif

        public void ApplySettings()
        {
            if (_cam == null) return;

            // 1) Меняем field of view
            _cam.fieldOfView = fieldOfView;

            // 2) Включаем/отключаем HDR
            _cam.allowHDR = enableHDR;

            // 3) Если текущий RenderTexture не соответствует нужному разрешению или HDR-режиму — пересоздаём его
            if (_rt != null)
            {
                if (_rt.width != renderWidth || _rt.height != renderHeight ||
                    _rt.format != (_cam.allowHDR ? RenderTextureFormat.DefaultHDR
                                                : RenderTextureFormat.Default))
                {
                    _cam.targetTexture = null;
                    _rt.Release();
                    DestroyImmediate(_rt);
                    _rt = null;
                }
            }

            if (_rt == null)
            {
                _rt = new RenderTexture(renderWidth, renderHeight, /* depth */ 16,
                                       _cam.allowHDR ? RenderTextureFormat.DefaultHDR
                                                     : RenderTextureFormat.Default);
                _rt.name = $"{name}_RT";
                _cam.targetTexture = _rt;
            }
        }
    }

}
