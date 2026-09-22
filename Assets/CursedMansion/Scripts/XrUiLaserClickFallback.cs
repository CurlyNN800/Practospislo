using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CursedMansion
{
    /// <summary>
    /// Drives world-space UI clicks from an <see cref="XRRayInteractor"/> when the active EventSystem
    /// uses <c>InputSystemUIInputModule</c> instead of <see cref="XRUIInputModule"/> (no tracked-device UI path).
    /// Uses legacy <see cref="InputDevices"/> for the trigger so it does not depend on Input System wiring.
    /// </summary>
    [RequireComponent(typeof(TrackedDeviceGraphicRaycaster))]
    [DisallowMultipleComponent]
    public sealed class XrUiLaserClickFallback : MonoBehaviour
    {
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
        [SerializeField] XRNode controllerNode = XRNode.RightHand;
        [Tooltip("If set, click detection uses the Input System (recommended for OpenXR). Should match XRRayInteractor UIPress bindings.")]
        [SerializeField] InputActionReference uiPressButton;
        [Tooltip("Optional axis fallback when the button action does not report held state.")]
        [SerializeField] InputActionReference uiPressValue;
        [SerializeField] float triggerPressThreshold = 0.35f;
        [Tooltip("Negative ids are typical for synthetic pointers in uGUI.")]
        [SerializeField] int syntheticPointerId = -234882481;

        TrackedDeviceGraphicRaycaster _raycaster;
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor _cachedRay;
        Vector3[] _linePointsArray;
        readonly List<Vector3> _linePointsList = new List<Vector3>(32);
        readonly List<RaycastResult> _raycastHits = new List<RaycastResult>();
        static readonly List<UnityEngine.XR.InputDevice> s_ControllerDevices = new List<UnityEngine.XR.InputDevice>();
        InputAction _uiPressButtonAction;
        InputAction _uiPressValueAction;

        GameObject _pressedTarget;
        RaycastResult _pressRaycast;
        bool _wasTriggered;

        void Awake() => _raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();

        void OnEnable()
        {
            CacheInputActions();
            _uiPressButtonAction?.Enable();
            _uiPressValueAction?.Enable();
        }

        void OnDisable()
        {
            _uiPressButtonAction?.Disable();
            _uiPressValueAction?.Disable();
        }

        void CacheInputActions()
        {
            _uiPressButtonAction = uiPressButton != null ? uiPressButton.action : null;
            _uiPressValueAction = uiPressValue != null ? uiPressValue.action : null;
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            var es = EventSystem.current;
            if (es == null || _raycaster == null)
                return;

            var ray = EffectiveRay();
            if (ray == null || !ray.isActiveAndEnabled)
                return;

            bool triggered = ReadTriggerPressed();
            bool haveLine = TryCopyLinePoints(ray, out int n) && n >= 2;
            GameObject hitGo = null;
            var raycast = default(RaycastResult);
            bool haveHit = haveLine && TryRaycastAgainstThisCanvas(es, out hitGo, out raycast);

            if (triggered && !_wasTriggered && haveHit)
            {
                _pressedTarget = hitGo;
                _pressRaycast = raycast;
                var down = BuildPointerEventData(es, raycast);
                ExecuteEvents.ExecuteHierarchy(hitGo, down, ExecuteEvents.pointerDownHandler);
            }
            else if (!triggered && _wasTriggered && _pressedTarget != null)
            {
                var up = BuildPointerEventData(es, _pressRaycast);
                up.pointerPress = _pressedTarget;
                ExecuteEvents.ExecuteHierarchy(_pressedTarget, up, ExecuteEvents.pointerUpHandler);

                if (haveHit && IsSameUiBranch(_pressedTarget, hitGo))
                {
                    up.pointerCurrentRaycast = raycast;
                    up.position = raycast.screenPosition;
                    ExecuteEvents.ExecuteHierarchy(hitGo, up, ExecuteEvents.pointerClickHandler);
                }

                _pressedTarget = null;
            }

            _wasTriggered = triggered;
        }

        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor EffectiveRay()
        {
            if (rayInteractor != null)
                return rayInteractor;

            if (_cachedRay != null && _cachedRay.isActiveAndEnabled)
                return _cachedRay;

            _cachedRay = null;
            foreach (var candidate in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.isActiveAndEnabled && candidate.enableUIInteraction)
                {
                    _cachedRay = candidate;
                    break;
                }
            }

            return _cachedRay;
        }

        bool TryCopyLinePoints(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor ray, out int count)
        {
            count = 0;
            if (!ray.GetLinePoints(ref _linePointsArray, out count) || count < 2)
                return false;

            _linePointsList.Clear();
            for (var i = 0; i < count; i++)
                _linePointsList.Add(_linePointsArray[i]);

            return true;
        }

        bool TryRaycastAgainstThisCanvas(EventSystem es, out GameObject hitGo, out RaycastResult rr)
        {
            var tracked = new TrackedDeviceEventData(es)
            {
                pointerId = syntheticPointerId,
                rayPoints = _linePointsList,
                layerMask = Physics.AllLayers,
            };

            _raycastHits.Clear();
            _raycaster.Raycast(tracked, _raycastHits);
            if (_raycastHits.Count == 0)
            {
                hitGo = null;
                rr = default;
                return false;
            }

            rr = _raycastHits[0];
            hitGo = rr.gameObject;
            return true;
        }

        bool ReadTriggerPressed()
        {
            if (_uiPressButtonAction != null && _uiPressButtonAction.controls.Count > 0)
            {
                if (_uiPressButtonAction.IsPressed())
                    return true;
            }

            if (_uiPressValueAction != null && _uiPressValueAction.controls.Count > 0)
            {
                var v = _uiPressValueAction.ReadValue<float>();
                if (v >= triggerPressThreshold)
                    return true;
            }

            if (!TryGetLegacyController(out var dev) || !dev.isValid)
                return false;

            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool button) && button)
                return true;

            return dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float axis) && axis >= triggerPressThreshold;
        }

        bool TryGetLegacyController(out UnityEngine.XR.InputDevice dev)
        {
            dev = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (dev.isValid)
                return true;

            var hand =
                controllerNode == XRNode.RightHand
                    ? InputDeviceCharacteristics.Right
                    : InputDeviceCharacteristics.Left;

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | hand, s_ControllerDevices);

            if (s_ControllerDevices.Count > 0)
            {
                dev = s_ControllerDevices[0];
                return dev.isValid;
            }

            dev = default;
            return false;
        }

        static bool IsSameUiBranch(GameObject a, GameObject b)
        {
            if (a == null || b == null)
                return false;

            return a == b || a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform);
        }

        PointerEventData BuildPointerEventData(EventSystem es, RaycastResult rr)
        {
            return new PointerEventData(es)
            {
                pointerId = syntheticPointerId,
                position = rr.screenPosition,
                button = PointerEventData.InputButton.Left,
                pointerCurrentRaycast = rr,
                pointerPressRaycast = rr,
            };
        }
    }
}
