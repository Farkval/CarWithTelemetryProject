using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MapEditor
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
                SceneManager.LoadScene("MainMenu");
        }
    }
}
