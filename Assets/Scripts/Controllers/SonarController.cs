using UnityEngine;

public class SonarSensor : MonoBehaviour
{
    [Header("Настройки сонар-сенсора")]
    [Tooltip("Максимальная дистанция сканирования")]
    public float maxDistance = 10f;

    [Tooltip("Направление луча в локальных координатах")]
    public Vector3 localDirection = Vector3.forward;

    [Tooltip("Слои, по которым сенсор будет видеть препятствия")]
    public LayerMask detectionMask = ~0; // По умолчанию все

    [Tooltip("Добавлять шум?")]
    public bool addNoise = false;

    [Tooltip("Максимальная погрешность (в метрах)")]
    public float noiseRange = 0.05f;

    [Tooltip("Визуализировать луч в Gizmos?")]
    public bool showGizmos = true;

    [Tooltip("Цвет луча при попадании")]
    public Color hitColor = Color.green;

    [Tooltip("Цвет луча при отсутствии попадания")]
    public Color missColor = Color.red;

    [HideInInspector] public float measuredDistance = -1f;
    [HideInInspector] public Vector3 hitPoint;

    public void PerformScan()
    {
        Vector3 direction = transform.TransformDirection(localDirection.normalized);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, direction, out hit, maxDistance, detectionMask))
        {
            measuredDistance = hit.distance;
            hitPoint = hit.point;

            if (addNoise)
                measuredDistance += Random.Range(-noiseRange, noiseRange);
        }
        else
        {
            measuredDistance = -1f;
            hitPoint = transform.position + direction * maxDistance;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 direction = transform.TransformDirection(localDirection.normalized);
        Color color = measuredDistance >= 0 ? hitColor : missColor;

        Gizmos.color = color;
        Gizmos.DrawLine(transform.position, hitPoint);
        Gizmos.DrawWireSphere(hitPoint, 0.1f);
    }
}


