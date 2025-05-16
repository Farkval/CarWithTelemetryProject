using Assets.Scripts.MapEditor.Consts;
using Assets.Scripts.Robot;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Game.Controllers
{
    /// <summary>
    /// Обработчик хоткеев во время игры
    /// </summary>
    public class GameKeyboardHandler : MonoBehaviour
    {
        public FourWheelsCarController carController;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (carController == null)
                    return;

                carController.ResetCar();
            }
        }
    }
}
