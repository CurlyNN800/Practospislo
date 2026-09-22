#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedMansion.Editor
{
    /// <summary>Places concise decor on house floors above the -1floor basement.</summary>
    public static class HouseFurnishingMenu
    {
        const string FurnishingsRoot = "=== FURNISHINGS ===";
        const float UpperFloorY = 6.35f;
        const float GroundFloorY = 0f;

        struct Placement
        {
            public string prefab;
            public Vector3 position;
            public float rotY;
            public bool wallMounted;
        }

        [MenuItem("CursedMansion/Scene/Furnish house floors above basement")]
        public static void FurnishHouseFloorsAboveBasement()
        {
            var root = GameObject.Find(FurnishingsRoot);
            if (root == null)
            {
                Debug.LogError("[Furnish] Missing '=== FURNISHINGS ===' in scene.");
                return;
            }

            var upper = GetOrCreateChild(root.transform, "UpperFloor");
            ClearChildren(upper);

            var groundAdd = GetOrCreateChild(root.transform, "GroundFloor_Additions");
            ClearChildren(groundAdd);

            var count = 0;
            count += PlaceSet(upper.transform, UpperFloorY, UpperFloorSet());
            count += PlaceSet(groundAdd.transform, GroundFloorY, GroundFloorAdditions());
            count += PlaceUpperFloorLights(upper.transform);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[Furnish] Done — {count} objects on upper + ground floors (basement untouched).");
        }

        static List<Placement> UpperFloorSet() => new()
        {
            // Bedroom
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Bed_B.prefab", position = new(454.5f, 0f, 311.5f), rotY = 90f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Rug_B.prefab", position = new(454.5f, 0.01f, 311.5f), rotY = 0f },
            new() { prefab = "Assets/Furniture/Prefabs/BigCloset.prefab", position = new(451.5f, 0f, 313.5f), rotY = 0f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Lamp_A.prefab", position = new(452f, 0f, 309.5f), rotY = 45f },

            // Study / hall
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_LargeTable_A.prefab", position = new(461.5f, 0f, 314.5f), rotY = 0f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Chair_B.prefab", position = new(459.8f, 0f, 313.8f), rotY = 115f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Chair_B.prefab", position = new(463.2f, 0f, 313.8f), rotY = -115f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Vase_A.prefab", position = new(461.5f, 0f, 315.2f), rotY = 25f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Painting_C.prefab", position = new(461.5f, 1.15f, 308.8f), rotY = 0f, wallMounted = true },

            // Lounge
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Sofa_A.prefab", position = new(468f, 0f, 319.5f), rotY = 180f },
            new() { prefab = "Assets/Furniture/Prefabs/RoundTable.prefab", position = new(466.2f, 0f, 317.8f), rotY = 0f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Rug_C.prefab", position = new(468f, 0.01f, 319.5f), rotY = 0f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Lamp_B.prefab", position = new(470.5f, 0f, 321.5f), rotY = 270f },

            // Dressing nook
            new() { prefab = "Assets/Furniture/Prefabs/MirrorComode2.prefab", position = new(474.5f, 0f, 325.5f), rotY = 270f },
            new() { prefab = "Assets/Furniture/Prefabs/closet2.prefab", position = new(476.5f, 0f, 327.5f), rotY = 180f },
            new() { prefab = "Assets/Furniture/Prefabs/chair.prefab", position = new(472.5f, 0f, 324.5f), rotY = 90f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Painting_D.prefab", position = new(476f, 1.15f, 325.5f), rotY = 270f, wallMounted = true },
        };

        static List<Placement> GroundFloorAdditions() => new()
        {
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Cabinet_B.prefab", position = new(474.5f, 0f, 326.5f), rotY = 270f },
            new() { prefab = "Assets/Furniture/Prefabs/MetalTable.prefab", position = new(471.5f, 0f, 325f), rotY = 0f },
            new() { prefab = "Assets/Furniture/Prefabs/Fotel.prefab", position = new(469.5f, 0f, 321f), rotY = 200f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Rug_D.prefab", position = new(468f, 0.01f, 320f), rotY = 0f },
            new() { prefab = "Assets/Furniture/Prefabs/3Seat2.prefab", position = new(463.5f, 0f, 316.5f), rotY = 0f },
            new() { prefab = "Assets/Furniture/Prefabs/RoundTable.prefab", position = new(461f, 0f, 314f), rotY = 30f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_SmallTable_B.prefab", position = new(451f, 0f, 312f), rotY = 90f },
            new() { prefab = "Assets/Flooded_Grounds/Prefabs/Props/Prop_Lamp_A.prefab", position = new(450f, 0f, 314f), rotY = 0f },
        };

        static int PlaceSet(Transform parent, float floorY, List<Placement> items)
        {
            var count = 0;
            foreach (var p in items)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.prefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Furnish] Missing prefab: {p.prefab}");
                    continue;
                }

                var pos = p.position;
                pos.y = p.wallMounted ? floorY + p.position.y : floorY + (p.position.y > 0.001f ? p.position.y : 0f);

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, p.rotY, 0f);
                Undo.RegisterCreatedObjectUndo(go, "Furnish house");
                count++;
            }
            return count;
        }

        static int PlaceUpperFloorLights(Transform parent)
        {
            var spots = new[]
            {
                new Vector3(454.5f, 2.2f, 311.5f),
                new Vector3(461.5f, 2.2f, 314.5f),
                new Vector3(468f, 2.2f, 319.5f),
                new Vector3(474.5f, 2.2f, 325.5f),
            };
            var count = 0;
            for (var i = 0; i < spots.Length; i++)
            {
                var go = new GameObject($"Light_Upper_{i + 1}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(spots[i].x, UpperFloorY + spots[i].y, spots[i].z);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 9f;
                light.intensity = 1.1f;
                light.color = new Color(1f, 0.92f, 0.78f);
                Undo.RegisterCreatedObjectUndo(go, "Furnish lights");
                count++;
            }
            return count;
        }

        static Transform GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Furnish folder");
            return go.transform;
        }

        static void ClearChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
        }
    }
}
#endif
