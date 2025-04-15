using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Обработчик хоткеев во время игры
/// </summary>
public class GameKeyboardHandler : MonoBehaviour
{
    public CarControllerNew carController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(SceneName.MAIN_MENU_SCENE);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (carController == null)
                return;

            carController.ResetCarPosition();
        }
    }
}
