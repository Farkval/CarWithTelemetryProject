using Assets.Scripts.MapEditor.Actions;
using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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

        private ElementData _activeElement;
        private GameObject _previewInstance;
        private GameObject _spawnInstance, _finishInstance;

        private readonly UndoRedoController _undoRedo = new UndoRedoController(100);
        private readonly List<PlacedObject> _placedObjects = new();
        private Vector3 _origPreviewScale = Vector3.one;
        private float _currentScaleFactor = 1f;
        private Dictionary<Renderer, Color> _original = new();
        private PlacedObject _dragObj;           // объект, который сейчас тянут
        private Coroutine _dragRoutine;       // сама корутина-перетаскивания
        private Vector3 _beforePos, _beforeRot, _beforeScale;

        public UndoRedoController UndoRedoManager { get { return _undoRedo; } }

        private void Update()
        {
            HandleSelection();
            HandlePreview();
            HandlePlacement();
            HandleUndoRedo();
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
            var res =_undoRedo.Redo();
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
                var mm = FindFirstObjectByType<MapController>();
                var index = Enum.GetValues(typeof(MapSize));
                mm.SetMap(data.mapSize);
                mm.SetEnvironment(data.timeOfDay);

                var terr = FindFirstObjectByType<MapTerrain>();

                if (data.heights != null && data.heightRes > 0)
                    terr.ImportHeights(data.heightRes, data.heights);
                else terr.Init((int)data.mapSize);

                // покрытие
                if (data.surfaces != null && data.surfaceRes > 0)
                    terr.ImportSurfaces(data.surfaceRes, data.surfaces);

                foreach (var po in _placedObjects)
                    Destroy(po.instance);
                _placedObjects.Clear();

                foreach (var inst in data.instances)
                {
                    var ed = Resources.Load<ElementData>(inst.elementPath);
                    if (!ed)
                    {
                        Debug.LogWarning($"ElementData {inst.elementPath} not found");
                        continue;
                    }

                    GameObject obj = Instantiate(ed.prefab);
                    obj.transform.SetPositionAndRotation(inst.position, Quaternion.Euler(inst.rotation));
                    obj.transform.localScale = inst.localScale;
                    PostHandleStartFinishPoint(obj, ed);
                    _placedObjects.Add(new PlacedObject(obj, ed));
                }
            });
        }

        public void ExitCommand()
        {
            SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
        }

        public void SetActiveElement(ElementData data)
        {
            _activeElement = data;

            if (_previewInstance) Destroy(_previewInstance);

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

        void HandleSelection()
        {
            /* 1. Если уже тащим – ничего не делаем */
            if (_dragRoutine != null || _activeElement != null) return;

            /* 2. Кликнули ЛКМ? */
            if (Input.GetMouseButtonDown(0) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                if (Physics.Raycast(sceneCamera.ScreenPointToRay(Input.mousePosition),
                                    out var hit, 500f, LayerMask.GetMask("Default")))
                {
                    // ищем объект среди размещённых
                    _dragObj = _placedObjects.Find(p => p.instance == hit.collider.gameObject
                                                     || p.instance.transform.IsChildOf(hit.collider.transform));
                    if (_dragObj != null)
                    {
                        _dragRoutine = StartCoroutine(DragObjectLoop(_dragObj));
                    }
                }
            }
        }

        IEnumerator DragObjectLoop(PlacedObject po)
        {
            Transform tr = po.instance.transform;
            Highlight(po, true);             // вкл. подсветку

            // зафиксируем состояние «до»
            _beforePos = tr.position;
            _beforeRot = tr.rotation.eulerAngles;
            _beforeScale = tr.localScale;

            MapTerrain terr = FindFirstObjectByType<MapTerrain>();
            float half = terr.MapHalfWorld;      // helper свойство в MapTerrain

            bool changed = false;

            while (Input.GetMouseButton(0))
            {
                // — перемещение / поворот / масштаб — точь-в-точь как было
                if (Input.GetKey(KeyCode.R))                      // ROTATE
                {
                    float dx = Input.GetAxis("Mouse X");
                    tr.Rotate(Vector3.up, dx * 3f, Space.World);
                    changed = true;
                }
                else if (Input.GetKey(KeyCode.S))                 // SCALE
                {
                    float dy = Input.GetAxis("Mouse Y");
                    float k = 1 + dy * .01f;
                    tr.localScale *= k;
                    changed = true;
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

            _dragRoutine = null;
            _dragObj = null;
        }

        void Highlight(PlacedObject po, bool state)
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

                float h = FindFirstObjectByType<MapTerrain>().MapHalfWorld;
                p.x = Mathf.Clamp(p.x, -h, h);
                p.z = Mathf.Clamp(p.z, -h, h);

                const float STEP = .05f;                     // привязка
                p.x = Mathf.Round(p.x / STEP) * STEP;
                p.z = Mathf.Round(p.z / STEP) * STEP;

                _previewInstance.transform.position =
                    new Vector3(p.x, _previewInstance.transform.position.y, p.z);
            }

            // Вращение
            if (rotating)
            {
                float rotDelta = Input.GetAxis("Mouse X") * 5f;
                Quaternion before = _previewInstance.transform.rotation;
                _previewInstance.transform.Rotate(Vector3.up, rotDelta, Space.World);

                Vector3 e = _previewInstance.transform.eulerAngles;
                e.y = Mathf.Round(e.y / 2.5f) * 2.5f;
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
                float dy = Input.GetAxis("Mouse Y") * 0.05f;
                var pos = _previewInstance.transform.position;
                _previewInstance.transform.position = pos + Vector3.up * dy;
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
                PreHandleStartFinishPlacement();

                GameObject obj = Instantiate(_activeElement.prefab);
                obj.transform.position = _previewInstance.transform.position;
                obj.transform.rotation = _previewInstance.transform.rotation;
                obj.transform.localScale = _previewInstance.transform.localScale;

                PostHandleStartFinishPoint(obj, _activeElement);

                var placedObject = new PlacedObject(obj, _activeElement);

                _placedObjects.Add(placedObject);

                _undoRedo.AddAction(new PlaceAction(placedObject));
            }
        }

        private void PreHandleStartFinishPlacement()
        {
            if (_activeElement.name == ElementNameConst.START_INSTANCE_NAME)
            {
                Destroy(_spawnInstance);
                _placedObjects.RemoveAll(po => po.data.name == ElementNameConst.START_INSTANCE_NAME);
            }
            if (_activeElement.name == ElementNameConst.FINISH_INSTANCE_NAME)
            {
                Destroy(_finishInstance);
                _placedObjects.RemoveAll(po => po.data.name == ElementNameConst.FINISH_INSTANCE_NAME);
            }
        }

        private void PostHandleStartFinishPoint(GameObject obj, ElementData ed)
        {
            if (ed.name == ElementNameConst.START_INSTANCE_NAME)
                _spawnInstance = obj;
            if (ed.name == ElementNameConst.FINISH_INSTANCE_NAME)
                _finishInstance = obj;
        }

        private void HandleUndoRedo()
        {
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
