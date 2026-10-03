using UnityEngine;

// Сколько монстров одного типа появится в волне
[System.Serializable]
public class SpawnEntry
{
    public GameObject enemyPrefab;
    public int count;
}
