using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedMansion
{
    /// <summary>
    /// Spawns a few extra magazines on nearby tables at runtime.
    /// This avoids manual placement in scenes (works even if scene asset is binary).
    /// </summary>
    public class ExtraMagazinesOnTables : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] int extraCount = 2;
        [SerializeField] string magazineNameContains = "M17 17rd Magazine";

        [Header("Targets (optional, by name)")]
        [SerializeField] string[] tableNameHints = { "MetalTable", "RoundTable", "Prop_LargeTable_A", "Prop_SmallTable_B" };

        [Header("Placement")]
        [SerializeField] Vector3 localOffset = new(0.15f, 0.85f, 0.05f);
        [SerializeField] Vector3 localOffsetStep = new(0.12f, 0f, 0.0f);
        [SerializeField] Vector3 localEuler = new(0f, 90f, 90f);

        bool _spawned;

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawn();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene _, LoadSceneMode __) => TrySpawn();

        void TrySpawn()
        {
            if (_spawned) return;

            var source = FindMagazineSource();
            if (source == null) return;

            var table = FindTableTransform();
            if (table == null) return;

            for (var i = 0; i < extraCount; i++)
            {
                var clone = Instantiate(source, table);
                clone.name = $"Extra_{source.name}_{i + 1}";
                clone.SetActive(true);

                var t = clone.transform;
                t.localPosition = localOffset + localOffsetStep * i;
                t.localRotation = Quaternion.Euler(localEuler);
            }

            _spawned = true;
        }

        GameObject FindMagazineSource()
        {
            // Prefer an existing magazine in the loaded scene so we copy the exact configured prefab instance.
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t == null) continue;
                var n = t.name;
                if (string.IsNullOrEmpty(n)) continue;
                if (n.Contains(magazineNameContains))
                    return t.gameObject;
            }
            return null;
        }

        Transform FindTableTransform()
        {
            foreach (var hint in tableNameHints)
            {
                if (string.IsNullOrWhiteSpace(hint)) continue;
                var go = GameObject.Find(hint);
                if (go != null) return go.transform;
            }

            // Fallback: first object with "Table" in name.
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t != null && t.name.Contains("Table"))
                    return t;
            }

            return null;
        }
    }
}

