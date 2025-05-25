using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.Robot.Cars;
using Assets.Scripts.Robot.Sensors.Cameras;
using Assets.Scripts.Robot.Sensors.Lidars;
using Assets.Scripts.Robot.Vizualizers;
using System.Collections.Generic;
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
            // Строим список кнопок. При клике вызывается OnVehicleSelected(prefab).
            listUI.Build(OnVehicleSelected);
            lidarVizalizerToggle.onValueChanged.AddListener(UpdateLidarVisualizersEnabled);
            cameraVizalizerToggle.onValueChanged.AddListener(UpdateCameraVisualizersEnabled);
        }

        void OnVehicleSelected(GameObject prefab)
        {
            // Удаляем старый экземпляр, если был
            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
                _currentLidarVisualizers = null;
                _currentCameraVisualizers = null;
            }

            _currentPrefab = prefab;

            // Спавним новый экземпляр машины
            _currentInstance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            _currentLidarVisualizers = new List<LidarVisualizer>(
                _currentInstance.GetComponentsInChildren<LidarVisualizer>());
            _currentCameraVisualizers = new List<CameraVisualizer>(
                _currentInstance.GetComponentsInChildren<CameraVisualizer>());

            UpdateLidarVisualizersEnabled(lidarVizalizerToggle.isOn);
            UpdateCameraVisualizersEnabled(cameraVizalizerToggle.isOn);

            // Отключаем её встроенный контроллер и камеры, чтобы не мешали UI-редактированию
            var carCtrl = _currentInstance.GetComponent<FourWheelsCarController>();
            if (carCtrl != null) carCtrl.enabled = false;

            // Собираем список компонентов ИМЕННО с экземпляра
            _currentComponents = GatherComponents(_currentInstance);

            // Загружаем сохранённые настройки (если есть)
            VehicleLoader.LoadSettings(_currentPrefab.name, _currentComponents);

            // Строим UI по этим компонентам
            inspectorUI.BuildFor(_currentComponents);
        }

        public void OnSavePressed()
        {
            if (_currentInstance == null) return;

            // Включаем контроллер при сохранении, если нужно
            var carCtrl = _currentInstance.GetComponent<FourWheelsCarController>();
            if (carCtrl != null) carCtrl.enabled = true;

            VehicleLoader.SaveSettings(_currentPrefab.name, _currentComponents);

            // Снова отключаем его, чтобы не управлялся игроком
            if (carCtrl != null) carCtrl.enabled = false;
        }

        public void OnBackPressed()
        {
            SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
        }

        public void OnResetPressed()
        {
            if (_currentPrefab == null) return;

            // Удаляем сохранённое для этого префаба
            PlayerPrefs.DeleteKey($"VehicleData_{_currentPrefab.name}");
            PlayerPrefs.Save();

            // Респавним чистый экземпляр
            OnVehicleSelected(_currentPrefab);
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

        public static List<Component> GatherComponents(GameObject inst)
        {
            var list = new List<Component>();

            var carCtrl = inst.GetComponent<FourWheelsCarController>();
            if (carCtrl != null) list.Add(carCtrl);

            list.AddRange(inst.GetComponentsInChildren<FlashLidar>());
            list.AddRange(inst.GetComponentsInChildren<MechanicalLidar>());
            list.AddRange(inst.GetComponentsInChildren<MemsLidar>());
            list.AddRange(inst.GetComponentsInChildren<CameraSettings>());

            return list;
        }
    }
}
