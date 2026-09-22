#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CursedMansion.Editor
{
    public static class NavMeshBakeMenu
    {
        const float EnemyVisualScale = 1.2f;
        const float NavAgentRadius = 0.18f;
        const float NavAgentHeight = 1.65f;

        [MenuItem("CursedMansion/AI/Apply enemy body profile (scene)")]
        public static void ApplyEnemyBodyProfile()
        {
            var count = ApplyEnemyNavigationProfile();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[NavMeshBake] Body profile applied to {count} enemies (scale {EnemyVisualScale}, nav radius {NavAgentRadius}, full-body hit volumes).");
        }

        [MenuItem("CursedMansion/AI/Bake NavMesh and warp enemies")]
        public static void BakeNavMeshAndWarpEnemies()
        {
            ApplyEnemyNavigationProfile();

            var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            if (surfaces.Length == 0)
            {
                Debug.LogWarning("[NavMeshBake] NavMeshSurface not found in open scenes.");
                return;
            }

            foreach (var surface in surfaces)
            {
                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
                Debug.Log($"[NavMeshBake] Built NavMesh on '{surface.name}'.");
            }

            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var warped = 0;
            var failed = 0;

            foreach (var enemy in enemies)
            {
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    Debug.LogWarning($"[NavMeshBake] Enemy '{enemy.name}' has no NavMeshAgent.", enemy);
                    failed++;
                    continue;
                }

                var pos = enemy.transform.position;
                if (NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    warped++;
                }
                else
                {
                    Debug.LogWarning($"[NavMeshBake] No NavMesh near '{enemy.name}' at {pos}.", enemy);
                    failed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[NavMeshBake] Done. Surfaces: {surfaces.Length}, enemies: {enemies.Length}, warped: {warped}, failed: {failed}.");
        }

        static int ApplyEnemyNavigationProfile()
        {
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                Undo.RecordObject(enemy.transform, "Enemy visual scale");
                enemy.transform.localScale = Vector3.one * EnemyVisualScale;

                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    Undo.RecordObject(agent, "Enemy nav agent");
                    agent.radius = NavAgentRadius;
                    agent.height = NavAgentHeight;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                }

                enemy.detectionHeightRange = 1.35f;
                enemy.attackHeightRange = 1.1f;
                Undo.RecordObject(enemy, "Enemy body colliders");
                enemy.ConfigureBodyColliders(rebuildHitVolumes: true);
                EditorUtility.SetDirty(enemy);
            }

            return enemies.Length;
        }
    }
}
#endif
