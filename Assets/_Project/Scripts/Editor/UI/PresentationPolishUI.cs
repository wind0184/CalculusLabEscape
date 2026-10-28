#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static partial class PresentationPolishBuilder
    {
        static RectTransform UIRect(Transform parent, string name, float x, float y, float width, float height)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = new Vector2(0, 1);
            root.pivot = new Vector2(0, 1);
            root.anchoredPosition = new Vector2(x, -y);
            root.sizeDelta = new Vector2(width, height);
            return root;
        }

        static void UIText(Transform parent, string name, string value, float x, float y, float width, TMP_FontAsset font)
        {
            var text = UIRect(parent, name, x, y, width, 28).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = 17;
            text.color = new Color(.69f, .8f, .84f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
        }

        static void Icon(Transform parent, string filename, float x, float width = 28)
        {
            var image = UIRect(parent, "Prompt icon " + filename, x, 9, width, 27).gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/InputPrompts/Sprites/" + filename + ".png");
            if (image.sprite == null) throw new Exception("Missing prompt sprite: " + filename);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void LayoutPrompts(GameObject root)
        {
            var nodes = root.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(group => group.Key, group => group.First());
            var font = nodes["WorldPrompt"].GetComponent<TMP_Text>().font;
            var panel = nodes["PromptPanel"].GetComponent<RectTransform>();
            panel.anchoredPosition = new Vector2(140, -578);
            panel.sizeDelta = new Vector2(1000, 54);
            var prompt = nodes["WorldPrompt"].GetComponent<TMP_Text>();
            prompt.fontSize = 19;
            prompt.rectTransform.anchoredPosition = new Vector2(23, -11);
            var guide = nodes["WorldGuide"].GetComponent<RectTransform>();
            guide.anchoredPosition = new Vector2(29, -405);
            foreach (string name in new[] { "WorldHUD", "WorldGuide", "PromptPanel" })
                RoundedPanel(nodes[name]);

            if (!nodes.ContainsKey("Exploration control strip"))
            {
                var strip = UIRect(nodes["World"], "Exploration control strip", 160, 646, 960, 45);
                var background = strip.gameObject.AddComponent<Image>();
                background.color = new Color(.02f, .04f, .06f, .94f);
                background.raycastTarget = false;
                RoundedPanel(strip);
                var accent = UIRect(strip, "Control strip accent", 0, 0, 960, 1).gameObject.AddComponent<Image>();
                accent.color = new Color(.17f, .44f, .5f, .7f); accent.raycastTarget = false;
                string[] keys = { "w", "a", "s", "d" };
                for (int i = 0; i < keys.Length; i++) Icon(strip, "keyboard_" + keys[i], 20 + i * 29, 25);
                UIText(strip, "Movement hint", "Move", 144, 8, 80, font);
                Icon(strip, "mouse_move", 254);
                UIText(strip, "Look hint", "Look", 295, 8, 90, font);
                Icon(strip, "keyboard_e", 425);
                UIText(strip, "Inspect hint", "Inspect", 466, 8, 105, font);
                Icon(strip, "keyboard_escape", 607, 35);
                UIText(strip, "Pause hint", "Pause", 657, 8, 88, font);
                // G is already the gameplay key for showing/hiding the first-use guides.
                var key = UIRect(strip, "Guide key", 791, 10, 25, 25).gameObject.AddComponent<Image>();
                key.color = new Color(.83f, .9f, .93f); key.raycastTarget = false;
                var keyText = UIRect(key.transform, "Guide key label", 0, 0, 25, 25).gameObject.AddComponent<TextMeshProUGUI>();
                keyText.font = font; keyText.fontSize = 18; keyText.text = "G"; keyText.color = new Color(.04f, .08f, .1f); keyText.alignment = TextAlignmentOptions.Center; keyText.raycastTarget = false;
                UIText(strip, "Guide hint", "Guide", 829, 8, 100, font);
            }

            RoundedPanel(nodes["World"].Find("Exploration control strip"));

            if (nodes.TryGetValue("MasterCover", out var cover))
            {
                var button = cover.GetComponent<Button>();
                var colors = button.colors;
                colors.disabledColor = new Color(.17f, .24f, .28f);
                button.colors = colors;
                button.interactable = false;
                var label = cover.GetComponentInChildren<TMP_Text>();
                MathText.Set(label, "[LOCKED] Complete all four modules");
                label.color = new Color(.62f, .73f, .78f);
            }
        }

        static void RoundedPanel(Transform parent)
        {
            var original = parent.GetComponent<Image>();
            if (original != null) original.color = Color.clear;
            if (parent.Find("Polished glass surface") != null) return;
            var surface = UIRect(parent, "Polished glass surface", 0, 0, 0, 0);
            surface.anchorMin = Vector2.zero; surface.anchorMax = Vector2.one;
            surface.offsetMin = surface.offsetMax = Vector2.zero;
            surface.SetAsFirstSibling();
            var graphic = surface.gameObject.AddComponent<SimulationAnswerBackdrop>();
            graphic.panel = true; graphic.cornerRadius = 10;
            graphic.color = new Color(.025f, .055f, .085f, .94f);
            graphic.lightTint = Cyan; graphic.rim = new Color(.18f, .35f, .43f, .8f);
            graphic.raycastTarget = false;
        }

        static void UpgradePrompts()
        {
            foreach (string name in new[] { "DifferentiationUI", "IntegrationUI" })
            {
                string path = "Assets/_Project/Prefabs/Resources/Production/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try { LayoutPrompts(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            for (int topic = 1; topic <= 2; topic++)
            {
                var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[topic] + ".unity");
                var canvas = UnityEngine.Object.FindFirstObjectByType<MissionCanvas>();
                LayoutPrompts(canvas.gameObject);
                foreach (var component in canvas.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
#endif
