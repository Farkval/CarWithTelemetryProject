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
            // Считаем среднее время между кадрами
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            // Преобразуем в FPS
            float fps = 1.0f / deltaTime;

            // Обновляем текст
            fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
        }
    }

}
