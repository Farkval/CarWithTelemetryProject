using Assets.Scripts.MapEditor.Actions;
using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Models.Enums;
using Assets.Scripts.MapEditor.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor.Controllers
{
    /// <summary>
    /// Главный контроллер редактора – размещение объектов, предпросмотр, операции Undo/Redo.
    /// </summary>
    public class MapEditorController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Transform previewParent;
        [SerializeField] private MapSerializer mapSerializer;

        [Header("Рельеф")]
        [SerializeField] private Toggle modifyTerrainToggle;
        [SerializeField] private TMP_Text modifyTerrainModeText;
        [SerializeField] private TMP_Dropdown modifyTerrainModeDropdown;
        [SerializeField] private TMP_Text modifyTerrainStrengthText;
        [SerializeField] private Slider modifyTerrainStrengthSlider;
        [SerializeField] private TMP_Text modifyTerrainRadiusText;
        [SerializeField] private Slider modifyTerrainRadiusSlider;

        [Header("Покрытие")]
        [SerializeField] private Toggle modifySurfaceToggle;
        [SerializeField] private TMP_Text modifySurfaceModeText;
        [SerializeField] private TMP_Dropdown modifySurfaceModeDropdown;
        [SerializeField] private TMP_Text modifySurfaceRadiusText;
        [SerializeField] private Slider modifySurfaceRadiusSlider;

        private ElementData _activeElement;
        private GameObject _previewInstance;

        private readonly Dictionary<Renderer, Color> _original = new();
        private readonly UndoRedoController _undoRedo = new UndoRedoController(100);
        private List<PlacedObject> _placedObjects = new();
        private ElementPaletteUIController _elementPaletteUIController;
        private MapTerrain _terrain;
        private PlacedObject _dragObj;           // объект, который сейчас тянут
        private Coroutine _dragRoutine;       // сама корутина-перетаскивания
        private Vector3 _origPreviewScale = Vector3.one;
        private Vector3 _beforePos, _beforeRot, _beforeScale;
        private float _currentScaleFactor = 1f;

        private const float CELL_STEP = 0.1f;
        private const float ROTATE_STEP = 5f;

        public UndoRedoController UndoRedoManager { get { return _undoRedo; } }

        private void Awake()
        {
            _elementPaletteUIController = FindFirstObjectByType<ElementPaletteUIController>();
            _terrain = FindFirstObjectByType<MapTerrain>();

            ConfigureTerrainTools();
        }

        private void ConfigureTerrainTools()
        {
            SetModifyTerrainToolsActive(false);
            modifyTerrainToggle.onValueChanged.AddListener(SetModifyTerrainToolsActive);
            modifyTerrainModeDropdown.onValueChanged.AddListener((i) => modifyTerrainToggle.GetComponent<TerrainBrushTool>().mode = i == 0 ? TerrainBrushToolMode.Raise : TerrainBrushToolMode.Pit);
            modifyTerrainStrengthSlider.onValueChanged.AddListener((v) => modifyTerrainToggle.GetComponent<TerrainBrushTool>().strength = v);
            modifyTerrainRadiusSlider.onValueChanged.AddListener((v) => modifyTerrainToggle.GetComponent<TerrainBrushTool>().radius = v);

            SetModifySurfaceToolsActive(false);
            modifySurfaceToggle.onValueChanged.AddListener(SetModifySurfaceToolsActive);
            modifySurfaceModeDropdown.onValueChanged.AddListener((i) => modifySurfaceToggle.GetComponent<SurfaceBrushTool>().surfaceType = (SurfaceType)Enum.GetValues(typeof(SurfaceType)).GetValue(i));
            modifySurfaceRadiusSlider.onValueChanged.AddListener((v) => modifySurfaceToggle.GetComponent<SurfaceBrushTool>().radius = v);
        }

        private void SetModifyTerrainToolsActive(bool active)
        {
            modifyTerrainModeDropdown.gameObject.SetActive(active);
            modifyTerrainModeText.gameObject.SetActive(active);
            modifyTerrainStrengthSlider.gameObject.SetActive(active);
            modifyTerrainStrengthText.gameObject.SetActive(active);
            modifyTerrainRadiusSlider.gameObject.SetActive(active);
            modifyTerrainRadiusText.gameObject.SetActive(active);
        }

        private void SetModifySurfaceToolsActive(bool active)
        {
            modifySurfaceModeText.gameObject.SetActive(active);
            modifySurfaceModeDropdown.gameObject.SetActive(active);
            modifySurfaceRadiusSlider.gameObject.SetActive(active);
            modifySurfaceRadiusText.gameObject.SetActive(active);
        }

        private void Update()
        {
            HandleHotKey();
            HandleSelection();
            HandlePreview();
            HandlePlacement();
        }

        public void UndoCommand()
        {
            var res = _undoRedo.Undo();
            if (res is PlacedObject obj)
            {
                _placedObjects.Remove(obj);
            }
        }

        public void RedoCommand()
        {
            var res = _undoRedo.Redo();
            if (res is PlacedObject obj)
            {
                _placedObjects.Add(obj);
            }
        }

        public void SaveCommand()
        {
            var mc = FindFirstObjectByType<MapController>();
            mapSerializer.Save(_placedObjects, mc.CurrentMapSize, mc.CurrentTOD);
        }

        public void LoadCommand()
        {
            mapSerializer.Load(data =>
            {
                _placedObjects = MapLoader.Load(data, _terrain, FindFirstObjectByType<MapController>(), _placedObjects);
                _undoRedo.Clear();
            });
        }

        public void ExitCommand()
        {
            SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
        }

        public void SetActiveElement(ElementData data)
        {
            _activeElement = data;

            if (_previewInstance)
                Destroy(_previewInstance);

            if (data != null)
            {
                _previewInstance = Instantiate(data.prefab, previewParent);
                SetLayerRecursively(_previewInstance, LayerMask.NameToLayer("Ignore Raycast"));

                // ── Хайлайт превью тем же способом, что и выбранный объект
                foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
                {
                    if (r.sharedMaterial.HasFloat("_Outline"))
                        r.material.SetFloat("_Outline", 1f);
                    else
                        r.material.color = Color.yellow;
                }

                _origPreviewScale = _previewInstance.transform.localScale;
                _currentScaleFactor = 1f;

                Plane g = new(Vector3.up, 0);
                var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                if (g.Raycast(ray, out float dst))
                    _previewInstance.transform.position = ray.GetPoint(dst);
            }
        }

        private void HandleSelection()
        {
            /* 1. Если уже тащим – ничего не делаем */
            if (_dragRoutine != null || _activeElement != null)
                return;

            /* 2. Кликнули ЛКМ? */
            if (Input.GetMouseButtonDown(0) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                var mask = LayerMask.GetMask("Default");
                if (Physics.Raycast(ray, out var hit, 500f, mask, QueryTriggerInteraction.Collide))
                {
                    // ищем объект среди размещённых
                    _dragObj = _placedObjects.Find(p => 
                            p.instance == hit.collider.gameObject ||
                            hit.collider.transform.IsChildOf(p.instance.transform));

                    if (_dragObj != null)
                    {
                        _dragRoutine = StartCoroutine(DragObjectLoop(_dragObj));
                    }
                }
            }
        }

        private void HandleHotKey()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _elementPaletteUIController.ClearSelection();
            }
            if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
            {
                var res = _undoRedo.Undo();
                if (res is PlacedObject obj)
                {
                    _placedObjects.Remove(obj);
                }
            }
            if (Input.GetKeyDown(KeyCode.Y) && Input.GetKey(KeyCode.LeftControl))
            {
                var res = _undoRedo.Redo();
                if (res is PlacedObject obj)
                {
                    _placedObjects.Add(obj);
                }
            }
        }

        private IEnumerator DragObjectLoop(PlacedObject po)
        {
            Transform tr = po.instance.transform;
            Highlight(po, true);             // вкл. подсветку

            // зафиксируем состояние «до»
            _beforePos = tr.position;
            _beforeRot = tr.rotation.eulerAngles;
            _beforeScale = tr.localScale;

            float half = _terrain.MapHalfWorld;      // helper свойство в MapTerrain

            bool changed = false;
            bool deleted = false;

            while (Input.GetMouseButton(0))
            {
                // — перемещение / поворот / масштаб — точь-в-точь как было
                if (Input.GetKey(KeyCode.R))                      // ROTATE
                {
                    float dx = Input.GetAxis("Mouse X");
                    tr.Rotate(Vector3.up, dx * 3f, Space.World);
                    Vector3 e = tr.rotation.eulerAngles;
                    e.y = Mathf.Round(e.y / ROTATE_STEP) * ROTATE_STEP;
                    tr.rotation = Quaternion.Euler(e);
                    changed = true;
                }
                else if (Input.GetKey(KeyCode.S))                 // SCALE
                {
                    float dy = Input.GetAxis("Mouse Y");
                    float k = 1 + dy * .01f;
                    tr.localScale *= k;
                    changed = true;
                }
                else if (Input.GetKey(KeyCode.H))
                {
                    float dyRaw = Input.GetAxis("Mouse Y") * CELL_STEP;
                    float newYRaw = tr.position.y + dyRaw;
                    float newY = Mathf.Round(newYRaw / CELL_STEP) * CELL_STEP;
                    tr.position = new Vector3(tr.position.x, newY, tr.position.z);
                    changed = true;
                }
                else if (Input.GetKey(KeyCode.Delete))
                {
                    deleted = true;
                    break;
                }
                else                                              // MOVE
                {
                    Plane g = new(Vector3.up, 0);
                    var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                    if (g.Raycast(ray, out float dst))
                    {
                        Vector3 p = ray.GetPoint(dst);
                        // «скольжение» по границе
                        p.x = Mathf.Clamp(p.x, -half, half);
                        p.z = Mathf.Clamp(p.z, -half, half);

                        p.x = Mathf.Round(p.x / CELL_STEP) * CELL_STEP;
                        p.z = Mathf.Round(p.z / CELL_STEP) * CELL_STEP;

                        p.y = tr.position.y;
                        tr.position = p;
                        changed = true;
                    }
                }
                yield return null;
            }

            Highlight(po, false);            // выкл. подсветку

            if (changed)
            {
                _undoRedo.AddAction(
                    new TransformModifyAction(
                        po,
                        _beforePos, _beforeRot, _beforeScale,
                        tr.position, tr.rotation.eulerAngles, tr.localScale));
            }
            if (deleted)
            {
                po.instance.SetActive(false);
                _placedObjects.Remove(po);
                _undoRedo.AddAction(
                    new DeleteAction(po));
            }

            _dragRoutine = null;
            _dragObj = null;
        }

        private void Highlight(PlacedObject po, bool state)
        {
            var r = po.instance.GetComponentInChildren<Renderer>();
            if (!r)
                return;

            if (!_original.ContainsKey(r))                    // кэш оригинала
                _original[r] = r.material.color;

            if (r.sharedMaterial.HasFloat("_Outline"))
                r.material.SetFloat("_Outline", state ? 1f : 0f);
            else
                r.material.color = state ? Color.yellow : _original[r];
        }

        private void HandlePreview()
        {
            if (_activeElement == null || _previewInstance == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            bool heighting = Input.GetMouseButton(0) && Input.GetKey(KeyCode.H);
            bool rotating = Input.GetMouseButton(0) && Input.GetKey(KeyCode.R);
            bool scaling = Input.GetMouseButton(0) && Input.GetKey(KeyCode.S);

            if (!rotating && !scaling && !heighting)
            {
                Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 p;
                if (Physics.Raycast(ray, out var hit, 1000f, LayerMask.GetMask("Default")))
                {
                    p = hit.point;
                }
                else
                {
                    // пересекаем плоскость Y=0
                    new Plane(Vector3.up, 0).Raycast(ray, out float dist);
                    p = ray.GetPoint(dist);
                }

                var h = _terrain.MapHalfWorld;
                p.x = Mathf.Clamp(p.x, -h, h);
                p.z = Mathf.Clamp(p.z, -h, h);

                p.x = Mathf.Round(p.x / CELL_STEP) * CELL_STEP;
                p.z = Mathf.Round(p.z / CELL_STEP) * CELL_STEP;

                _previewInstance.transform.position =
                    new Vector3(p.x, _previewInstance.transform.position.y, p.z);
            }

            // Вращение
            if (rotating)
            {
                float rotDelta = Input.GetAxis("Mouse X") * 5f;
                _previewInstance.transform.Rotate(Vector3.up, rotDelta, Space.World);

                Vector3 e = _previewInstance.transform.eulerAngles;
                e.y = Mathf.Round(e.y / ROTATE_STEP) * ROTATE_STEP;
                _previewInstance.transform.eulerAngles = e;
            }

            // Масштабирование
            if (scaling)
            {
                float delta = Input.GetAxis("Mouse Y") * 0.02f;
                _currentScaleFactor = Mathf.Clamp
                (
                    _currentScaleFactor + delta,
                    _activeElement.minScale,
                    _activeElement.maxScale
                );
                _previewInstance.transform.localScale = _origPreviewScale * _currentScaleFactor;
            }

            if (heighting)
            {
                float dyRaw = Input.GetAxis("Mouse Y") * CELL_STEP;
                Vector3 pos = _previewInstance.transform.position;
                float newYraw = pos.y + dyRaw;
                float newY = Mathf.Round(newYraw / CELL_STEP) * CELL_STEP;
                _previewInstance.transform.position = new Vector3(pos.x, newY, pos.z);
            }
        }

        private void HandlePlacement()
        {
            if (_activeElement == null || _previewInstance == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.R) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.H))
            {
                RemoveFinishInstanceIfExisting();

                GameObject obj = Instantiate(_activeElement.prefab);
                obj.transform.position = _previewInstance.transform.position;
                obj.transform.rotation = _previewInstance.transform.rotation;
                obj.transform.localScale = _previewInstance.transform.localScale;

                var placedObject = new PlacedObject(obj, _activeElement);

                _placedObjects.Add(placedObject);

                _undoRedo.AddAction(new PlaceAction(placedObject));
            }
        }

        private void RemoveFinishInstanceIfExisting()
        {
            if (_activeElement.name != ElementNameConst.FINISH_INSTANCE_NAME)
                return;

            var obj = _placedObjects.FirstOrDefault(o => o.data.name == ElementNameConst.FINISH_INSTANCE_NAME);
            if (obj != null)
            {
                Destroy(obj.instance);
                _placedObjects.Remove(obj);
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
