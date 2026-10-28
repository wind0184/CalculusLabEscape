#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class LaboratoryAIThinkRevision
    {
        static string Evidence => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../qa/laboratory-ai-think-support-2026-10-23"));

        public static void RefreshAndCapture()
        {
            Directory.CreateDirectory(Evidence);
            string source = LaboratoryAIImport.Root + "/Models/LaboratoryAI_v7_Think.fbx";
            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var imported = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.length > 0);
            var updated = Object.Instantiate(imported);
            updated.name = "Think";
            updated.legacy = true;
            updated.wrapMode = WrapMode.ClampForever;
            var settings = AnimationUtility.GetAnimationClipSettings(updated);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(updated, settings);
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(LaboratoryAIImport.Root + "/Animations/Think.anim");
            EditorUtility.CopySerialized(updated, saved);
            Object.DestroyImmediate(updated);
            EditorUtility.SetDirty(saved);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(saved.length - 4) > .01f) throw new Exception("Think duration changed");

            EditorSceneManager.OpenScene(LaboratoryAIImport.ScenePath);
            var preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var hand = preview.Part("Ceramic hand paddle 1");
            preview.Sample(3, 0);
            Vector3 rest = hand.position;
            preview.Sample(3, 4);
            if (Vector3.Distance(rest, hand.position) > .0001f) throw new Exception("Think no longer returns to rest");
            preview.Sample(3, 2);
            if (Vector3.Distance(rest, hand.position) < .02f) throw new Exception("Think hand did not animate");
            LaboratoryAIImport.Capture(camera, Path.Combine(Evidence, "Think-after.png"));
            Vector3 cameraPosition = camera.transform.position;
            Quaternion cameraRotation = camera.transform.rotation;
            Vector3 centre = new Vector3(0, .48f, 0);
            camera.transform.RotateAround(centre, Vector3.up, 55);
            LaboratoryAIImport.Capture(camera, Path.Combine(Evidence, "Think-side.png"));
            camera.transform.position = cameraPosition;
            camera.transform.rotation = cameraRotation;
            for (int frame = 0; frame < 48; frame++)
            {
                preview.Sample(3, frame / 12f);
                LaboratoryAIImport.Capture(camera, Path.Combine(Evidence, "ThinkFrames", frame.ToString("000") + ".png"), 960, 540);
            }
            preview.Sample(3, 2);
            preview.cycleAll = false;
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            File.WriteAllText(Path.Combine(Evidence, "unity-validation.txt"),
                "PASS: Think refreshed in place, preserving the clip GUID and prefab references.\n" +
                "PASS: 4 seconds; returns to original rest pose; hand moves during Think.\n" +
                "PASS: 48 camera frames plus front and side views captured in Unity.\n" +
                "Bindings: " + AnimationUtility.GetCurveBindings(saved).Length + "\n");
            Debug.Log("LABORATORY_AI_THINK_REVISION_PASS");
        }

        public static void OpenThinkPreview()
        {
            LaboratoryAIPreviewWindow.OpenShowcase();
            var preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            if (preview == null) return;
            preview.cycleAll = false;
            preview.PlayIndex(3);
            Debug.Log("LABORATORY_AI_THINK_PREVIEW_OPEN");
        }
    }
}
#endif
