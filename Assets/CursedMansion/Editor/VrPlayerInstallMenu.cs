#if UNITY_EDITOR
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using VRFPSKit;

namespace CursedMansion.Editor
{
    /// <summary>
    /// Путь Б: замена Starter-рига (NearFar) на VR Player Variant из VRFPS Kit.
    /// Настраивает физические слои и матрицу коллизий как в CursedMansion,
    /// чистит Missing Script (PostProcessVolume v2) в базовом префабе и меняет риг в открытой сцене.
    /// </summary>
    public static class VrPlayerInstallMenu
    {
        const string VariantPath = "Assets/_GAME/Prefabs/VR Player Variant.prefab";
        const string BasePlayerPath = "Assets/VRFPS Kit/Prefabs/Objects/VR Player.prefab";

        // Номера слоёв как в CursedMansion (код кита ищет их по имени через NameToLayer)
        static readonly (int index, string name)[] Layers =
        {
            (6, "Player"), (7, "Bullet"), (8, "Hand"), (9, "HandPresence"),
            (11, "Interactable"), (12, "ChildInteractable"), (14, "PostProcessing"),
        };

        // Пары слоёв, которые НЕ сталкиваются (матрица из CursedMansion)
        static readonly (int a, int b)[] IgnoredPairs =
        {
            (6, 9), (6, 11), (6, 12),
            (7, 7), (7, 11), (7, 12),
            (8, 8), (8, 9),
            (11, 12), (12, 12),
        };

        const int PostProcessingLayer = 14;
        const float KitDamageableHealth = 1e9f;

        [MenuItem("CursedMansion/Scene/Install VR Player (Path B)")]
        public static void InstallAll()
        {
            if (!ApplyLayers()) return;
            ApplyCollisionMatrix();
            CleanBasePrefabMissingScripts();
            ReplaceRigInActiveScene();
        }

        [MenuItem("CursedMansion/Scene/VR Player: only layers + collision matrix")]
        public static void InstallLayersOnly()
        {
            if (!ApplyLayers()) return;
            ApplyCollisionMatrix();
        }

        static bool ApplyLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            // Сначала проверяем, что не затрём чужие слои
            foreach (var (index, name) in Layers)
            {
                var current = layers.GetArrayElementAtIndex(index).stringValue;
                bool free = string.IsNullOrEmpty(current) || current == name || (index == 11 && current == "FirearmParts");
                if (!free)
                {
                    Debug.LogError($"[VrPlayerInstall] Слой {index} занят '{current}', ожидался '{name}'. Ничего не изменено.");
                    return false;
                }
            }

            foreach (var (index, name) in Layers)
                layers.GetArrayElementAtIndex(index).stringValue = name;

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[VrPlayerInstall] Физические слои выставлены как в CursedMansion (11 FirearmParts → Interactable).");
            return true;
        }

        static void ApplyCollisionMatrix()
        {
            // Сначала всё сталкивается со всем, затем выключаем пары из CursedMansion
            for (int a = 0; a < 32; a++)
                for (int b = a; b < 32; b++)
                    Physics.IgnoreLayerCollision(a, b, false);

            foreach (var (a, b) in IgnoredPairs)
                Physics.IgnoreLayerCollision(a, b, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"[VrPlayerInstall] Матрица коллизий перенесена: выключено пар {IgnoredPairs.Length}.");
        }

        static void CleanBasePrefabMissingScripts()
        {
            var root = PrefabUtility.LoadPrefabContents(BasePlayerPath);
            try
            {
                int removed = 0;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                if (removed > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, BasePlayerPath);

                Debug.Log($"[VrPlayerInstall] Удалено Missing Script в '{BasePlayerPath}': {removed} (PostProcessVolume v2).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ReplaceRigInActiveScene()
        {
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            if (variant == null)
            {
                Debug.LogError($"[VrPlayerInstall] Не найден префаб '{VariantPath}'.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            XROrigin oldRig = null;
            foreach (var origin in Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(origin.gameObject) ==
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(variant))
                {
                    Debug.LogWarning("[VrPlayerInstall] VR Player уже стоит в сцене, замена рига пропущена.");
                    return;
                }
                oldRig = origin;
            }

            var newRoot = (GameObject)PrefabUtility.InstantiatePrefab(variant, scene);
            Undo.RegisterCreatedObjectUndo(newRoot, "Install VR Player");

            var newOrigin = newRoot.GetComponent<XROrigin>();
            var damageable = newRoot.GetComponent<Damageable>();
            var camera = newOrigin.Camera;
            var urpData = camera.GetComponent<UniversalAdditionalCameraData>();

            // Правки экземпляра префаба надо записать как оверрайды, иначе они не сохранятся в сцене
            Object[] edited = { newRoot, newRoot.transform, newOrigin, damageable, camera.gameObject, camera, urpData };
            foreach (var o in edited)
                if (o != null) Undo.RecordObject(o, "Install VR Player");

            if (oldRig != null)
                newRoot.transform.SetPositionAndRotation(oldRig.transform.position, oldRig.transform.rotation);

            newRoot.tag = "Player";
            newOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // Кит-смерть отключаем: смерть игрока идёт через PlayerHealth (удары монстров)
            if (damageable != null)
            {
                damageable.health = KitDamageableHealth;
                damageable.deathSceneName = string.Empty;
            }

            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f); // прозрачный фон для passthrough

            if (urpData != null)
                urpData.volumeLayerMask |= 1 << PostProcessingLayer; // виньетка урона на слое PostProcessing

            foreach (var o in edited)
                if (o != null) PrefabUtility.RecordPrefabInstancePropertyModifications(o);

            var oldCamera = oldRig != null ? oldRig.Camera : null;
            CopyOrAdd<PlayerHealth>(oldCamera, camera.gameObject);
            CopyOrAdd<ARCameraManager>(oldCamera, camera.gameObject);

            if (oldRig != null)
            {
                Debug.Log($"[VrPlayerInstall] Удаляю старый риг '{oldRig.name}' вместе с дочерними руками.");
                Undo.DestroyObjectImmediate(oldRig.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = newRoot;
            Debug.Log($"[VrPlayerInstall] Риг заменён на VR Player Variant, сцена '{scene.name}' сохранена.");
        }

        static void CopyOrAdd<T>(Camera from, GameObject to) where T : Component
        {
            var target = to.GetComponent<T>();
            if (target == null)
                target = Undo.AddComponent<T>(to);
            var source = from != null ? from.GetComponent<T>() : null;
            if (source != null)
                EditorUtility.CopySerialized(source, target);
            else
                Debug.LogWarning($"[VrPlayerInstall] {typeof(T).Name} на старой камере не найден, добавлен со значениями по умолчанию.");
        }
    }
}
#endif
