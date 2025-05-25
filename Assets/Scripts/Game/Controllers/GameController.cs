using Assets.Scripts.Game.Map;
using Assets.Scripts.MapEditor.Consts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Game.Controllers
{
    /// <summary>
    /// ���������� ������� �� ����� ����
    /// </summary>
    public class GameKeyboardHandler : MonoBehaviour
    {
        [SerializeField] MapLoader mapLoader;
        
        private List<Camera> cameras = new();
        private int _currentCameraIndex = -1;

        private void Start()
        {
            mapLoader = FindFirstObjectByType<MapLoader>();
            mapLoader.Load();

            cameras.AddRange(FindObjectsByType<Camera>(FindObjectsSortMode.None));

            ActivateNextCamera();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                var rb = mapLoader.carPrefab.GetComponent<Rigidbody>();
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                var spawnPos = mapLoader.spawnPos;
                var spawnRot = mapLoader.spawnRot;

                mapLoader.carPrefab.transform.SetPositionAndRotation(new Vector3(spawnPos.x, spawnPos.y + 1, spawnPos.z), spawnRot);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                ActivateNextCamera();
            }
        }

        private void ActivateNextCamera()
        {
            _currentCameraIndex++;

            if (_currentCameraIndex >= cameras.Count)
                _currentCameraIndex = 0;

            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].enabled = i == _currentCameraIndex;
            }
        }
    }
}
