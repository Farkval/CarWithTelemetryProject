using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    /// <summary>
    /// Контроллер камеры игрока
    /// </summary>
    public class CarCameraSwitch : MonoBehaviour
    {
        public Camera thirdPersonCamera;
        public Camera topDownCamera;
        public Camera frontalCamera;

        private List<Camera> _cameras = new List<Camera>();
        private int _currentCameraIndex = -1;

        private void Start()
        {
            _cameras.AddRange(new List<Camera>() { thirdPersonCamera, topDownCamera, frontalCamera });
            SwitchCamera();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SwitchCamera();
            }
        }

        private void SwitchCamera()
        {
            _currentCameraIndex++;
            if (_currentCameraIndex >= _cameras.Count)
                _currentCameraIndex = 0;

            for (int i = 0; i < _cameras.Count; i++)
            {
                _cameras[i].enabled = (i == _currentCameraIndex);
            }
        }
    }
}
