using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// На сцене Road в фазе EndingRoad переключает дочерние объекты финала.
    /// Имена по умолчанию: MVP_Ending_CleanState, MVP_Ending_PlayerCorpse (создаются при отсутствии).
    /// </summary>
    public class RoadEndingVisualApplier : MonoBehaviour
    {
        const string CleanName = "MVP_Ending_CleanState";
        const string CorpseName = "MVP_Ending_PlayerCorpse";

        void Start()
        {
            var gp = GameProgress.Instance;
            if (gp == null || gp.Phase != GamePhase.EndingRoad)
                return;

            Transform clean = EnsureChild(CleanName);
            Transform corpse = EnsureChild(CorpseName);

            bool defeat = gp.LastFinale == FinaleOutcome.Defeat;
            clean.gameObject.SetActive(!defeat);
            corpse.gameObject.SetActive(defeat);
        }

        Transform EnsureChild(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t;
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }
    }
}
