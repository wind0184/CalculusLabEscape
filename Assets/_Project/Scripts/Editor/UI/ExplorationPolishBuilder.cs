#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class ExplorationPolishBuilder
    {
        static string Evidence => Path.GetFullPath("../../qa/exploration-polish-2026-10-19");

        static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        static TMP_Text Label(Transform parent, string name, string value, TMP_FontAsset font, float x, float y, float width, float size)
        {
            var text = Rect(parent, name, x, y, width, 22).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size;
            text.color = new Color(.5f, .68f, .73f);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.raycastTarget = false;
            return text;
        }

        [MenuItem("Calculus Lab/Presentation/Refine exploration HUD")]
        public static void Apply()
        {
            for (int topic = 1; topic <= 2; topic++)
            {
                string path = "Assets/_Project/Prefabs/Resources/Production/" + (topic == 1 ? "DifferentiationUI" : "IntegrationUI") + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var nodes = root.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
                    var hud = root.GetComponent<ExplorationHUD>() ?? root.AddComponent<ExplorationHUD>();
                    hud.objectivePanel = (RectTransform)nodes["WorldHUD"];
                    hud.guidePanel = (RectTransform)nodes["WorldGuide"];
                    hud.promptPanel = (RectTransform)nodes["PromptPanel"];
                    hud.progress = nodes["WorldProgress"].GetComponent<TMP_Text>();
                    hud.objective = nodes["WorldObjective"].GetComponent<TMP_Text>();
                    hud.guideTitle = nodes["WorldGuideTitle"].GetComponent<TMP_Text>();
                    hud.guideBody = nodes["WorldGuideBody"].GetComponent<TMP_Text>();
                    hud.guideKeys = nodes["WorldGuideKeys"].GetComponent<TMP_Text>();
                    hud.prompt = nodes["WorldPrompt"].GetComponent<TMP_Text>();
                    hud.promptSurface = hud.promptPanel.GetComponentInChildren<SimulationAnswerBackdrop>(true);
                    var existing = hud.objectivePanel.Find("Installation progress");
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);
                    var segments = Rect(hud.objectivePanel, "Installation progress", 680, 10, 200, 32);
                    Label(segments, "Installation legend", topic == 1 ? "REPAIR COMPONENTS" : "DEVICE MODULES", hud.progress.font, 0, -4, 200, 10);
                    hud.progressSegments = new Image[topic == 1 ? 3 : 4];
                    float width = (200 - (hud.progressSegments.Length - 1) * 5) / hud.progressSegments.Length;
                    for (int i = 0; i < hud.progressSegments.Length; i++)
                    {
                        var segment = Rect(segments, "Progress segment " + i, i * (width + 5), 22, width, 4).gameObject.AddComponent<Image>();
                        segment.raycastTarget = false;
                        hud.progressSegments[i] = segment;
                    }
                    var state = hud.promptPanel.Find("Interaction state");
                    if (state != null) Object.DestroyImmediate(state.gameObject);
                    hud.interactionState = Label(hud.promptPanel, "Interaction state", "APPROACH", hud.progress.font, 734, 15, 161, 13);
                    hud.enabled = false;
                    hud.enabled = true;
                    hud.Present(false, false, 0);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("EXPLORATION_POLISH_APPLIED");
        }

        public static void VerifyAndCapture()
        {
            Directory.CreateDirectory(Evidence);
            for (int topic = 1; topic <= 2; topic++)
            {
                EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[topic] + ".unity");
                var world = Object.FindFirstObjectByType<Chapter4World>();
                var canvas = Object.FindFirstObjectByType<MissionCanvas>().GetComponent<Canvas>();
                var hud = canvas.GetComponent<ExplorationHUD>();
                if (hud == null || hud.progressSegments.Length != (topic == 1 ? 3 : 4)) throw new Exception("Saved scene did not inherit the HUD component");
                foreach (string mode in new[] { "initial", "moving", "near", "complete" })
                {
                    SceneAuthoringWindow.ApplyView(world, canvas, mode == "near" ? "Device guide" : "Movement guide");
                    hud.Present(mode != "initial", mode == "near", mode == "complete" ? hud.progressSegments.Length : 0);
                    if (mode == "moving") hud.guideTitle.text = "FIND / " + (topic == 1 ? "Power Core" : "Containment case");
                    if (mode == "complete") hud.progress.text = topic == 1 ? "Bridge repair: 3/3" : "Modules: 4/4";
                    world.player.GetComponentInChildren<Camera>().transform.LookAt(topic == 1 ? world.devices[0].position + Vector3.up * .4f : new Vector3(0, 1.3f, 1));
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in canvas.GetComponentsInChildren<TMP_Text>())
                    { text.ForceMeshUpdate(); if (text.isTextOverflowing) throw new Exception("HUD overflow: " + mode + " / " + text.name); }
                    if (hud.guidePanel.rect.height != (mode == "initial" ? 136 : 68)) throw new Exception("Guide did not compact");
                    SceneAuthoring.Capture(world, canvas, Path.Combine(Evidence, topic + "-" + mode + ".png"), false);
                }
            }
            File.WriteAllText(Path.Combine(Evidence, "editor-result.txt"), "PASS: both saved mission scenes inherit compact HUD; initial/moving/near/complete states fit; no session started.\n");
            Debug.Log("EXPLORATION_POLISH_EDITOR_PASS");
        }

        public static void ApplyAndBuild()
        {
            Apply();
            VerifyAndCapture();
            SceneAuthoring.ValidateAndCaptureTo(Path.Combine(Evidence, "all-views"));
            ProductionUIBuilder.BuildDelivery();
            Debug.Log("EXPLORATION_POLISH_DELIVERY_PASS");
        }
    }
}
#endif
