#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // Saved Edit Mode showroom scenes, labels, and keyboard/mouse sample cards.
    public static partial class AssetTrials
    {
        static void Cube(string name, Vector3 position, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
        static RectTransform Rect(GameObject go, Transform parent, float x, float y, float w, float h)
        {
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }
        static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Production/MainMenuUI.prefab").GetComponentsInChildren<TMP_Text>(true).First().font;
        static void Label(Transform parent, string name, string text, float x, float y, float w, float h, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Rect(go, parent, x, y, w, h);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = Font;
            t.fontSize = size;
            t.text = text;
            t.color = color;
            t.raycastTarget = false;
        }
        static void Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Rect(go, parent, x, y, w, h);
            go.GetComponent<Image>().color = color;
        }
        static void CreateScene(int i)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .53f, .6f);
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.015f, .027f, .045f);
            cam.orthographic = true;
            cam.orthographicSize = 10.8f;
            cam.transform.position = new Vector3(13, 17, -25);
            cam.transform.LookAt(new Vector3(0, .6f, 3.5f));
            var light = new GameObject("Studio light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48, -35, 0);
            var floor = Solid("TrialFloor", new Color(.045f, .075f, .1f));
            var pedestal = Solid("TrialPedestal", new Color(.13f, .19f, .23f));
            var trim = Solid("TrialCyan", Cyan, true);
            if (i < 3)
            {
                Cube("Simulation floor", new Vector3(0, -.35f, 3.8f), new Vector3(28, .4f, 23), floor);
                for (int k = 0; k < 9; k++)
                {
                    var pos = new Vector3((k % 3 - 1) * 7, 0, (2 - k / 3) * 6 - 2);
                    Cube("Display base " + k, pos + Vector3.down * .1f, new Vector3(5.7f, .25f, 4.9f), pedestal);
                    Cube("Cyan edge " + k, pos + new Vector3(0, .035f, -2.43f), new Vector3(5.7f, .035f, .045f), trim);
                    var sample = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/" + Names[i] + "_" + Samples[i][k] + ".prefab"));
                    sample.transform.position = pos + Vector3.up * .05f;
                    if (i == 2 && (k == 2 || k == 3))
                        sample.transform.rotation = Quaternion.Euler(0, 180, 0);
                    var text = new GameObject("Label " + k).AddComponent<TextMeshPro>();
                    text.font = Font;
                    text.text = (k + 1).ToString("00") + " / " + Samples[i][k].Replace('_', ' ').Replace('-', ' ');
                    text.fontSize = 2.4f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = Cyan;
                    text.rectTransform.sizeDelta = new Vector2(6, .6f);
                    text.transform.position = pos + new Vector3(0, .25f, -2.65f);
                    text.transform.rotation = cam.transform.rotation;
                }
            }
            var canvas = new GameObject("Asset trial information", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1;
            var scale = canvas.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1600, 900);
            scale.matchWidthOrHeight = .5f;
            Panel(canvas.transform, "Header", 0, 0, 1600, 125, new Color(.025f, .045f, .07f, .98f));
            Panel(canvas.transform, "Accent", 36, 31, 4, 62, Cyan);
            string[] titles = { "FACTORY KIT / MECHANICAL SYSTEMS", "MODULAR SPACE / ROOMS & CORRIDORS", "SCI-FI MEGAKIT / CONTAINMENT LAB", "INPUT PROMPTS / PLAYER CONTROLS" };
            Label(canvas.transform, "Title", titles[i], 58, 26, 1500, 52, 34, Color.white);
            Label(canvas.transform, "Subtitle", (i == 2 ? "QUATERNIUS / STANDARD" : "KENNEY") + "   /   CC0   /   UNITY BUILT-IN   /   EDIT MODE PREVIEW", 60, 83, 1500, 30, 18, Cyan);
            string[] foot = { "Candidate use: bridge repair machinery, power routing, valve and scanner stations.", "Candidate use: connected laboratory rooms, access gates, corridor depth and transitions.", "Candidate use: Integration containment room, computers, door frames and detailed wall panels.", "Candidate use: exploration prompts, interaction hints, pause and answer controls." };
            Panel(canvas.transform, "Footer", 0, 819, 1600, 81, new Color(.025f, .045f, .07f, .98f));
            Label(canvas.transform, "Footer text", foot[i] + "\nPREVIEW LIBRARY  /  Samples normalized for comparison; inspect prefab scale before gameplay placement.", 40, 834, 1530, 58, 18, Color.white);
            if (i == 3)
                CreateInputCards(canvas.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath(i));
        }
        static void CreateInputCards(Transform parent)
        {
            string[][] icons = { new[] { "keyboard_w", "keyboard_a", "keyboard_s", "keyboard_d" }, new[] { "keyboard_e" }, new[] { "mouse_left", "mouse_move" }, new[] { "keyboard_escape" }, new[] { "keyboard_enter" }, new[] { "mouse_right", "mouse_scroll" } };
            string[] titles = { "MOVE", "INTERACT", "SELECT & DRAG", "PAUSE", "CONFIRM", "MOUSE OPTIONS" };
            string[] details = { "Explore the simulation world", "Approach a station and press E", "Pick answers or move a component", "Open the pause menu", "Submit the selected answer", "Available icons for future controls" };
            for (int k = 0; k < 6; k++)
            {
                float x = 40 + (k % 3) * 520, y = 155 + (k / 3) * 320;
                Panel(parent, "Control card " + k, x, y, 496, 292, new Color(.055f, .095f, .13f));
                Panel(parent, "Card accent " + k, x, y, 496, 3, Cyan);
                Label(parent, "Control heading " + k, titles[k], x + 24, y + 22, 450, 42, 26, Cyan);
                int n = icons[k].Length;
                for (int j = 0; j < n; j++)
                {
                    var go = new GameObject(icons[k][j], typeof(RectTransform), typeof(Image));
                    Rect(go, parent, x + 248 - n * 49 + j * 98, y + 91, 88, 88);
                    var image = go.GetComponent<Image>();
                    image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Roots[3] + "/Sprites/" + icons[k][j] + ".png");
                    image.preserveAspect = true;
                    if (image.sprite == null)
                        throw new Exception("Missing icon " + icons[k][j]);
                }
                Label(parent, "Control description " + k, details[k], x + 24, y + 218, 450, 58, 20, Color.white);
            }
        }
    }
}
#endif
