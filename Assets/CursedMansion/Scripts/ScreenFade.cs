using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CursedMansion
{
    /// <summary>
    /// Чёрная панель-дочка камеры (стабильно в VR, без world-space Canvas).
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        public static ScreenFade Instance { get; private set; }

        static float s_FadeInAfterLoadSeconds = -1f;

        MeshRenderer _renderer;
        Material _material;
        float _alpha;

        public static ScreenFade Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(ScreenFade));
            go.AddComponent<ScreenFade>();
            return Instance;
        }

        public static void RequestFadeInAfterLoad(float seconds) => s_FadeInAfterLoadSeconds = seconds;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
                Instance = null;

            if (_material != null)
                Destroy(_material);
        }

        void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            AttachToCamera();

            if (s_FadeInAfterLoadSeconds <= 0f)
                return;

            float duration = s_FadeInAfterLoadSeconds;
            s_FadeInAfterLoadSeconds = -1f;
            StartCoroutine(FadeFromBlack(duration));
        }

        public IEnumerator FadeToBlack(float duration)
        {
            yield return WaitForCamera();
            AttachToCamera();
            SetQuadVisible(true);
            yield return AnimateAlpha(0f, 1f, duration);
            SetAlpha(1f);
        }

        public IEnumerator FadeFromBlack(float duration)
        {
            yield return WaitForCamera();
            AttachToCamera();
            SetAlpha(1f);
            SetQuadVisible(true);
            yield return AnimateAlpha(1f, 0f, duration);
            SetAlpha(0f);
            SetQuadVisible(false);
        }

        IEnumerator WaitForCamera()
        {
            const float timeout = 10f;
            float elapsed = 0f;
            while (ResolveCamera() == null && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void AttachToCamera()
        {
            var cam = ResolveCamera();
            if (cam == null)
                return;

            if (_renderer == null)
                CreateQuad(cam);

            var quad = _renderer.transform;
            if (quad.parent != cam.transform)
            {
                quad.SetParent(cam.transform, false);
                quad.localPosition = new Vector3(0f, 0f, cam.nearClipPlane + 0.02f);
                quad.localRotation = Quaternion.identity;

                float dist = cam.nearClipPlane + 0.02f;
                float height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float width = height * cam.aspect;
                quad.localScale = new Vector3(width, height, 1f);
            }
        }

        void CreateQuad(Camera cam)
        {
            var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGo.name = "FadeQuad";
            Destroy(quadGo.GetComponent<Collider>());
            quadGo.transform.SetParent(cam.transform, false);

            _renderer = quadGo.GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            _material = new Material(shader);
            _material.color = new Color(0f, 0f, 0f, 0f);
            _material.renderQueue = 5000;
            _renderer.material = _material;

            quadGo.transform.localPosition = new Vector3(0f, 0f, cam.nearClipPlane + 0.02f);
            quadGo.transform.localRotation = Quaternion.identity;
            SetQuadVisible(false);
        }

        void SetQuadVisible(bool visible)
        {
            if (_renderer != null)
                _renderer.enabled = visible;
        }

        void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            if (_material != null)
                _material.color = new Color(0f, 0f, 0f, _alpha);
        }

        IEnumerator AnimateAlpha(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetAlpha(to);
        }

        static Camera ResolveCamera()
        {
            var xrOrigin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (xrOrigin != null && xrOrigin.Camera != null)
                return xrOrigin.Camera;

            return Camera.main;
        }
    }
}
