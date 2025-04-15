using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerOld : MonoBehaviour
{
    [Header("Wheel Colliders")]
    // Коллайдеры колёс: передние и задние
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Car Settings")]
    public float maxMotorTorque = 1000f;   // максимальный крутящий момент колеса
    public float maxSteeringAngle = 30f;   // максимальный угол поворота колеса
    public float currentSpeedLimit = 80f;  // лимит скорости км/ч
    public float _resetHeight = 1f;        // высота сброса с респы

    [Header("Wheel Meshes (Optional)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private float _currentSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Запомнили точку старта
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        float verticalInput = Input.GetAxis("Vertical");     // W/S
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D

        // угол поворота колёс
        float steerAngle = maxSteeringAngle * horizontalInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // скорость машины (м/с * 3.6 = км/ч)
        _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;

        // если не превысили скорость и не едем назад
        if (_currentSpeed < currentSpeedLimit || verticalInput < 0f)
        {
            float motor = maxMotorTorque * verticalInput;

            // Подаём момент на задние колёса
            rearLeftWheel.motorTorque = motor;
            rearRightWheel.motorTorque = motor;
        }
        else
        {
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
        }

        // Визуализация поворота
        UpdateWheelMeshes();
    }

    private void UpdateWheelMeshes()
    {
        float frontLeftSteer = frontLeftWheel.steerAngle;
        float frontRightSteer = frontRightWheel.steerAngle;

        // поворачиваем колеса
        if (frontLeftMesh != null)
        {
            Vector3 euler = frontLeftMesh.localEulerAngles;
            euler.y = frontLeftSteer;
            frontLeftMesh.localEulerAngles = euler;
        }
        if (frontRightMesh != null)
        {
            Vector3 euler = frontRightMesh.localEulerAngles;
            euler.y = frontRightSteer;
            frontRightMesh.localEulerAngles = euler;
        }
    }

    /// <summary>
    /// Текущая скорость км/ч
    /// </summary>
    /// <returns></returns>
    public float GetSpeed()
    {
        return _currentSpeed;
    }

    /// <summary>
    /// Текущие координаты
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Текущая ориентация
    /// </summary>
    /// <returns></returns>
    public float GetRotationAngle()
    {
        return transform.eulerAngles.y;
    }

    /// <summary>
    /// Рестарт тачки
    /// </summary>
    public void ResetCarPosition()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.SetPositionAndRotation(new Vector3(_startPosition.x, _resetHeight, _startPosition.z), _startRotation);
    }
}