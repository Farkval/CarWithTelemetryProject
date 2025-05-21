using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor
{
    [RequireComponent(typeof(Toggle))]
    public class SurfaceBrushTool : MonoBehaviour
    {
        public SurfaceType surfaceType = SurfaceType.Mud;
        public float radius = 3f;

        private MapTerrain _terrain;
        private Toggle _toggle;
        private UndoRedoManager _undo;
        private SurfaceType[,] _before;
        private Coroutine _editRoutine;

        void Awake()
        {
            _terrain = FindFirstObjectByType<MapTerrain>();
            _toggle = GetComponent<Toggle>();
            _undo = FindFirstObjectByType<MapEditorController>().UndoRedoManager;

            _toggle.onValueChanged.AddListener(isOn => enabled = isOn);
            enabled = _toggle.isOn;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && _editRoutine == null)
                _editRoutine = StartCoroutine(EditLoop());
        }

        private IEnumerator EditLoop()
        {
            _before = (SurfaceType[,])_terrain.SurfaceArray.Clone();
            while (Input.GetMouseButton(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject()
                    && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                                       out var hit, 500f, LayerMask.GetMask("Default")))
                {
                    _terrain.ModifySurfaceWorld(hit.point, surfaceType, radius);
                }
                yield return null;
            }
            var after = (SurfaceType[,])_terrain.SurfaceArray.Clone();
            _undo.AddAction(new SurfaceModifyAction(_terrain, _before, after));
            _editRoutine = null;
        }
    }
}
