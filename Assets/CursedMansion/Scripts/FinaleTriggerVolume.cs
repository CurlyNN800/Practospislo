using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Игрок заходит в триггер — фиксируется финал и загрузка Road.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinaleTriggerVolume : MonoBehaviour
    {
        [SerializeField] bool victoryOnEnter = true;
        [SerializeField] string roadSpawnId = "FromInterior";
        [SerializeField] float fadeOutSeconds = 2f;
        [SerializeField] float fadeInSeconds = 1.5f;

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

            var gp = GameProgress.Instance;
            if (gp == null)
                return;

            _loading = true;

            if (victoryOnEnter)
                gp.SetFinaleVictory();
            else
                gp.SetFinaleDefeat();

            SceneTransitionRunner.TransitionTo(GameScenes.Road, roadSpawnId, fadeOutSeconds, fadeInSeconds);
        }
    }
}
