using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] TMP_Dropdown todDropdown;
        [SerializeField] private MapTerrain terrain;
        [SerializeField] private DayNightController dayNightController;

        public TimeOfDay CurrentTOD { get; private set; }
        public MapSize CurrentMapSize { get; private set; }

        public void SetMap(MapSize mapSize) => OnSizeChanged(-1, incomingMapSize: mapSize);

        public void SetEnvironment(TimeOfDay tod)
        {
            todDropdown.value = (int)tod;
        }

        private void Start()
        {
            sizeDropdown.ClearOptions();
            sizeDropdown.AddOptions(GetMapSizeOptions());
            sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            OnSizeChanged(sizeDropdown.value);

            todDropdown.ClearOptions();
            todDropdown.AddOptions(new List<string>() { "Утро", "День", "Вечер", "Ночь" });
            todDropdown.onValueChanged.AddListener(i => OnTODChanged((TimeOfDay)i));

            terrain.Init((int)Enum.GetValues(typeof(MapSize)).GetValue(sizeDropdown.value));
            dayNightController.OnTimeChanged(todDropdown.value);
        }

        private void OnTODChanged(TimeOfDay tod)
        {
            CurrentTOD = tod;
            dayNightController.OnTimeChanged((int)tod);
        }

        private List<string> GetMapSizeOptions()
        {
            var sizes = Enum.GetValues(typeof(MapSize));
            var list = new List<string>();
            foreach (var size in sizes)
            {
                list.Add($"{(int)size}x{(int)size}м");
            }
            return list;
        }

        private void OnSizeChanged(int index) => OnSizeChanged(index, null);

        private void OnSizeChanged(int mapSizeIndex, MapSize? incomingMapSize = null)
        {
            var mapSize = incomingMapSize ?? (MapSize)Enum.GetValues(typeof(MapSize)).GetValue(mapSizeIndex);
            CurrentMapSize = mapSize;
            float groundScale = (int)mapSize / 10f;
            terrain.Init((int)mapSize);
        }
    }
}
