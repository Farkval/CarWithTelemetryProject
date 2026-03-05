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
        [Header("Root")]
        [SerializeField] private Toggle settingsToggle;

        [Header("Map")]
        [SerializeField] private TMP_InputField selectMapInput;

        [Header("Session")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;

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
        [SerializeField] private TMP_Text timeText;

        [Header("System")]
        [SerializeField] private Button exitButton;

        private readonly List<GameObject> _uiElements = new();
        private GameController _game;

        private int CurrentCarIndex => selectCarDropdown.value - 1;
        private int CurrentSpawnPointIndex => selectSpawnPointsDropdown.value - 1;
        private int CurrentPlayerIndex => selectPlayerDropdown.value - 1;

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

            ShowUI(settingsToggle.isOn);

            timeText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_game.GameStarted)
            {
                timeText.text = _game.GameEllapsedTime.ToString();
            }
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

            selectPlayerDropdown.onValueChanged.AddListener(OnPlayerChanged);
            addPlayerButton.onClick.AddListener(AddNewPlayer);
            deletePlayerButton.onClick.AddListener(DeletePlayer);

            selectCarDropdown.onValueChanged.AddListener(OnCarSelected);
            selectSpawnPointsDropdown.onValueChanged.AddListener(OnSpawnPointSelected);
            loadRobotButton.onClick.AddListener(OnLoadRobotPressed);

            lidarVisualizersEnabledToggle.onValueChanged.AddListener(OnUpdateLidarVisualizers);
            cameraVisualizersEnabledToggle.onValueChanged.AddListener(OnUpdateCameraVisualizers);
            carManualControlToggle.onValueChanged.AddListener(OnUpdateManualControl);

            selectScriptInput.onSelect.AddListener(_ => OnSelectScriptInputPressed());
            selectMapInput.onSelect.AddListener(_ => OnSelectMapInputPressed());

            consoleEnabledToggle.onValueChanged.AddListener(consolePanel.SetActive);
            consolePanel.SetActive(consoleEnabledToggle.isOn);
            startButton.onClick.AddListener(OnStart);
            stopButton.onClick.AddListener(OnStop);
            exitButton.onClick.AddListener(() =>
                SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE));
        }

        private void AddNewPlayer()
        {
            if (!_game.CanAddNewPlayer()) return;

            var name = _game.AddNewPlayer();

            addPlayerButton.interactable = _game.CanAddNewPlayer();
            deletePlayerButton.interactable = true;

            selectPlayerDropdown.options.Add(new TMP_Dropdown.OptionData(name));
            selectPlayerDropdown.SetValueWithoutNotify(selectPlayerDropdown.options.Count - 1);
            OnPlayerChanged(0);
        }

        private void DeletePlayer()
        {
            if (CurrentPlayerIndex == -1) 
                return;

            int count = _game.DeletePlayer(CurrentPlayerIndex);

            deletePlayerButton.interactable = count > 0;
            addPlayerButton.interactable = _game.CanAddNewPlayer();
            startButton.interactable = _game.AnyPlayerSpawned();

            selectPlayerDropdown.options.RemoveAt(CurrentPlayerIndex + 1);
            selectPlayerDropdown.SetValueWithoutNotify(0);

            UpdateSpawnPointsDropdown();
            OnPlayerChanged(0);
        }

        private void OnPlayerChanged(int _)
        {
            bool playerSelected = _game.TryGetPlayer(CurrentPlayerIndex, out var player);

            selectSpawnPointsDropdown.interactable = playerSelected;
            selectCarDropdown.interactable = playerSelected;

            if (!playerSelected)
            {
                selectSpawnPointsDropdown.SetValueWithoutNotify(0);
                selectCarDropdown.SetValueWithoutNotify(0);
                loadRobotButton.interactable = false;
                carManualControlToggle.interactable = false;
                cameraVisualizersEnabledToggle.interactable = false;
                lidarVisualizersEnabledToggle.interactable= false;
            }
            else
            {
                _game.DisableManualControlForOtherCars(player);

                selectCarDropdown.SetValueWithoutNotify(player.HaveCar ? player.SelectedCarIndex + 1 : 0);
                selectSpawnPointsDropdown.SetValueWithoutNotify(player.HaveSpawnPoint ? player.SelectedSpawnPointIndex + 1 : 0);
            }

            selectScriptInput.interactable = playerSelected && player.Spawned;

            _game.DisableCamerasForOtherCars(player);

            // Синхронизируем тогглы
            SyncToggles(playerSelected ? player : null);
        }

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

            player.UpdateCameraVisualizersEnabled(cameraVisualizersEnabledToggle.isOn);
            player.UpdateLidarVisualizersEnabled(lidarVisualizersEnabledToggle.isOn);
            player.UpdateCarManualControl(carManualControlToggle.isOn);

            _game.DisableManualControlForOtherCars(player);

            UpdateSpawnPointsDropdown();
            OnPlayerChanged(selectPlayerDropdown.value);

            startButton.interactable = true;
        }

        private void OnSelectMapInputPressed()
        {
            string mapName = _game.LoadMap();
            bool ok = mapName.Length != 0;

            selectMapInput.text = ok ? mapName :
                selectMapInput.text is "Выберите карту" ? "Выберите карту" : selectMapInput.text;

            if (!ok) 
                return;

            int currentPlayersCount = selectPlayerDropdown.options.Count - 1;
            for (int i = currentPlayersCount; i > 0; i--)
            {
                selectPlayerDropdown.options.RemoveAt(i);
                _game.DeletePlayer(i);
            }
            startButton.interactable = false;
            deletePlayerButton.interactable = false;
            if (currentPlayersCount != 0)
                selectPlayerDropdown.value = 0;

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
            Debug.Log("OnSelectScriptInputPressed");
            if (!_game.TryGetPlayer(CurrentPlayerIndex, out var player)) return;

            string path = FileDialog.ShowOpen("Файлы (*.py)|*.py", "Выбрать скрипт")?.FirstOrDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            Debug.Log("SetScript");
            player.SetScript(path);
            selectScriptInput.text = player.ScriptFileName;
        }
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
        private void ShowUI(bool show)
        {
            foreach (var go in _uiElements)
                go.SetActive(show);
        }


        private (bool f1, bool f2) _preStartUIState;

        private void OnStart()
        {
            _game.StartGame();
            startButton.interactable = false;
            stopButton.interactable = true;

            _preStartUIState = (addPlayerButton.interactable, deletePlayerButton.interactable);
            addPlayerButton.interactable = false;
            deletePlayerButton.interactable = false;
            loadRobotButton.interactable = false;
            selectSpawnPointsDropdown.interactable = false;
            selectCarDropdown.interactable = false;
            selectScriptInput.interactable = false;
            carManualControlToggle.interactable = false;
            carManualControlToggle.SetIsOnWithoutNotify(false);
            timeText.gameObject.SetActive(true);
            selectMapInput.interactable = false;
        }

        private void OnStop()
        {
            _game.StopGame();
            startButton.interactable = true;
            stopButton.interactable = false;

            addPlayerButton.interactable = _preStartUIState.f1;
            deletePlayerButton.interactable = _preStartUIState.f2;
            loadRobotButton.interactable = CurrentCarIndex != 1 && CurrentPlayerIndex != -1 && CurrentSpawnPointIndex != 1;
            selectSpawnPointsDropdown.interactable = CurrentPlayerIndex != -1;
            selectCarDropdown.interactable = CurrentPlayerIndex != -1;
            selectScriptInput.interactable = CurrentPlayerIndex != -1;
            carManualControlToggle.interactable = CurrentPlayerIndex != -1;
            timeText.gameObject.SetActive(false);
            selectMapInput.interactable = true;
        }
    }
}
