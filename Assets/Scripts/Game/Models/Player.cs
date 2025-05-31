using Assets.Scripts.Garage;
using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Vizualizers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Game.Models
{
    public class Player : MonoBehaviour
    {
        /* ---------------------  Components / runtime  --------------------- */

        private GameObject _carPrefab;
        private GameObject _carInstance;
        private List<LidarVisualizer> _lidarVisualizers;
        private List<CameraVisualizer> _cameraVisualizers;
        private IRobotAPI _robotAPI;
        private PythonScriptRunner _pythonRunner;
        private SpawnPoint _spawnPoint;
        private Camera _camera;

        /* --------------------------  State  ------------------------------- */

        public string Name { get; private set; }

        public bool VisualizeLidars { get; private set; }
        public bool VisualizeCameras { get; private set; }
        public bool ManualControl => _robotAPI?.ManualControl == true;

        public string ScriptFileName { get; private set; }
        public string CarName => _carPrefab != null ? _carPrefab.name : "–";

        public bool HaveCar => _carPrefab != null;
        public bool HaveSpawnPoint => _spawnPoint != null;
        public bool Spawned => _carInstance != null;

        public int SelectedSpawnPointIndex { get; private set; } // 0-based
        public int SelectedCarIndex { get; private set; } // 0-based

        /* =================================================================== */

        public void Initialize(int index) => Name = $"Игрок {index}";

        /* =================================================================== */
        /*                       Загрузка / спавн машины                       */
        /* =================================================================== */

        public void LoadCar(GameObject prefab, SpawnPoint sp, int spawnIdx, int carIdx)
        {
            // Сносим предыдущую
            if (_carInstance != null) Destroy(_carInstance);
            _spawnPoint?.ClearSpawn();

            _carPrefab = prefab;
            _spawnPoint = sp;

            SelectedSpawnPointIndex = spawnIdx;
            SelectedCarIndex = carIdx;

            Spawn();
        }

        private void Spawn()
        {
            _spawnPoint.OnPlayerSpawned(Name);

            _carInstance = Instantiate(
                _carPrefab,
                _spawnPoint.SpawnPosition + Vector3.up, // чуть над землей
                _spawnPoint.SpawnRotation);

            VehicleLoader.LoadSettings(_carPrefab.name,
                GarageController.GatherComponents(_carInstance));

            CacheComponents();
        }

        private void CacheComponents()
        {
            _lidarVisualizers = _carInstance.GetComponentsInChildren<LidarVisualizer>().ToList();
            _cameraVisualizers = _carInstance.GetComponentsInChildren<CameraVisualizer>().ToList();
            _robotAPI = _carInstance.GetComponentInChildren<IRobotAPI>();
            _pythonRunner = _carInstance.GetComponentInChildren<PythonScriptRunner>();
            _camera = _carInstance.GetComponentInChildren<Camera>();
        }

        /* =================================================================== */
        /*                           Визуализаторы                              */
        /* =================================================================== */

        public void UpdateCameraVisualizersEnabled(bool value)
        {
            if (_cameraVisualizers == null) return;

            _cameraVisualizers.ForEach(v => v.enabled = value);
            VisualizeCameras = value;
        }

        public void UpdateLidarVisualizersEnabled(bool value)
        {
            if (_lidarVisualizers == null) return;

            _lidarVisualizers.ForEach(v => v.enabled = value);
            VisualizeLidars = value;
        }

        /* =================================================================== */
        /*                       Ручное управление                              */
        /* =================================================================== */

        public void UpdateCarManualControl(bool value)
        {
            if (_robotAPI == null) return;
            _robotAPI.ManualControl = value;
        }

        /* =================================================================== */
        /*                               Скрипт                                 */
        /* =================================================================== */

        public void SetScript(string path)
        {
            if (_pythonRunner == null) return;

            _pythonRunner.scriptFile = path;
            _pythonRunner.Initizalize();

            ScriptFileName = Path.GetFileName(path);
        }

        public void LaunchScript(bool launch)
        {
            if (_pythonRunner == null) return;

            if (launch) _pythonRunner.Launch();
            else _pythonRunner.Stop();
        }

        /* =================================================================== */
        /*                                 Misc                                 */
        /* =================================================================== */

        public void UpdateCameraEnabled(bool value)
        {
            if (_camera != null) _camera.enabled = value;
        }

        public void OnPlayerDeleted()
        {
            _spawnPoint?.ClearSpawn();
            if (_carInstance != null) Destroy(_carInstance);
            GC.SuppressFinalize(this);
        }
    }
}
