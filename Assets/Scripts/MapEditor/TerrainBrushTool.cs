using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor
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
        private ElementPaletteUI _elementPaletteUI;
        private Coroutine _editRoutine;
        private int _layerMask = 0;

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
            // нажали ЛКМ и рутин ещё не запущен
            if (Input.GetMouseButtonDown(0) && _editRoutine == null)
            {
                _editRoutine = StartCoroutine(EditTerrainLoop());
            }

            // отпустили ЛКМ — останавливаем рутину
            if (Input.GetMouseButtonUp(0) && _editRoutine != null)
            {
                StopCoroutine(_editRoutine);
                _editRoutine = null;
            }
        }

        private IEnumerator EditTerrainLoop()
        {
            // выполняем пока ЛКМ зажата
            while (Input.GetMouseButton(0))
            {
                // проверяем, что не на UI
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (Physics.Raycast(
                        _cam.ScreenPointToRay(Input.mousePosition),
                        out var hit,
                        500f,
                        _layerMask))
                    {
                        float delta = (mode == TerrainBrushToolMode.Raise ? 1 : -1)
                                      * strength * Time.deltaTime;
                        _terrain.ModifyWorld(hit.point, delta, radius);
                    }
                }
                yield return null; // ждём до следующего кадра
            }
            // по выходу выставляем флаг, чтобы можно было запустить рутину снова
            _editRoutine = null;
        }
    }
}
