using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedMansion
{
    public static class GameLifetimeHooks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameProgress.Instance == null)
            {
                var go = new GameObject(nameof(GameProgress));
                go.AddComponent<GameProgress>();
            }

            if (!string.IsNullOrEmpty(SceneTransfer.PendingSpawnId))
            {
                var host = new GameObject("CM_ApplySceneSpawn");
                host.AddComponent<ApplySceneSpawnOnLoad>();
            }

            SceneGrabbablesBootstrap.ApplyAll();

            if (scene.name == GameScenes.Road)
            {
                SceneTransitionRunner.ResetForNewRoadScene();
                EnsureRoadRuntimePack();
                EnsureRoadPathSceneTransition();

                var ride = Object.FindAnyObjectByType<VrPassengerRide>();
                if (ride != null)
                    ride.BeginAutoMount(forceRestart: true);
            }
        }

        static void EnsureRoadPathSceneTransition()
        {
            if (Object.FindAnyObjectByType<RoadPathSceneTransition>() != null)
                return;

            var vehicle = Object.FindAnyObjectByType<SimpleWaypointVehicle>();
            if (vehicle == null)
                return;

            vehicle.gameObject.AddComponent<RoadPathSceneTransition>();
        }

        static void EnsureRoadRuntimePack()
        {
            if (Object.FindAnyObjectByType<RoadRuntimePack>() != null) return;
            var go = new GameObject("CursedMansion_RoadRuntimePack");
            go.AddComponent<RoadRuntimePack>();
        }
    }
}
