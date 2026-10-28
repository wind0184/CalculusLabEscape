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
    // Load saved preview scenes, validate text, and capture actual Unity camera output.
    public static partial class AssetTrials
    {
        public static void CaptureAll()
        {
            string folder = Path.GetFullPath("../../qa/asset-trials-2026-10-18");
            Directory.CreateDirectory(folder);
            for (int i = 0; i < 4; i++)
            {
                EditorSceneManager.OpenScene(ScenePath(i));
                if (Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)
                    throw new Exception("Gameplay in preview");
                var cam = Object.FindFirstObjectByType<Camera>();
                var rt = RenderTexture.GetTemporary(1600, 900, 24);
                var active = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    {
                        text.ForceMeshUpdate();
                        if (text.isTextOverflowing)
                            throw new Exception("Text overflow " + text.name);
                    }
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes(folder + "/" + Names[i] + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
                finally { cam.targetTexture = null; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); }
            }
            EditorSceneManager.OpenScene(ScenePath(0));
            Debug.Log("ASSET_TRIAL_CAPTURE_PASS");
        }
    }
}
#endif
