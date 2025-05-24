using Assets.Scripts.Consts;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.MapEditor.Models.Enums;
using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Models.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Robot.Cars
{
    [RequireComponent(typeof(Rigidbody))]
    public class FourWheelsCarController : MonoBehaviour, IRobotAPI
    {
        #region ⭑ Public fields
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

        [Header("Suspension")]
        public float suspensionDistance = 0.15f;
        public float springStrength = 35000f;
        public float springDamper = 4500f;
        public float antiRollStrength = 5000f;     // стабилизатор

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

        public void Brake(float power = 1)
        { 
            _brakeCmd = Mathf.Clamp01(power); 
            ManualControl = false; 
        }

        public float[] WheelRPM => _rpm;
        public Vector3 Position => transform.position;
        public float YawDeg => transform.eulerAngles.y;
        public List<ILidar> Lidars { get; } = new();
        public bool ManualControl { get; set; } = true;         // TRUE – WASD, FALSE – script
        #endregion

        #region ⭑ private state
        Rigidbody _rb;
        float _currentSpeed;
        readonly float[] _rpm = new float[4];

        // cmd-каналы от скрипта
        float _cmdLeft, _cmdRight, _brakeCmd;

        // friction templates
        WheelFrictionCurve _flFwd0, _flSide0, _frFwd0, _frSide0, _rlFwd0, _rlSide0, _rrFwd0, _rrSide0;

        private MapTerrain _terrain;
        #endregion

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = new Vector3(0, -0.35f, 0);

            SetSuspension(frontLeftWheel);
            SetSuspension(frontRightWheel);
            SetSuspension(rearLeftWheel);
            SetSuspension(rearRightWheel);

            // сохранить базовые кривые трения
            _flFwd0 = frontLeftWheel.forwardFriction; _flSide0 = frontLeftWheel.sidewaysFriction;
            _frFwd0 = frontRightWheel.forwardFriction; _frSide0 = frontRightWheel.sidewaysFriction;
            _rlFwd0 = rearLeftWheel.forwardFriction; _rlSide0 = rearLeftWheel.sidewaysFriction;
            _rrFwd0 = rearRightWheel.forwardFriction; _rrSide0 = rearRightWheel.sidewaysFriction;

            // собрать лидары в детях
            Lidars.AddRange(GetComponentsInChildren<ILidar>());
            _terrain = FindFirstObjectByType<MapTerrain>();
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
                if (Input.GetKey(KeyCode.Space)) 
                    _brakeCmd = 1;
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

            ApplyAntiRoll(frontLeftWheel, frontRightWheel);    // NEW
            ApplyAntiRoll(rearLeftWheel, rearRightWheel);

            _brakeCmd = 0;                                // сбросить до следующего кадра
        }

        void SetSuspension(WheelCollider wc)           // NEW
        {
            wc.suspensionDistance = suspensionDistance;

            JointSpring js = wc.suspensionSpring;
            js.spring = springStrength;
            js.damper = springDamper;
            js.targetPosition = .5f;                   // 50 % хода
            wc.suspensionSpring = js;
        }

        void ApplyAntiRoll(WheelCollider left, WheelCollider right)   // NEW
        {
            bool lGround = left.GetGroundHit(out WheelHit hitL);
            bool rGround = right.GetGroundHit(out WheelHit hitR);

            if (!lGround && !rGround) return;

            float travelL = lGround ? (-left.transform.InverseTransformPoint(hitL.point).y - left.radius) / left.suspensionDistance : 1;
            float travelR = rGround ? (-right.transform.InverseTransformPoint(hitR.point).y - right.radius) / right.suspensionDistance : 1;

            float force = (travelL - travelR) * antiRollStrength;

            if (lGround)
                _rb.AddForceAtPosition(left.transform.up * -force, left.transform.position);
            if (rGround)
                _rb.AddForceAtPosition(right.transform.up * force, right.transform.position);
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
            ApplyFrictionToWheel(frontLeftWheel, _flFwd0, _flSide0);
            ApplyFrictionToWheel(frontRightWheel, _frFwd0, _frSide0);
            ApplyFrictionToWheel(rearLeftWheel, _rlFwd0, _rlSide0);
            ApplyFrictionToWheel(rearRightWheel, _rrFwd0, _rrSide0);
        }

        void ApplyFrictionToWheel(WheelCollider wc,
                                   WheelFrictionCurve baseFwd,
                                   WheelFrictionCurve baseSide)
        {
            SurfaceType st = _terrain.SurfaceAt(wc.transform.position);
            (float kFwd, float kSide) = SurfaceFrictionConst.SurfaceFriction.TryGetValue(st, out var k) ? k : (1, 1);

            // глобальные мультипликаторы пользователя
            kFwd *= globalFrictionMultiplier * forwardFrictionMultiplier;
            kSide *= globalFrictionMultiplier * sidewaysFrictionMultiplier;

            baseFwd.asymptoteValue = baseFwd.extremumValue = baseFwd.extremumValue * kFwd;
            baseSide.asymptoteValue = baseSide.extremumValue = baseSide.extremumValue * kSide;

            wc.forwardFriction = baseFwd;
            wc.sidewaysFriction = baseSide;
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
    }
}
