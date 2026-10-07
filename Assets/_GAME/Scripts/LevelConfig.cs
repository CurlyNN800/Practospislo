using UnityEngine;

// Конфиг уровня: волны и тайминги для EnemySpawner
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public Wave[] waves;
    // Пауза перед первой волной: время зарядить пистолет и взять магазины (с отсчётом на экране)
    public float startDelay = 8f;
    public float timeBetweenWaves = 6f;
    public float timeBetweenSpawns = 1f;
}
