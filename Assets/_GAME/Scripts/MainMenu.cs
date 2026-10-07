using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Главное меню в World Space Canvas: кнопки нажимаются лучом Ray Interactor.
// Меню само ничего не спавнит — оно передаёт нужный LevelConfig в EnemySpawner.StartLevel.
public class MainMenu : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelsPanel;

    [Header("Главное меню")]
    [SerializeField] Button playButton;
    [SerializeField] Button levelsButton;
    [SerializeField] Button exitButton;

    [Header("Выбор уровня")]
    // levelButtons[i] запускает levels[i] (индекс 0 = Уровень 1)
    [SerializeField] Button[] levelButtons;
    [SerializeField] Button backButton;

    [Header("Уровни")]
    [SerializeField] EnemySpawner spawner;
    [SerializeField] LevelConfig[] levels;

    [Header("Положение перед игроком")]
    [SerializeField] float distance = 1.5f;
    [SerializeField] float heightOffset = 0f;

    void Awake()
    {
        playButton.onClick.AddListener(() => StartLevel(0));
        levelsButton.onClick.AddListener(ShowLevels);
        exitButton.onClick.AddListener(Quit);
        backButton.onClick.AddListener(ShowMain);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i; // копия для замыкания
            levelButtons[i].onClick.AddListener(() => StartLevel(index));
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    void Start()
    {
        ShowMain();
        StartCoroutine(PlaceInFrontOfPlayer());
    }

    // Ставим панель перед глазами игрока. Ждём пару кадров, чтобы XR-трекинг
    // успел выставить позицию камеры (в первом кадре она может быть нулевой).
    IEnumerator PlaceInFrontOfPlayer()
    {
        yield return null;
        yield return null;

        Camera cam = Camera.main;
        if (cam == null)
            yield break;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        transform.position = cam.transform.position + forward * distance + Vector3.up * heightOffset;
        // Лицевая сторона Canvas смотрит по -Z, поэтому разворачиваем по направлению взгляда
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void ShowMain()
    {
        mainPanel.SetActive(true);
        levelsPanel.SetActive(false);
    }

    void ShowLevels()
    {
        RefreshLevelButtons();
        mainPanel.SetActive(false);
        levelsPanel.SetActive(true);
    }

    // Закрытые уровни — серые и некликабельные
    void RefreshLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            bool unlocked = LevelProgress.IsUnlocked(levelNumber) && i < levels.Length && levels[i] != null;
            levelButtons[i].interactable = unlocked;

            Text label = levelButtons[i].GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = unlocked ? $"Уровень {levelNumber}" : $"Уровень {levelNumber} (закрыт)";
                label.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            }
        }
    }

    void StartLevel(int index)
    {
        if (spawner == null)
        {
            Debug.LogError("[MainMenu] Не назначен EnemySpawner.");
            return;
        }

        if (index < 0 || index >= levels.Length || levels[index] == null)
        {
            Debug.LogError($"[MainMenu] Нет LevelConfig для уровня {index + 1}.");
            return;
        }

        if (!LevelProgress.IsUnlocked(index + 1))
        {
            Debug.LogWarning($"[MainMenu] Уровень {index + 1} ещё закрыт.");
            return;
        }

        spawner.StartLevel(levels[index]);
        // Прячем меню, пока идёт уровень
        gameObject.SetActive(false);
    }

    void Quit()
    {
        Debug.Log("[MainMenu] Выход из игры.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
