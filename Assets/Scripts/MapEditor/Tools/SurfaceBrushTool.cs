using Assets.Scripts.MapEditor.Actions;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models.Enums;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor.Tools
{
    [RequireComponent(typeof(Toggle))]
    public class SurfaceBrushTool : MonoBehaviour
    {
        public SurfaceType surfaceType = SurfaceType.Mud;
        public float radius = 3f;

        private MapTerrain _terrain;
        private Toggle _toggle;
        private UndoRedoController _undo;
        private SurfaceType[,] _before;
        private Coroutine _editRoutine;
        private bool _isModifed;

        void Awake()
        {
            _terrain = FindFirstObjectByType<MapTerrain>();
            _toggle = GetComponent<Toggle>();
            _undo = FindFirstObjectByType<MapEditorController>().UndoRedoManager;

            var elementPaletteUIController = FindFirstObjectByType<ElementPaletteUIController>();
            _toggle.onValueChanged.AddListener(isOn =>
            {
                enabled = isOn;
                if (isOn)
                {
                    elementPaletteUIController.ClearSelection();
                }
            });

            enabled = _toggle.isOn;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && _editRoutine == null && _toggle.isOn)
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

                    if (!_isModifed)
                        _isModifed = true;
                }
                yield return null;
            }
            if (_isModifed)
            {
                var after = (SurfaceType[,])_terrain.SurfaceArray.Clone();
                _undo.AddAction(new SurfaceModifyAction(_terrain, _before, after));
                _isModifed = false;
            }
            _editRoutine = null;
        }
    }
}
