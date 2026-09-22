using UnityEngine;
using UnityEngine.UI;

namespace CursedMansion
{
    /// <summary>
    /// Простой оверлей для VR: world-space canvas у камеры на несколько секунд.
    /// </summary>
    public class NoteOverlay : MonoBehaviour
    {
        public static NoteOverlay Instance { get; private set; }

        [SerializeField] float autoHideSeconds = 12f;

        Canvas _canvas;
        Text _text;
        float _hideTime;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUi();
        }

        void BuildUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var img = panel.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.72f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            _text = textGo.GetComponent<Text>();
            _text.color = Color.white;
            _text.fontSize = 28;
            _text.supportRichText = true;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.08f, 0.12f);
            trt.anchorMax = new Vector2(0.92f, 0.88f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _canvas.enabled = false;
        }

        void Update()
        {
            if (_canvas == null || !_canvas.enabled) return;
            if (Time.unscaledTime >= _hideTime)
                _canvas.enabled = false;
        }

        public static void ShowNote(string title, string body)
        {
            if (Instance == null)
            {
                var go = new GameObject(nameof(NoteOverlay));
                go.AddComponent<NoteOverlay>();
            }

            Instance.Display(title, body);
        }

        void Display(string title, string body)
        {
            if (_canvas == null || _text == null)
            {
                Debug.LogError("[NoteOverlay] UI не инициализирован.");
                return;
            }

            _text.text = $"<b>{title}</b>\n\n{body}";
            _canvas.enabled = true;
            _hideTime = Time.unscaledTime + autoHideSeconds;
        }
    }
}
