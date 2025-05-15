using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class MapManager : MonoBehaviour
    {
        public enum MapSize { Small = 24, Medium = 48, Large = 96 }

        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] private MapTerrain terrain;
        public int CurrentMapMeters { get; private set; }

        public void SetMap(int meters) => OnSizeChanged((meters / 24) - 1);

        private void Start()
        {
            sizeDropdown.ClearOptions();
            sizeDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "Маленькая 24×24м", 
                "Средняя 48×48м", 
                "Большая 96×96м" 
            });
            sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            OnSizeChanged(sizeDropdown.value);
        }

        private void OnSizeChanged(int index)
        {
            MapSize size = (MapSize)((int)MapSize.Small + index * 24);
            int meters = (int)size;
            CurrentMapMeters = meters;
            float groundScale = meters / 10f;
            terrain.Init(meters);
        }
    }
}
