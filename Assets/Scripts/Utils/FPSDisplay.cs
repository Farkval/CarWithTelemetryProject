using TMPro;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class FPSDisplay : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;

        private float deltaTime = 0.0f;

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            float fps = 1.0f / deltaTime;

            fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
        }
    }

}
