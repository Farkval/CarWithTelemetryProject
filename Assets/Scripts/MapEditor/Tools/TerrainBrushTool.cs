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
    public class TerrainBrushTool : MonoBehaviour
    {
        public TerrainBrushToolMode mode = TerrainBrushToolMode.Raise;
        public float strength = 0.5f;
        public float radius = 3f;

        private Camera _cam;
        private MapTerrain _terrain;
        private Toggle _toggle;
        private UndoRedoController _undo;
        private Coroutine _editRoutine;
        private float[,] _beforeHeights;
        private bool _isModifed = false;

        void Awake()
        {
            _cam = Camera.main;
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
            _beforeHeights = _terrain.GetHeightsCopy();
            while (Input.GetMouseButton(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject()
                    && Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition),
                                       out var hit, 500f, LayerMask.GetMask("Default")))
                {
                    float d = (mode == TerrainBrushToolMode.Raise ? 1 : -1)
                               * strength * Time.deltaTime;
                    _terrain.ModifyWorld(hit.point, d, radius);

                    if (!_isModifed)
                        _isModifed = true;
                }
                yield return null;
            }
            if (_isModifed)
            {
                var after = _terrain.GetHeightsCopy();
                _undo.AddAction(new TerrainModifyAction(_terrain, _beforeHeights, after));
                _isModifed = false;
            }
            _editRoutine = null;
        }
    }
}
