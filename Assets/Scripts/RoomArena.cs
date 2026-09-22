using UnityEngine;

// Attach to a trigger zone. Activates sleeping enemies when the player enters.
public class RoomArena : MonoBehaviour
{
    [Header("Enemies in this room")]
    public GameObject[] enemies;

    [Header("Settings")]
    public bool oneShot = true;
    [Tooltip("When enabled, enemies are never deactivated/activated by this trigger (all are pre-spawned).")]
    public bool preSpawnAll = true;

    private bool _triggered;

    void Start()
    {
        if (preSpawnAll) return;

        // Enemies start inactive - awaken on player entry
        foreach (var e in enemies)
            if (e != null) e.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (preSpawnAll) return;
        if (_triggered && oneShot) return;
        if (!other.CompareTag("Player") && other.gameObject.layer != 6) return;

        _triggered = true;
        foreach (var e in enemies)
            if (e != null) e.SetActive(true);

        Debug.Log($"[RoomArena] {gameObject.name}: triggered, activating {enemies.Length} enemies");
    }
}
