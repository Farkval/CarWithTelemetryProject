using UnityEngine;

namespace Assets.Scripts.Game.Triggers
{
    [RequireComponent(typeof(Collider))]
    public class FinishTrigger : MonoBehaviour
    {
        void Awake() 
        { 
            GetComponent<Collider>().isTrigger = true; 
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {

            }
        }
    }
}
