using UnityEngine;
using UnityEngine.SceneManagement;
using VRFPSKit;

namespace CursedMansion
{
    /// <summary>
    /// Поражение при смерти игрока: загрузка сцены defeat (Interior 1) с UI «Вы погибли».
    /// </summary>
    public class PlayerDeathToGameProgress : MonoBehaviour
    {
        public const string DefeatSceneName = "Interior 1";

        void Start() => ConfigurePlayerForScene(SceneManager.GetActiveScene().name);

        void OnEnable()
        {
            Damageable.GlobalDeathEvent += OnGlobalDeath;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Damageable.GlobalDeathEvent -= OnGlobalDeath;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            ConfigurePlayerForScene(scene.name);

        void OnGlobalDeath(Damageable d)
        {
            if (d == null) return;
            if (!d.CompareTag("Player") && !d.transform.root.CompareTag("Player"))
                return;

            Time.timeScale = 1f;

            if (string.IsNullOrEmpty(d.deathSceneName))
                d.deathSceneName = DefeatSceneName;

            var playerRoot = d.transform.root;
            foreach (var respawner in playerRoot.GetComponentsInChildren<PlayerRespawner>(true))
                respawner.enabled = false;
        }

        static void ConfigurePlayerForScene(string sceneName)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var damageables = Object.FindObjectsByType<Damageable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var dmg in damageables)
                {
                    if (dmg.CompareTag("Player") || dmg.transform.root.CompareTag("Player"))
                    {
                        player = dmg.transform.root.gameObject;
                        break;
                    }
                }
            }

            if (player == null) return;

            var damageable = player.GetComponentInChildren<Damageable>(true);
            if (damageable == null) return;

            bool gameplay = sceneName is GameScenes.Interior or GameScenes.HouseScene or GameScenes.Road
                or GameScenes.InsideHouse;

            damageable.deathSceneName = gameplay ? DefeatSceneName : string.Empty;

            var respawner = player.GetComponentInChildren<PlayerRespawner>(true);
            if (respawner != null)
                respawner.enabled = gameplay;
        }
    }
}
