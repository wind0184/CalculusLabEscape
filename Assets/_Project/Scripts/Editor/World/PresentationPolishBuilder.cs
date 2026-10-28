#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class PresentationPolishBuilder
    {
        const string Art = "Assets/_Project/Art/PresentationPolish";
        const string Factory = "Assets/ThirdParty/Kenney/FactoryKit/Models/";
        const string DetailGroup = "Factory detail pass";
        static string Evidence => Path.GetFullPath("../../qa/presentation-polish-2026-10-19");
        static Material steel, edge, copper, face, textMaterial;
        static readonly Color Cyan = new Color(.18f, .82f, .89f);
        static readonly Color Pale = new Color(.8f, .93f, .95f);

        [MenuItem("Calculus Lab/Presentation/Apply machinery and controls polish")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PrepareMaterials();
            ApplyBridge();
            ApplyCaseLabels();
            UpgradePrompts();
            AssetDatabase.SaveAssets();
            VerifyAndCapture();
            Debug.Log("PRESENTATION_POLISH_APPLIED");
        }

        public static void ApplyAndBuild()
        {
            Apply();
            ProductionUIBuilder.BuildDelivery();
        }

        public static void RebuildOwnedDetails()
        {
            var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[1] + ".unity");
            var world = Object.FindFirstObjectByType<Chapter4World>();
            var stations = world.transform.Find("Repair station machinery");
            foreach (Transform station in stations)
            {
                var detail = station.Find(DetailGroup);
                if (detail == null) continue;
                var indicator = station.GetComponent<RepairStationIndicator>();
                indicator.lights = indicator.lights.Where(r => r != null && !r.transform.IsChildOf(detail)).ToArray();
                Object.DestroyImmediate(detail.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Apply();
        }

        public static void RefineAndBuild()
        {
            RebuildOwnedDetails();
            SceneAuthoring.ValidateAndCaptureTo(Path.Combine(Evidence, "all-views"));
            ProductionUIBuilder.BuildDelivery();
            Debug.Log("PRESENTATION_POLISH_DELIVERY_PASS");
        }

        static Material Material(string name, Color color, float metallic = .15f, bool glow = false)
        {
            string path = Art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", .32f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .6f); }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void PrepareMaterials()
        {
            steel = Material("Anodized field steel", new Color(.075f, .12f, .15f), .4f);
            edge = Material("Machined edge", new Color(.31f, .42f, .45f), .5f);
            copper = Material("Safety copper", new Color(.62f, .35f, .12f), .3f);
            face = Material("Dark instrument glass", new Color(.012f, .026f, .035f), .05f);
            string path = Art + "/Depth tested labels.mat";
            textMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (textMaterial == null)
            {
                textMaterial = new Material(Shader.Find("Calculus Lab/Depth Tested World Label")) { name = "Depth tested labels" };
                AssetDatabase.CreateAsset(textMaterial, path);
            }
        }

        static Transform Group(Transform parent, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        static Renderer Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new Exception("Missing imported mesh: " + root.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static Material FactoryMaterial(Material source)
        {
            string name = "Factory " + source.name;
            string path = Art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null;
            if (created) material = new Material(source) { name = name };
            if (source.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) < 0)
            {
                material.color = new Color(.72f, .82f, .84f);
                material.SetFloat("_Metallic", .15f);
                material.SetFloat("_Glossiness", .38f);
            }
            if (created) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static Transform Imported(Transform parent, string model, Vector3 bottom, float size, float yaw = 0)
        {
            var wrapper = Group(parent, "Imported " + model);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Factory + model + ".fbx");
            if (source == null) throw new Exception("Missing Factory model " + model);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.transform.SetParent(wrapper, false);
            visual.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            if (model == "robot-arm-a")
            {
                var parts = visual.GetComponentsInChildren<Transform>();
                var shoulder = parts.First(t => t.name == "element-b");
                var elbow = parts.First(t => t.name == "element-d");
                shoulder.localRotation *= Quaternion.Euler(-50, 0, 0);
                elbow.localRotation *= Quaternion.Euler(85, 0, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(shoulder);
                PrefabUtility.RecordPrefabInstancePropertyModifications(elbow);
            }
            var b = BoundsOf(visual);
            visual.transform.localScale *= size / Mathf.Max(b.size.x, b.size.y, b.size.z);
            b = BoundsOf(visual);
            visual.transform.position += wrapper.position - new Vector3(b.center.x, b.min.y, b.center.z);
            wrapper.localPosition = bottom;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(FactoryMaterial).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (var animator in visual.GetComponentsInChildren<Animator>(true))
            { animator.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(animator); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
            if (wrapper.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Decorative FBX added collision");
            return wrapper;
        }

        static void DepthLabel(TextMesh label)
        {
            var atlas = label.GetComponent<WorldLabelAtlas>();
            if (atlas == null) atlas = label.gameObject.AddComponent<WorldLabelAtlas>();
            atlas.depthMaterial = textMaterial;
            atlas.Apply();
            EditorUtility.SetDirty(atlas);
        }

        static void SmallLabel(Transform parent, string value, Vector3 position, float size, TextMesh source)
        {
            var label = Group(parent, value).gameObject.AddComponent<TextMesh>();
            label.transform.localPosition = position;
            label.font = source.font;
            label.fontSize = 64;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Pale;
            label.text = value;
            DepthLabel(label);
        }

        static void ApplyBridge()
        {
            var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[1] + ".unity");
            var world = Object.FindFirstObjectByType<Chapter4World>();
            string data = JsonUtility.ToJson(world);
            var colliders = world.GetComponentsInChildren<Collider>(true).Select(c => c.GetInstanceID()).OrderBy(i => i).ToArray();
            var motion = world.GetComponent<RepairStationMotion>();
            string animation = JsonUtility.ToJson(motion);
            var stations = world.transform.Find("Repair station machinery");
            if (stations == null || stations.childCount != 3) throw new Exception("Authored repair machinery required");
            var signs = new SerializedObject(world).FindProperty("repairSigns");
            for (int i = 0; i < 3; i++)
            {
                var station = stations.GetChild(i);
                var sign = (TextMesh)signs.GetArrayElementAtIndex(i).objectReferenceValue;
                sign.characterSize = .027f;
                var plate = sign.transform.Find("Repair machinery nameplate") ?? sign.transform.Find("Device sign backing");
                if (plate != null)
                {
                    plate.localScale = new Vector3(1.95f, .42f, .025f);
                    plate.GetComponent<Renderer>().sharedMaterial = steel;
                }
                DepthLabel(sign);
                EditorUtility.SetDirty(sign);
                if (station.Find(DetailGroup) != null) continue;
                var root = Group(station, DetailGroup);
                var indicator = station.GetComponent<RepairStationIndicator>();
                var lamps = new List<Renderer>(indicator.lights);
                Box(root, "Service platform", new Vector3(0, .035f, 0), new Vector3(2.16f, .065f, 1.7f), steel);
                lamps.Add(Box(root, "Stage status rail", new Vector3(0, .074f, -.835f), new Vector3(1.3f, .025f, .026f), indicator.source.sharedMaterial));
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(root, "Platform rim", new Vector3(side * 1.035f, .08f, 0), new Vector3(.035f, .025f, 1.55f), edge);
                    for (int tick = 0; tick < 3; tick++)
                    {
                        var stripe = Box(root, "Service caution stripe", new Vector3(side * .91f, .078f, -.6f + tick * .12f), new Vector3(.12f, .012f, .035f), copper);
                        stripe.transform.localRotation = Quaternion.Euler(0, -30, 0);
                    }
                }
                if (i == 0)
                {
                    Imported(root, "pipe-large-valve", new Vector3(-.73f, .22f, -.3f), .7f, 0);
                    Imported(root, "screen-panel-small", new Vector3(.59f, .54f, -.58f), .53f, 0);
                    Box(root, "Power readout", new Vector3(.59f, .91f, -.724f), new Vector3(.36f, .18f, .02f), face);
                    SmallLabel(root, "PWR / 01\nFIELD UNIT", new Vector3(.59f, .91f, -.741f), .008f, sign);
                }
                else if (i == 1)
                {
                    for (int channel = 0; channel < 3; channel++)
                    {
                        Imported(root, "machine-connection-pipe", new Vector3((channel - 1) * .52f, .4f, .45f), .38f, 180);
                        Box(root, "Channel identification plate", new Vector3((channel - 1) * .52f, .26f, -.66f), new Vector3(.34f, .15f, .035f), steel);
                        SmallLabel(root, "CH / 0" + (channel + 1), new Vector3((channel - 1) * .52f, .26f, -.685f), .007f, sign);
                    }
                    Imported(root, "screen-panel-wide", new Vector3(0, .32f, -.59f), .8f, 0);
                    Box(root, "Converter instrument face", new Vector3(0, .75f, -.798f), new Vector3(.65f, .22f, .025f), face);
                    SmallLabel(root, "FLOW / 3 CHANNELS", new Vector3(0, .75f, -.82f), .008f, sign);
                }
                else
                {
                    Imported(root, "robot-arm-a", new Vector3(-.64f, 1.02f, .37f), .76f, -75);
                    Imported(root, "lever-single", new Vector3(-.72f, 1.025f, -.42f), .23f, 0);
                    Box(root, "Tool shelf face", new Vector3(0, .66f, -.61f), new Vector3(1.4f, .16f, .03f), steel);
                    SmallLabel(root, "BRIDGE ASSEMBLY / 03", new Vector3(0, .66f, -.635f), .01f, sign);
                }
                indicator.lights = lamps.ToArray();
                indicator.Apply();
                EditorUtility.SetDirty(indicator);
            }
            if (JsonUtility.ToJson(world) != data || JsonUtility.ToJson(motion) != animation)
                throw new Exception("Detail pass changed mission or animation bindings");
            if (!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c => c.GetInstanceID()).OrderBy(i => i)))
                throw new Exception("Detail pass changed walking collision");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ApplyCaseLabels()
        {
            var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[2] + ".unity");
            var world = Object.FindFirstObjectByType<Chapter4World>();
            var status = world.GetComponent<CaseShutdownSequence>().deviceStatus;
            var title = world.GetComponentsInChildren<TextMesh>(true).First(t => t.text == "03 / CONTAINMENT CASE");
            title.transform.position = new Vector3(-.78f, 1.04f, .013f);
            title.characterSize = .014f;
            status.transform.position = new Vector3(.93f, 1.04f, .013f);
            status.characterSize = .015f;
            DepthLabel(title); DepthLabel(status);
            if (world.transform.Find("Case front information plates") == null)
            {
                var root = Group(world.transform, "Case front information plates");
                Box(root, "Device identity plate", new Vector3(-.78f, 1.04f, .045f), new Vector3(1.7f, .45f, .025f), steel);
                Box(root, "Device status plate", new Vector3(.93f, 1.04f, .045f), new Vector3(1.48f, .45f, .025f), face);
                Box(root, "Status accent", new Vector3(.23f, 1.04f, .026f), new Vector3(.025f, .33f, .012f), edge);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
