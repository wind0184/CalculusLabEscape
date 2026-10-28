#if UNITY_EDITOR
using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static class PrototypeSceneGenerator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Level01_Limits.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string DifferentiationScenePath = "Assets/_Project/Scenes/Level02_Differentiation.unity";
        private const string IntegrationScenePath = "Assets/_Project/Scenes/Level03_Integration.unity";

        public static void Generate()
        {
            Directory.CreateDirectory("Assets/_Project/Materials");
            Directory.CreateDirectory("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level 01 - Limits Lab");
            var builder = root.AddComponent<CyberLabSceneBuilder>();

            SerializedObject serialized = new SerializedObject(builder);
            serialized.FindProperty("floorMaterial").objectReferenceValue = Material("Floor", new Color(.025f, .035f, .08f), 0f);
            serialized.FindProperty("wallMaterial").objectReferenceValue = Material("Wall", new Color(.04f, .055f, .12f), .25f);
            serialized.FindProperty("cyanMaterial").objectReferenceValue = Material("NeonCyan", new Color(.02f, .75f, .95f), .45f);
            serialized.FindProperty("magentaMaterial").objectReferenceValue = Material("NeonMagenta", new Color(.9f, .04f, .65f), .4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated prototype scene: {ScenePath}");
        }

        public static void GenerateDifferentiation()
        {
            Directory.CreateDirectory("Assets/_Project/Materials");
            Directory.CreateDirectory("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level 02 - Differentiation Lab");
            var builder = root.AddComponent<DifferentiationSceneBuilder>();

            SerializedObject serialized = new SerializedObject(builder);
            serialized.FindProperty("floorMaterial").objectReferenceValue = Material("Floor", new Color(.025f, .035f, .08f), 0f);
            serialized.FindProperty("wallMaterial").objectReferenceValue = Material("Wall", new Color(.04f, .055f, .12f), .25f);
            serialized.FindProperty("cyanMaterial").objectReferenceValue = Material("NeonCyan", new Color(.02f, .75f, .95f), .45f);
            serialized.FindProperty("magentaMaterial").objectReferenceValue = Material("NeonMagenta", new Color(.9f, .04f, .65f), .4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, DifferentiationScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DifferentiationScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated rate-of-change prototype scene: {DifferentiationScenePath}");
        }

        public static void GenerateDifferentiationBatch()
        {
            GenerateDifferentiation();
            EditorApplication.Exit(0);
        }

        public static void GenerateIntegration()
        {
            Directory.CreateDirectory("Assets/_Project/Materials");
            Directory.CreateDirectory("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level 03 - Integration Lab");
            var builder = root.AddComponent<IntegrationSceneBuilder>();

            SerializedObject serialized = new SerializedObject(builder);
            serialized.FindProperty("floorMaterial").objectReferenceValue = Material("Floor", new Color(.025f, .035f, .08f), 0f);
            serialized.FindProperty("wallMaterial").objectReferenceValue = Material("Wall", new Color(.04f, .055f, .12f), .25f);
            serialized.FindProperty("cyanMaterial").objectReferenceValue = Material("NeonCyan", new Color(.02f, .75f, .95f), .45f);
            serialized.FindProperty("magentaMaterial").objectReferenceValue = Material("NeonMagenta", new Color(.9f, .04f, .65f), .4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, IntegrationScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DifferentiationScenePath, true),
                new EditorBuildSettingsScene(IntegrationScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated integration prototype scene: {IntegrationScenePath}");
        }

        public static void GenerateIntegrationBatch()
        {
            GenerateIntegration();
            EditorApplication.Exit(0);
        }

        public static void GenerateMainMenu()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMenuHierarchy();
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DifferentiationScenePath, true),
                new EditorBuildSettingsScene(IntegrationScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated frontend scene: {MainMenuScenePath}");
        }

        private static void BuildMenuHierarchy()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color background = new Color(.008f, .022f, .032f, 1f);
            Color panel = new Color(.018f, .055f, .07f, .97f);
            Color cyan = new Color(.04f, .76f, .86f, 1f);
            Color white = new Color(.88f, .95f, .97f, 1f);
            Color muted = new Color(.55f, .67f, .7f, 1f);

            GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera));
            Camera menuCamera = cameraObject.GetComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = background;
            menuCamera.cullingMask = 0;
            cameraObject.transform.position = new Vector3(0, 0, -10);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsFirstSibling();
            GameObject canvasObject = new GameObject("FrontendCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MenuHierarchyController));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            Image backgroundImage = canvasObject.AddComponent<Image>();
            backgroundImage.color = background;

            GameObject header = UiPanel(canvasObject.transform, "PersistentHeader", new Vector2(0, 642), new Vector2(1280, 78), background);
            UiText(header.transform, "Logo", "∫  CALCULUS LAB\n     ESCAPE PROTOCOL", new Vector2(45, 12), new Vector2(440, 55), 18, white, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(header.transform, "SystemStatus", "●  SYSTEM ONLINE   |   v1.0", new Vector2(965, 20), new Vector2(265, 35), 12, new Color(.2f, .95f, .57f), font, TextAnchor.MiddleRight, FontStyle.Bold);

            GameObject login = UiPanel(canvasObject.transform, "LoginPage", Vector2.zero, new Vector2(1280, 642), background);
            UiText(login.transform, "FacilityLabel", "RESTRICTED EDUCATIONAL FACILITY", new Vector2(88, 505), new Vector2(600, 25), 13, cyan, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(login.transform, "HeroTitle", "REPAIR THE\nMATHEMATICS.\nESCAPE THE LAB.", new Vector2(88, 190), new Vector2(650, 310), 57, white, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(login.transform, "HeroDescription", "A corrupted virtual laboratory. Three calculus chambers.\nEvery correct solution restores the world around you.", new Vector2(90, 105), new Vector2(620, 65), 17, muted, font, TextAnchor.MiddleLeft);
            GameObject loginPanel = UiPanel(login.transform, "LoginPanel", new Vector2(845, 68), new Vector2(285, 450), panel);
            UiText(loginPanel.transform, "PanelTitle", "IDENTITY VERIFICATION\n\nSECURE LOGIN", new Vector2(35, 330), new Vector2(215, 85), 18, white, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(loginPanel.transform, "StudentIdLabel", "STUDENT ID", new Vector2(35, 275), new Vector2(205, 20), 11, cyan, font);
            UiInput(loginPanel.transform, "StudentIdInput", new Vector2(35, 225), new Vector2(205, 42), panel * 1.8f, white, font);
            UiText(loginPanel.transform, "AccessKeyLabel", "ACCESS KEY", new Vector2(35, 185), new Vector2(205, 20), 11, cyan, font);
            UiInput(loginPanel.transform, "AccessKeyInput", new Vector2(35, 135), new Vector2(205, 42), panel * 1.8f, white, font);
            UiButton(loginPanel.transform, "AuthenticateButton", "AUTHENTICATE  →", new Vector2(35, 70), new Vector2(205, 50), cyan, Color.black, font);
            UiText(loginPanel.transform, "LoginStatus", "●  TRAINING ENVIRONMENT READY", new Vector2(35, 28), new Vector2(220, 25), 10, new Color(.2f, .95f, .57f), font);

            GameObject main = UiPanel(canvasObject.transform, "MainMenuPage", Vector2.zero, new Vector2(1280, 642), background);
            UiText(main.transform, "WelcomeText", "WELCOME BACK, TRAINEE", new Vector2(88, 470), new Vector2(600, 25), 13, cyan, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(main.transform, "MainTitle", "CALCULUS\nLAB ESCAPE", new Vector2(88, 260), new Vector2(620, 200), 62, white, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(main.transform, "MainDescription", "Restore all three laboratory systems by solving limits,\ndifferentiation, and integration challenges.", new Vector2(90, 190), new Vector2(600, 60), 17, muted, font);
            GameObject buttons = UiContainer(main.transform, "MenuButtons", new Vector2(1280, 642));
            string[] menuNames = { "StartButton", "PerformanceButton", "InstructionsButton", "LogoutButton", "ExitButton" };
            string[] menuLabels = { "01   START / CONTINUE", "02   PERFORMANCE", "03   INSTRUCTIONS", "04   LOG OUT", "05   EXIT" };
            for (int i = 0; i < menuNames.Length; i++) UiButton(buttons.transform, menuNames[i], menuLabels[i], new Vector2(765, 430 - i * 75), new Vector2(420, 62), panel, white, font);

            GameObject levels = BasePage(canvasObject.transform, "LevelSelectPage", "LABORATORY MAP", "SELECT A CHAMBER", background, cyan, white, font);
            UiButton(levels.transform, "BackButton", "← MAIN MENU", new Vector2(1040, 480), new Vector2(150, 42), panel, white, font);
            GameObject cards = UiContainer(levels.transform, "LevelCards", new Vector2(1280, 642));
            LevelCard(cards.transform, "LimitsCard", "01\n\nlim\n\nLIMITS\nSECURITY CALIBRATION", 88, cyan, panel, white, font);
            LevelCard(cards.transform, "DifferentiationCard", "02\n\nd/dx\n\nDIFFERENTIATION\nMOVING CABLE LOCK", 467, new Color(.95f, .58f, .16f), panel, white, font);
            LevelCard(cards.transform, "IntegrationCard", "03\n\n∫\n\nINTEGRATION\nENERGY BRIDGE", 846, new Color(.53f, .24f, .86f), panel, white, font);

            GameObject dashboard = BasePage(canvasObject.transform, "DashboardPage", "LEARNING ANALYTICS", "PERFORMANCE", background, cyan, white, font);
            UiButton(dashboard.transform, "BackButton", "← MAIN MENU", new Vector2(1040, 480), new Vector2(150, 42), panel, white, font);
            UiText(dashboard.transform, "DashboardSummary", "CHAMBERS RESTORED     0 / 3\n\nOVERALL PROGRESS      0%\n\nACTIVE USER           TRAINEE", new Vector2(88, 140), new Vector2(1030, 300), 24, white, font, TextAnchor.UpperLeft, FontStyle.Bold);

            GameObject instructions = BasePage(canvasObject.transform, "InstructionsPage", "FIELD MANUAL", "INSTRUCTIONS", background, cyan, white, font);
            UiButton(instructions.transform, "BackButton", "← MAIN MENU", new Vector2(1040, 480), new Vector2(150, 42), panel, white, font);
            UiText(instructions.transform, "InstructionsBody", "01  NAVIGATE                 02  INTERACT                 03  SYSTEM\n\nW A S D                         E                              ESC\n\nMove through the lab.       Access glowing terminals.       Pause or close a puzzle.\nUse the mouse to look.", new Vector2(88, 130), new Vector2(1100, 300), 21, white, font, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private static GameObject BasePage(Transform parent, string name, string overline, string title, Color background, Color cyan, Color white, Font font)
        {
            GameObject page = UiPanel(parent, name, Vector2.zero, new Vector2(1280, 642), background);
            UiText(page.transform, "SectionLabel", overline, new Vector2(88, 505), new Vector2(700, 25), 13, cyan, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiText(page.transform, "PageTitle", title, new Vector2(88, 430), new Vector2(800, 70), 54, white, font, TextAnchor.MiddleLeft, FontStyle.Bold);
            return page;
        }

        private static void LevelCard(Transform parent, string name, string label, float x, Color accent, Color panel, Color white, Font font)
        {
            GameObject card = UiPanel(parent, name, new Vector2(x, 55), new Vector2(346, 355), panel);
            UiText(card.transform, "CardText", label, new Vector2(25, 65), new Vector2(296, 255), 22, accent, font, TextAnchor.UpperLeft, FontStyle.Bold);
            UiButton(card.transform, "EnterButton", "ENTER CHAMBER  →", new Vector2(25, 18), new Vector2(296, 42), accent, Color.black, font);
        }

        private static GameObject UiPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().color = color; return go;
        }

        private static GameObject UiContainer(Transform parent, string name, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return go;
        }

        private static Text UiText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, Font font, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
            Text text = go.GetComponent<Text>(); text.text = value; text.font = font; text.fontSize = fontSize; text.color = color; text.alignment = alignment; text.fontStyle = style; return text;
        }

        private static Button UiButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Color textColor, Font font)
        {
            GameObject go = UiPanel(parent, name, position, size, color); Button button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            UiText(go.transform, "Label", label, Vector2.zero, size, 13, textColor, font, TextAnchor.MiddleCenter, FontStyle.Bold); return button;
        }

        private static InputField UiInput(Transform parent, string name, Vector2 position, Vector2 size, Color color, Color textColor, Font font)
        {
            GameObject go = UiPanel(parent, name, position, size, color); InputField field = go.AddComponent<InputField>();
            Text text = UiText(go.transform, "Text", string.Empty, new Vector2(12, 3), new Vector2(size.x - 24, size.y - 6), 14, textColor, font);
            Text placeholder = UiText(go.transform, "Placeholder", "Enter value", new Vector2(12, 3), new Vector2(size.x - 24, size.y - 6), 14, new Color(.45f, .55f, .58f), font);
            field.textComponent = text; field.placeholder = placeholder; return field;
        }

        public static void GenerateMainMenuBatch()
        {
            GenerateMainMenu();
            EditorApplication.Exit(0);
        }

        public static void GenerateBatch()
        {
            Generate();
            EditorApplication.Exit(0);
        }

        public static void BuildWindowsBatch()
        {
            Generate();
            Directory.CreateDirectory("Builds/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/CalculusLabEscape_v1.0.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Prototype build failed: {report.summary.result}");
            Debug.Log($"Prototype build succeeded: {report.summary.outputPath}");
            EditorApplication.Exit(0);
        }

        private static Material Material(string name, Color color, float metallic)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name, color = color };
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", .75f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
#endif
