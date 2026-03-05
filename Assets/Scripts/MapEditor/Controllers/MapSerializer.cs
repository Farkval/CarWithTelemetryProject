using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Models.Enums;
using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Controllers
{
    public class MapSerializer : MonoBehaviour
    {
        public void Save(List<PlacedObject> objs, MapSize size, TimeOfDay tod)
        {
            var path = FileDialog.ShowSave("JSON файлы (*.json)|*.json", "Сохранить карту");
            if (string.IsNullOrEmpty(path))
                return;

            if (string.IsNullOrEmpty(path))
                return;

            var data = new MapData(objs, size, tod);
            var terr = FindFirstObjectByType<MapTerrain>();

            data.heightRes = terr.HeightResolution;
            data.surfaceRes = terr.SurfaceResolution;

            data.heights = terr.ExportHeights();
            data.surfaces = terr.ExportSurfaces();

            if (!File.Exists(path))
                File.Create(path).Close();
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }

        public void Load(Action<MapData> onLoaded)
        {
            var path = FileDialog.ShowOpen("JSON файлы (*.json)|*.json", "Загрузить карту")?.FirstOrDefault();
            if (string.IsNullOrEmpty(path))
                return;

            if (string.IsNullOrEmpty(path)) return;

            var data = JsonUtility.FromJson<MapData>(File.ReadAllText(path));
            onLoaded?.Invoke(data);
        }
    }
}
