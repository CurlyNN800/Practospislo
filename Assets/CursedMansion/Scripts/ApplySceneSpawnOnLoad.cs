using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Ждёт появления XROrigin после загрузки сцены и ставит игрока на SceneSpawnPoint.
    /// </summary>
    public class ApplySceneSpawnOnLoad : MonoBehaviour
    {
        IEnumerator Start()
        {
            const int maxFrames = 120;
            for (var i = 0; i < maxFrames; i++)
            {
                if (!string.IsNullOrEmpty(SceneTransfer.PendingSpawnId) &&
                    Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include) != null)
                    break;

                yield return null;
            }

            SceneTransfer.ApplyPendingSpawn();
            Destroy(gameObject);
        }
    }
}
