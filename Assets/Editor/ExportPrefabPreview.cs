using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class ExportPrefabPreview
    {
        [MenuItem("Tools/Export Selected Prefab Preview")]
        static void ExportPreview()
        {
            var go = Selection.activeObject as GameObject;
            if (go == null) 
            { 
                Debug.LogError("Select a prefab in Project window");
                return; 
            }

            var tex = AssetPreview.GetAssetPreview(go);
            if (tex == null)
            { 
                Debug.LogError("Preview not ready, try again."); 
                return; 
            }

            var bytes = tex.EncodeToPNG();
            var path = "Assets/" + go.name + "_Preview.png";
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log("Saved preview to " + path);
        }
    }
}
