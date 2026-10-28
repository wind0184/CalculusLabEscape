#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class LaboratoryAIImport
    {
        public const string Root = "Assets/_Project/Art/LaboratoryAI";
        public const string ScenePath = "Assets/_Project/Scenes/AssetPreviews/LaboratoryAIPreview.unity";
        public const string PrefabPath = "Assets/_Project/Prefabs/LaboratoryAI.prefab";
        public static string Evidence => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../qa/laboratory-ai-unity-2026-10-23"));
        static string Absolute(string path) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        static readonly Color Cyan = new Color(.10f, .76f, .95f);

        [MenuItem("Calculus Lab/Laboratory AI/Import and build preview")]
        public static void ImportAndBuild()
        {
            Directory.CreateDirectory(Evidence);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var materials = new Dictionary<string, Material>();
            var models = Directory.GetFiles(Absolute(Root + "/Models"), "*.fbx").Select(p => Root + "/Models/" + Path.GetFileName(p)).OrderBy(p => p).ToArray();
            if (models.Length != 10) throw new Exception("Expected base model and nine motion FBX files.");
            var report = new StringBuilder();
            foreach (string path in models)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importCameras = false;
                importer.importLights = false;
                importer.globalScale = 1;
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = !path.EndsWith("_Base.fbx", StringComparison.Ordinal);
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.resampleCurves = true;
                importer.optimizeGameObjects = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var source in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                {
                    string name = source.name;
                    if (!materials.TryGetValue(name, out var material))
                    {
                        material = CreateMaterial(name);
                        materials[name] = material;
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
                }
                importer.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                report.AppendLine(Path.GetFileName(path) + " imported; renderers=" + model.GetComponentsInChildren<Renderer>(true).Length);
            }
            var clips = new AnimationClip[9];
            for (int index = 0; index < clips.Length; index++)
            {
                string name = LaboratoryAIPreview.MotionNames[index];
                string path = Root + "/Models/LaboratoryAI_v7_" + name + ".fbx";
                var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.length > 0);
                if (imported == null) throw new Exception("Missing imported animation: " + name);
                var clip = Object.Instantiate(imported);
                clip.name = name;
                clip.legacy = true;
                clip.wrapMode = index == 0 || index == 6 ? WrapMode.Loop : WrapMode.ClampForever;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = index == 0 || index == 6;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                string target = Root + "/Animations/" + name + ".anim";
                var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
                if (old == null) AssetDatabase.CreateAsset(clip, target);
                else { EditorUtility.CopySerialized(clip, old); Object.DestroyImmediate(clip); clip = old; }
                clips[index] = clip;
                report.AppendLine(name + ": " + clip.length.ToString("0.000") + " s, curves=" + AnimationUtility.GetCurveBindings(clip).Length);
            }
            var wrapper = new GameObject("Laboratory AI");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/LaboratoryAI_v7_Base.fbx"));
            visual.name = "Animated model";
            visual.transform.SetParent(wrapper.transform, false);
            var preview = wrapper.AddComponent<LaboratoryAIPreview>();
            preview.model = visual;
            preview.clips = clips;
            var core = preview.Part("EnergyCore");
            var lamp = new GameObject("Hover illumination").AddComponent<Light>();
            lamp.transform.SetParent(core, false);
            lamp.transform.position = core.position + Vector3.down * .12f;
            lamp.type = LightType.Point;
            lamp.range = 1.2f;
            lamp.intensity = 1.8f;
            preview.hoverLight = lamp;
            preview.Sample(0, 0);
            PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
            Object.DestroyImmediate(wrapper);
            CreateScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(Evidence, "import-report.txt"), report.ToString());
            ValidateAndCapture();
            Debug.Log("LABORATORY_AI_UNITY_IMPORT_PASS");
        }

        static Material CreateMaterial(string name)
        {
            string safe = new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            string path = Root + "/Materials/" + safe + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            bool transparent = name.Contains("transparent glass") || name.Contains("projection light") || name.Contains("radial fade");
            bool glow = name.Contains("emission") || name.Contains("Soft cyan") || name.Contains("bright cyan") || name.Contains("strokes") || name.Contains("bridge scan");
            Shader shader = Shader.Find(transparent ? "CalculusLab/AI/Hologram" : glow ? "CalculusLab/AI/Emission" : "Standard");
            if (shader == null) throw new Exception("Missing AI shader: " + name);
            var material = new Material(shader) { name = name };
            if (transparent)
            {
                material.SetColor("_Color", name.Contains("radial fade") ? new Color(.004f, .52f, 1f, .75f) : name.Contains("projection") ? new Color(.01f, .3f, .7f, .035f) : new Color(.015f, .22f, .38f, .14f));
                material.SetFloat("_UseVertexAlpha", name.Contains("radial fade") ? 1 : 0);
            }
            else if (glow)
            {
                float strength = name.Contains("dim strokes") ? .55f : name.Contains("bright") ? 2.8f : name.Contains("scan") ? 2.5f : 1.7f;
                material.SetColor("_Color", new Color(.004f, .52f, 1f) * strength);
            }
            else
            {
                material.color = name.Contains("Pearl") ? new Color(.62f, .70f, .78f) : name.Contains("Copper") ? new Color(.50f, .28f, .12f) : name.Contains("Brushed") ? new Color(.20f, .30f, .37f) : name.Contains("visor") ? new Color(.004f, .013f, .025f) : new Color(.022f, .047f, .08f);
                material.SetFloat("_Metallic", name.Contains("Copper") ? .72f : name.Contains("Pearl") ? .35f : .55f);
                material.SetFloat("_Glossiness", name.Contains("visor") ? .65f : .48f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material Solid(string name, Color colour, float metallic)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = material == null;
            if (create) material = new Material(Shader.Find("Standard")) { name = name };
            material.color = colour;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", .4f);
            material.SetFloat("_GlossyReflections", 0);
            material.SetFloat("_SpecularHighlights", 0);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if (create) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Absolute("Assets/_Project/Scenes/AssetPreviews"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.26f, .31f, .39f);
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var preview = actor.GetComponent<LaboratoryAIPreview>();
            preview.Sample(2, 1.8f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Studio floor";
            floor.transform.position = new Vector3(0, -.082f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = Solid("AI_StudioFloor", new Color(.018f, .032f, .052f), .2f);
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Display plinth";
            plinth.transform.position = new Vector3(0, -.04f, 0);
            plinth.transform.localScale = new Vector3(1.35f, .04f, 1.35f);
            plinth.GetComponent<Renderer>().sharedMaterial = Solid("AI_DisplayPlinth", new Color(.04f, .08f, .12f), .55f);
            LightOf("Soft key", LightType.Directional, new Vector3(42, -35, 0), new Color(.76f, .88f, 1), 1.5f);
            LightOf("Cyan rim", LightType.Directional, new Vector3(15, 140, 0), new Color(.16f, .6f, 1), 1.15f);
            var fill = LightOf("Warm fill", LightType.Point, Vector3.zero, new Color(1, .82f, .66f), 1.5f);
            fill.transform.position = new Vector3(1.4f, 1.5f, -1.5f);
            fill.range = 5;
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.015f, .03f, .05f);
            cam.orthographic = true;
            cam.orthographicSize = .74f;
            cam.nearClipPlane = .02f;
            cam.farClipPlane = 50;
            cam.allowHDR = true;
            var head = preview.Part("Head");
            var visor = preview.Part("Recessed octagonal faceted visor").GetComponent<Renderer>();
            Vector3 front = visor.bounds.center - head.position;
            front.y = 0;
            front.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, front);
            Vector3 centre = new Vector3(0, .48f, 0);
            cam.transform.position = centre + front * 2.8f + right * .8f + Vector3.up * .65f;
            cam.transform.LookAt(centre);
            Vector3 offset = cam.transform.right * .30f;
            cam.transform.position += offset;
            cam.transform.LookAt(centre + offset);
            var bloom = cam.gameObject.AddComponent<LaboratoryAIBloom>();
            bloom.shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/LaboratoryAIBloom.shader");
            bloom.threshold = 1;
            BuildUI(preview, cam);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = actor;
        }

        static Light LightOf(string name, LightType type, Vector3 rotation, Color colour, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type; light.color = colour; light.intensity = intensity;
            light.transform.rotation = Quaternion.Euler(rotation);
            return light;
        }
        static RectTransform Rect(GameObject go, Transform parent, float x, float y, float w, float h)
        {
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Production/MainMenuUI.prefab").GetComponentsInChildren<TMP_Text>(true).First().font;
        static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Rect(go, parent, x, y, w, h);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = Font; text.text = value; text.fontSize = size; text.color = colour;
            text.raycastTarget = false;
            return text;
        }
        static Button ButtonAt(Transform parent, string name, float x, float y, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Rect(go, parent, x, y, width, 38);
            go.GetComponent<Image>().color = new Color(.055f, .15f, .21f, .97f);
            var text = Text(go.transform, "Label", name, 12, 4, width - 24, 30, 17, new Color(.79f, .91f, .96f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return go.GetComponent<Button>();
        }
        static void BuildUI(LaboratoryAIPreview preview, Camera cam)
        {
            var canvasObject = new GameObject("Laboratory AI preview controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .4f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("Library panel", typeof(RectTransform), typeof(Image));
            Rect(panel, canvas.transform, 954, 44, 294, 632);
            panel.GetComponent<Image>().color = new Color(.014f, .04f, .065f, .95f);
            Text(canvas.transform, "Title", "LABORATORY AI", 36, 26, 780, 52, 34, new Color(.84f, .95f, 1));
            Text(canvas.transform, "Subtitle", "UNITY PREVIEW  /  V7 MODEL + 9 MOTIONS", 38, 78, 800, 30, 17, Cyan);
            preview.stateLabel = Text(canvas.transform, "State", "", 38, 111, 800, 28, 16, new Color(.52f, .7f, .8f));
            preview.motionLabel = Text(canvas.transform, "Current motion", "", 38, 584, 800, 48, 31, Color.white);
            preview.timeLabel = Text(canvas.transform, "Motion time", "", 40, 631, 600, 32, 20, Cyan);
            Text(canvas.transform, "Help", "Edit Mode: Calculus Lab > Laboratory AI > Open preview", 39, 676, 900, 28, 16, new Color(.43f, .61f, .71f));
            Text(canvas.transform, "Library heading", "ANIMATION LIBRARY", 976, 68, 250, 32, 18, Cyan);
            for (int i = 0; i < 9; i++)
            {
                var button = ButtonAt(canvas.transform, (i + 1).ToString("00") + "  " + LaboratoryAIPreview.MotionNames[i], 976, 115 + i * 49, 250);
                UnityEventTools.AddIntPersistentListener(button.onClick, preview.PlayIndex, i);
            }
            var pause = ButtonAt(canvas.transform, "Play / Pause", 976, 572, 122);
            UnityEventTools.AddPersistentListener(pause.onClick, preview.TogglePlayback);
            var cycle = ButtonAt(canvas.transform, "Cycle all", 1106, 572, 120);
            UnityEventTools.AddPersistentListener(cycle.onClick, preview.ToggleCycle);
            Text(canvas.transform, "Library footer", "Preview repeats the selected motion.\nPlay Mode buttons switch animations.", 977, 622, 250, 48, 14, new Color(.43f, .61f, .71f));
            var events = new GameObject("Preview input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            preview.Sample(2, 1.8f);
        }

        public static void Capture(Camera camera, string file, int width = 1280, int height = 720)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf);
            var active = RenderTexture.active;
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(file, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            finally { camera.targetTexture = null; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); ShaderUtil.allowAsyncCompilation = asyncCompilation; }
        }

        public static void Diagnose()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var text = new StringBuilder();
            text.AppendLine("Model: " + preview.model.name + " persistent=" + EditorUtility.IsPersistent(preview.model));
            foreach (var part in preview.model.GetComponentsInChildren<Transform>(true).Take(18)) text.AppendLine("Hierarchy: " + AnimationUtility.CalculateTransformPath(part, preview.model.transform));
            var deck = preview.Part("BridgeLive Deck 12");
            text.AppendLine("Deck path: " + AnimationUtility.CalculateTransformPath(deck, preview.model.transform));
            var bindings = AnimationUtility.GetCurveBindings(preview.clips[2]);
            foreach (var binding in bindings.Where(b => b.path.Contains("BridgeLive Deck 12") && b.propertyName.Contains("Position")))
            {
                var curve = AnimationUtility.GetEditorCurve(preview.clips[2], binding);
                text.AppendLine("Curve " + binding.path + " / " + binding.propertyName + " = " + curve.Evaluate(1) + ", " + curve.Evaluate(3) + " target=" + (preview.model.transform.Find(binding.path) != null));
            }
            preview.Sample(2, 1);
            text.AppendLine("Sample 1: deck=" + deck.localPosition + " root=" + preview.model.transform.localScale + " panel=" + preview.Part("HologramPanel").localScale);
            preview.Sample(2, 3);
            text.AppendLine("Sample 3: deck=" + deck.localPosition + " root=" + preview.model.transform.localScale + " panel=" + preview.Part("HologramPanel").localScale);
            text.AppendLine("Camera: " + camera.transform.position + " forward=" + camera.transform.forward + " bg=" + camera.backgroundColor);
            var headRenderer = preview.Part("Recessed octagonal faceted visor").GetComponent<Renderer>();
            text.AppendLine("Visor bounds=" + headRenderer.bounds + " shader=" + headRenderer.sharedMaterial.shader.name);
            File.WriteAllText(Path.Combine(Evidence, "diagnostic.txt"), text.ToString());
            var bloom = camera.GetComponent<LaboratoryAIBloom>();
            bloom.enabled = false;
            Capture(camera, Path.Combine(Evidence, "diagnostic-no-bloom.png"));
            bloom.enabled = true;
            Capture(camera, Path.Combine(Evidence, "diagnostic-bloom.png"));
            Debug.Log("LABORATORY_AI_DIAGNOSTIC_READY");
        }

        [MenuItem("Calculus Lab/Laboratory AI/Validate and capture")]
        public static void ValidateAndCapture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var report = new StringBuilder();
            for (int index = 0; index < 9; index++)
            {
                var clip = preview.clips[index];
                if (clip == null || Mathf.Abs(clip.length - 4) > .05f) throw new Exception("Wrong motion duration " + index);
                preview.Sample(index, 2);
                foreach (var renderer in preview.model.GetComponentsInChildren<Renderer>(true))
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat == null || mat.shader == null || !mat.shader.isSupported) throw new Exception("Invalid Unity material " + renderer.name);
                int eyes = preview.model.GetComponentsInChildren<Transform>(true).Count(t => t.name.Contains(" eye ") && t.localScale.sqrMagnitude > .001f);
                if (eyes != 2) throw new Exception("Expected one visible eye pair: " + index + " / " + eyes);
                if (index == 4)
                {
                    if (!(preview.CurrentColour.g > .9f && preview.CurrentColour.b < .02f)) throw new Exception("Green state failed");
                    int greenSurfaces = 0;
                    foreach (var renderer in preview.model.GetComponentsInChildren<Renderer>(true))
                    {
                        for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                        {
                            if (renderer.sharedMaterials[slot].name.Replace('_', ' ') != "AI Cyan emission") continue;
                            var block = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(block, slot);
                            Color actual = block.GetColor("_Color");
                            if (!(actual.g > .9f && actual.r < .02f && actual.b < .02f)) throw new Exception("Green renderer tint failed: " + renderer.name);
                            greenSurfaces++;
                        }
                    }
                    if (greenSurfaces < 10) throw new Exception("Missing green feedback surfaces");
                    report.AppendLine("Green material property blocks: PASS, surfaces=" + greenSurfaces);
                }
                Capture(camera, Path.Combine(Evidence, LaboratoryAIPreview.MotionNames[index] + ".png"));
                report.AppendLine(LaboratoryAIPreview.MotionNames[index] + ": PASS, 4s, 2 eyes, " + AnimationUtility.GetCurveBindings(clip).Length + " animated bindings");
            }
            preview.Sample(2, 1);
            var deck = preview.Part("BridgeLive Deck 12");
            Vector3 first = deck.position;
            Capture(camera, Path.Combine(Evidence, "Bridge-A.png"));
            preview.Sample(2, 3);
            if ((deck.position - first).magnitude < .001f) throw new Exception("Bridge diagram did not animate in Unity");
            Capture(camera, Path.Combine(Evidence, "Bridge-B.png"));
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) throw new Exception("Preview text overflow: " + text.name);
            }
            preview.Sample(2, 1.8f);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            File.WriteAllText(Path.Combine(Evidence, "validation.txt"), report + "Bridge deck changes in Unity: PASS\nMaterials and text fit: PASS\n");
            Debug.Log("LABORATORY_AI_UNITY_CAPTURE_PASS");
        }

        public static void CaptureMotionPreviews()
        {
            ValidateAndCapture();
            var preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            var camera = Object.FindFirstObjectByType<Camera>();
            foreach (int index in new[] { 2, 4 })
            {
                string folder = Path.Combine(Evidence, LaboratoryAIPreview.MotionNames[index] + "Frames");
                for (int frame = 0; frame < 48; frame++)
                {
                    preview.Sample(index, frame / 12f);
                    Capture(camera, Path.Combine(folder, frame.ToString("000") + ".png"), 960, 540);
                }
            }
            preview.Sample(2, 1.8f);
            Debug.Log("LABORATORY_AI_MOTION_CAPTURE_PASS");
        }
    }
}
#endif
