using Assets.Scripts.MapEditor.Models.Enums;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Controllers
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] private TMP_Dropdown todDropdown;
        [SerializeField] private MapTerrain terrain;
        [SerializeField] private DayNightController dayNightController;

        public TimeOfDay CurrentTOD { get; private set; }
        public MapSize CurrentMapSize { get; private set; }

        public void SetMap(MapSize mapSize)
        {
            // выставляем dropdown без срабатывания лишнего onValueChanged
            sizeDropdown.SetValueWithoutNotify(Array.IndexOf(
                         (MapSize[])Enum.GetValues(typeof(MapSize)), mapSize));
            OnSizeChanged(sizeDropdown.value);  // обновляем сцену
        }

        public void SetEnvironment(TimeOfDay tod)
        {
            todDropdown.SetValueWithoutNotify((int)tod);
            OnTODChanged((int)tod);
        }

        private void Start()
        {
            sizeDropdown.ClearOptions();
            sizeDropdown.AddOptions(GetMapSizeOptions());
            sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            OnSizeChanged(sizeDropdown.value);

            todDropdown.ClearOptions();
            todDropdown.AddOptions(new List<string>() { "Утро", "День", "Вечер", "Ночь" });
            todDropdown.onValueChanged.AddListener(OnTODChanged);

            terrain.Init((int)Enum.GetValues(typeof(MapSize)).GetValue(sizeDropdown.value));
            dayNightController.OnTimeChanged(todDropdown.value);
        }

        private void OnTODChanged(int index)
        {
            var tod = (TimeOfDay)Enum.GetValues(typeof(TimeOfDay)).GetValue(index);
            CurrentTOD = tod;
            Debug.Log($"Current tod seted: {CurrentTOD}");
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
