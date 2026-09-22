#if UNITY_EDITOR
using CursedMansion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedMansion.Editor
{
    public static class MvpSceneSetupMenu
    {
        [MenuItem("CursedMansion/MVP/Add House → Interior portal (uses selection as parent)")]
        static void AddHouseToInteriorPortal()
        {
            var parent = Selection.activeTransform;
            if (parent == null)
            {
                Debug.LogWarning("Выберите родителя в Hierarchy (например пустой объект у двери).");
                return;
            }

            var go = new GameObject("CM_Portal_ToInterior");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 3f, 2f);

            var trigger = go.AddComponent<SceneTransitionTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("targetScene").stringValue = GameScenes.Interior;
            so.FindProperty("targetSpawnId").stringValue = "FromHouseScene";
            so.FindProperty("progressTarget").enumValueIndex = (int)CursedMansionSceneTarget.Interior;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Add CM Portal To Interior");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
        }

        [MenuItem("CursedMansion/MVP/Add Interior finale triggers (win / lose volumes)")]
        static void AddInteriorFinaleTriggers()
        {
            var parent = Selection.activeTransform;
            if (parent == null)
            {
                Debug.LogWarning("Выберите родителя в Hierarchy.");
                return;
            }

            var root = new GameObject("CM_FinaleTriggers");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Add CM Finale Triggers");

            CreateFinaleChild(root.transform, "CM_Finale_Win", new Vector3(2f, 0f, 0f), true);
            CreateFinaleChild(root.transform, "CM_Finale_Lose", new Vector3(-2f, 0f, 0f), false);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
        }

        static void CreateFinaleChild(Transform parent, string name, Vector3 localPos, bool victory)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2.5f, 2f);

            var ft = go.AddComponent<FinaleTriggerVolume>();
            var so = new SerializedObject(ft);
            so.FindProperty("victoryOnEnter").boolValue = victory;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Add finale trigger");
        }

        [MenuItem("CursedMansion/MVP/Make selection VR-grabbable")]
        static void MakeSelectionGrabbable()
        {
            foreach (var go in Selection.gameObjects)
            {
                var setup = go.GetComponent<VrGrabbableSetup>();
                if (setup == null)
                    setup = Undo.AddComponent<VrGrabbableSetup>(go);
                setup.Configure();
                EditorUtility.SetDirty(go);
            }

            Debug.Log("[CursedMansion] VrGrabbableSetup применён к выделению.");
        }

        [MenuItem("CursedMansion/MVP/Add scene spawn point (selection)")]
        static void AddSceneSpawnPoint()
        {
            var parent = Selection.activeTransform;
            if (parent == null)
            {
                Debug.LogWarning("Выберите точку в Hierarchy (пустой объект у входа).");
                return;
            }

            var go = new GameObject("CM_Spawn_FromHouseScene");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var spawn = go.AddComponent<SceneSpawnPoint>();
            var so = new SerializedObject(spawn);
            so.FindProperty("spawnId").stringValue = "FromHouseScene";
            so.FindProperty("isDefault").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Add Scene Spawn Point");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
        }

        [MenuItem("CursedMansion/MVP/Add Inspectable note (trigger)")]
        static void AddInspectableNote()
        {
            var parent = Selection.activeTransform;
            if (parent == null)
            {
                Debug.LogWarning("Выберите родителя в Hierarchy.");
                return;
            }

            var go = new GameObject("CM_InspectableNote");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 1.2f;

            go.AddComponent<InspectableNote>();
            Undo.RegisterCreatedObjectUndo(go, "Add Inspectable Note");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
        }

        [MenuItem("CursedMansion/MVP/Add VR passenger ride (selection = vehicle root)")]
        static void AddVrPassengerRide()
        {
            var vehicle = Selection.activeTransform;
            if (vehicle == null)
            {
                Debug.LogWarning("Выберите корень машины в Hierarchy.");
                return;
            }

            var seat = new GameObject("VR_PassengerSeat");
            seat.transform.SetParent(vehicle, false);
            seat.transform.localPosition = new Vector3(0f, 0.65f, 0.12f);
            seat.transform.localRotation = Quaternion.identity;
            seat.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(seat, "VR Passenger Seat");

            var path = vehicle.GetComponent<SimpleWaypointVehicle>();
            if (path == null)
                path = Undo.AddComponent<SimpleWaypointVehicle>(vehicle.gameObject);

            var ride = vehicle.GetComponent<VrPassengerRide>();
            if (ride == null)
                ride = Undo.AddComponent<VrPassengerRide>(vehicle.gameObject);

            var soRide = new SerializedObject(ride);
            soRide.FindProperty("seatAnchor").objectReferenceValue = seat.transform;
            soRide.FindProperty("vehiclePath").objectReferenceValue = path;
            soRide.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(vehicle.gameObject.scene);
            Selection.activeGameObject = seat;
        }

        [MenuItem("CursedMansion/Debug/Toggle Road intro auto scene change (Editor)", false, 200)]
        static void ToggleRoadIntroAutoSceneChange()
        {
            RoadIntroSequenceController.AllowAutoSceneChange = !RoadIntroSequenceController.AllowAutoSceneChange;
            Debug.Log(
                $"[CursedMansion] Road intro → HouseScene после задержки: " +
                $"{(RoadIntroSequenceController.AllowAutoSceneChange ? "ВКЛ" : "ВЫКЛ")} (только смысл в Play Mode в Editor).");
        }
    }
}
#endif
