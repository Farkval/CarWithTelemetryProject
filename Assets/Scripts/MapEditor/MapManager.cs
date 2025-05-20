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

        public MapSize CurrentMapSize { get; private set; }

        public void SetMap(MapSize mapSize) => OnSizeChanged(-1, incomingMapSize: mapSize);

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
