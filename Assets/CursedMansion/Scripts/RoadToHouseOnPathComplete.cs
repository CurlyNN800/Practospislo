using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// По окончании маршрута машины — переход в HouseScene (на объекте waypoints / SimpleWaypointVehicle).
    /// </summary>
    public class RoadToHouseOnPathComplete : MonoBehaviour
    {
        [SerializeField] SimpleWaypointVehicle vehicle;
        [SerializeField] float fadeToBlackSeconds = 5f;
        [SerializeField] float fadeInSeconds = 2f;

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponent<SimpleWaypointVehicle>();
        }

        void OnEnable()
        {
            if (vehicle != null)
                vehicle.onPathComplete.AddListener(OnPathFinished);
        }

        void OnDisable()
        {
            if (vehicle != null)
                vehicle.onPathComplete.RemoveListener(OnPathFinished);
        }

        void OnPathFinished() => SceneTransitionRunner.LoadHouseFromRoad(fadeToBlackSeconds, fadeInSeconds);
    }
}
