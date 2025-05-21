using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    /// <summary>
    /// Сохраняет и загружает карту в JSON через диалоги Unity (EditorUtility).
    /// Для билдов можно подключить NativeFilePicker или аналогичный плагин.
    /// </summary>
    public class MapSerializer : MonoBehaviour
    {
        public void Save(List<PlacedObject> objects, MapSize mapSize, TimeOfDay timeOfDay)
        {
            string path = UnityEditor.EditorUtility.SaveFilePanel("Сохранить карту", "", "map.json", "json");
            if (string.IsNullOrEmpty(path)) 
                return;

            var data = new MapData(objects, mapSize, timeOfDay);
            var terr = FindFirstObjectByType<MapTerrain>();
            float[,] h = terr.ExportHeights();
            data.heights = new float[h.Length];
            Buffer.BlockCopy(h, 0, data.heights, 0, sizeof(float) * h.Length);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"Map saved to {path}");
        }

        public void Load(Action<MapData> onLoaded)
        {
            string path = UnityEditor.EditorUtility.OpenFilePanel("Загрузить карту", "", "json");
            if (string.IsNullOrEmpty(path)) 
                return;

            var data = JsonUtility.FromJson<MapData>(File.ReadAllText(path));
            onLoaded?.Invoke(data);
        }
    }
}
