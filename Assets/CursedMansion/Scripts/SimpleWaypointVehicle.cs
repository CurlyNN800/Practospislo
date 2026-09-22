using System;
using UnityEngine;
using UnityEngine.Events;

namespace CursedMansion
{
    /// <summary>
    /// Двигает объект (корень машины) по цепочке waypoints. Для VR-пассажира игрок сидит дочерним к машине и едет вместе с ней.
    /// Движение ведётся только в плоскости XZ; Y прижимается к дороге через raycast.
    /// Поворот синхронизирован с сегментами: к моменту прибытия в точку N машина уже смотрит в направлении N→N+1.
    /// </summary>
    public class SimpleWaypointVehicle : MonoBehaviour
    {
        [Tooltip("Если задано, двигается этот объект (например корень машины). Иначе двигается объект с этим скриптом.")]
        [SerializeField] Transform drivenTransform;
        [SerializeField] Transform[] waypoints = Array.Empty<Transform>();
        [SerializeField] float moveSpeedMetersPerSecond = 8f;
        [Tooltip("Расстояние до последней точки, начиная с которого машина тормозит.")]
        [SerializeField] float slowdownDistance = 4f;
        [SerializeField] float arriveDistance = 0.3f;
        [Tooltip("Если выключено, вызовите BeginDrive() вручную (например из VrPassengerRide после посадки).")]
        [SerializeField] bool playOnEnable;
        [SerializeField] bool disableWhenComplete = true;

        [Header("Привязка к дороге")]
        [Tooltip("Высота от текущей Y-позиции машины, с которой стартует луч вниз.")]
        [SerializeField] float raycastOriginHeight = 2f;
        [Tooltip("Максимальная дальность луча вниз от точки старта.")]
        [SerializeField] float raycastMaxDistance = 8f;
        [Tooltip("Слои, считающиеся дорогой/землёй. Настройте под проект (исключите слой машины).")]
        [SerializeField] LayerMask groundLayers = ~0;
        [Tooltip("Вертикальный отступ машины над точкой касания дороги.")]
        [SerializeField] float groundOffset = 0.52f;

        public UnityEvent onPathComplete;

        [Header("Ранний переход сцены")]
        [Tooltip("Событие за столько секунд до конца маршрута (оценка по скорости).")]
        [SerializeField] float approachEndSeconds = 7f;
        public UnityEvent onApproachingPathEnd;

        // Публичное свойство для SteeringWheelFollow
        public float CurrentSpeedMetersPerSecond => _currentSpeed;

        int _index;
        bool _moving;
        bool _completed;
        bool _approachEndFired;
        Transform _drive;
        Transform _raycastIgnoreRoot;
        float _currentSpeed;
        float _lastWaypointStuckTime;

        static int s_playerLayer = -1;
        static int s_handLayer = -1;

        // Данные текущего сегмента для синхронизированного поворота
        float _segmentLength;
        Quaternion _segmentStartRot;
        Quaternion _segmentEndRot;


        void Awake()
        {
            _drive = drivenTransform != null ? drivenTransform : transform;
            if (s_playerLayer < 0)
            {
                s_playerLayer = LayerMask.NameToLayer("Player");
                s_handLayer = LayerMask.NameToLayer("Hand");
            }
        }

        /// <summary>Игнорировать коллайдеры этого рига при raycast к земле (VR-пассажир).</summary>
        public void SetGroundRaycastIgnoreRoot(Transform root) => _raycastIgnoreRoot = root;

        public void ClearGroundRaycastIgnoreRoot() => _raycastIgnoreRoot = null;

        void OnEnable()
        {
            if (playOnEnable)
                BeginDrive();
        }

