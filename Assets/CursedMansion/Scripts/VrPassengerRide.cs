using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace CursedMansion
{
    /// <summary>
    /// VR-пассажир: садит риг в сиденье, отключает локомоцию, запускает маршрут, по окончании — HouseScene.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class VrPassengerRide : MonoBehaviour
    {
        [Header("Seat")]
        [SerializeField] Transform seatAnchor;
        [Tooltip("Если задан — используется вместо поиска по сцене (надёжнее на шлеме).")]
        [SerializeField] Transform playerRigOverride;
        [SerializeField] string playerObjectName = "VR Player";
        [SerializeField] float delayBeforeMountSeconds = 0.5f;
        [SerializeField] bool autoMountOnStart = true;

        [Header("Drive")]
        [SerializeField] SimpleWaypointVehicle vehiclePath;
        [SerializeField] bool unmountWhenPathCompletes;

        [Header("HouseScene")]
        [SerializeField] bool loadHouseWhenPathComplete = true;
        [SerializeField] float fadeToBlackSeconds = 5f;
        [SerializeField] float fadeInSeconds = 2f;

        Transform _playerRoot;
        Transform _originalParent;
        Vector3 _originalLocalPosition;
        Quaternion _originalLocalRotation;
        Vector3 _originalWorldPosition;
        Quaternion _originalWorldRotation;
        XROrigin _xrOrigin;
        XROrigin.TrackingOriginMode _prevTrackingMode;
        LocomotionMediator _locomotionMediator;
        bool _mediatorWasEnabled;

        readonly List<(Behaviour behaviour, bool wasEnabled)> _locomotionStates = new();
        readonly List<(CharacterController cc, bool wasEnabled, bool detectCollisions)> _ccStates = new();
        readonly List<(Collider collider, bool wasEnabled)> _colliderStates = new();
        bool _mounted;
        bool _mountRoutineRunning;
        bool _pathCompleteHandled;

        void Awake()
        {
            if (vehiclePath == null)
                return;

            vehiclePath.onApproachingPathEnd.AddListener(OnPathEndTransition);
            vehiclePath.onPathComplete.AddListener(OnPathEndTransition);
        }

        void Start()
        {
            if (autoMountOnStart)
                BeginAutoMount();
        }

        public void BeginAutoMount(bool forceRestart = false)
        {
            if (!autoMountOnStart || _mounted)
                return;

            if (_mountRoutineRunning)
            {
                if (!forceRestart)
                    return;
                StopAllCoroutines();
                _mountRoutineRunning = false;
            }

            StartCoroutine(MountWhenPlayerReady());
        }

        void OnDestroy()
        {
            if (vehiclePath == null)
                return;

            vehiclePath.onApproachingPathEnd.RemoveListener(OnPathEndTransition);
            vehiclePath.onPathComplete.RemoveListener(OnPathEndTransition);
        }

        IEnumerator MountWhenPlayerReady()
        {
            _mountRoutineRunning = true;

            if (delayBeforeMountSeconds > 0f)
                yield return new WaitForSecondsRealtime(delayBeforeMountSeconds);

            const float timeoutSeconds = 45f;
            float elapsed = 0f;
            while (!_mounted && elapsed < timeoutSeconds)
            {
                TryMount();
                if (_mounted)
                    break;

                yield return new WaitForSecondsRealtime(0.15f);
                elapsed += 0.15f;
            }

            _mountRoutineRunning = false;

            if (_mounted)
                Debug.Log($"[{nameof(VrPassengerRide)}] Игрок посажен в {seatAnchor?.name}.");
            else
                Debug.LogError($"[{nameof(VrPassengerRide)}] {name}: не удалось посадить игрока за {timeoutSeconds} с.");
        }

        public void TryMount()
        {
            if (_mounted) return;
            if (seatAnchor == null)
            {
                Debug.LogWarning($"[{nameof(VrPassengerRide)}] {name}: не назначен seatAnchor.");
                return;
            }

            _playerRoot = ResolvePlayerRig();
            if (_playerRoot == null)
                return;

            _xrOrigin = _playerRoot.GetComponent<XROrigin>();
            if (_xrOrigin != null)
            {
                _prevTrackingMode = _xrOrigin.RequestedTrackingOriginMode;
                _xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            }

            CacheLocomotionAndCc(_playerRoot);

            _originalParent = _playerRoot.parent;
            _originalLocalPosition = _playerRoot.localPosition;
            _originalLocalRotation = _playerRoot.localRotation;
            _originalWorldPosition = _playerRoot.position;
            _originalWorldRotation = _playerRoot.rotation;

            _locomotionMediator = _playerRoot.GetComponentInChildren<LocomotionMediator>(true);
            if (_locomotionMediator != null)
            {
                _mediatorWasEnabled = _locomotionMediator.enabled;
                _locomotionMediator.enabled = false;
            }

            DisableLocomotionAndCc();
            DisablePlayerColliders();

            _playerRoot.SetParent(seatAnchor, worldPositionStays: false);
            _playerRoot.localPosition = Vector3.zero;
            _playerRoot.localRotation = Quaternion.identity;

            _mounted = true;

            if (vehiclePath != null)
            {
                vehiclePath.SetGroundRaycastIgnoreRoot(_playerRoot);
                vehiclePath.BeginDrive();
            }
        }

        public void TryUnmount()
        {
            if (!_mounted || _playerRoot == null) return;

            _playerRoot.SetParent(_originalParent, worldPositionStays: false);
            _playerRoot.localPosition = _originalLocalPosition;
            _playerRoot.localRotation = _originalLocalRotation;
            _playerRoot.SetPositionAndRotation(_originalWorldPosition, _originalWorldRotation);

            if (vehiclePath != null)
                vehiclePath.ClearGroundRaycastIgnoreRoot();

            if (_locomotionMediator != null)
                _locomotionMediator.enabled = _mediatorWasEnabled;

            if (_xrOrigin != null)
                _xrOrigin.RequestedTrackingOriginMode = _prevTrackingMode;

            RestoreLocomotionAndCc();
            RestorePlayerColliders();

            _mounted = false;
            _playerRoot = null;
            _xrOrigin = null;
            _locomotionMediator = null;
        }

        void OnPathEndTransition()
        {
            if (_pathCompleteHandled)
                return;

            _pathCompleteHandled = true;
            Debug.Log($"[{nameof(VrPassengerRide)}] Старт перехода в HouseScene (fade, затем телепорт).");

            if (loadHouseWhenPathComplete)
                SceneTransitionRunner.LoadHouseFromRoad(fadeToBlackSeconds, fadeInSeconds);
            else if (unmountWhenPathCompletes)
                TryUnmount();
        }

        Transform ResolvePlayerRig()
        {
            if (playerRigOverride != null)
                return playerRigOverride;

            if (!string.IsNullOrEmpty(playerObjectName))
            {
                var byName = GameObject.Find(playerObjectName);
                if (byName != null)
                    return byName.transform;
            }

            var xrOrigin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (xrOrigin != null)
                return xrOrigin.transform;

            var mainCam = Camera.main;
            if (mainCam != null)
                return mainCam.transform.root;

            var cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (cam != null)
                return cam.transform.root;

            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null && tagged.GetComponentInChildren<XROrigin>(true) != null)
                return tagged.transform;

            Debug.LogWarning($"[{nameof(VrPassengerRide)}] Игрок не найден (override / \"{playerObjectName}\" / XROrigin).");
            return null;
        }

        void CacheLocomotionAndCc(Transform root)
        {
            _locomotionStates.Clear();
            _ccStates.Clear();

            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (IsLocomotionBehaviour(mb))
                    _locomotionStates.Add((mb, mb.enabled));
            }

            foreach (var cc in root.GetComponentsInChildren<CharacterController>(true))
                _ccStates.Add((cc, cc.enabled, cc.detectCollisions));
        }

        static bool IsLocomotionBehaviour(MonoBehaviour mb)
        {
            if (mb is LocomotionProvider)
                return true;
            var typeName = mb.GetType().Name;
            return typeName.Contains("TunnelingVignette");
        }

        void DisableLocomotionAndCc()
        {
            foreach (var (behaviour, _) in _locomotionStates)
            {
                if (behaviour != null) behaviour.enabled = false;
            }

            foreach (var (cc, _, _) in _ccStates)
            {
                if (cc == null) continue;
                cc.detectCollisions = false;
                cc.enabled = false;
            }
        }

        void RestoreLocomotionAndCc()
        {
            foreach (var (behaviour, wasEnabled) in _locomotionStates)
            {
                if (behaviour != null) behaviour.enabled = wasEnabled;
            }

            foreach (var (cc, wasEnabled, detectCollisions) in _ccStates)
            {
                if (cc == null) continue;
                cc.enabled = wasEnabled;
                cc.detectCollisions = detectCollisions;
            }

            _locomotionStates.Clear();
            _ccStates.Clear();
        }

        void DisablePlayerColliders()
        {
            _colliderStates.Clear();
            foreach (var col in _playerRoot.GetComponentsInChildren<Collider>(true))
            {
                if (col == null) continue;
                if (ShouldKeepColliderForInteraction(col))
                    continue;
                _colliderStates.Add((col, col.enabled));
                col.enabled = false;
            }
        }

        static bool ShouldKeepColliderForInteraction(Collider col)
        {
            if (col.GetComponent<XRBaseInteractor>() != null)
                return true;
            if (col.GetComponentInParent<XRBaseInteractor>() != null)
                return true;

            var t = col.transform;
            while (t != null)
            {
                var n = t.name;
                if (n.Contains("Interactor") || n.Contains("NearFar") || n.Contains("Poke"))
                    return true;
                t = t.parent;
            }

            return false;
        }

        void RestorePlayerColliders()
        {
            foreach (var (col, wasEnabled) in _colliderStates)
            {
                if (col != null) col.enabled = wasEnabled;
            }

            _colliderStates.Clear();
        }
    }
}
