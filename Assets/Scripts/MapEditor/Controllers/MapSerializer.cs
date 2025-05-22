using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Controllers
{
    /// <summary>
    /// Сохраняет и загружает карту в JSON через диалоги Unity (EditorUtility).
    /// Для билдов можно подключить NativeFilePicker или аналогичный плагин.
    /// </summary>
    public class MapSerializer : MonoBehaviour
    {
        // Assets/Scripts/MapEditor/Controllers/MapSerializer.cs
        public void Save(List<PlacedObject> objs, MapSize size, TimeOfDay tod)
        {
            var path = UnityEditor.EditorUtility.SaveFilePanel(
                       "Сохранить карту", "", "map", "json");
            if (string.IsNullOrEmpty(path)) return;      // окно вызываем ОДИН раз

            var data = new MapData(objs, size, tod);
            Debug.Log($"{data.timeOfDay}");
            var terr = FindFirstObjectByType<MapTerrain>();

            // meta
            data.heightRes = terr.HeightResolution;     // добавьте геттер-свойства
            data.surfaceRes = terr.SurfaceResolution;

            // сами массивы
            data.heights = terr.ExportHeights();
            data.surfaces = terr.ExportSurfaces();

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Debug.Log($"Map saved: {path}");
        }

        public void Load(Action<MapData> onLoaded)
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("Загрузить карту", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = JsonUtility.FromJson<MapData>(File.ReadAllText(path));
            onLoaded?.Invoke(data);
        }

    }
}
