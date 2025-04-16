using UnityEngine;

/// <summary>
/// Контроллер камеры мини карты
/// </summary>
public class MiniMapFollow : MonoBehaviour
{
    /// <summary>
    /// Transform тачки
    /// </summary>
    public Transform target;

    private Vector3 offset;

    private void Start()
    {
        offset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        if (target == null) 
            return;

        Vector3 newPosition = target.position + offset;
        newPosition.y = transform.position.y;
        
        transform.SetPositionAndRotation(newPosition, Quaternion.Euler(90f, target.eulerAngles.y, 180f));
    }
}
