using UnityEngine;

// Конфиг уровня: волны и тайминги для EnemySpawner
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public Wave[] waves;
    public float timeBetweenWaves = 6f;
    public float timeBetweenSpawns = 1f;
}
