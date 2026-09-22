using UnityEngine;

namespace CursedMansion
{
    public enum CursedMansionSceneTarget
    {
        Interior,
        HouseScene,
        Road
    }

    /// <summary>
    /// Триггер для перехода между сценами (обёртка над SceneTransitionTrigger).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AreaLoadSceneTrigger : MonoBehaviour
    {
        [SerializeField] CursedMansionSceneTarget loadTarget = CursedMansionSceneTarget.Interior;
        [SerializeField] string spawnId = "Default";
        [SerializeField] float fadeOutSeconds = 1.5f;
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

            _loading = true;

            var gp = GameProgress.Instance;
            string sceneName;
            switch (loadTarget)
            {
                case CursedMansionSceneTarget.Interior:
                    if (gp != null) gp.EnterMansionInterior();
                    sceneName = GameScenes.Interior;
                    break;
                case CursedMansionSceneTarget.HouseScene:
                    if (gp != null) gp.EnterMansionExterior();
                    sceneName = GameScenes.HouseScene;
                    break;
                case CursedMansionSceneTarget.Road:
                    sceneName = GameScenes.Road;
                    break;
                default:
                    return;
            }

            SceneTransitionRunner.TransitionTo(sceneName, spawnId, fadeOutSeconds, fadeInSeconds);
        }
    }
}
