using Unity.XR.CoreUtils;
using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Состояние переноса между сценами: отвязка рига, точка спавна после загрузки.
    /// </summary>
    public static class SceneTransfer
    {
        public static string PendingSpawnId { get; private set; }

        public static bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.GetComponentInParent<XROrigin>() != null)
                return true;

            if (other.GetComponent<CharacterController>() != null)
                return true;

            if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
                return true;

            var rootName = other.transform.root.name;
            if (rootName.Contains("VR Player"))
                return true;

            return false;
        }

        public static void SetPendingSpawn(string spawnId) => PendingSpawnId = spawnId;

        public static void PreparePlayerForLoad()
        {
            var ride = Object.FindAnyObjectByType<VrPassengerRide>();
            if (ride != null)
                ride.TryUnmount();

            var xr = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (xr == null)
                return;

            if (xr.transform.parent != null)
                xr.transform.SetParent(null, true);
        }

        public static void ApplyPendingSpawn()
        {
            if (string.IsNullOrEmpty(PendingSpawnId))
                return;

            var spawnId = PendingSpawnId;
            PendingSpawnId = null;

            var points = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);
            SceneSpawnPoint chosen = null;
            foreach (var point in points)
            {
                if (point != null && point.Matches(spawnId))
                {
                    chosen = point;
                    break;
                }
            }

            if (chosen == null)
            {
                foreach (var point in points)
                {
                    if (point != null && point.IsDefault)
                    {
                        chosen = point;
                        break;
                    }
                }
            }

            if (chosen != null)
            {
                chosen.PlacePlayer();
                Debug.Log($"[SceneTransfer] Игрок на точке спавна '{chosen.SpawnId}'.");
                return;
            }

            var named = GameObject.Find("CM_Spawn_" + spawnId) ?? GameObject.Find("PlayerSpawn");
            if (named != null)
            {
                PlacePlayerAt(named.transform, eyeLevel: false);
                Debug.Log($"[SceneTransfer] Игрок на объекте '{named.name}'.");
                return;
            }

            Debug.LogWarning($"[SceneTransfer] Точка спавна '{spawnId}' не найдена — позиция VR Player из сцены.");
        }

        public static void PlacePlayerAt(Transform spawn, bool eyeLevel)
        {
            var xr = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (xr == null || spawn == null)
                return;

            xr.transform.SetParent(null, true);

            if (eyeLevel)
            {
                xr.MoveCameraToWorldLocation(spawn.position);
                return;
            }

            xr.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            if (xr.Camera != null)
                xr.MoveCameraToWorldLocation(spawn.position + Vector3.up * xr.CameraYOffset);
        }
    }
}
