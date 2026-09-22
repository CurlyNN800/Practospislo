using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VolumetricLines;

namespace CursedMansion
{
    /// <summary>
    /// Делает объект хватаемым VR-контроллерами (Rigidbody + Collider + XRGrabInteractable).
    /// </summary>
    [DisallowMultipleComponent]
    public class VrGrabbableSetup : MonoBehaviour
    {
        const int InteractablePhysicsLayer = 11;
        const int GrabInteractionLayers = 2045;

        [SerializeField] bool applyOnAwake = true;
        [SerializeField] Vector3 walkieTalkieColliderSize = new(0.09f, 0.22f, 0.05f);
        [SerializeField] float lineColliderThickness = 0.12f;
        [SerializeField] float minLineColliderLength = 0.15f;

        public void Configure()
        {
            var existingGrab = GetComponent<XRGrabInteractable>();
            if (existingGrab != null &&
                GetComponent<Collider>() != null &&
                GetComponent<Rigidbody>() != null &&
                existingGrab.colliders.Count > 0)
                return;

            SetLayerRecursive(transform, InteractablePhysicsLayer);

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = Mathf.Max(rb.mass, 0.25f);

            ConfigureCollider();

            if (TryGetComponent<XRSimpleInteractable>(out var simple))
                Destroy(simple);

            var grab = GetComponent<XRGrabInteractable>();
            if (grab == null)
                grab = gameObject.AddComponent<XRGrabInteractable>();

            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = true;
            grab.useDynamicAttach = true;
            grab.snapToColliderVolume = true;
            grab.interactionLayers = new InteractionLayerMask { value = GrabInteractionLayers };

            grab.colliders.Clear();
            foreach (var col in GetComponents<Collider>())
            {
                if (col != null && col.enabled)
                    grab.colliders.Add(col);
            }
        }

        void Awake()
        {
            if (applyOnAwake)
                Configure();
        }

        void ConfigureCollider()
        {
            foreach (var col in GetComponents<Collider>())
            {
                if (col is MeshCollider mesh && !mesh.convex)
                    Destroy(col);
            }

            var line = GetComponent<VolumetricLineBehavior>();
            if (line != null)
            {
                ConfigureLineCollider(line);
                return;
            }

            if (GetComponent<VolumetricLineStripBehavior>() != null)
            {
                ConfigureRendererBoundsCollider();
                return;
            }

            if (name.Contains("WalkieTalkie", System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigureWalkieCollider();
                return;
            }

            ConfigureRendererBoundsCollider();
        }

        void ConfigureLineCollider(BoxCollider box, VolumetricLineBehavior line)
        {
            var start = line.StartPos;
            var end = line.EndPos;
            var delta = end - start;
            var length = delta.magnitude;

            if (length < 0.001f)
            {
                box.size = Vector3.one * lineColliderThickness;
                box.center = start;
                return;
            }

            var dir = delta / length;
            length = Mathf.Max(length, minLineColliderLength);
            var thickness = Mathf.Max(line.LineWidth * 0.01f, lineColliderThickness);

            box.size = new Vector3(length, thickness, thickness);
            box.center = (start + end) * 0.5f;
        }

        void ConfigureLineCollider(VolumetricLineBehavior line)
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            ConfigureLineCollider(box, line);
        }

        void ConfigureWalkieCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.size = walkieTalkieColliderSize;
            box.center = Vector3.zero;

            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            box.center = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(
                Mathf.Max(Mathf.Abs(localSize.x), walkieTalkieColliderSize.x),
                Mathf.Max(Mathf.Abs(localSize.y), walkieTalkieColliderSize.y),
                Mathf.Max(Mathf.Abs(localSize.z), walkieTalkieColliderSize.z));
        }

        void ConfigureRendererBoundsCollider()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                var fallback = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
                fallback.isTrigger = false;
                fallback.size = Vector3.one * 0.1f;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(
                Mathf.Max(Mathf.Abs(localSize.x), 0.05f),
                Mathf.Max(Mathf.Abs(localSize.y), 0.05f),
                Mathf.Max(Mathf.Abs(localSize.z), 0.05f));
        }

        static void SetLayerRecursive(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
                SetLayerRecursive(root.GetChild(i), layer);
        }
    }
}
