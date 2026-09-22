using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Отключает XR Device Simulator в загруженной сцене (только для редактора / тестов без шлема).
    /// </summary>
    public static class DisableXrDeviceSimulator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null)
                    continue;

                var n = t.gameObject.name;
                if (n.Contains("XR Device Simulator") || n.Contains("Device Simulator"))
                    t.gameObject.SetActive(false);
            }
        }
    }
}
