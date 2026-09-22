using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRFPSKit;

public class VictoryChecker : MonoBehaviour
{
    [Header("Victory condition")]
    public string stickTag = "Stick";
    public int sticksNeeded = 3;

    [Header("Scene transition")]
    [Tooltip("Имя сцены для перехода после победы")]
    public string nextScene = "Interior 2";

    [Tooltip("Длительность плавного затемнения (секунды)")]
    public float fadeDuration = 2f;

    readonly List<GameObject> _itemsInZone = new List<GameObject>();
    bool _isWon;
    float _fadeAlpha = 0f;
    Texture2D _fadeTexture;

    void Awake()
    {
        // Создаём 1x1 чёрный пиксель для IMGUI-затемнения
        _fadeTexture = new Texture2D(1, 1);
        _fadeTexture.SetPixel(0, 0, Color.black);
        _fadeTexture.Apply();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isWon || !other.CompareTag(stickTag)) return;

        if (!_itemsInZone.Contains(other.gameObject))
        {
            _itemsInZone.Add(other.gameObject);
            Debug.Log($"[VictoryChecker] Стик на столе: {_itemsInZone.Count}/{sticksNeeded}");
            CheckVictory();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_isWon) return;
        _itemsInZone.Remove(other.gameObject);
    }

    void CheckVictory()
    {
        if (_itemsInZone.Count < sticksNeeded) return;

        _isWon = true;
        Debug.Log("[VictoryChecker] Победа — проклятье снято. Переход на сцену: " + nextScene);

        FreezeWorldExceptPlayer();
        StartCoroutine(FadeAndLoadScene());
    }

    IEnumerator FadeAndLoadScene()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeAlpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _fadeAlpha = 1f;

        SceneManager.LoadScene(nextScene);
    }

    // Рисуем чёрный прямоугольник поверх всего экрана через IMGUI (без Canvas)
    void OnGUI()
    {
        if (_fadeAlpha <= 0f) return;

        GUI.color = new Color(0f, 0f, 0f, _fadeAlpha);
        GUI.depth = -100; // Поверх всех GUI-элементов
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _fadeTexture);
        GUI.color = Color.white;
    }

    void FreezeWorldExceptPlayer()
    {
        foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ai != null && ai.enabled)
                ai.enabled = false;
        }

        foreach (var health in Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (health != null && health.enabled)
                health.enabled = false;
        }

        foreach (var agent in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (agent == null || !agent.enabled) continue;
            if (agent.CompareTag("Player") || agent.transform.root.CompareTag("Player")) continue;
            agent.isStopped = true;
            agent.enabled = false;
        }
    }
}