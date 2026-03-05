using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.Robot.Cars;
using Assets.Scripts.Robot.Sensors.Cameras;
using Assets.Scripts.Robot.Sensors.Lidars;
using Assets.Scripts.Robot.Vizualizers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.Garage
{
    public class GarageController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] VehicleListUI listUI;
        [SerializeField] InspectorPanelUI inspectorUI;
        [SerializeField] Toggle lidarVizalizerToggle;
        [SerializeField] Toggle cameraVizalizerToggle;

        [Header("Spawn")]
        [SerializeField] Transform spawnPoint;

        private GameObject _currentPrefab;
        private GameObject _currentInstance;
        private List<Component> _currentComponents;
        private List<LidarVisualizer> _currentLidarVisualizers;
        private List<CameraVisualizer> _currentCameraVisualizers;

        void Start()
        {
            listUI.Build(OnVehicleSelected);
            lidarVizalizerToggle.onValueChanged.AddListener(UpdateLidarVisualizersEnabled);
            cameraVizalizerToggle.onValueChanged.AddListener(UpdateCameraVisualizersEnabled);
        }

        void OnVehicleSelected(GameObject prefab)
        {
            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
                _currentLidarVisualizers = null;
                _currentCameraVisualizers = null;
            }

            _currentPrefab = prefab;

            _currentInstance = Instantiate(prefab, spawnPoint.position + Vector3.up * 1, spawnPoint.rotation);

            _currentLidarVisualizers = new List<LidarVisualizer>(
                _currentInstance.GetComponentsInChildren<LidarVisualizer>());
            _currentCameraVisualizers = new List<CameraVisualizer>(
                _currentInstance.GetComponentsInChildren<CameraVisualizer>());

            UpdateLidarVisualizersEnabled(lidarVizalizerToggle.isOn);
            UpdateCameraVisualizersEnabled(cameraVizalizerToggle.isOn);
            UpdateCarCameraDepth(_currentInstance, -1);

            ChangeCarLogicEnabled(false);

            _currentComponents = GatherComponents(_currentInstance);

            VehicleLoader.LoadSettings(_currentPrefab.name, _currentComponents);

            inspectorUI.BuildFor(_currentComponents);
        }

        private void ChangeCarLogicEnabled(bool enabled)
        {
            var carCtrl1 = _currentInstance.GetComponentsInChildren<FourWheelsCarController>().FirstOrDefault();
            if (carCtrl1 != null)
                carCtrl1.enabled = enabled;
            var carCtrl2 = _currentInstance.GetComponentsInChildren<TrackedTankController>().FirstOrDefault();
            if (carCtrl2 != null)
                carCtrl2.enabled = enabled;
        }

        public void OnSavePressed()
        {
            if (_currentInstance == null) return;

            ChangeCarLogicEnabled(true);

            VehicleLoader.SaveSettings(_currentPrefab.name, _currentComponents);

            ChangeCarLogicEnabled(false);
        }

        public void OnBackPressed()
        {
            SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
        }

        public void OnResetPressed()
        {
            if (_currentPrefab == null) return;

            PlayerPrefs.DeleteKey($"VehicleData_{_currentPrefab.name}");
            PlayerPrefs.Save();

            OnVehicleSelected(_currentPrefab);
        }

        private void UpdateCarCameraDepth(GameObject car, int depth = -1)
        {
            var cameras = car.GetComponentsInChildren<Camera>();
            foreach (var cam in cameras)
            {
                cam.depth = depth;
                cam.clearFlags = CameraClearFlags.Depth;
            }
        }

        void UpdateLidarVisualizersEnabled(bool enabled)
        {
            if (_currentLidarVisualizers == null)
                return;

            foreach (var vis in _currentLidarVisualizers)
            {
                if (vis == null)
                    continue;
                vis.enabled = enabled;
            }
        }

        void UpdateCameraVisualizersEnabled(bool enabled)
        {
            if (_currentCameraVisualizers == null)
                return;

            foreach (var vis in _currentCameraVisualizers)
            {
                if (vis == null)
                    continue;
                vis.enabled = enabled;
            }
        }

        public static List<Component> GatherComponents(GameObject car)
        {
            var list = new List<Component>();

            var carCtrl1 = car.GetComponentsInChildren<FourWheelsCarController>().FirstOrDefault();
            var carCtrl2 = car.GetComponentsInChildren<TrackedTankController>().FirstOrDefault();
            if (carCtrl1 != null) 
                list.Add(carCtrl1);
            if (carCtrl2 != null) 
                list.Add(carCtrl2);

            list.AddRange(car.GetComponentsInChildren<FlashLidar>());
            list.AddRange(car.GetComponentsInChildren<MechanicalLidar>());
            list.AddRange(car.GetComponentsInChildren<MemsLidar>());
            list.AddRange(car.GetComponentsInChildren<CameraSensor>());

            return list;
        }
    }
}
