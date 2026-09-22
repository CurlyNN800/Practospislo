using UnityEngine;
using VolumetricLines;

namespace CursedMansion
{
    /// <summary>
    /// На старте сцены настраивает все VolumetricLine и WalkieTalkie для VR-хвата.
    /// </summary>
    public class SceneGrabbablesBootstrap : MonoBehaviour
    {
        public static void ApplyAll()
        {
            var lines = Object.FindObjectsByType<VolumetricLineBehavior>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var line in lines)
            {
                if (line == null) continue;
                EnsureGrabbable(line.gameObject);
            }

            var strips = Object.FindObjectsByType<VolumetricLineStripBehavior>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var strip in strips)
            {
                if (strip == null) continue;
                EnsureGrabbable(strip.gameObject);
            }

            var transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var walkieCount = 0;
            foreach (var t in transforms)
            {
                if (t == null) continue;
                if (!IsWalkieTalkieRoot(t))
                    continue;

                walkieCount++;
                EnsureGrabbable(t.gameObject);
            }

            Debug.Log($"[SceneGrabbables] VR grab: {lines.Length} VolumetricLine, {strips.Length} полос, {walkieCount} WalkieTalkie.");
        }

        static bool IsWalkieTalkieRoot(Transform t)
        {
            if (!t.name.Contains("WalkieTalkie", System.StringComparison.OrdinalIgnoreCase))
                return false;

            if (t.parent != null &&
                t.parent.name.Contains("WalkieTalkie", System.StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        static void EnsureGrabbable(GameObject go)
        {
            if (go == null)
                return;

            var setup = go.GetComponent<VrGrabbableSetup>();
            if (setup == null)
                setup = go.AddComponent<VrGrabbableSetup>();

            setup.Configure();
        }
    }
}
