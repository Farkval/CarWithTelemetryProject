using Assets.Scripts.Game.Controllers;
using Assets.Scripts.Game.Models;
using UnityEngine;

namespace Assets.Scripts.Game.Triggers
{
    [RequireComponent(typeof(Collider))]
    public class FinishTrigger : MonoBehaviour
    {
        private GameController? _gameController;

        void Awake() 
        { 
            GetComponent<Collider>().isTrigger = true;
            _gameController = FindFirstObjectByType<GameController>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var p = other.GetComponentInParent<PlayerIdentifier>();
                if (p != null && _gameController != null && _gameController.GameStarted)
                {
                    Utils.Logger.Log($"{p.Name} пересек финишную точку в {_gameController.GameEllapsedTime}", true);
                    _gameController.StopRobot(p.Name);
                }
            }
        }
    }
}
