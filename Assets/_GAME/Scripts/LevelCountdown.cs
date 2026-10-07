using UnityEngine;
using UnityEngine.UI;

// Отсчёт перед первой волной ("Приготовься! 8 ... 1").
// Маленький World Space Canvas перед камерой, создаётся из кода (как DamageFlash), в сцене ничего настраивать не нужно.
// Не перехватывает луч UI (нет GraphicRaycaster, raycastTarget = false). Скрыт, пока игра не в состоянии Playing (пауза).
public class LevelCountdown : MonoBehaviour
{
    const float Distance = 1.5f;
    // Чуть ниже линии взгляда, чтобы не закрывать порталы
    const float DropBelowEyes = 0.15f;
    // 1000 px * 0.001 = 1 м ширины
    const float PixelSize = 0.001f;

    Canvas canvas;
    Text label;
    bool shown;

    // Создаёт отсчёт дочерним объектом камеры
    public static LevelCountdown Create(Camera cam)
    {
        var go = new GameObject("Level Countdown");
        go.layer = cam.gameObject.layer;
        go.transform.SetParent(cam.transform, false);
        var countdown = go.AddComponent<LevelCountdown>();
        countdown.Build(cam);
        return countdown;
    }

    void Build(Camera cam)
    {
        transform.localPosition = new Vector3(0f, -DropBelowEyes, Distance);
        transform.localRotation = Quaternion.identity;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.sortingOrder = 500;

        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(1000f, 300f);
        transform.localScale = Vector3.one * PixelSize;

        var labelGo = new GameObject("Text", typeof(RectTransform));
        labelGo.layer = gameObject.layer;
        labelGo.transform.SetParent(transform, false);
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelGo.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 90;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = new Color(1f, 0.85f, 0.3f);
        label.raycastTarget = false;
        var outline = labelGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        Hide();
    }

    public void Show(int secondsLeft)
    {
        shown = true;
        label.text = $"Приготовься!\nМонстры через {secondsLeft}";
        UpdateVisibility();
    }

    public void Hide()
    {
        shown = false;
        UpdateVisibility();
    }

    void Update() => UpdateVisibility();

    void UpdateVisibility()
    {
        bool playing = GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying;
        canvas.enabled = shown && playing;
    }
}
