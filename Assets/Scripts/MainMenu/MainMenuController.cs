using Assets.Scripts.MapEditor.Consts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MainMenu
{
    /// <summary>
    /// Контроллер главного меню
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public void OnPlayButtonPressed()
        {
            SceneManager.LoadScene(SceneNameConst.GAME_SCENE);
        }

        public void OnLoadMapEditorScenePressed()
        {
            SceneManager.LoadScene(SceneNameConst.MAP_EDITOR_SCENE);
        }

        public void OnCarSettingsButtonPressed()
        {
            SceneManager.LoadScene(SceneNameConst.GARAGE_SCENE);
        }

        public void OnExitButtonPressed()
        {
            Application.Quit();
        }
    }
}
