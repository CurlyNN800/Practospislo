using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Настройки")]
    public string gameSceneName = "HouseScene";

    // UI панели
    private GameObject _mainPanel;
    private GameObject _settingsPanel;

    // Цветовая палитра
    private readonly Color _bgColor        = new Color(0.04f, 0.03f, 0.06f, 1f);
    private readonly Color _panelColor     = new Color(0.08f, 0.05f, 0.10f, 0.95f);
    private readonly Color _btnNormal      = new Color(0.35f, 0.05f, 0.05f, 1f);
    private readonly Color _btnHighlight   = new Color(0.55f, 0.10f, 0.10f, 1f);
    private readonly Color _btnPressed     = new Color(0.20f, 0.02f, 0.02f, 1f);
    private readonly Color _textColor      = new Color(0.95f, 0.88f, 0.75f, 1f);
    private readonly Color _titleColor     = new Color(0.95f, 0.65f, 0.20f, 1f);
    private readonly Color _separatorColor = new Color(0.60f, 0.10f, 0.10f, 1f);

    void Awake()
    {
        BuildUI();
    }

    // ─────────────────────────────────────────────────────────────
    //  Построение всего UI
    // ─────────────────────────────────────────────────────────────

    void BuildUI()
    {
        // Фон-камера
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("MainCamera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _bgColor;

        // Canvas
        GameObject canvasGo = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Полупрозрачный оверлей поверх bg
        CreateImage(canvasGo, "Overlay", Vector2.zero, new Vector2(1, 1),
                    new Color(0f, 0f, 0f, 0.35f), Vector2.zero, Vector2.one);

        // Главная панель
        _mainPanel = BuildMainPanel(canvasGo);

        // Панель настроек
        _settingsPanel = BuildSettingsPanel(canvasGo);
        _settingsPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Главное меню
    // ─────────────────────────────────────────────────────────────

    GameObject BuildMainPanel(GameObject canvas)
    {
        // Центральная карточка 420×560
        GameObject panel = CreateImage(canvas, "MainPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            _panelColor, null, null, new Vector2(420, 560));
        AddOutline(panel, _separatorColor);

        // Заголовок "CURSED"
        CreateText(panel, "TitleTop", "CURSED",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -30), new Vector2(380, 70),
            44, FontStyle.Bold, _titleColor, TextAnchor.MiddleCenter);

        // Заголовок "MANSION"
        CreateText(panel, "TitleBottom", "MANSION",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -90), new Vector2(380, 70),
            44, FontStyle.Bold, _titleColor, TextAnchor.MiddleCenter);

        // Декоративная линия
        CreateImage(panel, "Separator",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            _separatorColor, null, null, new Vector2(340, 2),
            new Vector2(0, -168));

        // Кнопки
        CreateMenuButton(panel, "PlayBtn",    "▶  ИГРАТЬ",    new Vector2(0, -220), OnPlay);
        CreateMenuButton(panel, "SettingsBtn","⚙  НАСТРОЙКИ", new Vector2(0, -305), OnSettings);
        CreateMenuButton(panel, "QuitBtn",    "✕  ВЫХОД",     new Vector2(0, -390), OnQuit);

        // Версия внизу
        CreateText(panel, "Version", "v0.1 alpha",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 18), new Vector2(380, 24),
            14, FontStyle.Italic, new Color(0.5f, 0.4f, 0.3f, 1f),
            TextAnchor.MiddleCenter);

        return panel;
    }

    // ─────────────────────────────────────────────────────────────
    //  Панель настроек
    // ─────────────────────────────────────────────────────────────

    GameObject BuildSettingsPanel(GameObject canvas)
    {
        GameObject panel = CreateImage(canvas, "SettingsPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            _panelColor, null, null, new Vector2(420, 420));
        AddOutline(panel, _separatorColor);

        CreateText(panel, "SettingsTitle", "НАСТРОЙКИ",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -40), new Vector2(380, 60),
            36, FontStyle.Bold, _titleColor, TextAnchor.MiddleCenter);

        CreateImage(panel, "Sep",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            _separatorColor, null, null, new Vector2(340, 2),
            new Vector2(0, -110));

        // Метка громкости
        CreateText(panel, "VolumeLabel", "ГРОМКОСТЬ",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -140), new Vector2(380, 32),
            20, FontStyle.Normal, _textColor, TextAnchor.MiddleCenter);

        // Слайдер громкости
        CreateVolumeSlider(panel, new Vector2(0, -190));

        // Кнопка назад
        CreateMenuButton(panel, "BackBtn", "◀  НАЗАД", new Vector2(0, -310), OnBack);

        return panel;
    }

    // ─────────────────────────────────────────────────────────────
    //  Обработчики кнопок
    // ─────────────────────────────────────────────────────────────

    void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void OnSettings()
    {
        _mainPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    void OnBack()
    {
        _settingsPanel.SetActive(false);
        _mainPanel.SetActive(true);
    }

    void OnQuit() => GameApplicationQuit.Quit();

    // ─────────────────────────────────────────────────────────────
    //  Вспомогательные методы создания UI
    // ─────────────────────────────────────────────────────────────

    GameObject CreateImage(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
        Vector2? pivot = null, Vector2? anchoredPos = null,
        Vector2? size = null, Vector2? offset = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos ?? offset ?? Vector2.zero;
        if (size.HasValue) rt.sizeDelta = size.Value;

        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    void CreateText(GameObject parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size,
        int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = fontStyle;
        txt.color = color;
        txt.alignment = alignment;
    }

    void CreateMenuButton(GameObject parent, string name, string label,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        // Фон кнопки
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(320, 58);

        Image img = go.AddComponent<Image>();
        img.color = _btnNormal;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = _btnNormal;
        cb.highlightedColor = _btnHighlight;
        cb.pressedColor     = _btnPressed;
        cb.selectedColor    = _btnNormal;
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        AddOutline(go, new Color(0.7f, 0.15f, 0.15f, 0.6f));

        // Текст кнопки
        GameObject txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);

        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        Text txt = txtGo.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = _textColor;
        txt.alignment = TextAnchor.MiddleCenter;
    }

    void CreateVolumeSlider(GameObject parent, Vector2 anchoredPos)
    {
        GameObject go = new GameObject("VolumeSlider");
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(320, 30);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.08f, 0.08f, 1f);
        slider.targetGraphic = bgImg;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.sizeDelta = new Vector2(-10f, 0f);
        faRt.anchoredPosition = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0.5f, 1f);
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.65f, 0.12f, 0.12f, 1f);
        slider.fillRect = fillRt;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = _titleColor;
        slider.handleRect = handleRt;

        slider.onValueChanged.AddListener(v => AudioListener.volume = v);
    }

    void AddOutline(GameObject go, Color color)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
