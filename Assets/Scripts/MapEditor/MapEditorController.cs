using Assets.Scripts.MapEditor.Consts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MapEditor
{
    /// <summary>
    /// Главный контроллер редактора – размещение объектов, предпросмотр, операции Undo/Redo.
    /// </summary>
    public class MapEditorController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Transform previewParent;
        [SerializeField] private MapSerializer serializer;

        private ElementData _activeElement;
        private GameObject _previewInstance;
        private GameObject _spawnInstance, _finishInstance;

        private readonly UndoRedoManager _undoRedo = new UndoRedoManager(100);
        private readonly List<PlacedObject> _placedObjects = new();
        private Vector3 _origPreviewScale = Vector3.one;
        private float _currentScaleFactor = 1f;

        private void Update()
        {
            HandlePreview();
            HandlePlacement();
            HandleUndoRedo();
        }

        public void UndoCommand() => _undoRedo.Undo();

        public void RedoCommand() => _undoRedo.Redo();

        public void SaveCommand()
        {
            var mm = FindFirstObjectByType<MapManager>();
            serializer.Save(_placedObjects, mm.CurrentMapMeters);
        }

        public void LoadCommand()
        {
            serializer.Load(data =>
            {
                var mm = FindFirstObjectByType<MapManager>();
                mm.SetMap(data.mapMeters);

                var terr = FindFirstObjectByType<MapTerrain>();
                if (data.heights != null && data.heights.Length > 0)
                {
                    int n = (int)Mathf.Sqrt(data.heights.Length) - 1;
                    float[,] h = new float[n + 1, n + 1];
                    Buffer.BlockCopy(data.heights, 0, h, 0, sizeof(float) * data.heights.Length);
                    terr.ImportHeights(h);
                }
                else terr.Init(data.mapMeters);   // если старая карта без рельефа

                foreach (var po in _placedObjects) Destroy(po.instance);
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
            if (_previewInstance != null)
                Destroy(_previewInstance);

            if (data != null)
            {
                _previewInstance = Instantiate(data.prefab, previewParent);
                SetLayerRecursively(_previewInstance, LayerMask.NameToLayer("Ignore Raycast"));
                _origPreviewScale = _previewInstance.transform.localScale;
                _currentScaleFactor = 1f;
                _previewInstance.transform.position = new Vector3(_previewInstance.transform.position.x, 0, _previewInstance.transform.position.z);
            }
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
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Default")))
                {
                    Vector3 point = hit.point;
                    // Привязка к ближайшему центру мелкой ячейки
                    float step = 0.05f;
                    point.x = Mathf.Round(point.x / step) * step;
                    point.z = Mathf.Round(point.z / step) * step;
                    _previewInstance.transform.position = new Vector3(point.x, _previewInstance.transform.position.y, point.z);
                }
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

            HandleStartFinishPlacement();

            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.R) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.H))
            {
                GameObject obj = Instantiate(_activeElement.prefab);
                obj.transform.position = _previewInstance.transform.position;
                obj.transform.rotation = _previewInstance.transform.rotation;
                obj.transform.localScale = _previewInstance.transform.localScale;
                if (_activeElement.displayName == ElementNameConst.SPAWN_INSTANCE_NAME) 
                    _spawnInstance = obj;
                if (_activeElement.displayName == ElementNameConst.FINISH_INSTANCE_NAME)
                    _finishInstance = obj;
                _placedObjects.Add(new PlacedObject(obj, _activeElement));

                _undoRedo.AddAction(new PlaceAction(obj, transform));
            }
        }

        private void HandleStartFinishPlacement()
        {
            if (_activeElement.displayName == ElementNameConst.SPAWN_INSTANCE_NAME && _spawnInstance)
            {
                Destroy(_spawnInstance);
                _placedObjects.RemoveAll(po => po.data.displayName == ElementNameConst.SPAWN_INSTANCE_NAME);
            }
            if (_activeElement.displayName == ElementNameConst.FINISH_INSTANCE_NAME && _finishInstance)
            {
                Destroy(_finishInstance);
                _placedObjects.RemoveAll(po => po.data.displayName == ElementNameConst.FINISH_INSTANCE_NAME);
            }
        }

        private void HandleUndoRedo()
        {
            if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
            {
                _undoRedo.Undo();
            }
            if (Input.GetKeyDown(KeyCode.Y) && Input.GetKey(KeyCode.LeftControl))
            {
                _undoRedo.Redo();
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
