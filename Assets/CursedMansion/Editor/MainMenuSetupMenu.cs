#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CursedMansion.Editor
{
    /// <summary>
    /// Собирает в открытой сцене все экраны игры (главное меню, уровни, пауза, победа, поражение)
    /// в одном World Space Canvas под XR-луч, EventSystem с XRUIInputModule и GameStateManager.
    /// </summary>
    public static class MainMenuSetupMenu
    {
        const string MenuRootName = "Main Menu";
        const string ManagerName = "Game State Manager";
        const string ConfigsFolder = "Assets/_GAME/Configs";
        const int LevelCount = 3;
        // 820 px * 0.001 = 0.82 м в ширину
        const float CanvasScale = 0.001f;

        // Разметка панелей (в пикселях Canvas)
        const float ContentWidth = 820f;
        const float TitleHeight = 130f;
        const float Spacing = 28f;
        const int TopPadding = 50;
        const int BottomPadding = 60;
        // Кнопки в 1.3 раза крупнее прежних 520x110
        static readonly Vector2 ButtonSize = new(676f, 143f);
        const int ButtonFontSize = 62;
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
            var oldManager = Object.FindFirstObjectByType<GameStateManager>();
            if (oldManager != null)
                Undo.DestroyObjectImmediate(oldManager.gameObject);

            EnsureXrEventSystem();

            // --- Менеджер состояний ---
            var managerGo = new GameObject(ManagerName);
            Undo.RegisterCreatedObjectUndo(managerGo, "Create Game State Manager");
            var manager = managerGo.AddComponent<GameStateManager>();
            SetupManager(manager);
            SetupAudio(managerGo.AddComponent<GameAudio>(), manager);

            // --- Canvas ---
            var root = new GameObject(MenuRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(ContentWidth, 1000f);
            rootRect.localScale = Vector3.one * CanvasScale;
            // Временная позиция для редактора; в игре MainMenu переставит панель перед игроком
            rootRect.position = new Vector3(0f, 1.5f, 1.5f);

            // Content = фон + панели. Высота фона подстраивается под активную панель (ContentSizeFitter),
            // поэтому у панелей с разным числом кнопок нет пустого места снизу.
            var content = CreateUiObject("Content", root.transform);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(ContentWidth, 0f);
            content.anchoredPosition = Vector2.zero;

            var bgImage = content.gameObject.AddComponent<Image>();
            bgImage.sprite = s_Sprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- Главное меню ---
            var mainPanel = CreatePanel("MainPanel", content, "Они идут");
            var playButton = CreateButton("PlayButton", "Играть", mainPanel);
            var levelsButton = CreateButton("LevelsButton", "Уровни", mainPanel);
            var exitButton = CreateButton("ExitButton", "Выход", mainPanel);

            // --- Подменю уровней ---
            var levelsPanel = CreatePanel("LevelsPanel", content, "Выбор уровня");
            var levelButtons = new Button[LevelCount];
            for (int i = 0; i < levelButtons.Length; i++)
                levelButtons[i] = CreateButton($"Level{i + 1}Button", $"Уровень {i + 1}", levelsPanel);
            var backButton = CreateButton("BackButton", "Назад", levelsPanel);

            // --- Пауза ---
            var pausePanel = CreatePanel("PausePanel", content, "Пауза");
            var resumeButton = CreateButton("ResumeButton", "Продолжить", pausePanel);
            var pauseRestartButton = CreateButton("RestartButton", "Заново", pausePanel);
            var pauseMenuButton = CreateButton("MenuButton", "В меню", pausePanel);

            // --- Победа ---
            var victoryPanel = CreatePanel("VictoryPanel", content, "Победа!");
            var nextLevelButton = CreateButton("NextLevelButton", "Следующий уровень", victoryPanel);
            var victoryRestartButton = CreateButton("RestartButton", "Заново", victoryPanel);
            var victoryMenuButton = CreateButton("MenuButton", "В меню", victoryPanel);

            // --- Поражение ---
            var defeatPanel = CreatePanel("DefeatPanel", content, "Поражение");
            var defeatRestartButton = CreateButton("RestartButton", "Заново", defeatPanel);
            var defeatMenuButton = CreateButton("MenuButton", "В меню", defeatPanel);

            // На старте видно только главное меню
            levelsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            victoryPanel.gameObject.SetActive(false);
            defeatPanel.gameObject.SetActive(false);

            // --- Скрипт меню и ссылки ---
            var menu = root.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("gameState").objectReferenceValue = manager;
            so.FindProperty("content").objectReferenceValue = content.gameObject;
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel.gameObject;
            so.FindProperty("levelsPanel").objectReferenceValue = levelsPanel.gameObject;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel.gameObject;
            so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel.gameObject;
            so.FindProperty("defeatPanel").objectReferenceValue = defeatPanel.gameObject;

            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("levelsButton").objectReferenceValue = levelsButton;
            so.FindProperty("exitButton").objectReferenceValue = exitButton;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            var buttonsProp = so.FindProperty("levelButtons");
            buttonsProp.arraySize = levelButtons.Length;
            for (int i = 0; i < levelButtons.Length; i++)
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelButtons[i];

            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("pauseRestartButton").objectReferenceValue = pauseRestartButton;
            so.FindProperty("pauseMenuButton").objectReferenceValue = pauseMenuButton;
            so.FindProperty("nextLevelButton").objectReferenceValue = nextLevelButton;
            so.FindProperty("victoryRestartButton").objectReferenceValue = victoryRestartButton;
            so.FindProperty("victoryMenuButton").objectReferenceValue = victoryMenuButton;
            so.FindProperty("defeatRestartButton").objectReferenceValue = defeatRestartButton;
            so.FindProperty("defeatMenuButton").objectReferenceValue = defeatMenuButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[MainMenu] Меню и Game State Manager созданы. Сохрани сцену (Ctrl+S).");
        }

        static void SetupManager(GameStateManager manager)
        {
            var so = new SerializedObject(manager);

            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = LevelCount;
            for (int i = 0; i < LevelCount; i++)
            {
                string path = $"{ConfigsFolder}/Level{i + 1}.asset";
                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (config == null)
                    Debug.LogWarning($"[MainMenu] Не найден конфиг {path} — назначь его в Game State Manager вручную.");
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = config;
            }

            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
                Debug.LogWarning("[MainMenu] В сцене нет EnemySpawner — назначь его в Game State Manager вручную.");
            so.FindProperty("spawner").objectReferenceValue = spawner;

            var playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
                Debug.LogWarning("[MainMenu] В сцене нет PlayerHealth — назначь его в Game State Manager вручную.");
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Назначаем звуки, которые уже есть в проекте. Недостающие остаются пустыми —
        // их можно положить в Assets/_GAME/Audio/SFX и назначить в инспекторе GameAudio.
        static void SetupAudio(GameAudio audio, GameStateManager manager)
        {
            var so = new SerializedObject(audio);
            so.FindProperty("gameState").objectReferenceValue = manager;
            AssignClip(so, "buttonClickClip", "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/DemoSceneAssets/Audio/Button Pop.wav");
            AssignClip(so, "portalOpenClip", "Assets/_GAME/Audio/SFX/PortalOpen_MonsterWhirr.mp3");
            AssignClip(so, "menuMusicClip", "Assets/_GAME/Audio/Music/MenuMusic_DangerAroundTheCorner.mp3");
            AssignClip(so, "monsterAttackClip", "Assets/_GAME/Audio/SFX/MonsterAttack.wav");
            AssignClip(so, "monsterDeathClip", "Assets/_GAME/Audio/SFX/MonsterDeath.wav");
            AssignClip(so, "victoryClip", "Assets/_GAME/Audio/SFX/Victory.wav");
            AssignClip(so, "defeatClip", "Assets/_GAME/Audio/SFX/Defeat.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignClip(SerializedObject so, string property, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"[GameAudio] Не найден звук {path} — поле {property} останется пустым.");
            so.FindProperty(property).objectReferenceValue = clip;
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

        // Все панели размечены одинаково: столбец "заголовок + кнопки" по центру,
        // заголовок сразу над кнопками, между всеми элементами одинаковый отступ.
        static RectTransform CreatePanel(string name, Transform parent, string titleText)
        {
            var panel = CreateUiObject(name, parent);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, TopPadding, BottomPadding);
            layout.spacing = Spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            // Размер элементов задаёт LayoutElement — все кнопки одинаковые
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTitle(titleText, panel);
            return panel;
        }

        static void CreateTitle(string text, Transform parent)
        {
            var title = CreateUiObject("Title", parent);
            var element = title.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ContentWidth;
            element.preferredHeight = TitleHeight;

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
            label.fontSize = ButtonFontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }
    }
}
#endif
