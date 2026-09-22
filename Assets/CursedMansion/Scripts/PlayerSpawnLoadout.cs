using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CursedMansion
{
    /// <summary>
    /// На старте сцены кладёт пистолет в кобуру и магазины в слоты жилета (VRFPS Kit sockets).
    /// </summary>
    public class PlayerSpawnLoadout : MonoBehaviour
    {
        const string HolsterSocketName = "Pistol Holster Socket";
        static readonly string[] MagazineSocketNames =
        {
            "Magazine Socket 1",
            "Magazine Socket 2",
            "Magazine Socket 3",
        };

        [Header("Prefabs")]
        [SerializeField] GameObject pistolPrefab;
        [SerializeField] GameObject magazinePrefab;

        [Header("Counts")]
        [SerializeField] int magazineCount = 3;

        [Header("Timing")]
        [SerializeField] float applyDelaySeconds = 0.25f;

        [Header("Debug")]
        [SerializeField] bool debugLogs = true;

        void Start()
        {
            StartCoroutine(ApplyLoadoutWhenReady());
        }

        IEnumerator ApplyLoadoutWhenReady()
        {
            if (applyDelaySeconds > 0f)
                yield return new WaitForSeconds(applyDelaySeconds);

            var playerRoot = FindPlayerRoot();
            if (playerRoot == null)
            {
                if (debugLogs) Debug.LogWarning("[PlayerSpawnLoadout] Player not found.");
                yield break;
            }

            var interactionManager = playerRoot.GetComponentInChildren<XRInteractionManager>(true);
            if (interactionManager == null)
                interactionManager = FindFirstObjectByType<XRInteractionManager>();

            if (interactionManager == null)
            {
                if (debugLogs) Debug.LogWarning("[PlayerSpawnLoadout] XRInteractionManager not found.");
                yield break;
            }

            if (pistolPrefab != null)
            {
                var holster = FindSocket(playerRoot.transform, HolsterSocketName);
                if (holster != null)
                    SocketPrefab(interactionManager, holster, pistolPrefab, "pistol");
                else if (debugLogs)
                    Debug.LogWarning($"[PlayerSpawnLoadout] Socket '{HolsterSocketName}' not found under {playerRoot.name}.");
            }

            if (magazinePrefab != null)
            {
                int placed = 0;
                for (int i = 0; i < MagazineSocketNames.Length && placed < magazineCount; i++)
                {
                    var socket = FindSocket(playerRoot.transform, MagazineSocketNames[i]);
                    if (socket == null)
                    {
                        if (debugLogs) Debug.LogWarning($"[PlayerSpawnLoadout] Missing '{MagazineSocketNames[i]}'.");
                        continue;
                    }

                    SocketPrefab(interactionManager, socket, magazinePrefab, $"magazine_{i + 1}");
                    placed++;
                }

                if (debugLogs) Debug.Log($"[PlayerSpawnLoadout] Placed {placed} magazine(s) on {playerRoot.name}.");
            }
        }

        static GameObject FindPlayerRoot()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) return tagged;

            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) return cc.gameObject;

            if (Camera.main != null)
                return Camera.main.transform.root.gameObject;

            return null;
        }

        static XRSocketInteractor FindSocket(Transform playerRoot, string socketName)
        {
            foreach (var socket in playerRoot.GetComponentsInChildren<XRSocketInteractor>(true))
            {
                if (socket.name == socketName)
                    return socket;
            }

            return null;
        }

        void SocketPrefab(XRInteractionManager manager, XRSocketInteractor socket, GameObject prefab, string label)
        {
            if (socket.interactablesSelected.Count > 0)
            {
                if (debugLogs) Debug.Log($"[PlayerSpawnLoadout] '{socket.name}' already occupied, skip {label}.");
                return;
            }

            var instance = Instantiate(prefab, socket.transform.position, socket.transform.rotation);
            var grab = instance.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                Debug.LogWarning($"[PlayerSpawnLoadout] {prefab.name} has no XRGrabInteractable.");
                Destroy(instance);
                return;
            }

            manager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);

            if (debugLogs)
                Debug.Log($"[PlayerSpawnLoadout] Socketed {prefab.name} -> {socket.name}");
        }
    }
}
