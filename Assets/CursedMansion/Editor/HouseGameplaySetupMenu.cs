#if UNITY_EDITOR
using CursedMansion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedMansion.Editor
{
    public static class HouseGameplaySetupMenu
    {
        const string M17PistolPrefabPath = "Assets/VRFPS Kit/Prefabs/Items/Weapons/M17 9MM.prefab";
        const string M17MagazinePrefabPath = "Assets/VRFPS Kit/Prefabs/Items/Weapons/Magazines/M17 17rd Magazine.prefab";

        [MenuItem("CursedMansion/Scene/Add player spawn loadout (pistol + 3 mags)")]
        public static void AddPlayerSpawnLoadout()
        {
            var pistol = AssetDatabase.LoadAssetAtPath<GameObject>(M17PistolPrefabPath);
            var magazine = AssetDatabase.LoadAssetAtPath<GameObject>(M17MagazinePrefabPath);
            if (pistol == null || magazine == null)
            {
                Debug.LogError("[HouseGameplay] M17 prefabs not found. Check VRFPS Kit paths.");
                return;
            }

            var root = GameObject.Find("CM_PlayerLoadout");
            if (root == null)
            {
                root = new GameObject("CM_PlayerLoadout");
                Undo.RegisterCreatedObjectUndo(root, "Player spawn loadout");
            }

            var loadout = root.GetComponent<PlayerSpawnLoadout>();
            if (loadout == null)
                loadout = Undo.AddComponent<PlayerSpawnLoadout>(root);

            var so = new SerializedObject(loadout);
            so.FindProperty("pistolPrefab").objectReferenceValue = pistol;
            so.FindProperty("magazinePrefab").objectReferenceValue = magazine;
            so.FindProperty("magazineCount").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[HouseGameplay] CM_PlayerLoadout added — pistol + 3 magazines on play.");
        }

        const float UpperFloorY = 6.35f;
        const float GroundFloorY = 0.08253038f;

        [MenuItem("CursedMansion/Scene/Setup finale room + move upper-floor enemies")]
        public static void SetupFinaleAndEnemies()
        {
            SetupFinaleGate();
            MoveEnemiesToUpperFloor();
            FixGameManagerEnemyCount();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[HouseGameplay] Finale gate + upper-floor enemies configured. Rebake NavMesh if needed.");
        }

        static void SetupFinaleGate()
        {
            var root = GameObject.Find("CM_FinaleGate");
            if (root == null)
            {
                root = new GameObject("CM_FinaleGate");
                Undo.RegisterCreatedObjectUndo(root, "Finale gate");
            }

            var blocker = root.transform.Find("CM_FinaleBlocker");
            if (blocker == null)
            {
                var blockerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockerGo.name = "CM_FinaleBlocker";
                blockerGo.transform.SetParent(root.transform, false);
                // Past the weapon-room combat zone — blocks only the finale chamber.
                blockerGo.transform.position = new Vector3(475f, 2.2f, 330.5f);
                blockerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                blockerGo.transform.localScale = new Vector3(0.35f, 2.8f, 3.5f);
                Object.DestroyImmediate(blockerGo.GetComponent<MeshRenderer>());
                Undo.RegisterCreatedObjectUndo(blockerGo, "Finale blocker");
                blocker = blockerGo.transform;
            }

            var finaleRoom = root.transform.Find("CM_FinaleRoom");
            if (finaleRoom == null)
            {
                var roomGo = new GameObject("CM_FinaleRoom");
                roomGo.transform.SetParent(root.transform, false);
                roomGo.transform.position = new Vector3(476f, 1f, 332f);
                Undo.RegisterCreatedObjectUndo(roomGo, "Finale room");
                finaleRoom = roomGo.transform;
            }

            var triggers = finaleRoom.Find("CM_FinaleTriggers");
            if (triggers == null)
            {
                var triggersRoot = new GameObject("CM_FinaleTriggers");
                triggersRoot.transform.SetParent(finaleRoom, false);
                triggersRoot.transform.localPosition = Vector3.zero;
                triggersRoot.SetActive(false);
                CreateFinaleTrigger(triggersRoot.transform, "CM_Finale_Win", Vector3.zero, true);
                Undo.RegisterCreatedObjectUndo(triggersRoot, "Finale triggers");
                triggers = triggersRoot.transform;
            }

            var unlocker = root.GetComponent<FinaleRoomUnlocker>();
            if (unlocker == null)
                unlocker = Undo.AddComponent<FinaleRoomUnlocker>(root);

            var gm = Object.FindFirstObjectByType<GameManager>();
            var so = new SerializedObject(unlocker);
            so.FindProperty("gameManager").objectReferenceValue = gm;
            so.FindProperty("blockers").arraySize = 1;
            so.FindProperty("blockers").GetArrayElementAtIndex(0).objectReferenceValue = blocker.gameObject;
            so.FindProperty("doors").arraySize = 0;
            so.FindProperty("revealOnUnlock").arraySize = 1;
            so.FindProperty("revealOnUnlock").GetArrayElementAtIndex(0).objectReferenceValue = triggers.gameObject;
            so.FindProperty("startLocked").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateFinaleTrigger(Transform parent, string name, Vector3 localPos, bool victory)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 2.8f, 4f);
            var ft = go.AddComponent<FinaleTriggerVolume>();
            var so = new SerializedObject(ft);
            so.FindProperty("victoryOnEnter").boolValue = victory;
            so.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(go, "Finale trigger");
        }

        static void MoveEnemiesToUpperFloor()
        {
            var enemiesRoot = GameObject.Find("=== ENEMIES ===");
            if (enemiesRoot == null)
            {
                Debug.LogWarning("[HouseGameplay] === ENEMIES === not found.");
                return;
            }

            var upperFolder = enemiesRoot.transform.Find("UpperFloor_Enemies");
            if (upperFolder == null)
            {
                var folderGo = new GameObject("UpperFloor_Enemies");
                folderGo.transform.SetParent(enemiesRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(folderGo, "Upper enemies folder");
                upperFolder = folderGo.transform;
            }

            MoveEnemy(enemiesRoot.transform, upperFolder, "Zombie_Parasite_Sitting_1",
                new Vector3(454.5f, UpperFloorY, 311.5f), 90f, warpAgent: true);
            MoveEnemy(enemiesRoot.transform, upperFolder, "Zombie_Parasite_Central_1",
                new Vector3(461.5f, UpperFloorY, 314.5f), 180f, warpAgent: true);
            MoveEnemy(enemiesRoot.transform, upperFolder, "Zombie_Parasite_Living_1",
                new Vector3(468f, UpperFloorY, 319.5f), 180f, warpAgent: true);

            MoveEnemy(enemiesRoot.transform, upperFolder, "Zombie_Room_Central",
                new Vector3(463f, UpperFloorY, 316f), 0f, warpAgent: true);
            MoveEnemy(enemiesRoot.transform, upperFolder, "Zombie_Room_Living_1",
                new Vector3(469f, UpperFloorY, 321f), 180f, warpAgent: true);

            EnsureUpperArena(enemiesRoot.transform, "Arena_Upper_Central",
                new Vector3(461.5f, UpperFloorY + 1f, 314.5f), new[] { "Zombie_Room_Central" });
            EnsureUpperArena(enemiesRoot.transform, "Arena_Upper_Living",
                new Vector3(468f, UpperFloorY + 1f, 320f), new[] { "Zombie_Room_Living_1" });
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void MoveEnemy(Transform enemiesRoot, Transform upperFolder, string name, Vector3 pos, float rotY, bool warpAgent)
        {
            var t = FindDeep(enemiesRoot, name);
            if (t == null)
            {
                Debug.LogWarning($"[HouseGameplay] Enemy not found: {name}");
                return;
            }

            Undo.SetTransformParent(t, upperFolder, "Move enemy to upper floor");
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, rotY, 0f);

            if (!warpAgent) return;
            var agent = t.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
                agent.Warp(pos);
        }

        static void EnsureUpperArena(Transform enemiesRoot, string arenaName, Vector3 pos, string[] enemyNames)
        {
            var arenaT = enemiesRoot.Find(arenaName);
            if (arenaT == null)
            {
                var arenaGo = new GameObject(arenaName);
                arenaGo.transform.SetParent(enemiesRoot, false);
                arenaGo.transform.position = pos;
                var box = arenaGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 3f, 8f);
                arenaGo.AddComponent<RoomArena>();
                Undo.RegisterCreatedObjectUndo(arenaGo, "Upper arena");
                arenaT = arenaGo.transform;
            }

            var arena = arenaT.GetComponent<RoomArena>();
            var enemies = new System.Collections.Generic.List<GameObject>();
            foreach (var n in enemyNames)
            {
                var e = FindDeep(enemiesRoot, n);
                if (e != null) enemies.Add(e.gameObject);
            }

            var so = new SerializedObject(arena);
            var prop = so.FindProperty("enemies");
            prop.arraySize = enemies.Count;
            for (var i = 0; i < enemies.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void FixGameManagerEnemyCount()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null) return;
            var so = new SerializedObject(gm);
            so.FindProperty("totalEnemies").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
