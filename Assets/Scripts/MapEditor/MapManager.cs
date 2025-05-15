using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] private MapTerrain terrain;
        public int CurrentMapMeters { get; private set; }

        public void SetMap(int meters) => OnSizeChanged((meters / 24) - 1);

        private void Start()
        {
            sizeDropdown.ClearOptions();
            sizeDropdown.AddOptions(GetMapSizeOptions());
            sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            OnSizeChanged(sizeDropdown.value);
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

        private void OnSizeChanged(int index)
        {
            var sizes = Enum.GetValues(typeof(MapSize));
            int meters = (int)sizes.GetValue(index);
            CurrentMapMeters = meters;
            float groundScale = meters / 10f;
            terrain.Init(meters);
        }
    }
}
