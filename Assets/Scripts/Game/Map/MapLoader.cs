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
        [SerializeField] GameObject carPrefab;
        [SerializeField] MapTerrain terrain;
        [SerializeField] DayNightController dayNightController;

        void Start()
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

            // размер и рельеф
            terrain.Init((int)data.mapSize);
            if (data.heights != null && data.heights.Length > 0)
            {
                int n = (int)Mathf.Sqrt(data.heights.Length) - 1;
                float[,] h = new float[n + 1, n + 1];
                System.Buffer.BlockCopy(data.heights, 0, h, 0, sizeof(float) * data.heights.Length);
                //terrain.ImportHeights(h);
            }
            if (data.surfaces != null && data.surfaces.Length == (int)data.mapSize * (int)data.mapSize)
            {
                terrain.ImportSurfaces((int)data.mapSize, data.surfaces);
            }

            // объекты
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;
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

            Instantiate(carPrefab, spawnPos + Vector3.up * 0.5f, spawnRot);
        }
    }
}
