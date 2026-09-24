using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] float spawnInterval = 3f;
    [SerializeField] float minDistance = 3f;
    [SerializeField] float maxDistance = 5f;
    [SerializeField] float minHeight = 0.5f;
    [SerializeField] float maxHeight = 2.5f;
    Transform player;
    float nextSpawnTime;
    private void Start()
    {
        player = Camera.main.transform;
        nextSpawnTime = Time.time + spawnInterval;
    }
    private void Update()
    {
       if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    void SpawnEnemy()
    {
        int index = Random.Range(0, enemyPrefabs.Length);
        GameObject prefab = enemyPrefabs[index];
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minDistance, maxDistance);

        Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        Vector3 position = player.position + direction * distance;
        position.y = Random.Range(minHeight, maxHeight);
        Instantiate(prefab, position, Quaternion.identity);
    }
}
