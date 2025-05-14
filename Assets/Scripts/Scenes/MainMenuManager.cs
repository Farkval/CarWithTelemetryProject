using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Контроллер главного меню
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public void OnStartButtonPressed()
    {
        SceneManager.LoadScene(SceneName.GAME_SCENE);
    }

    public void OnLoadMapEditorScenePressed()
    {
        SceneManager.LoadScene(SceneName.MAP_EDITOR_SCENE);
    }

    public void OnSettingsButtonPressed()
    {
        // NOTE: можно открыть новую сцену с некоторыми настройками, звук, чувствительность и т.п.
        Debug.Log("Найстроки");
    }

    public void OnExitButtonPressed()
    {
        Application.Quit();
    }
}
