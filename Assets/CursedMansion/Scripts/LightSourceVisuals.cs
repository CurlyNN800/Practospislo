using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedMansion
{
    /// <summary>
    /// Adds small visible emissive objects to Light sources so light isn't "from nowhere".
    /// Runtime-only (doesn't touch scenes/assets).
    /// </summary>
    public class LightSourceVisuals : MonoBehaviour
    {
        const string MarkerName = "CM_LightSourceVisual";

        [Header("General")]
        [SerializeField] bool includeDisabledLights = true;
        [SerializeField] float intensityToSize = 0.015f;
        [SerializeField] float minSize = 0.03f;
        [SerializeField] float maxSize = 0.18f;

        [Header("Material")]
        [SerializeField] Color emissionColor = new(1f, 0.86f, 0.68f, 1f);
        [SerializeField] float emissionMultiplier = 2.2f;

        Material _mat;

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene _, LoadSceneMode __) => Apply();

        void Apply()
        {
            var lights = FindObjectsByType<Light>(includeDisabledLights ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (lights == null || lights.Length == 0) return;

            EnsureMaterial();

            foreach (var l in lights)
            {
                if (l == null) continue;
                if (l.type == LightType.Directional) continue;
                if (l.transform.Find(MarkerName) != null) continue;

                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = MarkerName;
                go.transform.SetParent(l.transform, worldPositionStays: false);

                // Place at light position; for spot lights put it slightly behind origin.
                go.transform.localPosition = l.type == LightType.Spot ? new Vector3(0f, 0f, -0.03f) : Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                var size = Mathf.Clamp(l.intensity * intensityToSize, minSize, maxSize);
                go.transform.localScale = Vector3.one * size;

                // No collisions.
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var r = go.GetComponent<MeshRenderer>();
                if (r != null)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    r.sharedMaterial = _mat;
                }
            }
        }

        void EnsureMaterial()
        {
            if (_mat != null) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            _mat = new Material(shader);
            _mat.name = "CM_LightSourceEmissive_Runtime";
            _mat.hideFlags = HideFlags.HideAndDontSave;

            if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", Color.black);
            else if (_mat.HasProperty("_Color"))
                _mat.SetColor("_Color", Color.black);

            var emissive = emissionColor * emissionMultiplier;
            if (_mat.HasProperty("_EmissionColor"))
            {
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", emissive);
            }
        }
    }
}

