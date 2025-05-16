using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Models.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot
{
    [RequireComponent(typeof(Rigidbody))]
    public class FourWheelsCarController : MonoBehaviour, IRobotAPI
    {
        #region ⭑ Public fields (как было)
        [Header("Wheel Colliders")]
        public WheelCollider frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;

        [Header("Wheel Meshes (Optional)")]
        public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;

        [Header("Car Settings")]
        public CarDriveType driveType = CarDriveType.RearWheelDrive;
        public float maxMotorTorque = 1500f;
        public float maxSteeringAngle = 30f;
        public float maxSpeed = 180f;
        public float forwardSpeedLimit = 120f;
        public float reverseSpeedLimit = 60f;
        public float brakeTorque = 2000f;
        public float resetHeight = 1f;

        [Header("Friction Settings")]
        [Range(0, 2)] public float globalFrictionMultiplier = 1f;
        [Range(0, 2)] public float forwardFrictionMultiplier = 1f;
        [Range(0, 2)] public float sidewaysFrictionMultiplier = 1f;
        #endregion

        #region ⭑ IRobotAPI implementation (новое)
        public bool ManualControl { get; set; } = true;         // TRUE – WASD, FALSE – script
        public void SetMotorPower(float left, float right)
        {
            _cmdLeft = Mathf.Clamp(left, -1, 1);
            _cmdRight = Mathf.Clamp(right, -1, 1);
            ManualControl = false;
        }
        public void Brake(float power = 1)
        { _brakeCmd = Mathf.Clamp01(power); ManualControl = false; }

        public float[] WheelRPM => _rpm;
        public Vector3 Position => transform.position;
        public float YawDeg => transform.eulerAngles.y;
        public List<ILidar> Lidars { get; } = new();
        #endregion

        #region ⭑ private state
        Vector3 _spawnPos; Quaternion _spawnRot;
        Rigidbody _rb;
        float _currentSpeed;
        readonly float[] _rpm = new float[4];

        // cmd-каналы от скрипта
        float _cmdLeft, _cmdRight, _brakeCmd;

        // friction templates
        WheelFrictionCurve _flFwd0, _flSide0, _frFwd0, _frSide0, _rlFwd0, _rlSide0, _rrFwd0, _rrSide0;
        #endregion

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _spawnPos = transform.position; _spawnRot = transform.rotation;

            // сохранить базовые кривые трения
            _flFwd0 = frontLeftWheel.forwardFriction; _flSide0 = frontLeftWheel.sidewaysFriction;
            _frFwd0 = frontRightWheel.forwardFriction; _frSide0 = frontRightWheel.sidewaysFriction;
            _rlFwd0 = rearLeftWheel.forwardFriction; _rlSide0 = rearLeftWheel.sidewaysFriction;
            _rrFwd0 = rearRightWheel.forwardFriction; _rrSide0 = rearRightWheel.sidewaysFriction;

            // собрать лидары в детях
            Lidars.AddRange(GetComponentsInChildren<ILidar>());
            Debug.Log($"Лидаров найдено: {Lidars.Count}");
        }

        void FixedUpdate()
        {
            // источники управления
            float throttleL, throttleR, steerInput;
            if (ManualControl)
            {
                float v = Input.GetAxis("Vertical");             // W/S
                float h = Input.GetAxis("Horizontal");           // A/D
                throttleL = throttleR = v;
                steerInput = h;
                if (Input.GetKey(KeyCode.Space)) _brakeCmd = 1;
            }
            else
            {
                throttleL = _cmdLeft;
                throttleR = _cmdRight;
                steerInput = Mathf.Clamp((_cmdRight - _cmdLeft), -1, 1);
            }

            ApplySteering(steerInput);
            _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;
            ApplyDrive(throttleL, throttleR);
            CapSpeed();
            UpdateWheelFriction();
            UpdateWheelMeshes();
            CaptureRPM();
            _brakeCmd = 0;                                // сбросить до следующего кадра
        }

        #region ► low-level actions
        void ApplySteering(float input)
        {
            float angle = maxSteeringAngle * Mathf.Clamp(input, -1, 1);
            frontLeftWheel.steerAngle = angle;
            frontRightWheel.steerAngle = angle;
        }

        void ApplyDrive(float left, float right)
        {
            ApplyBrake(_brakeCmd * brakeTorque);         // если задано скриптом / Space

            float speedLimit = (left >= 0 && right >= 0) ? forwardSpeedLimit : reverseSpeedLimit;
            if (_currentSpeed > speedLimit)
            {
                frontLeftWheel.motorTorque = 0;
                frontRightWheel.motorTorque = 0;
                rearLeftWheel.motorTorque = 0;
                rearRightWheel.motorTorque = 0;
                return;
            }

            float tqL = maxMotorTorque * left;
            float tqR = maxMotorTorque * right;

            switch (driveType)
            {
                case CarDriveType.FrontWheelDrive:
                    frontLeftWheel.motorTorque = tqL;
                    frontRightWheel.motorTorque = tqR; break;
                case CarDriveType.RearWheelDrive:
                    rearLeftWheel.motorTorque = tqL;
                    rearRightWheel.motorTorque = tqR; break;
                case CarDriveType.AllWheelDrive:
                    frontLeftWheel.motorTorque = tqL;
                    frontRightWheel.motorTorque = tqR;
                    rearLeftWheel.motorTorque = tqL;
                    rearRightWheel.motorTorque = tqR; break;
            }
        }

        void ApplyBrake(float tq)
        {
            frontLeftWheel.brakeTorque = tq;
            frontRightWheel.brakeTorque = tq;
            rearLeftWheel.brakeTorque = tq;
            rearRightWheel.brakeTorque = tq;
            if (tq > 0)                                 // глушим движок
            {
                frontLeftWheel.motorTorque = 0;
                frontRightWheel.motorTorque = 0;
                rearLeftWheel.motorTorque = 0;
                rearRightWheel.motorTorque = 0;
            }
        }

        void CapSpeed()
        {
            if (_currentSpeed > maxSpeed)
                _rb.AddForce(-_rb.linearVelocity.normalized * 50f);
        }

        void CaptureRPM()
        {
            _rpm[0] = frontLeftWheel.rpm;
            _rpm[1] = frontRightWheel.rpm;
            _rpm[2] = rearLeftWheel.rpm;
            _rpm[3] = rearRightWheel.rpm;
        }
        #endregion

        #region ► friction (без изменений визуально)
        void UpdateWheelFriction()
        {
            // Обновляем переднее левое колесо
            WheelFrictionCurve fF = frontLeftWheel.forwardFriction;
            fF.asymptoteValue = _flFwd0.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _flFwd0.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            frontLeftWheel.forwardFriction = fF;

            WheelFrictionCurve sF = frontLeftWheel.sidewaysFriction;
            sF.asymptoteValue = _frSide0.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _frSide0.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            frontLeftWheel.sidewaysFriction = sF;

            // Переднее правое колесо
            fF = frontRightWheel.forwardFriction;
            fF.asymptoteValue = _frFwd0.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _frFwd0.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            frontRightWheel.forwardFriction = fF;

            sF = frontRightWheel.sidewaysFriction;
            sF.asymptoteValue = _frSide0.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _frSide0.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            frontRightWheel.sidewaysFriction = sF;

            // Заднее левое колесо
            fF = rearLeftWheel.forwardFriction;
            fF.asymptoteValue = _rlFwd0.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _rlFwd0.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            rearLeftWheel.forwardFriction = fF;

            sF = rearLeftWheel.sidewaysFriction;
            sF.asymptoteValue = _rlSide0.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _rlSide0.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            rearLeftWheel.sidewaysFriction = sF;

            // Заднее правое колесо
            fF = rearRightWheel.forwardFriction;
            fF.asymptoteValue = _rrFwd0.asymptoteValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            fF.extremumValue = _rrFwd0.extremumValue * globalFrictionMultiplier * forwardFrictionMultiplier;
            rearRightWheel.forwardFriction = fF;

            sF = rearRightWheel.sidewaysFriction;
            sF.asymptoteValue = _rrSide0.asymptoteValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            sF.extremumValue = _rrSide0.extremumValue * globalFrictionMultiplier * sidewaysFrictionMultiplier;
            rearRightWheel.sidewaysFriction = sF;
        }
        #endregion

        void UpdateWheelMeshes()
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

        #region ► helpers
        public void ResetCar()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(new Vector3(_spawnPos.x, resetHeight, _spawnPos.z), _spawnRot);
        }
        #endregion
    }
}
