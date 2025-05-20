using Assets.Scripts.MapEditor.Consts;
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
            Debug.Log("Триггер");
            if (other.CompareTag("Player"))
            {
                Debug.Log("Триггер на игрока");
                SceneManager.LoadScene(SceneNameConst.MAIN_MENU_SCENE);
            }
        }
    }
}
