using Assets.Scripts.Game.Models;
using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Game.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private MapTerrain terrain;
        [SerializeField] private DayNightController dayNightController;
        [SerializeField] private GameUIController gameUIController;
        [SerializeField] private Camera mainCamera;

        private readonly List<SpawnPoint> _spawns = new();
        private readonly List<Player> _players = new();
        private readonly List<GameObject> _cars = new();

        private int _nextPlayerIndex = 1;
        private DateTime _gameStartedTime;
        private TimeSpan _gameEndedTime;
        public TimeSpan GameEllapsedTime => DateTime.UtcNow - _gameStartedTime;
        public bool GameStarted { get; private set; }

        /* ----------------------------  PUBLIC API  ---------------------------- */

        public IReadOnlyList<SpawnPoint> SpawnPoints => _spawns;
        public IReadOnlyList<Player> Players => _players;
        public IReadOnlyList<GameObject> Cars => _cars;

        /* --------------------------------------------------------------------- */

        private void Awake()
        {
            // Загружаем префабы машин
            _cars.AddRange(Resources.LoadAll<GameObject>("Vehicles"));

            // Инициализируем UI списком машин
            gameUIController.InitializeCars(_cars);
        }

        /// <summary>Открывает JSON-карту, создаёт объекты на сцене и формирует точки спавна.</summary>
        public string LoadMap()
        {
            string path = FileDialog.ShowOpen("JSON файлы (*.json)|*.json", "Выбрать карту")?.FirstOrDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return string.Empty;

            // Очищаем старые Spawn-ы
            foreach (var sp in _spawns) sp.ClearSpawn();
            _spawns.Clear();

            // Загружаем данные
            MapData data = JsonUtility.FromJson<MapData>(File.ReadAllText(path));

            dayNightController.OnTimeChanged((int)data.timeOfDay);

            var placedObjects = MapLoader.Load(data, terrain);

            foreach (var po in placedObjects)
            {
                if (po.data.name != ElementNameConst.START_INSTANCE_NAME) continue;

                _spawns.Add(new SpawnPoint(po.instance.transform.position,
                                           po.instance.transform.rotation));
            }

            return Path.GetFileName(path);
        }

        /* ----------------------  Index-safe геттеры  ------------------------ */

        public bool TryGetPlayer(int playerIndex, out Player player) =>
            IndexGuard(_players, playerIndex, out player);

        public bool TryGetCar(int carIndex, out GameObject car) =>
            IndexGuard(_cars, carIndex, out car);

        public bool TryGetSpawnPoint(int spawnIndex, out SpawnPoint spawnPoint) =>
            IndexGuard(_spawns, spawnIndex, out spawnPoint);

        private static bool IndexGuard<T>(IReadOnlyList<T> list, int index, out T element)
        {
            if (index < 0 || index >= list.Count)
            {
                element = default;
                return false;
            }

            element = list[index];
            return true;
        }

        /* --------------------  Управление игроками  ------------------------ */

        /// <summary>Добавляет нового игрока-контейнер (GameObject+Player) и возвращает текущее количество игроков.</summary>
        public string AddNewPlayer()
        {
            var go = new GameObject($"Player_{_nextPlayerIndex}");
            var player = go.AddComponent<Player>();
            player.Initialize(_nextPlayerIndex++);

            _players.Add(player);
            return player.Name;
        }

        public int DeletePlayer(int playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out var player)) return _players.Count;

            player.OnPlayerDeleted();
            Destroy(player.gameObject);

            _players.RemoveAt(playerIndex);
            return _players.Count;
        }

        public bool CanAddNewPlayer() => _players.Count < _spawns.Count;

        /* ---------  Синхронизация ручного управления/камер  ----------------- */

        public void DisableManualControlForOtherCars(Player active)
        {
            foreach (var p in _players)
                p.UpdateCarManualControl(p == active && active?.ManualControl == true);
        }

        public void DisableCamerasForOtherCars(Player active)
        {
            foreach (var p in _players)
                p.UpdateCameraEnabled(p == active && active?.Spawned == true);

            mainCamera.enabled = active == null || !active.Spawned;
        }

        /* -----------------------  Скрипты Python  -------------------------- */

        public void StartGame()
        {
            foreach (var p in _players)
                p.LaunchScript(true);

            _gameStartedTime = DateTime.UtcNow;
            GameStarted = true;
        }

        public void StopGame()
        {
            foreach (var p in _players)
                p.LaunchScript(false);

            _gameEndedTime = DateTime.UtcNow - _gameStartedTime;
            GameStarted = false;
        }

        public bool AnyPlayerSpawned()
        {
            return _players.Any(p => p.Spawned);
        }
    }
}
