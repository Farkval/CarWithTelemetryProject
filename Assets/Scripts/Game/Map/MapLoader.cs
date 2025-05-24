using Assets.Scripts.Garage;
using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Triggers;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Game.Map
{
    public class MapLoader : MonoBehaviour
    {
        [SerializeField] public GameObject carPrefab;
        [SerializeField] MapTerrain terrain;
        [SerializeField] DayNightController dayNightController;

        public Vector3 spawnPos;
        public Quaternion spawnRot;

        public void Load()
        {
            //string path = Path.Combine(Application.persistentDataPath, "lastMap.json");
            string path = UnityEditor.EditorUtility.OpenFilePanel("Выбрать карту", "", "json");

            if (!File.Exists(path)) 
            {
                Debug.LogError("Map file not found"); 
                return; 
            }

            var data = JsonUtility.FromJson<MapData>(File.ReadAllText(path));

            dayNightController.OnTimeChanged((int)data.timeOfDay);

            terrain.Init((int)data.mapSize);
            if (data.heights != null && data.heightRes > 0)
                terrain.ImportHeights(data.heightRes, data.heights);
            else terrain.Init((int)data.mapSize);

            // покрытие
            if (data.surfaces != null && data.surfaceRes > 0)
                terrain.ImportSurfaces(data.surfaceRes, data.surfaces);

            // объекты
            spawnPos = Vector3.zero;
            spawnRot = Quaternion.identity;
            foreach (var inst in data.instances)
            {
                var ed = Resources.Load<ElementData>(inst.elementPath);
                if (!ed) continue;

                // Spawn / Finish — специальная логика
                if (ed.name == ElementNameConst.START_INSTANCE_NAME)
                {
                    spawnPos = inst.position;
                    spawnRot = Quaternion.Euler(inst.rotation + new Vector3(0, 90, 0));
                    continue;
                }

                if (ed.name == ElementNameConst.FINISH_INSTANCE_NAME)
                {
                    var fin = Instantiate(ed.prefab, inst.position, Quaternion.identity);
                    fin.AddComponent<FinishTrigger>();
                    continue;
                }

                Instantiate(ed.prefab, inst.position, Quaternion.Euler(inst.rotation))
                          .transform.localScale = inst.localScale;
            }

            VehicleLoader.LoadSettings(carPrefab.name, GarageController.GatherComponents(carPrefab));
            Instantiate(carPrefab, spawnPos + Vector3.up * 0.5f, spawnRot);
        }
    }
}
