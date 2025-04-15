using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Car Reference")]
    public CarControllerNew carController;

    [Header("UI Elements")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI coordinatesText;
    public TextMeshProUGUI rotationText;
    public Button restartButton;

    private void Start()
    {
    }

    private void Update()
    {
        if (carController == null) 
            return;

        // Апдейт мониторинговых данных тачки
        float speed = carController.GetSpeed();
        Vector3 pos = carController.GetPosition();
        float angle = carController.GetRotationAngle();

        speedText.text = "Speed: " + speed.ToString("F2") + " m/s";
        coordinatesText.text = $"X: {pos.x:F2}, Z: {pos.z:F2}";
        rotationText.text = $"Angle: {angle:F1}°";
    }
}
