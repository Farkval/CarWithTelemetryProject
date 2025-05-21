using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class DayNightController : MonoBehaviour
    {
        [Header("Lights")]
        [Tooltip("Directional Light для солнца")]
        public Light sunLight;
        [Tooltip("Directional Light для луны")]
        public Light moonLight;

        [Header("Sun Settings")]
        public Color morningSunColor = new Color(1f, 0.8f, 0.6f);  // тёплый
        public Color daySunColor = Color.white;               // холодный дневной
        public Color eveningSunColor = new Color(1f, 0.5f, 0.3f);  // оранжево-красный

        public float morningIntensity = 0.8f;
        public float dayIntensity = 1.2f;
        public float eveningIntensity = 0.6f;

        public Vector3 morningRotation = new Vector3(30f, 30f, 0f);
        public Vector3 dayRotation = new Vector3(50f, 0f, 0f);
        public Vector3 eveningRotation = new Vector3(20f, 200f, 0f);

        [Header("Moon Settings")]
        public Color nightMoonColor = new Color(0.5f, 0.6f, 1f);
        public float nightMoonIntensity = 0.2f;
        public Vector3 nightRotation = new Vector3(340f, 150f, 0f);

        [Header("Ambient Settings")]
        public Color morningAmbient = new Color(0.5f, 0.4f, 0.3f);
        public Color dayAmbient = new Color(0.6f, 0.6f, 0.6f);
        public Color eveningAmbient = new Color(0.4f, 0.3f, 0.35f);
        public Color nightAmbient = new Color(0.1f, 0.1f, 0.2f);

        [Header("Optional Skyboxes")]
        public Material daySkybox;
        public Material nightSkybox;

        /// <summary>
        /// Вызывается при изменении значения dropdown
        /// </summary>
        public void OnTimeChanged(int dropdownIndex)
        {
            ApplySettings(dropdownIndex);
        }

        private void ApplySettings(int index)
        {
            // Скрываем/показываем moonLight
            moonLight.enabled = (index == 3);

            switch (index)
            {
                case 0: // Утро
                    sunLight.enabled = true;
                    sunLight.color = morningSunColor;
                    sunLight.intensity = morningIntensity;
                    sunLight.transform.rotation = Quaternion.Euler(morningRotation);
                    RenderSettings.ambientLight = morningAmbient;
                    if (daySkybox != null) RenderSettings.skybox = daySkybox;
                    break;

                case 1: // День
                    sunLight.enabled = true;
                    sunLight.color = daySunColor;
                    sunLight.intensity = dayIntensity;
                    sunLight.transform.rotation = Quaternion.Euler(dayRotation);
                    RenderSettings.ambientLight = dayAmbient;
                    if (daySkybox != null) RenderSettings.skybox = daySkybox;
                    break;

                case 2: // Вечер
                    sunLight.enabled = true;
                    sunLight.color = eveningSunColor;
                    sunLight.intensity = eveningIntensity;
                    sunLight.transform.rotation = Quaternion.Euler(eveningRotation);
                    RenderSettings.ambientLight = eveningAmbient;
                    if (daySkybox != null) RenderSettings.skybox = daySkybox;
                    break;

                case 3: // Ночь
                    sunLight.enabled = false;
                    moonLight.color = nightMoonColor;
                    moonLight.intensity = nightMoonIntensity;
                    moonLight.transform.rotation = Quaternion.Euler(nightRotation);
                    RenderSettings.ambientLight = nightAmbient;
                    if (nightSkybox != null) RenderSettings.skybox = nightSkybox;
                    break;
            }
        }
    }
}
