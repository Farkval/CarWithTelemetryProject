using Assets.Scripts.MapEditor.Consts;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Контроллер главного меню
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public void OnStartButtonPressed()
    {
        SceneManager.LoadScene(SceneNameConst.GAME_SCENE);
    }

    public void OnLoadMapEditorScenePressed()
    {
        SceneManager.LoadScene(SceneNameConst.MAP_EDITOR_SCENE);
    }

    public void OnSettingsButtonPressed()
    {
        Debug.Log("Найстроки");
    }

    public void OnExitButtonPressed()
    {
        Application.Quit();
    }
}
