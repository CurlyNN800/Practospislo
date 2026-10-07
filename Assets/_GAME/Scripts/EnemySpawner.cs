using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] float minDistance = 3f;
    [SerializeField] float maxDistance = 5f;
    [SerializeField] float minHeight = 0.5f;
    [SerializeField] float maxHeight = 2.5f;
    [SerializeField] GameObject portalPrefab;
    [SerializeField] float portalDelay = 2f;
    [SerializeField] float portalLifetime = 3f;
    // Конфиг текущего уровня — подставляется из меню через StartLevel
    [SerializeField] LevelConfig config;

    Transform player;
    // Монстры, которые ещё "в портале" и не созданы
    int pendingSpawns;
    bool running;
    // Порталы, которые сейчас висят в сцене (чтобы убрать их при остановке уровня)
    readonly List<GameObject> activePortals = new();

    // Все волны уровня зачищены
    public event Action LevelCompleted;

    public LevelConfig CurrentConfig => config;
    public bool IsRunning => running;

    // Спавнер сам не стартует: уровень запускается только командой из меню
    public void StartLevel(LevelConfig levelConfig)
    {
        if (running)
        {
            Debug.LogWarning("[EnemySpawner] Уровень уже идёт — повторный запуск проигнорирован.");
            return;
        }

        if (levelConfig == null)
        {
            Debug.LogError("[EnemySpawner] Не передан LevelConfig — уровень не запущен.");
            return;
        }

        config = levelConfig;
        player = Camera.main.transform;
        running = true;
        Debug.Log($"[EnemySpawner] Запуск уровня: {config.name}");
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        foreach (Wave wave in config.waves)
        {
            yield return StartCoroutine(SpawnWave(wave));

            // Ждём, пока все монстры выйдут из порталов и будут убиты
            yield return new WaitUntil(() => pendingSpawns == 0 && FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length == 0);

            yield return new WaitForSeconds(config.timeBetweenWaves);
        }

        running = false;
        Debug.Log("Все волны зачищены — победа!");
        LevelCompleted?.Invoke();
    }

    // Полная остановка уровня: корутины, живые монстры, порталы
    public void StopLevel()
    {
        StopAllCoroutines();

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            Destroy(enemy.gameObject);

        foreach (GameObject portal in activePortals)
        {
            if (portal != null)
                Destroy(portal);
        }
        activePortals.Clear();

        pendingSpawns = 0;
        running = false;
    }

    IEnumerator SpawnWave(Wave wave)
    {
        foreach (SpawnEntry entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(config.timeBetweenSpawns);
            }
        }
    }

    void SpawnEnemy(GameObject prefab)
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        float distance = UnityEngine.Random.Range(minDistance, maxDistance);

        Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        Vector3 position = player.position + direction * distance;
        position.y = UnityEngine.Random.Range(minHeight, maxHeight);

        pendingSpawns++;
        StartCoroutine(SpawnWithPortal(prefab, position));
    }

    IEnumerator SpawnWithPortal(GameObject prefab, Vector3 position)
    {
        GameObject portal = Instantiate(portalPrefab, position, Quaternion.identity);
        activePortals.Add(portal);
        GameAudio.Instance?.PlayPortalOpen(position);

        yield return new WaitForSeconds(portalDelay);

        Instantiate(prefab, position, Quaternion.identity);
        pendingSpawns--;

        yield return new WaitForSeconds(portalLifetime);

        activePortals.Remove(portal);
        Destroy(portal);
    }
}
