using Assets.Scripts.MapEditor.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Controllers
{
    public class MapLoader : MonoBehaviour
    {
        public static List<PlacedObject> Load(
            MapData md,
            MapTerrain mt,
            MapController mc = null,
            List<PlacedObject> placedObjectsToDestroy = null)
        {
            if (mc != null)
            {
                mc.SetMap(md.mapSize);
                mc.SetEnvironment(md.timeOfDay);
            }

            mt.Init((int)md.mapSize);
            if (md.heights != null && md.heightRes > 0)
                mt.ImportHeights(md.heightRes, md.heights);

            if (md.surfaces != null && md.surfaceRes > 0)
                mt.ImportSurfaces(md.surfaceRes, md.surfaces);

            if (placedObjectsToDestroy != null)
            {
                foreach (var po in placedObjectsToDestroy)
                    Destroy(po.instance);
                placedObjectsToDestroy.Clear();
            }

            var placedObjects = new List<PlacedObject>();

            foreach (var inst in md.instances)
            {
                var ed = Resources.Load<ElementData>(inst.elementPath);
                if (!ed)
                {
                    continue;
                }

                GameObject obj = Instantiate(ed.prefab);
                obj.transform.SetPositionAndRotation(inst.position, Quaternion.Euler(inst.rotation));
                obj.transform.localScale = inst.localScale;
                placedObjects.Add(new PlacedObject(obj, ed));
            }

            return placedObjects;
        }
    }
}
