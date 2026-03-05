using Assets.Scripts.Garage.Attributes;
using Assets.Scripts.Robot.Api.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Robot.Cars
{
    [SectionName("Танк")]
    [RequireComponent(typeof(Rigidbody))]
    public class TrackedTankController : MonoBehaviour, IRobotAPI
    {
        [Header("Track WheelColliders")]
        public List<WheelCollider> leftWheels = new();
        public List<WheelCollider> rightWheels = new();

        [Header("Optional wheel meshes (same order as colliders)")]
        public List<Transform> leftMeshes = new();
        public List<Transform> rightMeshes = new();

        [Header("Power / Speed")]
        [DisplayName("Мощность двигателя (Н·м)")]
        public float maxMotorTorque = 1800f;
        [DisplayName("Мощность тормозной системы (Н·м)")]
        public float brakeTorque = 3500f;
        [DisplayName("Максимальная скорость вперед (км/ч)")]
        public float maxSpeedKmh = 35f;
        [DisplayName("Максимальная скорость назад (км/ч)")]
        public float reverseSpeedLimitKmh = 18f;

        [Tooltip("Сколько тяги теряем с ростом скорости. 0 = не теряем, 1 = сильно теряем")]
        [Range(0f, 1.5f)] public float torqueDropWithSpeed = 0.85f;

        [Header("Suspension")]
        [DisplayName("Длина хода подвески (м)")]
        public float suspensionDistance = 0.18f;
        [DisplayName("Жесткость пружины подвески (Н/м)")]
        public float springStrength = 55000f;
        [DisplayName("Демпфирование пружины Н·с/м")]
        public float springDamper = 6500f;
        [DisplayName("Усилие против опрокидывания (Н/м)")]
        public float antiRollStrength = 9000f;

        [Header("Grip / Traction")]
        [DisplayName("Глобальный множитель трения колес")]
        [Range(0f, 3f)] public float globalGripMultiplier = 1.6f;
        [DisplayName("Множитель продольного трения")]
        [Range(0f, 3f)] public float forwardGripMultiplier = 1.4f;
        [DisplayName("Множитель поперечного трения")]
        [Range(0f, 3f)] public float sidewaysGripMultiplier = 1.8f;

        [Tooltip("Помощь на подъеме: добавляет тягу, если едем вверх")]
        [Range(0f, 2f)] public float uphillAssist = 0.6f;

        [Tooltip("Стабилизация поворота на месте: снижает боковое скольжение корпуса при развороте")]
        [Range(0f, 2f)] public float pivotTurnAssist = 0.4f;

        [Header("Manual control")]
        public bool ManualControl { get; set; } = true;

        public void SetMotorPower(float left, float right)
        {
            _cmdLeft = Mathf.Clamp(left, -1f, 1f);
            _cmdRight = Mathf.Clamp(right, -1f, 1f);
            ManualControl = false;
        }

        public void SetSteerAngle(float steer)
        {
            _cmdSteer = Mathf.Clamp(steer, -1f, 1f);
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
        public List<ICameraSensor> Cameras { get; private set; } = new();
        public float CurrentSpeed => _currentSpeedKmh;
        public float CurrentSteerAngle => _currentYawRateDeg;

        private Rigidbody _rb;

        private float _cmdLeft, _cmdRight, _cmdSteer, _brakeCmd;
        private float _currentSpeedKmh;
        private float _currentYawRateDeg;

        private readonly float[] _rpm = new float[2];

        private readonly List<WheelFrictionCurve> _baseLeftFwd = new();
        private readonly List<WheelFrictionCurve> _baseLeftSide = new();
        private readonly List<WheelFrictionCurve> _baseRightFwd = new();
        private readonly List<WheelFrictionCurve> _baseRightSide = new();

        private readonly List<Quaternion> _leftMeshInitial = new();
        private readonly List<Quaternion> _rightMeshInitial = new();

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _rb.centerOfMass = new Vector3(0f, -0.45f, 0f);

            Lidars.AddRange(GetComponentsInChildren<ILidar>());
            Cameras.AddRange(GetComponentsInChildren<ICameraSensor>());

            SetupWheels(leftWheels, leftMeshes, _baseLeftFwd, _baseLeftSide, _leftMeshInitial);
            SetupWheels(rightWheels, rightMeshes, _baseRightFwd, _baseRightSide, _rightMeshInitial);
        }

        void SetupWheels(
            List<WheelCollider> wheels,
            List<Transform> meshes,
            List<WheelFrictionCurve> baseFwd,
            List<WheelFrictionCurve> baseSide,
            List<Quaternion> meshInitial)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                var wc = wheels[i];
                if (!wc) continue;

                wc.suspensionDistance = suspensionDistance;

                JointSpring js = wc.suspensionSpring;
                js.spring = springStrength;
                js.damper = springDamper;
                js.targetPosition = 0.5f;
                wc.suspensionSpring = js;

                baseFwd.Add(wc.forwardFriction);
                baseSide.Add(wc.sidewaysFriction);

                if (meshes != null && i < meshes.Count && meshes[i] != null)
                    meshInitial.Add(meshes[i].localRotation);
                else
                    meshInitial.Add(Quaternion.identity);
            }
        }

        void FixedUpdate()
        {
            _currentSpeedKmh = _rb.linearVelocity.magnitude * 3.6f;
            _currentYawRateDeg = _rb.angularVelocity.y * Mathf.Rad2Deg;

            float cmdL, cmdR;

            if (ManualControl)
            {
                float v = Input.GetAxis("Vertical");
                float h = -Input.GetAxis("Horizontal");

                // сырые команды
                cmdL = v - h;
                cmdR = v + h;

                float m = Mathf.Max(1f, Mathf.Abs(cmdL), Mathf.Abs(cmdR));
                cmdL /= m;
                cmdR /= m;

                // дальше всё как было
                ApplyBrake(_brakeCmd);
                ApplyDifferentialDrive(cmdL, cmdR);

                if (Input.GetKey(KeyCode.Space))
                    _brakeCmd = 1f;
            }
            else
            {
                cmdL = Mathf.Clamp(_cmdLeft - _cmdSteer, -1f, 1f);
                cmdR = Mathf.Clamp(_cmdRight + _cmdSteer, -1f, 1f);
            }

            ApplyBrake(_brakeCmd);
            ApplyDifferentialDrive(cmdL, cmdR);

            ApplyAntiRollBetweenTracks();
            UpdateFriction(cmdL, cmdR);
            UpdateWheelMeshes();
            CaptureRPM();

            _brakeCmd = 0f;

            //Debug.Log($"Distances: {string.Join(",", Lidars.FirstOrDefault().PointCloud.Select(p => p.Distance))}");
            Debug.Log($"X:{Position.x};Y:{Position.y};Z:{Position.z}:YawDeg:{YawDeg};Nearest:{Lidars.First().Nearest}");
        }

        /// <summary>
        /// Применяет логику дифференциального управления (Differential Drive) к моторным приводам транспортного средства
        /// </summary>
        /// <remarks>
        /// Метод выполняет следующие операции:
        /// <list type="bullet">
        /// <item>Проверяет ограничение скорости (вперед/назад) и отключает крутящий момент при превышении лимита</item>
        /// <item>Рассчитывает падение крутящего момента в зависимости от текущей скорости</item>
        /// <item>Применяет вспомогательное усилие при движении в гору</item>
        /// <item>Реализует ассистент разворота на месте (Pivot Turn), подавляя боковое скольжение для стабилизации вращения.</item>
        /// </list>
        /// </remarks>
        /// <param name="leftCmd">Команда для левого борта в диапазоне [-1, 1].</param>
        /// <param name="rightCmd">Команда для правого борта в диапазоне [-1, 1].</param>
        void ApplyDifferentialDrive(float leftCmd, float rightCmd)
        {
            float speedLimit = (leftCmd >= 0f && rightCmd >= 0f) ? maxSpeedKmh : reverseSpeedLimitKmh;
            if (_currentSpeedKmh > speedLimit)
            {
                SetMotorTorqueAll(0f, 0f);
                return;
            }

            float speed01 = Mathf.Clamp01(_currentSpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float torqueFactor = Mathf.Lerp(1f, 1f - torqueDropWithSpeed, speed01);

            float uphill = Vector3.Dot(_rb.transform.forward, Vector3.up);
            float uphillFactor = 1f + Mathf.Clamp(uphill, 0f, 0.35f) * uphillAssist;

            bool pivotTurn = Mathf.Sign(leftCmd) != Mathf.Sign(rightCmd) && Mathf.Abs(leftCmd) > 0.2f && Mathf.Abs(rightCmd) > 0.2f;
            if (pivotTurn)
            {
                Vector3 v = _rb.linearVelocity;
                Vector3 lateral = Vector3.Project(v, transform.right);
                _rb.AddForce(-lateral * pivotTurnAssist, ForceMode.Acceleration);
            }

            float turn01 = Mathf.Clamp01(Mathf.Abs(leftCmd - rightCmd));
            float turnBoost = Mathf.Lerp(1f, 2.2f, turn01);
            float tqL = maxMotorTorque * leftCmd * torqueFactor * uphillFactor * turnBoost;
            float tqR = maxMotorTorque * rightCmd * torqueFactor * uphillFactor * turnBoost;

            SetMotorTorqueAll(tqL, tqR);
        }

        void SetMotorTorqueAll(float leftTorque, float rightTorque)
        {
            for (int i = 0; i < leftWheels.Count; i++)
                if (leftWheels[i]) leftWheels[i].motorTorque = leftTorque;

            for (int i = 0; i < rightWheels.Count; i++)
                if (rightWheels[i]) rightWheels[i].motorTorque = rightTorque;
        }

        void ApplyBrake(float brake01)
        {
            float tq = brake01 * brakeTorque;

            for (int i = 0; i < leftWheels.Count; i++)
                if (leftWheels[i]) leftWheels[i].brakeTorque = tq;

            for (int i = 0; i < rightWheels.Count; i++)
                if (rightWheels[i]) rightWheels[i].brakeTorque = tq;

            if (tq > 0f)
                SetMotorTorqueAll(0f, 0f);
        }

        void ApplyAntiRollBetweenTracks()
        {
            ApplyAntiRollOnList(leftWheels);
            ApplyAntiRollOnList(rightWheels);

            int n = Mathf.Min(leftWheels.Count, rightWheels.Count);
            for (int i = 0; i < n; i++)
                ApplyAntiRollPair(leftWheels[i], rightWheels[i]);
        }

        void ApplyAntiRollOnList(List<WheelCollider> wheels)
        {
            if (wheels.Count < 2) return;

            ApplyAntiRollPair(wheels[0], wheels[wheels.Count - 1]);
        }

        /// <summary>
        /// Реализует логику стабилизатора поперечной устойчивости (Anti-Roll Bar) для пары колес одной оси
        /// </summary>
        /// <remarks>
        /// Метод вычисляет разницу в сжатии подвески между двумя колесами (левым и правым)
        /// На основе этой разницы прикладывает противоположные силы к корпусу (<see cref="Rigidbody"/>)
        /// <list type="bullet">
        /// <item>Давит вниз на более "разгруженное" (поднятое) колесо</item>
        /// <item>Подтягивает вверх более "загруженное" (сжатое) колесо</item>
        /// </list>
        /// Это перераспределение сил уменьшает крен корпуса и риск опрокидывания при маневрах
        /// </remarks>
        /// <param name="a">Первое колесо пары (например, левое переднее)</param>
        /// <param name="b">Второе колесо пары (например, правое переднее)</param>
        void ApplyAntiRollPair(WheelCollider a, WheelCollider b)
        {
            if (!a || !b) return;

            bool aGround = a.GetGroundHit(out WheelHit hitA);
            bool bGround = b.GetGroundHit(out WheelHit hitB);
            if (!aGround && !bGround) return;

            float travelA = aGround ? (-a.transform.InverseTransformPoint(hitA.point).y - a.radius) / Mathf.Max(0.001f, a.suspensionDistance) : 1f;
            float travelB = bGround ? (-b.transform.InverseTransformPoint(hitB.point).y - b.radius) / Mathf.Max(0.001f, b.suspensionDistance) : 1f;

            float force = (travelA - travelB) * antiRollStrength;

            if (aGround) _rb.AddForceAtPosition(a.transform.up * -force, a.transform.position);
            if (bGround) _rb.AddForceAtPosition(b.transform.up * force, b.transform.position);
        }

        void UpdateFriction(float leftCmd, float rightCmd)
        {
            ApplyFrictionList(leftWheels, _baseLeftFwd, _baseLeftSide);
            ApplyFrictionList(rightWheels, _baseRightFwd, _baseRightSide);

            float drive = Mathf.Max(Mathf.Abs(leftCmd), Mathf.Abs(rightCmd));
            float extra = Mathf.Lerp(1f, 1.15f, drive);

            MultiplyForwardFriction(leftWheels, extra);
            MultiplyForwardFriction(rightWheels, extra);
        }

        /// <summary>
        /// Динамически обновляет параметры трения (Friction Curves) для списка колес, применяя глобальные множители сцепления
        /// </summary>
        /// <remarks>
        /// Метод модифицирует как продольное (Forward), так и поперечное (Sideways) трение.
        /// Используется для настройки "зацепа" гусениц или колес без учета типа поверхности (Terrain)
        /// Рассчитывает итоговые значения экстремума и асимптоты путем умножения базовых кривых и соответствующие осевые коэффициенты
        /// </remarks>
        /// <param name="wheels">Список объектов <see cref="WheelCollider"/>, к которым будут применены настройки</param>
        /// <param name="baseFwd">Список исходных кривых продольного трения (шаблонные значения)</param>
        /// <param name="baseSide">Список исходных кривых поперечного трения (шаблонные значения)</param>
        void ApplyFrictionList(List<WheelCollider> wheels, List<WheelFrictionCurve> baseFwd, List<WheelFrictionCurve> baseSide)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                var wc = wheels[i];
                if (!wc) continue;

                var fwd = baseFwd[i];
                var side = baseSide[i];

                // общий “гусеничный” множитель. Без MapTerrain
                float kFwd = globalGripMultiplier * forwardGripMultiplier;
                float kSide = globalGripMultiplier * sidewaysGripMultiplier;

                fwd.extremumValue *= kFwd;
                fwd.asymptoteValue *= kFwd;
                side.extremumValue *= kSide;
                side.asymptoteValue *= kSide;

                wc.forwardFriction = fwd;
                wc.sidewaysFriction = side;
            }
        }

        void MultiplyForwardFriction(List<WheelCollider> wheels, float factor)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                var wc = wheels[i];
                if (!wc) continue;

                var fwd = wc.forwardFriction;
                fwd.extremumValue *= factor;
                fwd.asymptoteValue *= factor;
                wc.forwardFriction = fwd;
            }
        }

        void UpdateWheelMeshes()
        {
            UpdateMeshesSide(leftWheels, leftMeshes, _leftMeshInitial);
            UpdateMeshesSide(rightWheels, rightMeshes, _rightMeshInitial);
        }

        void UpdateMeshesSide(List<WheelCollider> wheels, List<Transform> meshes, List<Quaternion> initialLocal)
        {
            if (meshes == null) return;

            int n = Mathf.Min(wheels.Count, meshes.Count);
            for (int i = 0; i < n; i++)
            {
                var wc = wheels[i];
                var m = meshes[i];
                if (!wc || !m) continue;

                wc.GetWorldPose(out var pos, out var rot);
                m.position = pos;
                m.rotation = rot * initialLocal[i];
            }
        }

        void CaptureRPM()
        {
            float sumL = 0f, sumR = 0f;
            int cntL = 0, cntR = 0;

            for (int i = 0; i < leftWheels.Count; i++)
                if (leftWheels[i]) { sumL += leftWheels[i].rpm; cntL++; }

            for (int i = 0; i < rightWheels.Count; i++)
                if (rightWheels[i]) { sumR += rightWheels[i].rpm; cntR++; }

            _rpm[0] = (cntL > 0) ? sumL / cntL : 0f;
            _rpm[1] = (cntR > 0) ? sumR / cntR : 0f;
        }
    }
}
