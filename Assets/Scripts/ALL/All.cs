using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.ALL
{
    [RequireComponent(typeof(Rigidbody))]
    public class FourWheelsCarController : MonoBehaviour
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
        public float reverseSpeedLimit = 60f;
        [Tooltip("Сила тормозного момента, при нажатии 'тормоза' или при сбросе газа")]
        public float brakeTorque = 2000f;
        [Tooltip("Высота, на которую поднимается машина при сбросе (Reset)")]
        public float resetHeight = 1f;
        [Header("Friction Settings")]
        [Tooltip("Множитель трения (скольжения) для всех колёс. Чем меньше, тем более скользящая дорога.")]
        [Range(0f, 2f)] public float globalFrictionMultiplier = 1f;
        [Tooltip("Дополнительный множитель трения при разгоне (ForwardFriction).")]
        [Range(0f, 2f)] public float forwardFrictionMultiplier = 1f;
        [Tooltip("Дополнительный множитель трения при поворотах/скольжении (SidewaysFriction).")]
        [Range(0f, 2f)] public float sidewaysFrictionMultiplier = 1f;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Rigidbody _rb;
        private float _currentSpeed;
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
            float verticalInput = Input.GetAxis("Vertical");
            float horizontalInput = Input.GetAxis("Horizontal");
            ApplySteering(horizontalInput);
            _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;
            ApplyMotorTorque(verticalInput);
            CapSpeed();
            UpdateWheelFriction();
            UpdateWheelMeshes();
            Debug.Log($"{frontLeftWheel.isGrounded}|{frontRightWheel.isGrounded}|{rearLeftWheel.isGrounded}|{rearRightWheel.isGrounded}");
        }
        private void ApplySteering(float steerInput)
        {
            float steerAngle = maxSteeringAngle * steerInput;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;
        }
        private void ApplyMotorTorque(float verticalInput)
        {
            // Сброс всех тормозных моментов
            frontLeftWheel.brakeTorque = 0f;
            frontRightWheel.brakeTorque = 0f;
            rearLeftWheel.brakeTorque = 0f;
            rearRightWheel.brakeTorque = 0f;
            float velocityZ = transform.InverseTransformDirection(_rb.linearVelocity).z;
            bool isTryingToReverseWhileMovingForward = verticalInput < 0f && velocityZ > 1f;
            bool isTryingToDriveForwardWhileMovingBackward = verticalInput > 0f && velocityZ < -1f;
            if (isTryingToReverseWhileMovingForward || isTryingToDriveForwardWhileMovingBackward)
            {
                ApplyBrake(brakeTorque);
                return;
            }
            if (Input.GetKey(KeyCode.Space))
            {
                ApplyBrake(brakeTorque * 1.5f);
                return;
            }
            float motor = maxMotorTorque * verticalInput;
            float speedLimit = verticalInput >= 0f ? forwardSpeedLimit : reverseSpeedLimit;
            if (_currentSpeed < speedLimit)
            {
                switch (driveType)
                {
                    case CarDriveType.FrontWheelDrive:
                        frontLeftWheel.motorTorque = motor;
                        frontRightWheel.motorTorque = motor;
                        break;
                    case CarDriveType.RearWheelDrive:
                        rearLeftWheel.motorTorque = motor;
                        rearRightWheel.motorTorque = motor;
                        break;
                    case CarDriveType.AllWheelDrive:
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
                frontLeftWheel.motorTorque = 0f;
                frontRightWheel.motorTorque = 0f;
                rearLeftWheel.motorTorque = 0f;
                rearRightWheel.motorTorque = 0f;
            }
        }
        private void ApplyBrake(float torque)
        {
            frontLeftWheel.motorTorque = 0f;
            frontRightWheel.motorTorque = 0f;
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
            frontLeftWheel.brakeTorque = torque;
            frontRightWheel.brakeTorque = torque;
            rearLeftWheel.brakeTorque = torque;
            rearRightWheel.brakeTorque = torque;
        }
        private void CapSpeed()
        {
            if (_currentSpeed > maxSpeed)
            {
                _rb.AddForce(-_rb.linearVelocity.normalized * 50f);
            }
        }
        private void UpdateWheelFriction()
        {
            WheelFrictionCurve fF = frontLeftWheel.forwardFriction;
            fF.asymptoteValue = _frontLeftForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _frontLeftForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            frontLeftWheel.forwardFriction = fF;
            WheelFrictionCurve sF = frontLeftWheel.sidewaysFriction;
            sF.asymptoteValue = _frontLeftSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _frontLeftSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            frontLeftWheel.sidewaysFriction = sF;
            fF = frontRightWheel.forwardFriction;
            fF.asymptoteValue = _frontRightForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _frontRightForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            frontRightWheel.forwardFriction = fF;
            sF = frontRightWheel.sidewaysFriction;
            sF.asymptoteValue = _frontRightSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _frontRightSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            frontRightWheel.sidewaysFriction = sF;
            fF = rearLeftWheel.forwardFriction;
            fF.asymptoteValue = _rearLeftForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _rearLeftForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            rearLeftWheel.forwardFriction = fF;
            sF = rearLeftWheel.sidewaysFriction;
            sF.asymptoteValue = _rearLeftSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _rearLeftSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            rearLeftWheel.sidewaysFriction = sF;
            fF = rearRightWheel.forwardFriction;
            fF.asymptoteValue = _rearRightForwardFrictionDefault.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _rearRightForwardFrictionDefault.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            rearRightWheel.forwardFriction = fF;
            sF = rearRightWheel.sidewaysFriction;
            sF.asymptoteValue = _rearRightSidewaysFrictionDefault.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _rearRightSidewaysFrictionDefault.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            rearRightWheel.sidewaysFriction = sF;
        }
        public void SetSurfaceFriction(float newGlobalFriction, float newForwardMultiplier, float newSidewaysMultiplier)
        {
            globalFrictionMultiplier = newGlobalFriction;
            forwardFrictionMultiplier = newForwardMultiplier;
            sidewaysFrictionMultiplier = newSidewaysMultiplier;
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
        }
        public float GetSpeed()
        {
            return _currentSpeed;
        }
        public Vector3 GetPosition()
        {
            return transform.position;
        }
        public float GetRotationAngle()
        {
            return transform.eulerAngles.y;
        }
        public void ResetCarPosition()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            Vector3 newPos = new Vector3(_startPosition.x, resetHeight, _startPosition.z);
            transform.SetPositionAndRotation(newPos, _startRotation);
        }
        public CarDriveType GetCurrentDriveType()
        {
            return driveType;
        }
    }
    public interface ILidarSensor
    {
        void Initialize();
        void PerformScan();
        float GetNearestDistance();
        List<LidarPoint> GetPointCloud();
    }
    public struct LidarPoint
    {
        public Vector3 WorldPosition;
        public float Distance;
        public LidarPoint(Vector3 pos, float dist)
        {
            WorldPosition = pos;
            Distance = dist;
        }
    }
    public class FlashLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        public float maxDistance = 50f;
        public float horizontalFOV = 60f;
        public float verticalFOV = 30f;
        [Tooltip("Горизонтальное разрешение (кол-во лучей по горизонтали).")]
        public int horizontalResolution = 32;
        [Tooltip("Вертикальное разрешение (кол-во лучей по вертикали).")]
        public int verticalResolution = 16;
        [Tooltip("Слой, по которому стреляют лучи.")]
        public LayerMask layerMask = ~0;
        [Tooltip("Частота кадров/сканов в секунду.")]
        public float scanFrequency = 5f;
        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        private float _scanTimer = 0f;
        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _scanTimer = 0f;
        }
        private void Start()
        {
            Initialize();
        }
        private void Update()
        {
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 1f / scanFrequency)
            {
                _scanTimer = 0f;
                PerformScan();
            }
        }
        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            Vector3 origin = transform.position;

            for (int h = 0; h < horizontalResolution; h++)
            {
                float hPercent = (float)h / (horizontalResolution - 1);
                float hAngle = Mathf.Lerp(-horizontalFOV / 2f, horizontalFOV / 2f, hPercent);

                for (int v = 0; v < verticalResolution; v++)
                { 
                    float vPercent = (float)v / (verticalResolution - 1);
                    float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);
                    Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0f);
                    Vector3 direction = transform.rotation * rotation * Vector3.forward;
                    if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                    {
                        float dist = hit.distance;
                        LidarPoint pt = new LidarPoint(hit.point, dist);
                        _pointCloud.Add(pt);

                        if (dist < _nearestDistance)
                        {
                            _nearestDistance = dist;
                        }
                    }
                }
            }
        }
        public float GetNearestDistance()
        {
            return _nearestDistance;
        }
        public List<LidarPoint> GetPointCloud()
        {
            return _pointCloud;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
    public class MechanicalLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        [Tooltip("Максимальная дальность, на которой лидар регистрирует объекты.")]
        public float maxDistance = 100f;
        [Tooltip("Вертикальный угол обзора (сколько \"лучей\" будет формироваться по вертикали).")]
        public float verticalFOV = 30f;
        [Tooltip("Число \"линий\" сканирования по вертикали. Например, 16, 32 и т.д.")]
        public int verticalResolution = 16;
        [Tooltip("Частота вращения лидара (градусов в секунду).")]
        public float rotationSpeed = 30f;
        [Tooltip("Частота сканирования (обновление за секунду). При слишком высокой нужно оптимизировать код.")]
        public float scanFrequency = 10f;
        [Tooltip("Слой, по которому стреляют лучи. Лучше выделить отдельные слои для объектов окружения.")]
        public LayerMask layerMask = ~0;
        private float _currentRotationAngle = 0f;
        private float _scanTimer = 0f;
        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _currentRotationAngle = 0f;
            _scanTimer = 0f;
        }
        private void Start()
        {
            Initialize();
        }
        private void Update()
        {
            _currentRotationAngle += rotationSpeed * Time.deltaTime;
            if (_currentRotationAngle >= 360f) _currentRotationAngle -= 360f;
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 1f / scanFrequency)
            {
                _scanTimer = 0f;
                PerformScan();
            }
        }
        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            Quaternion baseRotation = Quaternion.Euler(0f, _currentRotationAngle, 0f);
            for (int i = 0; i < verticalResolution; i++)
            {
                float vPercent = (float)i / (verticalResolution - 1);
                float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);
                Quaternion verticalRot = Quaternion.Euler(vAngle, 0f, 0f);
                Quaternion rayRotation = baseRotation * verticalRot;
                Vector3 direction = rayRotation * Vector3.forward;
                Vector3 origin = transform.position;
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                {
                    float dist = hit.distance;
                    LidarPoint pt = new LidarPoint(hit.point, dist);
                    _pointCloud.Add(pt);
                    if (dist < _nearestDistance)
                    {
                        _nearestDistance = dist;
                    }
                }
            }
        }
        public float GetNearestDistance()
        {
            return _nearestDistance;
        }
        public List<LidarPoint> GetPointCloud()
        {
            return _pointCloud;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
    public class MemsLidar : MonoBehaviour, ILidarSensor
    {
        [Header("Main Settings")]
        public float maxDistance = 80f;
        public float horizontalFOV = 60f;
        public float verticalFOV = 20f;
        [Tooltip("Количество строк (вертикальное разрешение).")]
        public int verticalLines = 8;
        [Tooltip("Количество точек на каждую строку (горизонтальное разрешение).")]
        public int horizontalPointsPerLine = 32;
        [Tooltip("Частота сканирования (сколько раз в секунду мы обходим все строки).")]
        public float scanFrequency = 10f;
        [Tooltip("Слой для рейкаста.")]
        public LayerMask layerMask = ~0;
        private List<LidarPoint> _pointCloud = new List<LidarPoint>();
        private float _nearestDistance = Mathf.Infinity;
        private float _scanTimer = 0f;
        private int _currentLineIndex = 0;
        public void Initialize()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;
            _scanTimer = 0f;
            _currentLineIndex = 0;
        }
        private void Start()
        {
            Initialize();
        }
        private void Update()
        {
            _scanTimer += Time.deltaTime;
            float timePerFrame = 1f / (scanFrequency * verticalLines);
            if (_scanTimer >= timePerFrame)
            {
                _scanTimer = 0f;
                ScanSingleLine(_currentLineIndex);
                _currentLineIndex++;
                if (_currentLineIndex >= verticalLines)
                {
                    _currentLineIndex = 0;
                }
            }
        }
        private void ScanSingleLine(int lineIndex)
        {
            // Угол по вертикали для этой строки
            float vPercent = (float)lineIndex / (verticalLines - 1);
            float vAngle = Mathf.Lerp(-verticalFOV / 2f, verticalFOV / 2f, vPercent);
            if (lineIndex == 0)
            {
                _pointCloud.Clear();
                _nearestDistance = Mathf.Infinity;
            }
            for (int h = 0; h < horizontalPointsPerLine; h++)
            {
                float hPercent = (float)h / (horizontalPointsPerLine - 1);
                float hAngle = Mathf.Lerp(-horizontalFOV / 2f, horizontalFOV / 2f, hPercent);
                Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0f);
                Vector3 direction = transform.rotation * rotation * Vector3.forward;
                Vector3 origin = transform.position;
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                {
                    float dist = hit.distance;
                    LidarPoint pt = new LidarPoint(hit.point, dist);
                    _pointCloud.Add(pt);

                    if (dist < _nearestDistance)
                    {
                        _nearestDistance = dist;
                    }
                }
            }
        }
        public void PerformScan()
        {
            _pointCloud.Clear();
            _nearestDistance = Mathf.Infinity;

            for (int i = 0; i < verticalLines; i++)
            {
                ScanSingleLine(i);
            }
        }
        public float GetNearestDistance()
        {
            return _nearestDistance;
        }
        public List<LidarPoint> GetPointCloud()
        {
            return _pointCloud;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var pt in _pointCloud)
            {
                Gizmos.DrawSphere(pt.WorldPosition, 0.02f);
            }
        }
    }
    public class LidarVisualizer : MonoBehaviour
    {
        [Header("Lidar Reference")]
        public MonoBehaviour lidarComponent;
        private ILidarSensor lidarSensor;
        [Header("UI Settings")]
        public RawImage lidarImage;
        [Tooltip("Размер текстуры в пикселях (ширина и высота).")]
        public int textureSize = 128;
        [Tooltip("Максимальная дистанция лидара (должна совпадать или быть чуть больше, чем maxDistance в самом лидаре).")]
        public float maxDistance = 10f;
        [Tooltip("Какое расстояние будет \"центром\" миникарты (одна половина текстуры).")]
        public float mapExtent = 10f;
        private Texture2D tex;
        private Color[] pixels;
        private void Start()
        {
            lidarSensor = lidarComponent as ILidarSensor;
            if (lidarSensor == null)
            {
                Debug.LogError("LidarVisualizer: указан объект, не реализующий ILidarSensor!");
                enabled = false;
                return;
            }
            tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            pixels = new Color[textureSize * textureSize];
            if (lidarImage != null)
            {
                lidarImage.texture = tex;
            }
        }
        private void Update()
        {
            if (lidarSensor == null) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            List<LidarPoint> cloud = lidarSensor.GetPointCloud();
            Transform lidarTransform = (lidarComponent as MonoBehaviour).transform;

            foreach (var p in cloud)
            {
                Vector3 worldPos = p.WorldPosition;
                Vector3 localPos = lidarTransform.InverseTransformPoint(worldPos);
                float px = localPos.x;
                float pz = localPos.z;
                float dist = p.Distance;
                if (Mathf.Abs(px) > mapExtent || Mathf.Abs(pz) > mapExtent)
                    continue;
                float halfSize = textureSize / 2f;
                float scale = (textureSize / 2f) / mapExtent;

                int texX = Mathf.RoundToInt(halfSize + px * scale);
                int texY = Mathf.RoundToInt(halfSize + pz * scale);
                if (texX < 0 || texX >= textureSize || texY < 0 || texY >= textureSize)
                    continue;
                float t = Mathf.InverseLerp(0f, maxDistance, dist);
                Color colorNear = Color.red;
                Color colorMid = Color.yellow;
                Color colorFar = Color.green;
                Color c;
                if (t < 0.5f)
                {
                    float localT = t / 0.5f;  // нормируем на [0..1]
                    c = Color.Lerp(colorNear, colorMid, localT);
                }
                else
                {
                    float localT = (t - 0.5f) / 0.5f;
                    c = Color.Lerp(colorMid, colorFar, localT);
                }
                int index = texY * textureSize + texX;
                pixels[index] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply(false);
        }
    }
    public class CarCameraSwitch : MonoBehaviour
    {
        public Camera thirdPersonCamera;
        public Camera topDownCamera;
        public Camera frontalCamera;
        private List<Camera> _cameras = new List<Camera>();
        private int _currentCameraIndex = -1;
        private void Start()
        {
            _cameras.AddRange(new List<Camera>() { thirdPersonCamera, topDownCamera, frontalCamera });
            SwitchCamera();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SwitchCamera();
            }
        }
        private void SwitchCamera()
        {
            _currentCameraIndex++;
            if (_currentCameraIndex >= _cameras.Count)
                _currentCameraIndex = 0;
            for (int i = 0; i < _cameras.Count; i++)
            {
                _cameras[i].enabled = (i == _currentCameraIndex);
            }
        }
    }
    public class MiniMapFollow : MonoBehaviour
    {
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
    public class MainMenuManager : MonoBehaviour
    {
        public void OnStartButtonPressed()
        {
            SceneManager.LoadScene(SceneName.GAME_SCENE);
        }
        public void OnSettingsButtonPressed()
        {
            Debug.Log("Найстроки");
        }
        public void OnExitButtonPressed()
        {
            Application.Quit();
        }
    }
    public class SceneName
    {
        public const string MAIN_MENU_SCENE = "MainMenuScene";
        public const string GAME_SCENE = "GameScene";
    }
    public class UIManager : MonoBehaviour
    {
        [Header("Car Reference")]
        public FourWheelsCarController carController;
        [Header("UI Elements")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI coordinatesText;
        public TextMeshProUGUI rotationText;
        public Button restartButton;
        private void Start() { }
        private void Update()
        {
            if (carController == null)
                return;
            float speed = carController.GetSpeed();
            Vector3 pos = carController.GetPosition();
            float angle = carController.GetRotationAngle();
            speedText.text = "Speed: " + speed.ToString("F2") + " m/s";
            coordinatesText.text = $"X: {pos.x:F2}, Z: {pos.z:F2}";
            rotationText.text = $"Angle: {angle:F1}°";
        }
    }
}
