using Assets.Scripts.Consts;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Models.Enums;
using Assets.Scripts.Robot.Api.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Cars
{
    /// <summary>
    /// Контроллер шестиколёсного «Яндекс-ровера».
    /// Колёса не поворачиваются; манёвр осуществляется
    /// за счёт разницы скоростей правого и левого борта (skid-steer).
    /// Соблюдает интерфейс IRobotAPI.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class YandexRoverController : MonoBehaviour, IRobotAPI
    {
        #region ⭑ Публичные поля
        [Header("Wheel Colliders")]
        public WheelCollider leftFrontWheel, leftMiddleWheel, leftRearWheel;
        public WheelCollider rightFrontWheel, rightMiddleWheel, rightRearWheel;

        [Header("Wheel Meshes (Optional)")]
        public Transform leftFrontMesh, leftMiddleMesh, leftRearMesh;
        public Transform rightFrontMesh, rightMiddleMesh, rightRearMesh;

        [Header("Rover Settings")]
        [Tooltip("Максимальный крутящий момент на одно колесо, Н·м")]
        public float maxMotorTorque = 450f;
        [Tooltip("Желаемый верхний предел скорости, км/ч")]
        public float maxSpeed = 18f;          // ≈ 5 м/с
        public float forwardSpeedLimit = 15f; // ≈ 4 м/с
        public float reverseSpeedLimit = 10f; // ≈ 2.8 м/с
        public float brakeTorque = 1000f;

        [Header("Suspension (жёсткая)")]
        public float suspensionDistance = 0.02f;
        public float springStrength = 20000f;
        public float springDamper = 3000f;

        [Header("Friction Settings")]
        [Range(0, 2)] public float globalFrictionMultiplier = 1f;
        [Range(0, 2)] public float forwardFrictionMultiplier = 1f;
        [Range(0, 2)] public float sidewaysFrictionMultiplier = 1f;
        #endregion

        #region ⭑ IRobotAPI implementation
        public void SetMotorPower(float left, float right)
        {
            _cmdLeft = Mathf.Clamp(left, -1, 1);
            _cmdRight = Mathf.Clamp(right, -1, 1);
            ManualControl = false;
        }

        public void Brake(float power = 1f)
        {
            _brakeCmd = Mathf.Clamp01(power);
            ManualControl = false;
        }

        public float[] WheelRPM => _rpm;
        public Vector3 Position => transform.position;
        public float YawDeg => transform.eulerAngles.y;
        public List<ILidar> Lidars { get; private set; } = new();
        public bool ManualControl { get; set; } = true;
        public List<ICameraSensor> Cameras { get; private set; } = new();
        #endregion

        #region ⭑ Приватное состояние
        private Rigidbody _rb;
        private float _currentSpeed;
        private readonly float[] _rpm = new float[6];

        // Каналы команд
        private float _cmdLeft;
        private float _cmdRight;
        private float _brakeCmd;

        // Оригинальные кривые трения
        private readonly WheelFrictionCurve[] _baseFwd = new WheelFrictionCurve[6];
        private readonly WheelFrictionCurve[] _baseSide = new WheelFrictionCurve[6];

        private MapTerrain _terrain;
        private LayerMask _elementLayerMask;

        // Для сохранения исходных локальных поворотов мешей
        private Quaternion[] _initialMeshRot = new Quaternion[6];
        #endregion

        #region ⭑ Unity life-cycle
        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = new Vector3(0, -0.25f, 0); // ровер низкий, смещаем ЦМ пониже

            WheelCollider[] wcs = Wheels;
            for (int i = 0; i < wcs.Length; i++)
            {
                SetupSuspension(wcs[i]);
                _baseFwd[i] = wcs[i].forwardFriction;
                _baseSide[i] = wcs[i].sidewaysFriction;
            }

            // Сохраняем локальные повороты мешей (если заданы)
            Transform[] meshes = Meshes;
            for (int i = 0; i < meshes.Length; i++)
                if (meshes[i]) _initialMeshRot[i] = meshes[i].localRotation;

            // Сенсоры
            Lidars.AddRange(GetComponentsInChildren<ILidar>());
            Cameras.AddRange(GetComponentsInChildren<ICameraSensor>());

            _terrain = FindFirstObjectByType<MapTerrain>();
            _elementLayerMask = LayerMask.GetMask("Element");
        }

        void FixedUpdate()
        {
            // 1. Получаем команды
            float throttleL, throttleR;
            if (ManualControl)
            {
                float v = Input.GetAxis("Vertical");   // «вперёд/назад»
                float h = Input.GetAxis("Horizontal"); // «влево/вправо»

                /* Дифференциальное руление:
                 * для поворота влево нужно замедлить/развернуть левый борт,
                 * для поворота вправо — правый.
                 * Используем простую модель:  L = v - h,  R = v + h
                 */
                throttleL = Mathf.Clamp(v - h, -1, 1);
                throttleR = Mathf.Clamp(v + h, -1, 1);

                if (Input.GetKey(KeyCode.Space))
                    _brakeCmd = 1f;
            }
            else
            {
                throttleL = _cmdLeft;
                throttleR = _cmdRight;
            }

            // 2. Реализация команд
            _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;
            ApplyDrive(throttleL, throttleR);
            CapSpeed();
            UpdateFriction();
            UpdateWheelMeshes();
            CaptureRPM();

            _brakeCmd = 0; // сбрасываем ручник
        }
        #endregion

        #region ► Low-level actions
        private void SetupSuspension(WheelCollider wc)
        {
            wc.suspensionDistance = suspensionDistance;

            JointSpring js = wc.suspensionSpring;
            js.spring = springStrength;
            js.damper = springDamper;
            js.targetPosition = .5f;
            wc.suspensionSpring = js;
        }

        private void ApplyDrive(float left, float right)
        {
            ApplyBrake(_brakeCmd * brakeTorque);

            // Ограничиваем скорость (раздельный лимит вперёд/назад)
            float limit = (left >= 0 && right >= 0) ? forwardSpeedLimit : reverseSpeedLimit;
            if (_currentSpeed > limit)
            {
                ZeroMotorTorque();
                return;
            }

            float tqL = maxMotorTorque * left;
            float tqR = maxMotorTorque * right;

            // Левый борт
            leftFrontWheel.motorTorque = tqL;
            leftMiddleWheel.motorTorque = tqL;
            leftRearWheel.motorTorque = tqL;

            // Правый борт
            rightFrontWheel.motorTorque = tqR;
            rightMiddleWheel.motorTorque = tqR;
            rightRearWheel.motorTorque = tqR;
        }

        private void ApplyBrake(float tq)
        {
            WheelCollider[] wcs = Wheels;
            foreach (var wc in wcs)
                wc.brakeTorque = tq;

            if (tq > 0)
                ZeroMotorTorque();
        }

        private void ZeroMotorTorque()
        {
            WheelCollider[] wcs = Wheels;
            foreach (var wc in wcs)
                wc.motorTorque = 0;
        }

        private void CapSpeed()
        {
            if (_currentSpeed > maxSpeed)
                _rb.AddForce(-_rb.linearVelocity.normalized * 50f, ForceMode.Force);
        }
        #endregion

        #region ► Friction
        private void UpdateFriction()
        {
            WheelCollider[] wcs = Wheels;
            for (int i = 0; i < wcs.Length; i++)
                ApplyFrictionToWheel(wcs[i], _baseFwd[i], _baseSide[i]);
        }

        private void ApplyFrictionToWheel(WheelCollider wc,
                                          WheelFrictionCurve baseFwd,
                                          WheelFrictionCurve baseSide)
        {
            SurfaceType st = DetectSurface(wc);
            (float kFwd, float kSide) = SurfaceFrictionConst.SurfaceFriction.TryGetValue(st, out var k) ? k : (1, 1);

            kFwd *= globalFrictionMultiplier * forwardFrictionMultiplier;
            kSide *= globalFrictionMultiplier * sidewaysFrictionMultiplier;

            baseFwd.asymptoteValue = baseFwd.extremumValue = baseFwd.extremumValue * kFwd;
            baseSide.asymptoteValue = baseSide.extremumValue = baseSide.extremumValue * kSide;

            wc.forwardFriction = baseFwd;
            wc.sidewaysFriction = baseSide;
        }

        private SurfaceType DetectSurface(WheelCollider wc)
        {
            Vector3 origin = wc.transform.position + Vector3.up * .1f;

            if (Physics.Raycast(origin, Vector3.down, out var hit, 5f, _elementLayerMask))
            {
                var ov = hit.collider.GetComponent<SurfaceOverride>();
                if (ov) return ov.surface;
            }
            return _terrain.SurfaceAt(wc.transform.position);
        }
        #endregion

        #region ► Визуальное обновление
        private void UpdateWheelMeshes()
        {
            WheelCollider[] wcs = Wheels;
            Transform[] meshes = Meshes;

            for (int i = 0; i < meshes.Length; i++)
                UpdateSingleWheel(wcs[i], meshes[i], _initialMeshRot[i]);
        }

        private static void UpdateSingleWheel(WheelCollider col, Transform mesh, Quaternion initialLocal)
        {
            if (!mesh || !col) return;

            col.GetWorldPose(out Vector3 pos, out Quaternion colRot);

            // коллайдер отдаёт полный quaternion — вычленяем только spin (ось Х)
            Quaternion spin = Quaternion.AngleAxis(col.steerAngle, mesh.up) *
                              Quaternion.AngleAxis(col.rpm * 6f * Time.fixedDeltaTime, mesh.right);

            mesh.position = pos;
            mesh.rotation = col.transform.parent.rotation * spin * initialLocal;
        }
        #endregion

        #region ► RPM
        private void CaptureRPM()
        {
            _rpm[0] = leftFrontWheel.rpm;
            _rpm[1] = leftMiddleWheel.rpm;
            _rpm[2] = leftRearWheel.rpm;
            _rpm[3] = rightFrontWheel.rpm;
            _rpm[4] = rightMiddleWheel.rpm;
            _rpm[5] = rightRearWheel.rpm;
        }
        #endregion

        #region ► Служебные геттеры
        private WheelCollider[] Wheels => new[]
        {
            leftFrontWheel, leftMiddleWheel, leftRearWheel,
            rightFrontWheel, rightMiddleWheel, rightRearWheel
        };

        private Transform[] Meshes => new[]
        {
            leftFrontMesh, leftMiddleMesh, leftRearMesh,
            rightFrontMesh, rightMiddleMesh, rightRearMesh
        };
        #endregion
    }
}
