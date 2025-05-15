using Assets.Scripts.MapEditor;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Game.Map
{
    public class MapLoader : MonoBehaviour
    {
        [SerializeField] GameObject carPrefab;          // ваша машинка
        [SerializeField] MapTerrain terrain;            // ссылка из Hierarchy

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

            // размер и рельеф
            terrain.Init(data.mapMeters);
            if (data.heights != null && data.heights.Length > 0)
            {
                int n = (int)Mathf.Sqrt(data.heights.Length) - 1;
                float[,] h = new float[n + 1, n + 1];
                System.Buffer.BlockCopy(data.heights, 0, h, 0, sizeof(float) * data.heights.Length);
                terrain.ImportHeights(h);
            }

            // объекты
            Vector3 spawnPos = Vector3.zero;
            foreach (var inst in data.instances)
            {
                var ed = Resources.Load<ElementData>(inst.elementPath);
                if (!ed) continue;

                // Spawn / Finish — специальная логика
                if (ed.displayName == "SpawnPoint") 
                { 
                    spawnPos = inst.position;
                    continue; 
                }

                if (ed.displayName == "FinishPoint")
                {
                    var fin = Instantiate(ed.prefab, inst.position, Quaternion.identity);
                    fin.AddComponent<FinishTrigger>();
                    continue;
                }

                Instantiate(ed.prefab, inst.position, Quaternion.Euler(inst.rotation))
                          .transform.localScale = inst.localScale;
            }

            // машинка
            Instantiate(carPrefab, spawnPos + Vector3.up * 0.5f, Quaternion.identity);
        }
    }
}
