#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class IntegrationPolishAuthoring
    {
        const string ScenePath = "Assets/_Project/Scenes/Level03_Integration.unity";
        const string Art = "Assets/_Project/Art/IntegrationControlRoom/";
        const string Prefabs = "Assets/_Project/Prefabs/Resources/Production/";
        static string Evidence => Path.GetFullPath("../../qa/integration-polish-2026-10-25");
        static Material floor, floorAlt, wall, panel, steel, trim, ivory, black, cyan, amber, green, glass, labels;
        static readonly List<Transform> wheels = new List<Transform>();
        static readonly List<Renderer> bands = new List<Renderer>();
        static readonly List<Renderer>[] statuses = Enumerable.Range(0, 4).Select(_ => new List<Renderer>()).ToArray();
        static readonly List<Renderer>[] powers = Enumerable.Range(0, 4).Select(_ => new List<Renderer>()).ToArray();
        static readonly TextMesh[] circuitLabels = new TextMesh[4];
        static readonly Vector3[] Axes = { Vector3.right, Vector3.up, Vector3.forward };

        static Color Colour(string html) { ColorUtility.TryParseHtmlString("#" + html, out var c); return c; }
        static Material Mat(string name, string hex, float metal = 0, float glow = 0)
        {
            string path = Art + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Standard")) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.color = Colour(hex); m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", .34f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", m.color * glow); EditorUtility.SetDirty(m); return m;
        }
        static void Materials()
        {
            floor = Mat("Slate floor", "243743", .15f); floorAlt = Mat("Alternate floor", "283d48", .15f);
            wall = Mat("Blue ceramic walls", "354b59", .10f); panel = Mat("Inset armour", "1d303d", .4f);
            steel = Mat("Gunmetal", "172a34", .55f); trim = Mat("Brushed aluminium", "7b979e", .55f);
            ivory = Mat("Ceramic instrument faces", "acbfbd", .08f); black = Mat("Instrument glass", "07171e", .15f);
            cyan = Mat("Cyan power", "53c2d5", .1f, 1.2f); amber = Mat("Amber calibration", "d9a259", .15f, .65f);
            green = Mat("Green calibrated", "56dca9", .10f, 1);
            glass = Mat("Observation glass", "366577", .15f);
            glass.SetFloat("_Mode", 3); glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite", 0); glass.EnableKeyword("_ALPHAPREMULTIPLY_ON"); glass.renderQueue = 3000;
            var c = glass.color; c.a = .15f; glass.color = c;
            string labelPath = Art + "Depth tested lettering.mat"; labels = AssetDatabase.LoadAssetAtPath<Material>(labelPath);
            if (labels == null) { labels = new Material(Shader.Find("Calculus Lab/Depth Tested World Label")); AssetDatabase.CreateAsset(labels, labelPath); }
        }
        static Transform Group(Transform parent, string name)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
        static Renderer Shape(Transform parent, string name, Vector3 p, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>()); var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; return r;
        }
        static Mesh Chamfer(Vector3 size, float bevel)
        {
            string path = Art + "Chamfer-" + size.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "-" + size.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "-" + size.z.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            var v = new List<Vector3>(); var t = new List<int>(); var h = size * .5f; float b = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .6f);
            void Face(Vector3 normal, params Vector3[] points)
            {
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0) Array.Reverse(points);
                int n = v.Count; v.AddRange(points); for (int i = 1; i < points.Length - 1; i++) t.AddRange(new[] { n, n + i, n + i + 1 });
            }
            for (int a = 0; a < 3; a++) for (int s = -1; s <= 1; s += 2)
            {
                int j = (a + 1) % 3, k = (a + 2) % 3; Vector3 n = Axes[a] * s, center = n * h[a], u = Axes[j] * (h[j] - b), w = Axes[k] * (h[k] - b);
                Face(n, center - u - w, center + u - w, center + u + w, center - u + w);
            }
            for (int a = 0; a < 3; a++) for (int j = a + 1; j < 3; j++) for (int sa = -1; sa <= 1; sa += 2) for (int sj = -1; sj <= 1; sj += 2)
            {
                int k = 3 - a - j; Vector3 n = Axes[a] * sa + Axes[j] * sj, end = Axes[k] * (h[k] - b);
                Vector3 x = Axes[a] * sa * h[a] + Axes[j] * sj * (h[j] - b), y = Axes[a] * sa * (h[a] - b) + Axes[j] * sj * h[j];
                Face(n, x - end, x + end, y + end, y - end);
            }
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                var p = Vector3.Scale(new Vector3(x, y, z), h - Vector3.one * b);
                Face(new Vector3(x, y, z), p + Vector3.right * x * b, p + Vector3.up * y * b, p + Vector3.forward * z * b);
            }
            saved = new Mesh { name = "Chamfered metal panel" }; saved.SetVertices(v); saved.SetTriangles(t, 0); saved.RecalculateNormals(); saved.RecalculateBounds(); AssetDatabase.CreateAsset(saved, path); return saved;
        }
        static Renderer Box(Transform parent, string name, Vector3 p, Vector3 size, Material m, float bevel = .05f)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = p;
            go.GetComponent<MeshFilter>().sharedMesh = Chamfer(size, bevel); var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; return r;
        }
        static Renderer Pipe(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material m)
        {
            var r = Shape(parent, name, (a + b) / 2, new Vector3(radius * 2, Vector3.Distance(a, b) / 2, radius * 2), m, PrimitiveType.Cylinder);
            r.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); return r;
        }
        static Transform Ring(Transform parent, string name, Vector3 center, float radius, float width, Material m, int count = 48)
        {
            var root = Group(parent, name); root.localPosition = center;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                var r = Shape(root, "Ring segment", new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius), new Vector3(2 * radius * Mathf.Sin(Mathf.PI / count) * 1.02f, width, width), m);
                r.transform.localRotation = Quaternion.Euler(0, i * 360f / count, 0);
            }
            return root;
        }
        static TextMesh Label(Transform parent, string name, string text, Vector3 position, float size, Quaternion rotation, Color? colour = null)
        {
            var t = Group(parent, name).gameObject.AddComponent<TextMesh>(); t.transform.localPosition = position; t.transform.localRotation = rotation;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 80; t.characterSize = size; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center;
            t.color = colour ?? Colour("c6e2e5"); t.text = text; t.gameObject.AddComponent<WorldLabelAtlas>().depthMaterial = labels; t.GetComponent<WorldLabelAtlas>().Apply(); return t;
        }
        static Light Point(Transform parent, string name, Vector3 p, Color colour, float intensity, float range)
        {
            var l = Group(parent, name).gameObject.AddComponent<Light>(); l.transform.localPosition = p; l.type = LightType.Point; l.color = colour; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None; return l;
        }
        static void DisableVisuals(Transform group)
        { if (group != null) foreach (var r in group.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; EditorUtility.SetDirty(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r); } }
        static void FitLettering(Transform root)
        {
            var report = new List<string>();
            foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
            {
                float width = 2.05f, height = .7f;
                if (text.name == "Rack index") { width = .35f; height = .36f; }
                else if (text.name == "Lab title") { width = 10.1f; height = .55f; }
                else if (text.name == "Live room status") { width = 5.2f; height = .48f; }
                else if (text.name == "Entry title") { width = 4.5f; height = .33f; }
                else if (text.name == "Entry instructions" || text.name == "Return panel") { width = 2.35f; height = 1.1f; }
                else if (text.name == "Floor designation") { width = 3.2f; height = .3f; }
                else if (text.name.StartsWith("Module number")) { width = .12f; height = .08f; }
                text.transform.localScale = Vector3.one;
                text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
                var b = text.GetComponent<Renderer>().localBounds;
                float factor = Mathf.Min(width / Mathf.Max(.001f, b.size.x), height / Mathf.Max(.001f, b.size.y));
                text.transform.localScale = Vector3.one * Mathf.Min(1, factor);
                report.Add(text.name + " | mesh " + b.size + " | scale " + text.transform.localScale.x + " | maximum " + width + " x " + height);
                EditorUtility.SetDirty(text); EditorUtility.SetDirty(text.transform);
            }
            File.WriteAllLines(Path.Combine(Evidence, "lettering-fit.txt"), report);
        }

        static void Architecture(Transform root)
        {
            var deck = Group(root, "01 - Floor and approach");
            for (int x = -5; x <= 5; x++) for (int z = -3; z <= 10; z++)
                Shape(deck, "Inset floor tile", new Vector3(x * 2, .006f, z * 2), new Vector3(1.985f, .012f, 1.985f), (x + z) % 2 == 0 ? floor : floorAlt);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(deck, "Approach light", new Vector3(side * 2.9f, .025f, -1), new Vector3(.045f, .014f, 10), cyan);
                for (int z = -5; z <= -1; z += 2)
                    Box(deck, "Pathway marker", new Vector3(side * 2.64f, .025f, z), new Vector3(.3f, .02f, .08f), ivory, .01f);
            }
            for (int i = 0; i < 12; i++)
            {
                var stripe = Shape(deck, "Console amber perimeter", new Vector3(-2.5f + i * .45f, .028f, -.64f), new Vector3(.23f, .015f, .24f), amber);
                stripe.transform.localRotation = Quaternion.Euler(0, 25, 0);
            }
            Label(deck, "Floor designation", "03   /   INTEGRATION", new Vector3(0, .028f, -3.2f), .11f, Quaternion.Euler(90, 0, 0));
            var structure = Group(root, "02 - Structural ribs and ceiling");
            Shape(structure, "Ceiling", new Vector3(0, 7.95f, 7), new Vector3(22, .16f, 28), panel);
            for (int z = -4; z <= 20; z += 6)
            {
                Shape(structure, "Ceiling crossbeam", new Vector3(0, 6.9f, z), new Vector3(21.6f, .34f, .45f), steel);
                foreach (int side in new[] { -1, 1 })
                {
                    Box(structure, "Wall buttress", new Vector3(side * 10.45f, 3.35f, z), new Vector3(.45f, 6.7f, .6f), trim, .06f);
                    var support = Box(structure, "Angled shoulder", new Vector3(side * 9.35f, 6.15f, z), new Vector3(2.8f, .4f, .6f), steel);
                    support.transform.localRotation = Quaternion.Euler(0, 0, side * 34);
                    Box(structure, "Ceiling light cassette", new Vector3(side * 4.2f, 6.62f, z), new Vector3(3, .12f, .65f), trim);
                    Shape(structure, "Ceiling diffuser", new Vector3(side * 4.2f, 6.545f, z), new Vector3(2.7f, .025f, .38f), cyan);
                }
            }
            foreach (int side in new[] { -1, 1 })
            {
                Pipe(structure, "Overhead coolant main", new Vector3(side * 7, 6.7f, -6), new Vector3(side * 7, 6.7f, 20), .15f, trim);
                for (int z = -3; z <= 19; z += 2) Box(structure, "Wall inset", new Vector3(side * 10.69f, 3.6f, z), new Vector3(.055f, 4.4f, 1.86f), panel, .02f);
                Shape(structure, "Wall skirting", new Vector3(side * 10.55f, .45f, 7), new Vector3(.25f, .8f, 27), steel);
                Shape(structure, "Wall luminous rail", new Vector3(side * 10.5f, 5.1f, 7), new Vector3(.04f, .06f, 27), cyan);
            }
            Point(structure, "Front task lighting", new Vector3(0, 5.2f, -1.8f), Colour("cadfe1"), 2.0f, 12);
            Point(structure, "Warm service lighting", new Vector3(-7, 4, 7), Colour("ffc989"), 1.8f, 10);
            Point(structure, "Cool service lighting", new Vector3(7, 4, 8), Colour("79cce4"), 1.8f, 10);
        }

        static TextMesh EntryAndObservation(Transform root)
        {
            var entry = Group(root, "03 - Entry airlock");
            Box(entry, "Airlock surround", new Vector3(0, 2.5f, -6.67f), new Vector3(5.4f, 5, .25f), steel, .16f);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(entry, "Closed airlock leaf", new Vector3(side * 1.12f, 2.15f, -6.48f), new Vector3(2.18f, 4.25f, .16f), wall, .10f);
                Box(entry, "Airlock viewport", new Vector3(side * 1.12f, 2.65f, -6.375f), new Vector3(1.5f, 1.3f, .045f), black);
                Box(entry, "Airlock handle", new Vector3(side * .35f, 1.65f, -6.33f), new Vector3(.08f, .6f, .075f), trim);
                Shape(entry, "Entry light", new Vector3(side * 2.43f, 2.4f, -6.47f), new Vector3(.045f, 3.8f, .03f), cyan);
                Box(entry, "Entry notice", new Vector3(side * 4.35f, 2.8f, -6.61f), new Vector3(2.7f, 1.8f, .08f), panel);
            }
            var back = Quaternion.Euler(0, 180, 0);
            Label(entry, "Entry title", "SIMULATION ACCESS / 03", new Vector3(0, 4.6f, -6.48f), .10f, back);
            Label(entry, "Entry instructions", "INTEGRATION LAB\nCALIBRATE / VERIFY / DEACTIVATE", new Vector3(-4.35f, 2.8f, -6.54f), .073f, back);
            Label(entry, "Return panel", "LOCAL CONTROL ONLINE\nSUPPORT AVAILABLE", new Vector3(4.35f, 2.8f, -6.54f), .073f, back);
            var annex = Group(root, "04 - Observation window and server annex");
            Shape(annex, "Rear wall header", new Vector3(0, 6.85f, 20.9f), new Vector3(22, 2.3f, .3f), wall);
            Shape(annex, "Rear wall sill", new Vector3(0, 1.1f, 20.9f), new Vector3(22, 2.2f, .3f), wall);
            foreach (int side in new[] { -1, 1 }) Shape(annex, "Rear wall pier", new Vector3(side * 8.55f, 4, 20.9f), new Vector3(4.9f, 4, .3f), wall);
            Box(annex, "Window frame", new Vector3(0, 3.95f, 20.65f), new Vector3(12.6f, 4, .22f), trim);
            Shape(annex, "Window void", new Vector3(0, 3.95f, 20.49f), new Vector3(12.15f, 3.55f, .03f), black).enabled = false;
            // The original rear-wall collider is the physical observation glass boundary.
            Shape(annex, "Observation glass", new Vector3(0, 3.95f, 20.46f), new Vector3(12.12f, 3.53f, .025f), glass);
            // Cut the inside of the broad frame open with four individual rails.
            var broadFrame = annex.Find("Window frame"); Object.DestroyImmediate(broadFrame.gameObject);
            foreach (float y in new[] { 2.1f, 5.8f }) Box(annex, "Window horizontal frame", new Vector3(0, y, 20.6f), new Vector3(12.6f, .15f, .23f), trim);
            foreach (float x in new[] { -6.2f, -2.1f, 2.1f, 6.2f }) Box(annex, "Window mullion", new Vector3(x, 3.95f, 20.6f), new Vector3(.12f, 3.7f, .23f), steel);
            Shape(annex, "Annex back wall", new Vector3(0, 3.7f, 26), new Vector3(14, 7.4f, .2f), panel);
            Shape(annex, "Annex floor", new Vector3(0, 0, 23), new Vector3(14, .1f, 6), floor);
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3) * 1.7f; Box(annex, "Server cabinet", new Vector3(x, 2.3f, 24.8f), new Vector3(1.4f, 4.6f, 1), steel, .08f);
                for (int j = 0; j < 12; j++)
                {
                    Shape(annex, "Server tray", new Vector3(x, .45f + j * .33f, 24.25f), new Vector3(1.15f, .22f, .035f), trim);
                    Shape(annex, "Server heartbeat", new Vector3(x - .4f, .45f + j * .33f, 24.22f), new Vector3(.055f, .055f, .018f), (i + j) % 3 == 0 ? amber : cyan);
                }
            }
            Point(annex, "Server annex light", new Vector3(0, 5.4f, 22.6f), Colour("82c5d7"), 2, 11);
            Box(annex, "Lab title plate", new Vector3(0, 6.6f, 20.5f), new Vector3(11, .95f, .12f), steel);
            Label(annex, "Lab title", "INTEGRATION / CONTAINMENT CONTROL", new Vector3(0, 6.6f, 20.42f), .18f, Quaternion.identity);
            Box(annex, "Room status panel", new Vector3(0, 5.95f, 8.5f), new Vector3(5.7f, .7f, .14f), black);
            return Label(annex, "Live room status", "0 / 4 MODULES CALIBRATED\nCONTAINMENT CONTROL", new Vector3(0, 5.95f, 8.4f), .08f, Quaternion.identity);
        }

        static Light Core(Transform root)
        {
            var core = Group(root, "05 - Containment assembly"); core.localPosition = new Vector3(0, 0, 11.5f);
            var basePlate = Shape(core, "Containment plinth", new Vector3(0, .13f, 0), new Vector3(6.6f, .12f, 6.6f), steel, PrimitiveType.Cylinder);
            var collider = basePlate.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = basePlate.GetComponent<MeshFilter>().sharedMesh;
            var shell = Shape(core, "Containment lower body", new Vector3(0, 1.1f, 0), new Vector3(4.7f, .9f, 4.7f), panel, PrimitiveType.Cylinder);
            var solid = shell.gameObject.AddComponent<MeshCollider>(); solid.sharedMesh = shell.GetComponent<MeshFilter>().sharedMesh;
            Ring(core, "Plinth trim", new Vector3(0, .27f, 0), 3.05f, .10f, trim);
            bands.AddRange(Ring(core, "Lower power ring", new Vector3(0, .39f, 0), 2.55f, .07f, cyan).GetComponentsInChildren<Renderer>());
            Shape(core, "Central capsule", new Vector3(0, 3.0f, 0), new Vector3(2.8f, 1.05f, 2.8f), black, PrimitiveType.Cylinder);
            var centerCollider = core.Find("Central capsule").gameObject.AddComponent<MeshCollider>(); centerCollider.sharedMesh = centerCollider.GetComponent<MeshFilter>().sharedMesh;
            foreach (float y in new[] { 2f, 3.1f, 4.2f })
            {
                var rotor = Ring(core, "Suspended stabiliser", new Vector3(0, y, 0), 2.05f, .16f, trim, 40); wheels.Add(rotor);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4; var lamp = Box(rotor, "Stabiliser energy insert", new Vector3(Mathf.Sin(a) * 2.05f, -.095f, Mathf.Cos(a) * 2.05f), new Vector3(.10f, .06f, .34f), cyan, .012f);
                    lamp.transform.localRotation = Quaternion.Euler(0, i * 45, 0); bands.Add(lamp);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad; Vector3 p = new Vector3(Mathf.Sin(a) * 2.45f, 0, Mathf.Cos(a) * 2.45f);
                Box(core, "Containment upright", p + Vector3.up * 2.7f, new Vector3(.42f, 5, .42f), steel, .08f);
                var r = Box(core, "Vertical power channel", p + new Vector3(0, 2.7f, -.23f), new Vector3(.12f, 3.7f, .045f), cyan, .015f); powers[i].Add(r);
            }
            Shape(core, "Top crown", new Vector3(0, 5.28f, 0), new Vector3(5.5f, .18f, 5.5f), steel, PrimitiveType.Cylinder);
            Ring(core, "Top crown trim", new Vector3(0, 5.48f, 0), 2.45f, .10f, trim);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30 * Mathf.Deg2Rad; var fin = Box(core, "Base cooling fin", new Vector3(Mathf.Sin(a) * 2.4f, 1.1f, Mathf.Cos(a) * 2.4f), new Vector3(.14f, 1.35f, .45f), trim, .035f);
                fin.transform.localRotation = Quaternion.Euler(0, i * 30, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 1.1f;
                Pipe(root, "Floor conduit " + i, new Vector3(x, .11f, 2.7f), new Vector3(x, .11f, 7.8f), .045f, trim);
                powers[i].Add(Shape(root, "Floor conduit power " + i, new Vector3(x, .13f, 5.2f), new Vector3(.02f, .025f, 4.9f), cyan));
            }
            return Point(core, "Containment glow", new Vector3(0, 3.6f, -1.7f), Colour("60d4e0"), 2.2f, 8);
        }

        static void ServiceBays(Transform root)
        {
            var bays = Group(root, "06 - Four module service bays");
            for (int i = 0; i < 4; i++)
            {
                int side = i < 2 ? -1 : 1; float z = i % 2 == 0 ? 5.25f : 8.75f;
                var bay = Group(bays, "Module " + (i + 1) + " service rack"); bay.localPosition = new Vector3(side * 8, 0, z);
                Box(bay, "Service counter body", new Vector3(0, 1, 0), new Vector3(2.95f, 2, 3.5f), panel, .08f);
                Box(bay, "Stainless worktop", new Vector3(0, 2.04f, 0), new Vector3(3.05f, .13f, 3.5f), trim);
                // All new counter detail sits inside the existing workbench collision footprint.
                for (int s = -1; s <= 1; s += 2)
                {
                    Box(bay, "Vertical service frame", new Vector3(s * 1.2f, 3.2f, .5f), new Vector3(.16f, 2.4f, .17f), steel);
                    for (int j = 0; j < 7; j++) Box(bay, "Counter ventilation slot", new Vector3(s * 1.481f, .8f + j * .12f, 0), new Vector3(.02f, .035f, 2.7f), black, .005f);
                }
                Box(bay, "Module status display", new Vector3(0, 3.25f, .48f), new Vector3(2.3f, 1.5f, .20f), black);
                circuitLabels[i] = Label(bay, "Live calibration status", "", new Vector3(0, 3.25f, .36f), .06f, Quaternion.identity);
                var lamp = Box(bay, "Module calibrated light", new Vector3(0, 4.12f, .48f), new Vector3(2.3f, .08f, .08f), amber, .015f); statuses[i].Add(lamp);
                for (int j = 0; j < 3; j++)
                {
                    float x = -.7f + j * .7f;
                    Shape(bay, "Coolant canister", new Vector3(x, 2.55f, -.85f), new Vector3(.4f, .42f, .4f), steel, PrimitiveType.Cylinder);
                    powers[i].Add(Shape(bay, "Canister light", new Vector3(x, 2.5f, -.85f), new Vector3(.413f, .035f, .413f), cyan, PrimitiveType.Cylinder));
                    Shape(bay, "Canister lid", new Vector3(x, 2.98f, -.85f), new Vector3(.44f, .035f, .44f), trim, PrimitiveType.Cylinder);
                }
                Box(bay, "Service drawer face", new Vector3(0, 1.1f, -1.765f), new Vector3(2.7f, 1.45f, .05f), steel);
                for (int j = 0; j < 3; j++) Box(bay, "Drawer pull", new Vector3(0, .7f + j * .42f, -1.81f), new Vector3(.8f, .055f, .06f), trim);
                Label(bay, "Rack index", "0" + (i + 1), new Vector3(-.95f, 1.55f, -1.8f), .13f, Quaternion.identity, Colour("d9a259"));
            }
        }

        static void InstrumentDetails(Chapter4World world, Transform root)
        {
            var details = Group(root, "07 - Instrument machining");
            var parts = world.GetComponentsInChildren<Transform>(true);
            foreach (var r in world.transform.Find("Containment device").GetComponentsInChildren<Renderer>(true))
            {
                var text = r.GetComponent<TextMesh>();
                if (text != null)
                {
                    if (new[] { "DIAL", "WIRES", "STEPS", "GAUGE" }.Contains(text.name))
                    { var p = text.transform.position; p.y = 1.845f; text.transform.position = p; text.characterSize = .016f; EditorUtility.SetDirty(text); }
                    continue;
                }
                if (r.name == "Containment case") r.sharedMaterial = panel;
                else if (r.name.StartsWith("Case module ")) r.sharedMaterial = steel;
                else if (r.name.EndsWith(" face")) r.sharedMaterial = ivory;
                else if (r.name.EndsWith(" bezel") || r.name == "Module screw" || r.name.Contains("edge trim")) r.sharedMaterial = trim;
                EditorUtility.SetDirty(r);
            }
            // Keep moving needles, accepted wires/cards, labels and their saved references intact.
            Box(details, "Console armoured front", new Vector3(0, .66f, .072f), new Vector3(3.7f, 1.24f, .07f), wall, .07f);
            for (int i = 0; i < 9; i++) Box(details, "Front cooling slot", new Vector3(-1.45f + i * .36f, .48f, .027f), new Vector3(.17f, .18f, .02f), black, .015f);
            foreach (float x in new[] { -2.26f, 2.26f })
            {
                Box(details, "Console edge armour", new Vector3(x, 1.42f, 1.07f), new Vector3(.18f, .23f, 2.5f), trim);
                Box(details, "Console leg brace", new Vector3(x * .76f, .5f, 1), new Vector3(.22f, 1, 1.6f), steel);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 1.1f;
                Box(details, "Instrument cassette rim", new Vector3(x, 1.745f, 1.13f), new Vector3(1.02f, .07f, 1.51f), trim, .045f);
                Box(details, "Recessed instrument deck", new Vector3(x, 1.793f, 1.13f), new Vector3(.91f, .028f, 1.38f), panel, .03f);
                statuses[i].Add(Box(details, "Cassette calibrated bar", new Vector3(x, 1.82f, .445f), new Vector3(.38f, .018f, .035f), amber, .005f));
                foreach (int side in new[] { -1, 1 })
                    for (int j = 0; j < 4; j++) Shape(details, "Cassette side vent", new Vector3(x + side * .46f, 1.817f, .95f + j * .17f), new Vector3(.025f, .02f, .07f), black);
                Label(details, "Module number " + i, "0" + (i + 1), new Vector3(x - .32f, 1.825f, .53f), .018f, Quaternion.Euler(90, 0, 0), Colour("d9a259"));
                for (int side = -1; side <= 1; side += 2)
                {
                    var feet = Box(details, "Panel fastener", new Vector3(x + side * .4f, 1.835f, 1.73f), new Vector3(.06f, .025f, .06f), trim, .012f);
                    Shape(details, "Fastener slot", feet.transform.localPosition + Vector3.up * .014f, new Vector3(.035f, .005f, .008f), black);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float z = .96f + i * .27f;
                foreach (int side in new[] { -1, 1 }) Shape(details, "Wire socket collar", new Vector3(-.55f + side * .34f, 1.826f, z), new Vector3(.15f, .015f, .15f), trim, PrimitiveType.Cylinder);
            }
            // Lid trim follows the original lid hinge, rather than staying behind when it closes.
            var hinge = parts.First(t => t.name == "Hinged case lid");
            var lid = Group(hinge, "Machined lid details"); lid.SetParent(world.transform, true); lid.position = Vector3.zero; lid.rotation = Quaternion.identity;
            // Build in the closed-lid coordinates, then restore the saved hinge pose.
            var old = hinge.localRotation; hinge.localRotation = Quaternion.identity;
            Box(lid, "Inset guide border", new Vector3(0, 1.521f, 1.03f), new Vector3(3.95f, .018f, 1.92f), trim, .06f);
            Box(lid, "Inset guide backing", new Vector3(0, 1.509f, 1.03f), new Vector3(3.76f, .016f, 1.73f), black, .04f);
            for (int side = -1; side <= 1; side += 2) Box(lid, "Lid status strip", new Vector3(side * 2.13f, 1.51f, 1.03f), new Vector3(.04f, .025f, 1.74f), cyan, .01f);
            lid.SetParent(hinge, true); hinge.localRotation = old;
        }

        [MenuItem("Calculus Lab/World/Refine Integration control room")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before authoring Integration.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save open scene edits before authoring Integration.");
            Directory.CreateDirectory(Evidence); Directory.CreateDirectory(Art); AssetDatabase.Refresh(); Materials();
            var scene = EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            if (world.transform.Find("Integration Control Room") != null) throw new Exception("Integration room already authored. Edit its saved prefab directly.");
            var oldColliders = world.GetComponentsInChildren<Collider>(true); var poses = oldColliders.Select(c => c.transform.localToWorldMatrix).ToArray(); var enabled = oldColliders.Select(c => c.enabled).ToArray();
            string worldReferences = JsonUtility.ToJson(world);
            DisableVisuals(world.transform.Find("Simulation background")); DisableVisuals(world.transform.Find("Imported environment art"));
            var room = world.transform.Find("Room");
            foreach (var r in room.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = r.name == "Floor" || r.name == "Left wall" || r.name == "Right wall" || r.name == "Entry wall" || r.name == "Active device marker" || r.name == "Wall equipment" || r.name == "Equipment face" || r.name.StartsWith("Wall rib");
                if (r.name == "Active device marker") { r.enabled = true; r.sharedMaterial = cyan; }
                if (r.name == "Floor") r.sharedMaterial = floor; else if (r.name == "Equipment face") r.sharedMaterial = black; else if (r.enabled) r.sharedMaterial = wall;
                EditorUtility.SetDirty(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            wheels.Clear(); bands.Clear(); for (int i = 0; i < 4; i++) { statuses[i].Clear(); powers[i].Clear(); }
            var root = Group(world.transform, "Integration Control Room");
            Architecture(root); var roomStatus = EntryAndObservation(root); var light = Core(root); ServiceBays(root); InstrumentDetails(world, root);
            var presentation = world.gameObject.AddComponent<IntegrationRoomPresentation>(); presentation.world = world; presentation.circuits = new IntegrationRoomPresentation.Circuit[4];
            for (int i = 0; i < 4; i++) presentation.circuits[i] = new IntegrationRoomPresentation.Circuit { statusLamps = statuses[i].ToArray(), powerLamps = powers[i].ToArray(), label = circuitLabels[i] };
            presentation.flywheels = wheels.ToArray(); presentation.coreBands = bands.ToArray(); presentation.coreLight = light; presentation.roomStatus = roomStatus;
            presentation.approachMarker = room.GetComponentsInChildren<Renderer>().First(r => r.name == "Active device marker");
            presentation.Sample(new bool[4], 0, 0);
            FitLettering(root);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, Prefabs + "IntegrationControlRoom.prefab", InteractionMode.AutomatedAction);
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = Colour("526f82"); RenderSettings.ambientEquatorColor = Colour("344651"); RenderSettings.ambientGroundColor = Colour("202b35"); RenderSettings.reflectionIntensity = .32f;
            var key = world.GetComponentsInChildren<Light>().First(l => l.type == LightType.Directional); key.color = Colour("c2d9e4"); key.intensity = .85f; key.transform.rotation = Quaternion.Euler(52, -35, 0); key.shadows = LightShadows.Soft; key.shadowStrength = .7f;
            var sky = world.GetComponent<SimulationSky>(); sky.fogTint = Colour("132a36"); sky.fogDensity = .0025f; sky.Apply();
            Physics.SyncTransforms();
            for (int i = 0; i < oldColliders.Length; i++)
                if (oldColliders[i] == null || oldColliders[i].enabled != enabled[i] || !Same(poses[i], oldColliders[i].transform.localToWorldMatrix)) throw new Exception("Changed existing collision: " + oldColliders[i]);
            if (worldReferences != JsonUtility.ToJson(world)) throw new Exception("Room detail pass changed gameplay references.");
            EditorUtility.SetDirty(world); EditorUtility.SetDirty(presentation); EditorUtility.SetDirty(sky); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Validate(); CaptureSaved(); Debug.Log("INTEGRATION_POLISH_AUTHOR_PASS");
        }
        static bool Same(Matrix4x4 a, Matrix4x4 b) { for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > .0001f) return false; return true; }
        public static void Validate()
        {
            Directory.CreateDirectory(Evidence); EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            var room = world.transform.Find("Integration Control Room"); var presentation = world.GetComponent<IntegrationRoomPresentation>();
            if (room == null || !PrefabUtility.IsPartOfPrefabInstance(room) || presentation == null || presentation.world != world || presentation.circuits.Length != 4) throw new Exception("Room and presentation must persist in the saved scene.");
            foreach (var r in room.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials)
                if (m == null || !AssetDatabase.Contains(m) || ShaderUtil.ShaderHasError(m.shader)) throw new Exception("Invalid room material: " + r.name);
            if (world.devices.Count != 4 || world.indicators.Count != 4 || world.GetComponent<CaseShutdownSequence>() == null) throw new Exception("Keep all four original modules and the master shutdown.");
            // Out-of-order completion must light its own circuit; shutdown must never clear accepted work.
            var moduleState = new[] { true, false, false, true }; presentation.Sample(moduleState, 0, 2);
            if (presentation.CompletedCount != 2 || !presentation.circuits[3].label.text.Contains("CALIBRATED") || !presentation.circuits[1].label.text.Contains("AWAITING")) throw new Exception("Incorrect circuit mapping.");
            for (int i = 0; i <= 100; i++)
            {
                presentation.Sample(moduleState, i / 100f, 2); if (presentation.CompletedCount != 2) throw new Exception("Shutdown changed calibration evidence.");
            }
            if (!presentation.PoweredOff || presentation.coreLight.intensity > .001f) throw new Exception("Containment power remains on after shutdown.");
            Physics.SyncTransforms();
            var origin = new Vector3(0, .7f, -5); var target = new Vector3(0, .7f, -1.3f);
            if (Physics.CapsuleCast(origin, origin + Vector3.up * .6f, .3f, (target - origin).normalized, out var hit, Vector3.Distance(origin, target), ~0, QueryTriggerInteraction.Ignore)) throw new Exception("Entry route blocked by " + hit.collider.name);
            File.WriteAllText(Path.Combine(Evidence, "validation.txt"), "PASS: room prefab and material/shader references survive reopening; four original modules, lid and master retained; out-of-order circuits map correctly; 101 shutdown samples preserve completed work and extinguish core; console approach clear.\n");
            EditorSceneManager.OpenScene(ScenePath); ManualEditingMigration.Validate();
        }
        public static void CaptureSaved()
        {
            Directory.CreateDirectory(Evidence); EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            var presentation = world.GetComponent<IntegrationRoomPresentation>();
            world.EditorOpenCase(false); presentation.Sample(new bool[4], 0, 0); if (presentation.lidGuide != null) presentation.lidGuide.enabled = false;
            Capture(world, "room-entry", world.player.transform.position + Vector3.up * 1.6f, new Vector3(0, 3.4f, 10));
            Capture(world, "room-back", new Vector3(0, 1.7f, -2), new Vector3(0, 2.8f, -7));
            Capture(world, "room-wide", new Vector3(8.5f, 5.8f, -4.5f), new Vector3(0, 2.5f, 8), 65);
            Capture(world, "containment-assembly", new Vector3(5.8f, 3.8f, 5), new Vector3(0, 2.7f, 11.5f), 56);
            world.EditorOpenCase(true); if (presentation.lidGuide != null) presentation.lidGuide.enabled = true; if (presentation.approachMarker != null) presentation.approachMarker.enabled = false;
            Capture(world, "device-detail", new Vector3(0, 5.2f, -3.5f), new Vector3(0, 1.75f, 1), 48);
            world.EditorCaseCompletion(4, true); presentation.Sample(new[] { true, true, true, true }, 1, 4);
            Capture(world, "device-safe", new Vector3(5, 3.6f, -2.5f), new Vector3(0, 2.3f, 6), 65);
            EditorSceneManager.OpenScene(ScenePath);
        }
        public static void AuthorAndBuild() { Author(); ManualEditingMigration.BuildQA(); }
        public static void Refine()
        {
            Materials(); EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            var root = world.transform.Find("Integration Control Room"); var presentation = world.GetComponent<IntegrationRoomPresentation>();
            PrefabUtility.UnpackPrefabInstance(root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var statusPanel = root.GetComponentsInChildren<Transform>().First(t => t.name == "Room status panel");
            statusPanel.localPosition = new Vector3(0, 5.95f, 8.5f); statusPanel.GetComponent<MeshFilter>().sharedMesh = Chamfer(new Vector3(5.7f, .7f, .14f), .05f);
            presentation.roomStatus.transform.localPosition = new Vector3(0, 5.95f, 8.4f);
            presentation.Sample(new bool[4], 0, 0); FitLettering(root);
            presentation.approachMarker = world.transform.Find("Room").GetComponentsInChildren<Renderer>().First(r => r.name == "Active device marker");
            presentation.approachMarker.enabled = true; presentation.approachMarker.sharedMaterial = cyan; EditorUtility.SetDirty(presentation.approachMarker);
            // The panel is inspected up close; the approach preview should expose the whole room.
            world.EditorOpenCase(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, Prefabs + "IntegrationControlRoom.prefab", InteractionMode.AutomatedAction);
            EditorUtility.SetDirty(presentation); EditorSceneManager.MarkSceneDirty(world.gameObject.scene); EditorSceneManager.SaveScene(world.gameObject.scene); AssetDatabase.SaveAssets();
            Validate(); CaptureSaved(); Debug.Log("INTEGRATION_POLISH_REFINE_PASS");
        }
        public static void RefineAndBuild() { Refine(); ManualEditingMigration.BuildQA(); }
        public static void FinaliseAndBuild()
        {
            Materials(); EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            var root = world.transform.Find("Integration Control Room"); var presentation = world.GetComponent<IntegrationRoomPresentation>();
            var hinge = world.GetComponentsInChildren<Transform>(true).First(t => t.name == "Hinged case lid");
            if (hinge.Find("Deep lid skirt") == null)
            {
                var old = hinge.localRotation; hinge.localRotation = Quaternion.identity;
                // The old flat lid intersected the controls while closed. Raise its shell and
                // underside together, with a skirt deep enough to contain the physical dials.
                foreach (Transform child in hinge) child.localPosition += Vector3.up * .4f;
                var skirt = Group(world.transform, "Deep lid skirt");
                foreach (int side in new[] { -1, 1 })
                {
                    Box(skirt, "Lid side lip", new Vector3(side * 2.35f, 1.88f, 1.03f), new Vector3(.10f, .34f, 2.28f), steel);
                    Box(skirt, "Lid end lip", new Vector3(0, 1.88f, 1.03f + side * 1.10f), new Vector3(4.65f, .34f, .09f), steel);
                }
                skirt.SetParent(hinge, true); hinge.localRotation = old;
            }
            presentation.guideLid = hinge;
            presentation.lidGuide = hinge.GetComponentsInChildren<TextMesh>(true).First(t => t.text.StartsWith("INTEGRATION / FIELD GUIDE")).GetComponent<Renderer>();
            presentation.lidGuide.enabled = false; world.EditorOpenCase(false);
            // Keep the lids in their existing hierarchy and all progress references untouched.
            EditorUtility.SetDirty(hinge); EditorUtility.SetDirty(presentation); EditorSceneManager.MarkSceneDirty(world.gameObject.scene); EditorSceneManager.SaveScene(world.gameObject.scene); AssetDatabase.SaveAssets();
            Validate(); CaptureSaved(); ManualEditingMigration.BuildQA(); Debug.Log("INTEGRATION_FINAL_BUILD_PASS");
        }
        public static void CleanPreviewAndBuildRelease()
        {
            string path = Prefabs + "IntegrationUI.prefab"; var ui = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tag = ui.GetComponentsInChildren<Transform>(true).First(t => t.name == "ReportInspectionTag");
                // This decorative caption covered the pressure module in the narrow scene strip.
                tag.gameObject.SetActive(false); PrefabUtility.SaveAsPrefabAsset(ui, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(ui); }
            AssetDatabase.SaveAssets(); ManualEditingMigration.Validate();
            EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>();
            var canvas = world.gameManager.missionUI.GetComponent<Canvas>(); SceneAuthoringWindow.ApplyView(world, canvas, "Case overview");
            world.EditorOpenCase(true); var presentation = world.GetComponent<IntegrationRoomPresentation>();
            presentation.Sample(new bool[4], 0, 0); presentation.lidGuide.enabled = true; presentation.approachMarker.enabled = false;
            SceneAuthoring.Capture(world, canvas, Path.Combine(Evidence, "case-overview-clean.png"), false);
            EditorSceneManager.OpenScene(ScenePath);
            ProductionUIBuilder.BuildRelease(); Debug.Log("INTEGRATION_PREVIEW_AND_RELEASE_PASS");
        }
        [MenuItem("Calculus Lab/World/Open refined Integration room")]
        public static void Open()
        {
            EditorSceneManager.OpenScene(ScenePath); var world = Object.FindFirstObjectByType<Chapter4World>(); Selection.activeGameObject = world.transform.Find("Integration Control Room").gameObject;
            var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>(); view.LookAt(new Vector3(0, 2.5f, 7), Quaternion.Euler(18, -24, 0), 17, false, true); view.Focus();
        }

        static void Capture(Chapter4World world, string name, Vector3 position, Vector3 target, float fov = 65)
        {
            var go = new GameObject("Integration evidence camera", typeof(Camera));
            var camera = go.GetComponent<Camera>();
            camera.CopyFrom(world.player.GetComponentInChildren<Camera>());
            camera.enabled = false;
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = fov;
            camera.farClipPlane = 100;
            try { LaboratoryAIImport.Capture(camera, Path.Combine(Evidence, name + ".png"), 1600, 900); }
            finally { Object.DestroyImmediate(go); }
        }

        public static void Inspect()
        {
            Directory.CreateDirectory(Evidence);
            EditorSceneManager.OpenScene(ScenePath);
            var world = Object.FindFirstObjectByType<Chapter4World>();
            string PathOf(Transform t) => AnimationUtility.CalculateTransformPath(t, world.transform);
            File.WriteAllLines(Path.Combine(Evidence, "before-layout.txt"), world.GetComponentsInChildren<Transform>(true)
                .Where(t => t.parent == world.transform || t.parent != null && t.parent.parent == world.transform)
                .Select(t => PathOf(t) + " | active " + t.gameObject.activeSelf + " | p " + t.position + " | scale " + t.lossyScale
                    + " | renderers " + t.GetComponentsInChildren<Renderer>(true).Length + " | colliders " + t.GetComponentsInChildren<Collider>(true).Length));
            File.WriteAllLines(Path.Combine(Evidence, "before-colliders.txt"), world.GetComponentsInChildren<Collider>(true)
                .Select(c => PathOf(c.transform) + " | " + c.enabled + " | " + c.bounds));
            Capture(world, "before-entry", world.player.transform.position + Vector3.up * 1.6f, new Vector3(0, 2.2f, 8));
            Capture(world, "before-back", new Vector3(0, 1.7f, -3), new Vector3(0, 2.5f, -7));
            world.EditorOpenCase(true);
            Capture(world, "before-device", new Vector3(0, 5.2f, -3.5f), new Vector3(0, 1.75f, 1), 48);
            Debug.Log("INTEGRATION_INSPECTION_PASS");
        }
    }
}
#endif
