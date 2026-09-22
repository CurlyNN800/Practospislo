using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Locks the finale area until GameManager reports all enemies defeated.
    /// </summary>
    public class FinaleRoomUnlocker : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;
        [SerializeField] GameObject[] blockers = System.Array.Empty<GameObject>();
        [SerializeField] PushDoor[] doors = System.Array.Empty<PushDoor>();
        [SerializeField] GameObject[] revealOnUnlock = System.Array.Empty<GameObject>();
        [SerializeField] bool startLocked = true;

        bool _unlocked;

        void Awake()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;

            ApplyLockState(startLocked && !_unlocked);

            if (gameManager != null && startLocked && !_unlocked)
                gameManager.onAllEnemiesKilled.AddListener(Unlock);
        }

        void OnDestroy()
        {
            if (gameManager != null)
                gameManager.onAllEnemiesKilled.RemoveListener(Unlock);
        }

        public void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            ApplyLockState(false);
            Debug.Log("[FinaleRoom] All enemies defeated — finale room is open.");
        }

        void ApplyLockState(bool locked)
        {
            foreach (var blocker in blockers)
                if (blocker != null) blocker.SetActive(locked);

            foreach (var door in doors)
            {
                if (door == null) continue;
                if (locked) door.ForceClose();
                else door.ForceOpen(1f);
            }

            foreach (var obj in revealOnUnlock)
                if (obj != null) obj.SetActive(!locked);
        }
    }
}
