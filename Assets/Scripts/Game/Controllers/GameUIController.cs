using Assets.Scripts.Game.Controllers;
using Assets.Scripts.Game.Models;
using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Controllers
{
    public class GameUIController : MonoBehaviour
    {
        /* -------------  Ссылки из инспектора  ------------- */

        [Header("Root")]
        [SerializeField] private Toggle settingsToggle;

        [Header("Map")]
        [SerializeField] private TMP_InputField selectMapInput;

        [Header("Session")]
        [SerializeField] private Button startButton;

        [Header("Players")]
        [SerializeField] private TMP_Dropdown selectPlayerDropdown;
        [SerializeField] private Button addPlayerButton;
        [SerializeField] private Button deletePlayerButton;

        [Header("Robot")]
        [SerializeField] private TMP_Dropdown selectSpawnPointsDropdown;
        [SerializeField] private TMP_Dropdown selectCarDropdown;
        [SerializeField] private TMP_InputField selectScriptInput;
        [SerializeField] private Button loadRobotButton;

        [Header("Toggles")]
        [SerializeField] private Toggle lidarVisualizersEnabledToggle;
        [SerializeField] private Toggle cameraVisualizersEnabledToggle;
        [SerializeField] private Toggle carManualControlToggle;
        [SerializeField] private Toggle consoleEnabledToggle;

        [Header("Console")]
        [SerializeField] private GameObject consolePanel;

        [Header("System")]
        [SerializeField] private Button exitButton;

        /* -------------  Поля  ------------- */

        private readonly List<GameObject> _uiElements = new();
        private GameController _game;

        /* --------  Быстрые геттеры индексов («-1», если placeholder)  -------- */

        private int CurrentCarIndex => selectCarDropdown.value - 1;
        private int CurrentSpawnPointIndex => selectSpawnPointsDropdown.value - 1;
        private int CurrentPlayerIndex => selectPlayerDropdown.value - 1;

        /* =================================================================== */
        /*                               INIT                                  */
        /* =================================================================== */

        public void InitializeCars(IEnumerable<GameObject> carPrefabs)
        {
            selectCarDropdown.ClearOptions();

            var options = new List<string> { "Выберите машину" };
            options.AddRange(carPrefabs.Select(c => c.name));

            selectCarDropdown.AddOptions(options);
            selectCarDropdown.SetValueWithoutNotify(0);
        }

        private void Awake()
        {
            _game = FindFirstObjectByType<GameController>();

            selectScriptInput.text = "Выберите скрипт";
            selectMapInput.text = "Выберите карту";

            CacheUIElements();
            WireEvents();

            // Показ/скрытие панели настроек
            ShowUI(settingsToggle.isOn);
        }

        private void CacheUIElements()
        {
            foreach (var field in GetType()
                     .GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<SerializeField>() == null) continue;
                if (field.Name == nameof(settingsToggle)) continue;

                if (field.GetValue(this) is Component c)
                    _uiElements.Add(c.gameObject);
            }
        }

        private void WireEvents()
        {
            settingsToggle.onValueChanged.AddListener(ShowUI);

            /* Players */
            selectPlayerDropdown.onValueChanged.AddListener(OnPlayerChanged);
            addPlayerButton.onClick.AddListener(AddNewPlayer);
            deletePlayerButton.onClick.AddListener(DeletePlayer);

            /* Robot */
            selectCarDropdown.onValueChanged.AddListener(OnCarSelected);
            selectSpawnPointsDropdown.onValueChanged.AddListener(OnSpawnPointSelected);
            loadRobotButton.onClick.AddListener(OnLoadRobotPressed);

            /* Toggles */
            lidarVisualizersEnabledToggle.onValueChanged.AddListener(OnUpdateLidarVisualizers);
            cameraVisualizersEnabledToggle.onValueChanged.AddListener(OnUpdateCameraVisualizers);
            carManualControlToggle.onValueChanged.AddListener(OnUpdateManualControl);

            /* Script / Map */
            selectScriptInput.onSelect.AddListener(_ => OnSelectScriptInputPressed());
            selectMapInput.onSelect.AddListener(_ => OnSelectMapInputPressed());

            /* Console / System */
            consoleEnabledToggle.onValueChanged.AddListener(consolePanel.SetActive);
            consolePanel.SetActive(consoleEnabledToggle.isOn);
            startButton.onClick.AddListener(_game.TogglePlayersScripts);
            exitButton.onClick.AddListener(() =>
                SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE));
        }

        /* =================================================================== */
        /*                         Игроки                                       */
        /* =================================================================== */

        private void AddNewPlayer()
        {
            if (!_game.CanAddNewPlayer()) return;

            int count = _game.AddNewPlayer();

            addPlayerButton.interactable = _game.CanAddNewPlayer();
            deletePlayerButton.interactable = true;

            RefreshPlayersDropdown();
            // индекс первого игрока = 1 (0 - placeholder)
            selectPlayerDropdown.SetValueWithoutNotify(count);
            OnPlayerChanged(count);
        }

        private void DeletePlayer()
        {
            if (CurrentPlayerIndex == -1) 
                return;

            int count = _game.DeletePlayer(CurrentPlayerIndex);

            deletePlayerButton.interactable = count > 0;
            addPlayerButton.interactable = _game.CanAddNewPlayer();

            RefreshPlayersDropdown();
            selectPlayerDropdown.SetValueWithoutNotify(0);

            // Обновить точки спавна (освободились)
            UpdateSpawnPointsDropdown();
            OnPlayerChanged(0);
        }

        private void RefreshPlayersDropdown()
        {
            var options = new List<string> { "Выберите игрока" };
            options.AddRange(_game.Players.Select(p => p.Name));

            selectPlayerDropdown.ClearOptions();
            selectPlayerDropdown.AddOptions(options);
        }

        private void OnPlayerChanged(int _)
        {
            bool playerSelected = _game.TryGetPlayer(CurrentPlayerIndex, out var player);

            selectSpawnPointsDropdown.interactable = playerSelected;
            selectCarDropdown.interactable = playerSelected;

            /* ----------  Синхронизация выбранного игрока с UI  ---------- */
            if (!playerSelected)
            {
                selectSpawnPointsDropdown.SetValueWithoutNotify(0);
                selectCarDropdown.SetValueWithoutNotify(0);
            }
            else
            {
                _game.DisableManualControlForOtherCars(player);

                selectCarDropdown.SetValueWithoutNotify(player.HaveCar ? player.SelectedCarIndex + 1 : 0);
                selectSpawnPointsDropdown.SetValueWithoutNotify(player.HaveSpawnPoint ? player.SelectedSpawnPointIndex + 1 : 0);
            }

            // Поле скрипта доступно только если машина уже загружена
            selectScriptInput.interactable = playerSelected && player.Spawned;

            _game.DisableCamerasForOtherCars(player);

            // Синхронизируем тогглы
            SyncToggles(playerSelected ? player : null);
        }

        /* =================================================================== */
        /*                         Робот / Машина                               */
        /* =================================================================== */

        private void OnCarSelected(int index)
        {
            bool carSelected = _game.TryGetCar(CurrentCarIndex, out _);
            bool playerSelected = _game.TryGetPlayer(CurrentPlayerIndex, out var player);
            bool spawnPointSelected = _game.TryGetSpawnPoint(CurrentSpawnPointIndex, out _);

            selectScriptInput.interactable = playerSelected && player.Spawned;
            selectScriptInput.text = player?.ScriptFileName ?? "Выберите скрипт";

            lidarVisualizersEnabledToggle.interactable = carSelected;
            cameraVisualizersEnabledToggle.interactable = carSelected;
            carManualControlToggle.interactable = carSelected;

            SyncToggles(player);

            loadRobotButton.interactable = carSelected && spawnPointSelected;
        }

        private void OnSpawnPointSelected(int index)
        {
            bool carSelected = _game.TryGetCar(CurrentCarIndex, out _);
            bool spawnPointSelected = _game.TryGetSpawnPoint(CurrentSpawnPointIndex, out var spawn);

            loadRobotButton.interactable = carSelected && spawnPointSelected;

            // Заблокируем уже занятый спаун-поинт
            if (spawn?.PlayerSpawned == true)
                selectSpawnPointsDropdown.SetValueWithoutNotify(0);
        }

        private void OnLoadRobotPressed()
        {
            if (!_game.TryGetPlayer(CurrentPlayerIndex, out var player) ||
                !_game.TryGetSpawnPoint(CurrentSpawnPointIndex, out var sp) ||
                !_game.TryGetCar(CurrentCarIndex, out var carPrefab))
                return;

            player.LoadCar(carPrefab, sp, CurrentSpawnPointIndex, CurrentCarIndex);

            // Текущие значения тогглов ➜ в машину
            player.UpdateCameraVisualizersEnabled(cameraVisualizersEnabledToggle.isOn);
            player.UpdateLidarVisualizersEnabled(lidarVisualizersEnabledToggle.isOn);
            player.UpdateCarManualControl(carManualControlToggle.isOn);

            _game.DisableManualControlForOtherCars(player);

            UpdateSpawnPointsDropdown();
            OnPlayerChanged(selectPlayerDropdown.value);
        }

        /* =================================================================== */
        /*                         Файлы / Диалоги                              */
        /* =================================================================== */

        private void OnSelectMapInputPressed()
        {
            string mapName = _game.LoadMap();
            bool ok = mapName.Length != 0;

            selectMapInput.text = ok ? mapName :
                selectMapInput.text is "Выберите карту" ? "Выберите карту" : selectMapInput.text;

            if (!ok) return;

            UpdateSpawnPointsDropdown();
            InitializeCars(_game.Cars);

            selectPlayerDropdown.interactable = true;
            addPlayerButton.interactable = true;
        }

        public void InteractiveStart(bool flag) => startButton.interactable = flag;

        private void UpdateSpawnPointsDropdown()
        {
            var options = new List<string> { "Выберите старт" };
            options.AddRange(_game.SpawnPoints.Select((sp, i) =>
                sp.PlayerSpawned ? $"Точка_{i + 1} ({sp.PlayerName})" : $"Точка_{i + 1}"));

            selectSpawnPointsDropdown.ClearOptions();
            selectSpawnPointsDropdown.AddOptions(options);
            selectSpawnPointsDropdown.SetValueWithoutNotify(0);
        }

        private void OnSelectScriptInputPressed()
        {
            if (!_game.TryGetPlayer(CurrentPlayerIndex, out var player)) return;

            string path = FileDialog.ShowOpen("Файлы (*.py)|*.py", "Выбрать скрипт")?.FirstOrDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            player.SetScript(path);
            selectScriptInput.text = player.ScriptFileName;
        }

        /* =================================================================== */
        /*                         Тогглы                                       */
        /* =================================================================== */

        private void OnUpdateLidarVisualizers(bool value)
        {
            if (_game.TryGetPlayer(CurrentPlayerIndex, out var player))
                player.UpdateLidarVisualizersEnabled(value);
        }

        private void OnUpdateCameraVisualizers(bool value)
        {
            if (_game.TryGetPlayer(CurrentPlayerIndex, out var player))
                player.UpdateCameraVisualizersEnabled(value);
        }

        private void OnUpdateManualControl(bool value)
        {
            if (!_game.TryGetPlayer(CurrentPlayerIndex, out var player)) return;

            player.UpdateCarManualControl(value);
            if (value) _game.DisableManualControlForOtherCars(player);
        }

        private void SyncToggles(Player player)
        {
            lidarVisualizersEnabledToggle.SetIsOnWithoutNotify(player?.VisualizeLidars == true);
            cameraVisualizersEnabledToggle.SetIsOnWithoutNotify(player?.VisualizeCameras == true);
            carManualControlToggle.SetIsOnWithoutNotify(player?.ManualControl == true);
        }

        /* =================================================================== */

        private void ShowUI(bool show)
        {
            foreach (var go in _uiElements)
                go.SetActive(show);
        }
    }
}
