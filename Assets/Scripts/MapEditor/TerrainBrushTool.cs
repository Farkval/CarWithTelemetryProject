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
        public float strength = 0.5f;   // метров в секунду
        public float radius = 3f;

        Camera _cam;
        MapTerrain _terrain;
        Toggle _toggle;

        void Awake()
        {
            _cam = Camera.main;
            _terrain = FindFirstObjectByType<MapTerrain>();
            _toggle = GetComponent<Toggle>();

            // реагируем на включение / выключение
            _toggle.onValueChanged.AddListener(OnToggle);
            enabled = _toggle.isOn;               // активны только когда выбраны
        }

        void OnToggle(bool isOn)
        {
            if (!isOn)
                return;
        }

        void Update()
        {
            if (!Input.GetMouseButton(0)) return;
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition),
                                out var hit, 500f, LayerMask.GetMask("Default")))
            {
                float delta = (mode == Mode.Raise ? 1 : -1) * strength * Time.deltaTime;
                _terrain.ModifyWorld(hit.point, delta, radius);
            }
        }
    }
}
