using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Controllers
{
    public class MainLogUIController : MonoBehaviour
    {
        [SerializeField] private GameObject playerLogPanel;
        [SerializeField] private GameObject gameLogPanel;
        [SerializeField] private Button playerLogButton;
        [SerializeField] private Button gameLogButton;

        private void Awake()
        {
            gameLogPanel.SetActive(false);
            playerLogPanel.SetActive(false);
            playerLogButton.onClick.AddListener(() =>
            {
                gameLogPanel.SetActive(false);
                playerLogPanel.SetActive(true);
            });
            gameLogButton.onClick.AddListener(() =>
            {
                gameLogPanel.SetActive(true);
                playerLogPanel.SetActive(false);
            });
        }
    }
}
