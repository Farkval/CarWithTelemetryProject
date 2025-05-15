using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor
{
    [RequireComponent(typeof(Toggle))]
    public class TerrainBrushTool : MonoBehaviour
    {
        public enum Mode { Raise, Pit }
        public Mode mode = Mode.Raise;
        public float strength = 0.5f;
        public float radius = 3f;

        Camera _cam;
        MapTerrain _terrain;
        Toggle _toggle;
        ElementPaletteUI _elementPaletteUI;

        void Awake()
        {
            _cam = Camera.main;
            _terrain = FindFirstObjectByType<MapTerrain>();
            _toggle = GetComponent<Toggle>();
            _elementPaletteUI = FindFirstObjectByType<ElementPaletteUI>();

            _toggle.onValueChanged.AddListener(OnToggle);
            enabled = _toggle.isOn;
        }

        void OnToggle(bool isOn)
        {
            enabled = isOn;
            if (isOn)
                _elementPaletteUI.DeselectElement();
        }

        void Update()
        {
            if (!Input.GetMouseButton(0)) 
                return;

            if (EventSystem.current.IsPointerOverGameObject()) 
                return;

            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition),
                                out var hit, 500f, LayerMask.GetMask("Default")))
            {
                float delta = (mode == Mode.Raise ? 1 : -1) * strength * Time.deltaTime;
                _terrain.ModifyWorld(hit.point, delta, radius);
            }
        }
    }
}
