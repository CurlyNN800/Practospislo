using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Переход Road → HouseScene за approachEndSeconds до финиша: сначала fade, затем LoadScene на чёрном экране.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RoadPathSceneTransition : MonoBehaviour
    {
        [SerializeField] SimpleWaypointVehicle vehicle;
        [SerializeField] float fadeOutSeconds = 3f;
        [SerializeField] float fadeInSeconds = 2f;

        bool _handled;

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponent<SimpleWaypointVehicle>();

            if (vehicle == null)
            {
                Debug.LogError($"[{nameof(RoadPathSceneTransition)}] Нет SimpleWaypointVehicle.");
                return;
            }

            vehicle.onApproachingPathEnd.AddListener(BeginTransition);
            vehicle.onPathComplete.AddListener(BeginTransition);
        }

        void OnDestroy()
        {
            if (vehicle == null)
                return;

            vehicle.onApproachingPathEnd.RemoveListener(BeginTransition);
            vehicle.onPathComplete.RemoveListener(BeginTransition);
        }

        void BeginTransition()
        {
            if (_handled)
                return;

            _handled = true;
            vehicle.StopMoving();

            Debug.Log($"[{nameof(RoadPathSceneTransition)}] Затемнение → HouseScene (телепорт на чёрном экране).");
            SceneTransitionRunner.LoadHouseFromRoad(fadeOutSeconds, fadeInSeconds);
        }
    }
}
