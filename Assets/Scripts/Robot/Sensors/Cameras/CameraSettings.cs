//using Assets.Scripts.Garage.Attributes;
//using Assets.Scripts.Garage.Interfaces;
//using Assets.Scripts.Robot.Api.Interfaces;
//using UnityEngine;
//using UnityEngine.Experimental.Rendering;

//namespace Assets.Scripts.Robot.Sensors.Cameras
//{
//    [SectionName("Камера")]
//    public class CameraSettings : MonoBehaviour, ICameraSensor, IApplySettings
//    {
//        [Header("Поля камеры")]
//        [DisplayName("Угол обзора (FOV)")]
//        public float fieldOfView = 60f;

//        [DisplayName("Разрешение по ширине")]
//        public int renderWidth = 1280;

//        [DisplayName("Разрешение по высоте")]
//        public int renderHeight = 720;

//        [DisplayName("HDR рендер")]
//        public bool enableHDR = false;

//        [DisplayName("Активность")]
//        public bool isEnabled;

//        public int Width => renderWidth;
//        public int Height => renderHeight;

//         Внутренние ссылки
//        Camera _cam;
//        RenderTexture _rt;

//        void Awake()
//        {
//            _cam = GetComponent<Camera>();
//            _cam.enabled = isEnabled;
//            ApplySettings();
//        }

//#if UNITY_EDITOR
//        void OnValidate()
//        {
//            if (_cam == null) 
//                _cam = GetComponent<Camera>();
//            _cam.enabled = isEnabled;
//            ApplySettings();
//        }
//#endif

//        public void ApplySettings()
//        {
//            if (_cam == null) 
//                return;

//            _cam.enabled = isEnabled;

//             1) Меняем field of view
//            _cam.fieldOfView = fieldOfView;

//             2) Включаем/отключаем HDR
//            _cam.allowHDR = enableHDR;

//             3) Если текущий RenderTexture не соответствует нужному разрешению или HDR-режиму — пересоздаём его
//            if (_rt != null)
//            {
//                if (_rt.width != renderWidth || _rt.height != renderHeight ||
//                    _rt.format != (_cam.allowHDR ? RenderTextureFormat.DefaultHDR
//                                                : RenderTextureFormat.Default))
//                {
//                    if (_cam.targetTexture == _rt)
//                        _cam.targetTexture = null;
//                    _rt.Release();
//                    DestroyImmediate(_rt);
//                    _rt = null;
//                }
//            }

//            if (_rt == null)
//            {
//                _rt = new RenderTexture(renderWidth, renderHeight, /* depth */ 16,
//                                       _cam.allowHDR ? RenderTextureFormat.DefaultHDR
//                                                     : RenderTextureFormat.Default);
//                _rt.name = $"{name}_RT";
//                _cam.targetTexture = _rt;
//            }
//        }

//        public Texture2D CaptureTexture()
//        {
//            var rt = _rt; // ваш RenderTexture
//            var tex = new Texture2D(Width, Height, rt.graphicsFormat, TextureCreationFlags.None);
//            var prev = RenderTexture.active;
//            RenderTexture.active = rt;
//            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
//            tex.Apply();
//            RenderTexture.active = prev;
//            return tex;
//        }

//        public byte[] CaptureImageBytes(ImageFormat format = ImageFormat.PNG)
//        {
//            var tex = CaptureTexture();
//            if (format == ImageFormat.PNG)
//                return tex.EncodeToPNG();
//            else
//                return tex.EncodeToJPG();
//        }
//    }
//}
