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

        // Разметка панелей (в пикселях Canvas)
        const float TitleHeight = 220f;
        const float BottomPadding = 60f;
        const float ButtonSpacing = 36f;
        static readonly Vector2 ButtonSize = new(520f, 110f);
        // Мертвенно-голубой
        static readonly Color TitleColor = new(0.62f, 0.86f, 0.95f);

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
            var mainPanel = CreatePanel("MainPanel", root.transform, "Они идут", out var mainButtons);
            var playButton = CreateButton("PlayButton", "Играть", mainButtons);
            var levelsButton = CreateButton("LevelsButton", "Уровни", mainButtons);
            var exitButton = CreateButton("ExitButton", "Выход", mainButtons);

            // --- Подменю уровней (та же разметка, что у главного) ---
            var levelsPanel = CreatePanel("LevelsPanel", root.transform, "Выбор уровня", out var levelsButtonsRoot);
            var levelButtons = new Button[3];
            for (int i = 0; i < levelButtons.Length; i++)
                levelButtons[i] = CreateButton($"Level{i + 1}Button", $"Уровень {i + 1}", levelsButtonsRoot);
            var backButton = CreateButton("BackButton", "Назад", levelsButtonsRoot);
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

        // Обе панели размечены одинаково: заголовок сверху фиксированной высоты,
        // под ним область кнопок, в которой столбец кнопок центрируется по вертикали и горизонтали.
        static RectTransform CreatePanel(string name, Transform parent, string titleText, out RectTransform buttons)
        {
            var panel = CreateUiObject(name, parent);
            Stretch(panel);

            CreateTitle(titleText, panel);

            buttons = CreateUiObject("Buttons", panel);
            buttons.anchorMin = Vector2.zero;
            buttons.anchorMax = Vector2.one;
            buttons.offsetMin = new Vector2(0f, BottomPadding);
            buttons.offsetMax = new Vector2(0f, -TitleHeight);

            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // Размер кнопок задаёт LayoutElement — все кнопки одинаковые
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return panel;
        }

        static void CreateTitle(string text, Transform parent)
        {
            var title = CreateUiObject("Title", parent);
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(0f, TitleHeight);
            title.anchoredPosition = Vector2.zero;

            var label = title.gameObject.AddComponent<Text>();
            label.font = s_Font;
            label.text = text;
            label.fontSize = 80;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = TitleColor;
            label.raycastTarget = false;

            // Холодное "свечение" вокруг букв
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.35f, 0.55f, 0.6f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        static Button CreateButton(string name, string text, Transform parent)
        {
            var rect = CreateUiObject(name, parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ButtonSize.x;
            element.preferredHeight = ButtonSize.y;

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
