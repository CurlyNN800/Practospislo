using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Красная вспышка урона без пост-обработки (Post Processing на камере выключен ради passthrough).
// Полупрозрачный красный Image на маленьком World Space Canvas прямо перед камерой.
// По умолчанию невидим; Flash() — резкая вспышка до flashAlpha и плавное затухание.
// Не перехватывает луч UI (нет GraphicRaycaster, raycastTarget = false) и физику (нет коллайдеров).
public class DamageFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = new(0.8f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] float flashAlpha = 0.4f;
    [SerializeField] float fadeDuration = 0.5f;
    // Чуть дальше near clip plane камеры (0.01), но ближе рук и оружия
    [SerializeField] float distanceFromCamera = 0.05f;
    // Размер плоскости в метрах — с запасом перекрывает поле зрения шлема на этой дистанции
    [SerializeField] float size = 0.5f;

    Image image;
    Coroutine fade;

    // Создаёт вспышку дочерним объектом камеры
    public static DamageFlash Create(Camera cam)
    {
        var go = new GameObject("Damage Flash");
        go.layer = cam.gameObject.layer;
        go.transform.SetParent(cam.transform, false);
        var flash = go.AddComponent<DamageFlash>();
        flash.Build(cam);
        return flash;
    }

    void Build(Camera cam)
    {
        transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
        transform.localRotation = Quaternion.identity;

        // 1000 px * 0.0005 = 0.5 м
        const float pixels = 1000f;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        // Поверх остальных World Space Canvas (меню)
        canvas.sortingOrder = 1000;

        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(pixels, pixels);
        transform.localScale = Vector3.one * (size / pixels);

        var imageGo = new GameObject("Flash", typeof(RectTransform));
        imageGo.layer = gameObject.layer;
        imageGo.transform.SetParent(transform, false);
        var imageRect = (RectTransform)imageGo.transform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        image = imageGo.AddComponent<Image>();
        image.raycastTarget = false;
        SetAlpha(0f);
    }

    public void Flash()
    {
        if (image == null)
            return;
        if (fade != null)
            StopCoroutine(fade);
        fade = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        image.enabled = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            SetAlpha(Mathf.Lerp(flashAlpha, 0f, t / fadeDuration));
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        SetAlpha(0f);
        fade = null;
    }

    void SetAlpha(float alpha)
    {
        Color c = flashColor;
        c.a = alpha;
        image.color = c;
        // Полностью прозрачную плоскость не рисуем вовсе
        image.enabled = alpha > 0f;
    }
}
