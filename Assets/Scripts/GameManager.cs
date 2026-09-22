using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Win Condition")]
    public int totalEnemies;
    public UnityEvent onAllEnemiesKilled;

    [Header("Debug")]
    public bool debugLogs = true;

    private int _killed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (totalEnemies <= 0)
            totalEnemies = CountAllEnemies();

        if (debugLogs)
            Debug.Log($"[GameManager] Game started. Total enemies: {totalEnemies}");
    }

    static int CountAllEnemies()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
#else
        return Object.FindObjectsOfType<EnemyHealth>(true).Length;
#endif
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        _killed++;
        if (debugLogs)
            Debug.Log($"[GameManager] Enemy killed: {enemy.name}. Progress: {_killed}/{totalEnemies}");

        if (_killed >= totalEnemies)
        {
            if (debugLogs) Debug.Log("[GameManager] All enemies defeated!");
            onAllEnemiesKilled?.Invoke();
        }
    }

    public int KilledCount => _killed;
    public int RemainingCount => Mathf.Max(0, totalEnemies - _killed);
}
