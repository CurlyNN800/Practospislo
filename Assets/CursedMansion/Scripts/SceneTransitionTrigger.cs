using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Триггер-коллайдер: игрок зашёл → fade → следующая сцена → spawn point.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [SerializeField] string targetScene = GameScenes.HouseScene;
        [SerializeField] string targetSpawnId = "Default";
        [SerializeField] float fadeOutSeconds = 1.5f;
        [SerializeField] float fadeInSeconds = 1.5f;
        [SerializeField] bool updateGameProgress = true;
        [SerializeField] CursedMansionSceneTarget progressTarget;

        bool _loading;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_loading || !SceneTransfer.IsPlayerCollider(other))
                return;

            _loading = true;
            Debug.Log($"[SceneTransitionTrigger] {name} → {targetScene} (spawn: {targetSpawnId})");

            if (updateGameProgress)
                ApplyProgress();

            SceneTransitionRunner.TransitionTo(targetScene, targetSpawnId, fadeOutSeconds, fadeInSeconds);
        }

        void ApplyProgress()
        {
            var gp = GameProgress.Instance;
            if (gp == null)
                return;

            switch (progressTarget)
            {
                case CursedMansionSceneTarget.Interior:
                    gp.EnterMansionInterior();
                    break;
                case CursedMansionSceneTarget.HouseScene:
                    gp.EnterMansionExterior();
                    break;
                case CursedMansionSceneTarget.Road:
                    break;
            }
        }
    }
}
