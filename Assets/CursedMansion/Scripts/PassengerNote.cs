using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace CursedMansion
{
    /// <summary>
    /// Записка: руками «берётся» (select + визуал у руки), машину не двигает.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(BoxCollider))]
    [DefaultExecutionOrder(100)]
    public class PassengerNote : MonoBehaviour
    {
        const int InteractablePhysicsLayer = 11;
        const int GrabInteractionLayers = 2045;

        [SerializeField] TextMeshProUGUI noteText;
        [SerializeField] GameObject worldCanvas;
        [SerializeField] Transform visualRoot;
        [SerializeField] Vector3 grabColliderSize = new(0.42f, 0.52f, 0.06f);

        XRGrabInteractable _grab;
        BoxCollider _grabCollider;
        Transform _paper;
        Transform _heldFollowTarget;
        Vector3 _restLocalPosition;
        Quaternion _restLocalRotation;
        Vector3 _paperRestLocalPosition;
        Quaternion _paperRestLocalRotation;
        readonly List<Collider> _carColliders = new();

        void Awake()
        {
            if (noteText == null)
                noteText = GetComponentInChildren<TextMeshProUGUI>(true);

            _paper = transform.Find("Paper");
            if (visualRoot == null)
                visualRoot = _paper != null ? _paper : transform;

            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
            if (visualRoot != null)
            {
                _paperRestLocalPosition = visualRoot.localPosition;
                _paperRestLocalRotation = visualRoot.localRotation;
            }
        }

        void Start()
        {
            CacheCarColliders();
            SetupRigidbody();
            SetupCollider();
            IgnoreCollisionsWithCar();
            ConfigureGrab();
        }

        void OnDestroy()
        {
            if (_grab == null) return;
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }

        void LateUpdate()
        {
            if (_grab != null && _grab.isSelected)
                BlockPhysicsPull();

            if (_heldFollowTarget != null && visualRoot != null && _grab != null && _grab.isSelected)
            {
                visualRoot.position = _heldFollowTarget.position;
                visualRoot.rotation = _heldFollowTarget.rotation;
            }
        }

        void CacheCarColliders()
        {
            _carColliders.Clear();
            if (transform.parent == null)
                return;

            foreach (var col in transform.parent.GetComponentsInChildren<Collider>(true))
            {
                if (col != null && col.transform != transform && !col.transform.IsChildOf(transform))
                    _carColliders.Add(col);
            }
        }

        void IgnoreCollisionsWithCar()
        {
            var noteCols = GetComponentsInChildren<Collider>(true);
            foreach (var noteCol in noteCols)
            {
                if (noteCol == null) continue;
                foreach (var carCol in _carColliders)
                {
                    if (carCol != null)
                        Physics.IgnoreCollision(noteCol, carCol, true);
                }
            }
        }

        void SetupRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.mass = 0.001f;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        void SetupCollider()
        {
            SetLayerRecursive(transform, InteractablePhysicsLayer);

            if (TryGetComponent<SphereCollider>(out var sphere))
                Destroy(sphere);

            _grabCollider = GetComponent<BoxCollider>();
            _grabCollider.isTrigger = false;
            _grabCollider.size = grabColliderSize;
            _grabCollider.center = Vector3.zero;
            _grabCollider.enabled = true;
        }

        void ConfigureGrab()
        {
            if (TryGetComponent<XRSimpleInteractable>(out var simple))
                Destroy(simple);

            _grab = GetComponent<XRGrabInteractable>();

            // Не двигаем rigidbody записки — только select + визуал у руки.
            _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            _grab.trackPosition = false;
            _grab.trackRotation = false;
            _grab.retainTransformParent = true;
            _grab.throwOnDetach = false;
            _grab.snapToColliderVolume = false;
            _grab.useDynamicAttach = true;
            _grab.addDefaultGrabTransformers = false;

            _grab.interactionLayers = new InteractionLayerMask { value = GrabInteractionLayers };

            _grab.colliders.Clear();
            _grab.colliders.Add(_grabCollider);

            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
            _grab.selectExited.AddListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            _heldFollowTarget = args.interactorObject.GetAttachTransform(_grab);
            BlockPhysicsPull();
            ShowNote();
        }

        void OnReleased(SelectExitEventArgs _)
        {
            _heldFollowTarget = null;
            if (visualRoot != null)
            {
                visualRoot.localPosition = _paperRestLocalPosition;
                visualRoot.localRotation = _paperRestLocalRotation;
            }

            transform.localPosition = _restLocalPosition;
            transform.localRotation = _restLocalRotation;
            SetupRigidbody();
        }

        void BlockPhysicsPull()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            _grab.trackPosition = false;
            _grab.trackRotation = false;

            foreach (var interactor in _grab.interactorsSelecting)
            {
                if (interactor == null) continue;
                var handRb = interactor.transform.GetComponentInParent<Rigidbody>();
                if (handRb == null) continue;
                foreach (var joint in handRb.GetComponents<FixedJoint>())
                {
                    if (joint != null && joint.connectedBody == rb)
                        Destroy(joint);
                }
            }
        }

        static void SetLayerRecursive(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursive(root.GetChild(i), layer);
        }

        void ShowNote()
        {
            var body = noteText != null ? noteText.text : "…";
            NoteOverlay.ShowNote("Записка", body);
            if (worldCanvas != null)
                worldCanvas.SetActive(true);
        }
    }
}
