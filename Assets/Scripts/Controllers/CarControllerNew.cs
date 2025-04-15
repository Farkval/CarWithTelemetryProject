using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerNew : MonoBehaviour
{
    public enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes (Optional)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    [Tooltip("Какой тип привода у автомобиля")]
    public CarDriveType driveType = CarDriveType.RearWheelDrive;

    [Tooltip("Максимальный крутящий момент, подаваемый на колеса (Н·м)")]
    public float maxMotorTorque = 1500f;

    [Tooltip("Максимальный угол поворота управляемых колёс (в градусах)")]
    public float maxSteeringAngle = 30f;

    [Tooltip("Максимально допустимая скорость (км/ч)")]
    public float maxSpeed = 180f;

    [Tooltip("Лимит скорости при движении вперёд, км/ч (может отличаться от maxSpeed, чтобы задать безопасный предел)")]
    public float forwardSpeedLimit = 120f;

    [Tooltip("Лимит скорости при движении назад, км/ч")]
    public float reverseSpeedLimit = 30f;

    [Tooltip("Сила тормозного момента, при нажатии 'тормоза' или при сбросе газа")]
    public float brakeTorque = 2000f;

    [Tooltip("Высота, на которую поднимается машина при сбросе (Reset)")]
    public float resetHeight = 1f;

    // Параметры для управления фрикцией (скольжением)
    [Header("Friction Settings")]
    [Tooltip("Множитель трения (скольжения) для всех колёс. Чем меньше, тем более скользящая дорога.")]
    [Range(0f, 2f)] public float globalFrictionMultiplier = 1f;

    [Tooltip("Дополнительный множитель трения при разгоне (ForwardFriction).")]
    [Range(0f, 2f)] public float forwardFrictionMultiplier = 1f;

    [Tooltip("Дополнительный множитель трения при поворотах/скольжении (SidewaysFriction).")]
    [Range(0f, 2f)] public float sidewaysFrictionMultiplier = 1f;

    // Храним стартовые позиции для сброса
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private Rigidbody _rb;
    private float _currentSpeed;  // текущая скорость, км/ч

    // Для сохранения исходных фрикционных кривых каждого колеса:
    private WheelFrictionCurve _frontLeftForwardFrictionDefault;
    private WheelFrictionCurve _frontLeftSidewaysFrictionDefault;
    private WheelFrictionCurve _frontRightForwardFrictionDefault;
    private WheelFrictionCurve _frontRightSidewaysFrictionDefault;
    private WheelFrictionCurve _rearLeftForwardFrictionDefault;
    private WheelFrictionCurve _rearLeftSidewaysFrictionDefault;
    private WheelFrictionCurve _rearRightForwardFrictionDefault;
    private WheelFrictionCurve _rearRightSidewaysFrictionDefault;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _startPosition = transform.position;
        _startRotation = transform.rotation;

        // Сохраняем дефолтные кривые фрикции для каждого колеса, чтобы потом удобно управлять ими
        _frontLeftForwardFrictionDefault = frontLeftWheel.forwardFriction;
        _frontLeftSidewaysFrictionDefault = frontLeftWheel.sidewaysFriction;

        _frontRightForwardFrictionDefault = frontRightWheel.forwardFriction;
        _frontRightSidewaysFrictionDefault = frontRightWheel.sidewaysFriction;

        _rearLeftForwardFrictionDefault = rearLeftWheel.forwardFriction;
        _rearLeftSidewaysFrictionDefault = rearLeftWheel.sidewaysFriction;

        _rearRightForwardFrictionDefault = rearRightWheel.forwardFriction;
        _rearRightSidewaysFrictionDefault = rearRightWheel.sidewaysFriction;
    }

    private void FixedUpdate()
    {
        // Получаем оси ввода
        float verticalInput = Input.GetAxis("Vertical");     // W/S
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D

        //Debug.Log($"verticalInput: {verticalInput}; horizontalInput: {horizontalInput}");

        // 1) Применяем руление (угол поворота колёс)
        ApplySteering(horizontalInput);

        // 2) Рассчитываем скорость (км/ч)
        _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;

        // 3) Применяем тягу/торможение
        ApplyMotorTorque(verticalInput);

        // 4) Контроль максимальной скорости
        CapSpeed();

        // 5) Обновляем фрикцию (чтобы учитывать изменение globalFrictionMultiplier и т.д.)
        UpdateWheelFriction();

        // 6) Визуально обновляем положение/вращение мешей колёс
        UpdateWheelMeshes();

        Debug.Log($"{frontLeftWheel.isGrounded}|{frontRightWheel.isGrounded}|{rearLeftWheel.isGrounded}|{rearRightWheel.isGrounded}");
    }

    /// <summary>
    /// Применяем угол поворота для управляемых колёс (в данном примере считаем передние колёса рулевыми).
    /// Можно настроить так, чтобы и задние колёса слегка поворачивались при полном приводе.
    /// </summary>
    private void ApplySteering(float steerInput)
    {
        float steerAngle = maxSteeringAngle * steerInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        //Debug.Log($"steerAngle: {steerAngle}");
    }

    /// <summary>
    /// Применяем крутящий момент (тяга/торможение) в зависимости от типа привода.
    /// Если скорость превысила лимит, уменьшаем тягу. Также учитываем задний ход.
    /// </summary>
    private void ApplyMotorTorque(float verticalInput)
    {
        // Сбрасываем старые значения тормозного момента
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
        rearLeftWheel.brakeTorque = 0f;
        rearRightWheel.brakeTorque = 0f;

        // Если нажимаем "назад" (S) и машина движется вперёд — это торможение
        // или если нажимаем "вперёд" (W), а машина движется назад — тоже торможение.
        if ((verticalInput < 0f && _currentSpeed > 2f) ||
            (verticalInput > 0f && transform.InverseTransformDirection(_rb.linearVelocity).z < -2f))
        {
            // Применяем тормоз
            frontLeftWheel.brakeTorque = brakeTorque;
            frontRightWheel.brakeTorque = brakeTorque;
            rearLeftWheel.brakeTorque = brakeTorque;
            rearRightWheel.brakeTorque = brakeTorque;

            // При этом не подаём моторный момент
            frontLeftWheel.motorTorque = 0f;
            frontRightWheel.motorTorque = 0f;
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
            return;
        }

        float motor = maxMotorTorque * verticalInput;

        // Если движемся вперёд — не превышаем forwardSpeedLimit, если назад — reverseSpeedLimit
        bool movingForward = verticalInput > 0f && _currentSpeed < forwardSpeedLimit;
        bool movingBackward = verticalInput < 0f && _currentSpeed < reverseSpeedLimit;

        if (movingForward || movingBackward)
        {
            switch (driveType)
            {
                case CarDriveType.FrontWheelDrive:
                    frontLeftWheel.motorTorque = motor;
                    frontRightWheel.motorTorque = motor;
                    rearLeftWheel.motorTorque = 0f;
                    rearRightWheel.motorTorque = 0f;
                    break;

                case CarDriveType.RearWheelDrive:
                    rearLeftWheel.motorTorque = motor;
                    rearRightWheel.motorTorque = motor;
                    frontLeftWheel.motorTorque = 0f;
                    frontRightWheel.motorTorque = 0f;
                    break;

                case CarDriveType.AllWheelDrive:
                    // Для полного привода можно делить момент пополам на оси,
                    // либо подстраивать распределение, как нравится.
                    float halfMotor = motor * 0.5f;
                    frontLeftWheel.motorTorque = halfMotor;
                    frontRightWheel.motorTorque = halfMotor;
                    rearLeftWheel.motorTorque = halfMotor;
                    rearRightWheel.motorTorque = halfMotor;
                    break;
            }
        }
        else
        {
            // Скорость достигла лимита, значит моторный момент = 0, чтобы не разгоняться дальше
            frontLeftWheel.motorTorque = 0f;
            frontRightWheel.motorTorque = 0f;
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
        }

        //Debug.Log($"frontLeftWheel.motorTorque: {frontLeftWheel.motorTorque}\nfrontRightWheel.motorTorque: {frontRightWheel.motorTorque}\nrearLeftWheel.motorTorque: {rearLeftWheel.motorTorque}\nrearRightWheel.motorTorque: {rearRightWheel.motorTorque}");
    }

    /// <summary>
    /// Ограничение скорости (перестраховка, если, например, автомобиль разогнался под горку).
    /// </summary>
    private void CapSpeed()
    {
        // Если скорость выше maxSpeed, снижаем скорость (не даём машине разогнаться сверх лимита).
        // Можно сделать более плавно: напр. внедрить какой-то drag или дополнительный тормоз.
        if (_currentSpeed > maxSpeed)
        {
            // Небольшая "хитрость": прибавим drag
            _rb.AddForce(-_rb.linearVelocity.normalized * 50f);
        }
    }

    /// <summary>
    /// Обновляем фрикционные кривые колёс, используя глобальные мультипликаторы.
    /// Это упрощённый метод, который масштабирует исходные значения трения.
    /// </summary>
    private void UpdateWheelFriction()
    {
        // Обновляем переднее левое колесо
        WheelFrictionCurve fF = frontLeftWheel.forwardFriction;
        fF.asymptoteValue = _frontLeftForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        fF.extremumValue = _frontLeftForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        frontLeftWheel.forwardFriction = fF;

        WheelFrictionCurve sF = frontLeftWheel.sidewaysFriction;
        sF.asymptoteValue = _frontLeftSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        sF.extremumValue = _frontLeftSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        frontLeftWheel.sidewaysFriction = sF;

        // Переднее правое колесо
        fF = frontRightWheel.forwardFriction;
        fF.asymptoteValue = _frontRightForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        fF.extremumValue = _frontRightForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        frontRightWheel.forwardFriction = fF;

        sF = frontRightWheel.sidewaysFriction;
        sF.asymptoteValue = _frontRightSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        sF.extremumValue = _frontRightSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        frontRightWheel.sidewaysFriction = sF;

        // Заднее левое колесо
        fF = rearLeftWheel.forwardFriction;
        fF.asymptoteValue = _rearLeftForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        fF.extremumValue = _rearLeftForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        rearLeftWheel.forwardFriction = fF;

        sF = rearLeftWheel.sidewaysFriction;
        sF.asymptoteValue = _rearLeftSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        sF.extremumValue = _rearLeftSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        rearLeftWheel.sidewaysFriction = sF;

        // Заднее правое колесо
        fF = rearRightWheel.forwardFriction;
        fF.asymptoteValue = _rearRightForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        fF.extremumValue = _rearRightForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
        rearRightWheel.forwardFriction = fF;

        sF = rearRightWheel.sidewaysFriction;
        sF.asymptoteValue = _rearRightSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        sF.extremumValue = _rearRightSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
        rearRightWheel.sidewaysFriction = sF;
    }

    /// <summary>
    /// Пример метода, который вы можете вызывать в коде или триггерах
    /// (напр., когда колеса заезжают на лёд). Позволяет задать снижение фрикции.
    /// </summary>
    public void SetSurfaceFriction(float newGlobalFriction, float newForwardMultiplier, float newSidewaysMultiplier)
    {
        globalFrictionMultiplier = newGlobalFriction;
        forwardFrictionMultiplier = newForwardMultiplier;
        sidewaysFrictionMultiplier = newSidewaysMultiplier;
        // При следующем FixedUpdate оно применится через UpdateWheelFriction()
    }

    private void UpdateWheelMeshes()
    {
        float frontLeftSteer = frontLeftWheel.steerAngle;
        float frontRightSteer = frontRightWheel.steerAngle;

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

        // Остальные колёса можно не трогать или добавить по вкусу
    }

    /// <summary>
    /// Текущая скорость, км/ч
    /// </summary>
    public float GetSpeed()
    {
        return _currentSpeed;
    }

    /// <summary>
    /// Текущие координаты машины
    /// </summary>
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Угол поворота машины вокруг оси Y
    /// </summary>
    public float GetRotationAngle()
    {
        return transform.eulerAngles.y;
    }

    /// <summary>
    /// Рестарт машины в начальную позицию с обнулением скорости.
    /// </summary>
    public void ResetCarPosition()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        Vector3 newPos = new Vector3(_startPosition.x, resetHeight, _startPosition.z);
        transform.SetPositionAndRotation(newPos, _startRotation);
    }

    /// <summary>
    /// Возвращает текущий тип привода (для интерфейсов и т.д.).
    /// </summary>
    public CarDriveType GetCurrentDriveType()
    {
        return driveType;
    }
}
