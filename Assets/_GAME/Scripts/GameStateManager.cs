using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Victory,
    Defeat
}

// Единая точка управления состоянием игры: какой уровень идёт и что сейчас на экране.
// Меню (MainMenu) только вызывает методы менеджера и показывает панель по событию StateChanged.
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] EnemySpawner spawner;
    [SerializeField] PlayerHealth playerHealth;
    // Полное восстановление снаряжения (пистолет, магазины) при каждом запуске уровня
    [SerializeField] LevelLoadout loadout;
    // levels[0] = Уровень 1, levels[1] = Уровень 2, ...
    [SerializeField] LevelConfig[] levels;

    InputAction pauseAction;

    public GameState State { get; private set; } = GameState.Menu;
    // Номер текущего уровня, считается с 1 (0 — уровень не выбран)
    public int CurrentLevelNumber { get; private set; }
    public LevelConfig CurrentConfig => GetLevelConfig(CurrentLevelNumber);
    public int LevelCount => levels != null ? levels.Length : 0;
    public bool HasNextLevel => CurrentLevelNumber >= 1 && GetLevelConfig(CurrentLevelNumber + 1) != null;
    public bool IsPlaying => State == GameState.Playing;

    public event Action<GameState> StateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameStateManager] В сцене уже есть менеджер состояний — лишний удалён.");
            Destroy(this);
            return;
        }
        Instance = this;

        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (loadout == null)
            loadout = FindFirstObjectByType<LevelLoadout>();

        // Лучи на руках — только в меню (выключаются на время игры)
        if (GetComponent<MenuRayToggle>() == null)
            gameObject.AddComponent<MenuRayToggle>();

        // Пауза: кнопка Menu на левом контроллере, в симуляторе / на клавиатуре — Esc
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.performed += _ => TogglePause();
    }

    void OnEnable()
    {
        pauseAction?.Enable();
        if (spawner != null)
            spawner.LevelCompleted += OnLevelCompleted;
        if (playerHealth != null)
            playerHealth.Died += OnPlayerDied;
    }

    void OnDisable()
    {
        pauseAction?.Disable();
        if (spawner != null)
            spawner.LevelCompleted -= OnLevelCompleted;
        if (playerHealth != null)
            playerHealth.Died -= OnPlayerDied;
    }

    void OnDestroy()
    {
        pauseAction?.Dispose();
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        SetState(GameState.Menu);
    }

    public LevelConfig GetLevelConfig(int levelNumber)
    {
        int index = levelNumber - 1;
        if (levels == null || index < 0 || index >= levels.Length)
            return null;
        return levels[index];
    }

    // --- Команды из меню ---

    public void StartLevel(int levelNumber)
    {
        if (spawner == null)
        {
            Debug.LogError("[GameStateManager] Не найден EnemySpawner — уровень не запущен.");
            return;
        }

        LevelConfig config = GetLevelConfig(levelNumber);
        if (config == null)
        {
            Debug.LogError($"[GameStateManager] Нет LevelConfig для уровня {levelNumber}.");
            return;
        }
        if (!LevelProgress.IsUnlocked(levelNumber))
        {
            Debug.LogWarning($"[GameStateManager] Уровень {levelNumber} ещё закрыт.");
            return;
        }

        ResetLevel();
        CurrentLevelNumber = levelNumber;
        // Сюда приходят и «Заново», и «Следующий уровень»
        if (loadout != null)
            loadout.RestoreLoadout();
        else
            Debug.LogWarning("[GameStateManager] В сцене нет LevelLoadout — снаряжение не восстановлено.");
        spawner.StartLevel(config);
        SetState(GameState.Playing);
    }

    public void Restart()
    {
        if (CurrentLevelNumber < 1)
            return;
        StartLevel(CurrentLevelNumber);
    }

    public void NextLevel()
    {
        if (HasNextLevel)
            StartLevel(CurrentLevelNumber + 1);
    }

    public void GoToMenu()
    {
        ResetLevel();
        SetState(GameState.Menu);
    }

    public void Pause()
    {
        if (State != GameState.Playing)
            return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    void TogglePause()
    {
        if (State == GameState.Playing)
            Pause();
        else if (State == GameState.Paused)
            Resume();
    }

    // --- События игры ---

    void OnLevelCompleted()
    {
        if (State != GameState.Playing)
            return;

        LevelProgress.UnlockNextLevel(CurrentLevelNumber);
        spawner.StopLevel();
        SetState(GameState.Victory);
    }

    void OnPlayerDied()
    {
        if (State != GameState.Playing)
            return;

        spawner.StopLevel();
        SetState(GameState.Defeat);
    }

    // Общий сброс перед новым запуском или выходом в меню: время, спавн, здоровье
    void ResetLevel()
    {
        Time.timeScale = 1f;
        if (spawner != null)
            spawner.StopLevel();
        if (playerHealth != null)
            playerHealth.ResetHealth();
    }

    void SetState(GameState newState)
    {
        State = newState;
        Debug.Log($"[GameStateManager] Состояние: {newState}, уровень {CurrentLevelNumber}");
        StateChanged?.Invoke(newState);
    }
}
