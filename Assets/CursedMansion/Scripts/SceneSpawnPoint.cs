using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Куда ставить VR Player после загрузки сцены (см. SceneTransfer.PendingSpawnId).
    /// </summary>
    public class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] string spawnId = "Default";
        [SerializeField] bool isDefault;
        [Tooltip("Если включено, transform.position — уровень глаз (MoveCameraToWorldLocation).")]
        [SerializeField] bool eyeLevel;

        public string SpawnId => spawnId;
        public bool IsDefault => isDefault;

        public bool Matches(string id) =>
            !string.IsNullOrEmpty(id) &&
            string.Equals(spawnId, id, System.StringComparison.OrdinalIgnoreCase);

        public void PlacePlayer() => SceneTransfer.PlacePlayerAt(transform, eyeLevel);

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
        }
    }
}
