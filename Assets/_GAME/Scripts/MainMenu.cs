using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Все экраны игры в одном World Space Canvas: главное меню, выбор уровня, пауза, победа, поражение.
// Кнопки нажимаются лучом Ray Interactor. Сам скрипт ничего не решает — он вызывает методы
// GameStateManager и показывает нужную панель, когда менеджер сообщает о смене состояния.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameStateManager gameState;
    // Фон + все панели; прячется целиком, пока идёт игра
    [SerializeField] GameObject content;

    [Header("Панели")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelsPanel;
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;

    [Header("Главное меню")]
    [SerializeField] Button playButton;
    [SerializeField] Button levelsButton;
    [SerializeField] Button exitButton;

    [Header("Выбор уровня")]
    // levelButtons[i] запускает уровень i + 1
    [SerializeField] Button[] levelButtons;
    [SerializeField] Button backButton;

    [Header("Пауза")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button pauseRestartButton;
    [SerializeField] Button pauseMenuButton;

    [Header("Победа")]
    [SerializeField] Button nextLevelButton;
    [SerializeField] Button victoryRestartButton;
    [SerializeField] Button victoryMenuButton;

    [Header("Поражение")]
    [SerializeField] Button defeatRestartButton;
    [SerializeField] Button defeatMenuButton;

    [Header("Положение перед игроком")]
    [SerializeField] float distance = 1.5f;

    void Awake()
    {
        if (gameState == null)
            gameState = FindFirstObjectByType<GameStateManager>();

        if (!HasAllReferences())
        {
            Debug.LogError("[MainMenu] Меню собрано старой версией сборщика или в сцене нет Game State Manager. " +
                           "Запусти CursedMansion → MVP → Create Main Menu (World Space) и сохрани сцену.", this);
            enabled = false;
            return;
        }

        playButton.onClick.AddListener(() => gameState.StartLevel(1));
        levelsButton.onClick.AddListener(ShowLevels);
        exitButton.onClick.AddListener(Quit);
        backButton.onClick.AddListener(ShowMain);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1; // копия для замыкания
            levelButtons[i].onClick.AddListener(() => gameState.StartLevel(levelNumber));
        }

        resumeButton.onClick.AddListener(() => gameState.Resume());
        pauseRestartButton.onClick.AddListener(() => gameState.Restart());
        pauseMenuButton.onClick.AddListener(() => gameState.GoToMenu());

        nextLevelButton.onClick.AddListener(() => gameState.NextLevel());
        victoryRestartButton.onClick.AddListener(() => gameState.Restart());
        victoryMenuButton.onClick.AddListener(() => gameState.GoToMenu());

        defeatRestartButton.onClick.AddListener(() => gameState.Restart());
        defeatMenuButton.onClick.AddListener(() => gameState.GoToMenu());

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    bool HasAllReferences()
    {
        if (gameState == null || content == null)
            return false;
        if (mainPanel == null || levelsPanel == null || pausePanel == null || victoryPanel == null || defeatPanel == null)
            return false;
        if (playButton == null || levelsButton == null || exitButton == null || backButton == null)
            return false;
        if (resumeButton == null || pauseRestartButton == null || pauseMenuButton == null)
            return false;
        if (nextLevelButton == null || victoryRestartButton == null || victoryMenuButton == null)
            return false;
        if (defeatRestartButton == null || defeatMenuButton == null)
            return false;
        if (levelButtons == null)
            return false;
        foreach (Button button in levelButtons)
        {
            if (button == null)
                return false;
        }
        return true;
    }

    void OnEnable()
    {
        if (gameState != null)
            gameState.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (gameState != null)
            gameState.StateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        // Во время игры меню скрыто, на остальных состояниях — показываем нужный экран
        if (state == GameState.Playing)
        {
            content.SetActive(false);
            return;
        }

        ShowOnly(state switch
        {
            GameState.Paused => pausePanel,
            GameState.Victory => victoryPanel,
            GameState.Defeat => defeatPanel,
            _ => mainPanel
        });

        if (state == GameState.Victory)
            nextLevelButton.gameObject.SetActive(gameState.HasNextLevel);

        StartCoroutine(PlaceInFrontOfPlayer());
    }

    void ShowOnly(GameObject panel)
    {
        content.SetActive(true);
        mainPanel.SetActive(panel == mainPanel);
        levelsPanel.SetActive(panel == levelsPanel);
        pausePanel.SetActive(panel == pausePanel);
        victoryPanel.SetActive(panel == victoryPanel);
        defeatPanel.SetActive(panel == defeatPanel);
    }

    void ShowMain() => ShowOnly(mainPanel);

    void ShowLevels()
    {
        RefreshLevelButtons();
        ShowOnly(levelsPanel);
    }

    // Ставим панель перед глазами игрока на уровне глаз. Ждём кадр, чтобы XR-трекинг
    // успел выставить позицию камеры (на старте игры в первом кадре она может быть нулевой).
    // yield null работает и при Time.timeScale = 0, поэтому подходит и для паузы.
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

        transform.position = cam.transform.position + forward * distance;
        // Лицевая сторона Canvas смотрит по -Z, поэтому разворачиваем по направлению взгляда
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // Закрытые уровни — серые и некликабельные
    void RefreshLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            bool unlocked = LevelProgress.IsUnlocked(levelNumber) && gameState.GetLevelConfig(levelNumber) != null;
            levelButtons[i].interactable = unlocked;

            Text label = levelButtons[i].GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = unlocked ? $"Уровень {levelNumber}" : $"Уровень {levelNumber} (закрыт)";
                label.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            }
        }
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
