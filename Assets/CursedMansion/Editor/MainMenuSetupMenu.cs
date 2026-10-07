#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CursedMansion.Editor
{
    /// <summary>Собирает в открытой сцене главное меню: World Space Canvas под XR-луч + EventSystem с XRUIInputModule.</summary>
    public static class MainMenuSetupMenu
    {
        const string MenuRootName = "Main Menu";
        const string ConfigsFolder = "Assets/_GAME/Configs";
        static readonly Vector2 CanvasSize = new(800f, 900f);
        // 800 px * 0.001 = 0.8 м в ширину
        const float CanvasScale = 0.001f;

        static Font s_Font;
        static Sprite s_Sprite;

        [MenuItem("CursedMansion/MVP/Create Main Menu (World Space)")]
        public static void CreateMainMenu()
        {
            s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            s_Sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var old = GameObject.Find(MenuRootName);
            if (old != null)
                Undo.DestroyObjectImmediate(old);

            EnsureXrEventSystem();

            // --- Canvas ---
            var root = new GameObject(MenuRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = CanvasSize;
            rootRect.localScale = Vector3.one * CanvasScale;
            // Временная позиция для редактора; в игре MainMenu переставит панель перед игроком
            rootRect.position = new Vector3(0f, 1.5f, 1.5f);

            var background = CreateUiObject("Background", root.transform);
            Stretch(background);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.sprite = s_Sprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            // --- Главное меню ---
            var mainPanel = CreatePanel("MainPanel", root.transform);
            CreateTitle("CURSED MANSION", mainPanel);
            var playButton = CreateButton("PlayButton", "Играть", mainPanel);
            var levelsButton = CreateButton("LevelsButton", "Уровни", mainPanel);
            var exitButton = CreateButton("ExitButton", "Выход", mainPanel);

            // --- Подменю уровней ---
            var levelsPanel = CreatePanel("LevelsPanel", root.transform);
            CreateTitle("Выбор уровня", levelsPanel);
            var levelButtons = new Button[3];
            for (int i = 0; i < levelButtons.Length; i++)
                levelButtons[i] = CreateButton($"Level{i + 1}Button", $"Уровень {i + 1}", levelsPanel);
            var backButton = CreateButton("BackButton", "Назад", levelsPanel);
            levelsPanel.gameObject.SetActive(false);

            // --- Скрипт меню и ссылки ---
            var menu = root.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel.gameObject;
            so.FindProperty("levelsPanel").objectReferenceValue = levelsPanel.gameObject;
            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("levelsButton").objectReferenceValue = levelsButton;
            so.FindProperty("exitButton").objectReferenceValue = exitButton;
            so.FindProperty("backButton").objectReferenceValue = backButton;

            var buttonsProp = so.FindProperty("levelButtons");
            buttonsProp.arraySize = levelButtons.Length;
            for (int i = 0; i < levelButtons.Length; i++)
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelButtons[i];

            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                string path = $"{ConfigsFolder}/Level{i + 1}.asset";
                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (config == null)
                    Debug.LogWarning($"[MainMenu] Не найден конфиг {path} — назначь его вручную.");
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = config;
            }

            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
                Debug.LogWarning("[MainMenu] В сцене нет EnemySpawner — назначь его в MainMenu вручную.");
            so.FindProperty("spawner").objectReferenceValue = spawner;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[MainMenu] Главное меню создано. Сохрани сцену (Ctrl+S).");
        }

        [MenuItem("CursedMansion/Debug/Reset Level Progress")]
        public static void ResetLevelProgress() => LevelProgress.ResetProgress();

        // Для работы луча с UI нужен EventSystem с XRUIInputModule (а не Standalone/InputSystem модуль)
        static void EnsureXrEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                return;
            }

            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                if (module is not XRUIInputModule)
                    Undo.DestroyObjectImmediate(module);
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                Undo.AddComponent<XRUIInputModule>(eventSystem.gameObject);
        }

        static RectTransform CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static RectTransform CreatePanel(string name, Transform parent)
        {
            var panel = CreateUiObject(name, parent);
            Stretch(panel);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(80, 80, 60, 60);
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return panel;
        }

        static void CreateTitle(string text, Transform parent)
        {
            var title = CreateUiObject("Title", parent);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;
            var label = title.gameObject.AddComponent<Text>();
            label.font = s_Font;
            label.text = text;
            label.fontSize = 64;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.85f, 0.1f, 0.1f);
            label.raycastTarget = false;
        }

        static Button CreateButton(string name, string text, Transform parent)
        {
            var rect = CreateUiObject(name, parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = s_Sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.22f, 0.26f);
            colors.highlightedColor = new Color(0.45f, 0.12f, 0.12f);
            colors.pressedColor = new Color(0.65f, 0.15f, 0.15f);
            colors.selectedColor = colors.normalColor;
            // Заблокированный уровень — серый
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
            button.colors = colors;

            var labelRect = CreateUiObject("Text", rect);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = s_Font;
            label.text = text;
            label.fontSize = 48;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }
    }
}
#endif
