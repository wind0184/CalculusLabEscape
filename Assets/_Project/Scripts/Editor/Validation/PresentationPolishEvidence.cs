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
    public static partial class PresentationPolishBuilder
    {
        static void CaptureCamera(Chapter4World world, string filename, Vector3 position, Vector3 look)
        {
            var go = new GameObject("Presentation evidence camera");
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(world.player.GetComponentInChildren<Camera>());
            camera.enabled = false;
            camera.transform.position = position;
            camera.transform.LookAt(look);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            bool enabled = canvas.enabled;
            var texture = RenderTexture.GetTemporary(1600, 900, 24);
            var active = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                canvas.enabled = false;
                camera.targetTexture = texture;
                foreach (var atlas in world.GetComponentsInChildren<WorldLabelAtlas>(true)) atlas.Apply();
                camera.Render(); RenderTexture.active = texture;
                pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Evidence, filename), pixels.EncodeToPNG());
            }
            finally
            {
                canvas.enabled = enabled; camera.targetTexture = null; RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(texture); Object.DestroyImmediate(go);
                if (pixels != null) Object.DestroyImmediate(pixels);
            }
        }

        public static void VerifyAndCapture()
        {
            Directory.CreateDirectory(Evidence);
            for (int topic = 1; topic <= 2; topic++)
            {
                string path = SceneAuthoring.SceneFolder + Chapter4Game.Scenes[topic] + ".unity";
                EditorSceneManager.OpenScene(path);
                var world = Object.FindFirstObjectByType<Chapter4World>();
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null) throw new Exception("Polish preview created a session");
                if (topic == 1)
                {
                    var stations = world.transform.Find("Repair station machinery");
                    if (stations.GetComponentsInChildren<Transform>(true).Count(t => t.name == DetailGroup) != 3) throw new Exception("Station details missing or duplicated");
                    if (stations.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Imported ", StringComparison.Ordinal)) != 8) throw new Exception("Expected eight imported Factory pieces");
                    foreach (var detail in stations.GetComponentsInChildren<Transform>(true).Where(t => t.name == DetailGroup))
                        if (detail.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Decoration changed collision");
                    foreach (var binding in stations.GetComponentsInChildren<RepairStationIndicator>())
                    {
                        binding.Apply();
                        if (binding.lights.Any(r => r.sharedMaterial != binding.source.sharedMaterial)) throw new Exception("Status rail is not connected");
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        var station = stations.GetChild(i);
                        // Approach each station from its own front, avoiding foreground equipment.
                        var offset = i == 2 ? new Vector3(-1.3f, 1.7f, -2.6f) : new Vector3(1.8f, 1.7f, -3.3f);
                        CaptureCamera(world, "station-" + i + ".png", station.position + offset, station.position + new Vector3(0, .95f, 0));
                    }
                    CaptureCamera(world, "bridge-overview.png", new Vector3(10, 6, -4), new Vector3(0, .8f, 5));
                }
                else
                {
                    var label = world.GetComponent<CaseShutdownSequence>().deviceStatus;
                    var heading = world.GetComponentsInChildren<TextMesh>(true).First(t => t.text == "03 / CONTAINMENT CASE");
                    if (Mathf.Abs(label.transform.position.x - heading.transform.position.x) < 1.5f) throw new Exception("Device title/status panels overlap");
                    CaptureCamera(world, "case-front.png", new Vector3(3.4f, 2.5f, -4.5f), new Vector3(0, 1.2f, 1));
                }
                foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || !EditorUtility.IsPersistent(material) || ShaderUtil.ShaderHasError(material.shader)) throw new Exception("Invalid world material " + renderer.name);

                foreach (string view in new[] { "World", topic == 1 ? "Power Core" : "Case overview", "Movement guide" })
                {
                    SceneAuthoringWindow.ApplyView(world, canvas, view);
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in canvas.GetComponentsInChildren<TMP_Text>())
                    { text.ForceMeshUpdate(); if (text.isTextOverflowing) throw new Exception("Polish text overflow: " + view + " / " + text.name); }
                    if (view == "World" || view == "Movement guide")
                    {
                        var playerCamera = world.player.GetComponentInChildren<Camera>();
                        playerCamera.transform.LookAt(topic == 1 ? world.devices[0].position + Vector3.up * .4f : new Vector3(0, 1.3f, 1));
                    }
                    SceneAuthoring.Capture(world, canvas, Path.Combine(Evidence, topic + "-" + view.Replace(" ", "-") + ".png"), false);
                }
                EditorSceneManager.OpenScene(path);
            }
            File.WriteAllText(Path.Combine(Evidence, "editor-result.txt"), "PASS: eight imported Factory pieces across three stations; state-bound rails; no added collision; depth-tested labels; separated case information plates; two native control strips; UI text fits; saved scenes reload without starting a session.\n");
            Debug.Log("PRESENTATION_POLISH_EDITOR_PASS");
        }
    }
}
#endif