        public void BeginDrive()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SimpleWaypointVehicle)}] {name}: нет waypoints.");
                return;
            }

            _index = 0;
            _moving = true;
            _completed = false;
            _approachEndFired = false;
            _lastWaypointStuckTime = 0f;
            _currentSpeed = 0f;
            enabled = true;
            InitSegment();
        }

        // Вызывается при переходе на каждый новый сегмент.
        // Запоминает начальный поворот и вычисляет целевой: машина должна
        // смотреть в направлении следующего сегмента, когда достигнет конца текущего.
        void InitSegment()
        {
            if (_index >= waypoints.Length) return;

            var target = waypoints[_index];
            if (target == null) return;

            var driveFlat  = Flat(_drive.position);
            var targetFlat = Flat(target.position);
            _segmentLength = Mathf.Max(Vector3.Distance(driveFlat, targetFlat), 0.001f);

            _segmentStartRot = _drive.rotation;

            // Целевой поворот конца сегмента = направление СЛЕДУЮЩЕГО сегмента (N+1 → N+2).
            // Если следующего сегмента нет, смотрим вдоль текущего.
            bool hasNext = _index + 1 < waypoints.Length && waypoints[_index + 1] != null;
            Vector3 lookDirFlat;
            if (hasNext)
            {
                var next = waypoints[_index + 1];
                lookDirFlat = Flat(next.position) - Flat(target.position);
            }
            else
            {
                lookDirFlat = Flat(target.position) - Flat(_drive.position);
            }

            _segmentEndRot = lookDirFlat.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirFlat, Vector3.up)
                : _segmentStartRot;
        }

        public void StopMoving()
        {
            _moving = false;
            _currentSpeed = 0f;
        }

        void Update()
        {
            if (!_moving || waypoints == null || waypoints.Length == 0) return;

            TryFireApproachingPathEnd();

            var target = waypoints[_index];
            if (target == null) { AdvanceWaypoint(); return; }

            var currentFlat = Flat(_drive.position);
            var targetFlat  = Flat(target.position);
            float distXZ = Vector3.Distance(currentFlat, targetFlat);

            // Скорость: плавное торможение только у последней точки
            bool isLast     = _index == waypoints.Length - 1;
            float speedGoal = isLast && distXZ < slowdownDistance
                ? Mathf.SmoothStep(0f, 1f, distXZ / slowdownDistance) * moveSpeedMetersPerSecond
                : moveSpeedMetersPerSecond;

            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed, speedGoal,
                moveSpeedMetersPerSecond * 2f * Time.deltaTime);

            // Движение по XZ; Y — из raycast к поверхности дороги
            var newFlat = Vector3.MoveTowards(currentFlat, targetFlat, _currentSpeed * Time.deltaTime);
            float groundY = SampleGroundY(new Vector3(newFlat.x, _drive.position.y, newFlat.z));
            _drive.position = new Vector3(newFlat.x, groundY, newFlat.z);

            // Поворот: Slerp по нормированному прогрессу вдоль сегмента.
            // При distXZ == _segmentLength → t = 0 (начало), при distXZ == 0 → t = 1 (конец).
            float t = Mathf.Clamp01(1f - distXZ / _segmentLength);
            _drive.rotation = Quaternion.Slerp(_segmentStartRot, _segmentEndRot, t);

            if (distXZ <= arriveDistance)
            {
                AdvanceWaypoint();
                return;
            }

            // Последняя точка: если почти доехали, но тормозим до нуля — форсируем финиш.
            if (isLast && distXZ < slowdownDistance)
            {
                _lastWaypointStuckTime += Time.deltaTime;
                if (_lastWaypointStuckTime >= 1.5f || distXZ <= arriveDistance * 2.5f)
                {
                    _drive.position = new Vector3(targetFlat.x, _drive.position.y, targetFlat.z);
                    AdvanceWaypoint();
                }
            }
            else
            {
                _lastWaypointStuckTime = 0f;
            }
        }

        void AdvanceWaypoint()
        {
            // Фиксируем финальный поворот перед переходом, чтобы следующий сегмент
            // стартовал ровно с нужной ориентации.
            _drive.rotation = _segmentEndRot;

            _index++;
            if (_index >= waypoints.Length)
                Complete();
            else
                InitSegment();
        }

        void Complete()
        {
            if (_completed)
                return;

            _completed = true;
            StopMoving();

            Debug.Log($"[{nameof(SimpleWaypointVehicle)}] {name}: маршрут завершён ({waypoints.Length} точек).");
            onPathComplete?.Invoke();

            if (disableWhenComplete)
                enabled = false;
        }

        void TryFireApproachingPathEnd()
        {
            if (_approachEndFired || approachEndSeconds <= 0f)
                return;

            float remaining = EstimateRemainingDriveSeconds();
            if (remaining > approachEndSeconds)
                return;

            _approachEndFired = true;
            Debug.Log($"[{nameof(SimpleWaypointVehicle)}] ~{remaining:F1} с до финиша → ранний переход.");
            onApproachingPathEnd?.Invoke();
        }

        float EstimateRemainingDriveSeconds()
        {
            if (waypoints == null || waypoints.Length == 0 || _index >= waypoints.Length)
                return 0f;

            float speed = Mathf.Max(_currentSpeed, moveSpeedMetersPerSecond * 0.25f, 0.5f);
            float seconds = 0f;

            var target = waypoints[_index];
            if (target != null)
            {
                var currentFlat = Flat(_drive.position);
                var targetFlat = Flat(target.position);
                seconds += Vector3.Distance(currentFlat, targetFlat) / speed;
            }

            for (int i = _index + 1; i < waypoints.Length; i++)
            {
                var a = waypoints[i - 1];
                var b = waypoints[i];
                if (a == null || b == null)
                    continue;

                seconds += Vector3.Distance(Flat(a.position), Flat(b.position)) / speed;
            }

            return seconds;
        }

        float SampleGroundY(Vector3 from)
        {
            var origin = new Vector3(from.x, from.y + raycastOriginHeight, from.z);
            float maxDist = raycastOriginHeight + raycastMaxDistance;

            var hits = Physics.RaycastAll(origin, Vector3.down, maxDist, groundLayers,
                QueryTriggerInteraction.Ignore);

            // Берём максимальный Y (поверхность дороги, не террейн под ней).
            float bestY = float.MinValue;
            bool  found = false;
            foreach (var hit in hits)
            {
                if (ShouldIgnoreGroundHit(hit.collider)) continue;
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    found = true;
                }
            }

            return found ? bestY + groundOffset : _drive.position.y;
        }

        bool ShouldIgnoreGroundHit(Collider collider)
        {
            if (collider == null) return true;

            var t = collider.transform;
            if (t == _drive || t.IsChildOf(_drive)) return true;

            if (_raycastIgnoreRoot != null &&
                (t == _raycastIgnoreRoot || t.IsChildOf(_raycastIgnoreRoot)))
                return true;

            if (collider.CompareTag("Player")) return true;

            int layer = t.gameObject.layer;
            if (layer == s_playerLayer || layer == s_handLayer) return true;

            if (collider.GetComponentInParent<CharacterController>() != null) return true;

            return false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
