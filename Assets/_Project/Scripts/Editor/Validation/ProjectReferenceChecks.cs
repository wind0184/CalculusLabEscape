#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    /// <summary>Checks serialized script references without regenerating gameplay assets.</summary>
    public static class ProjectReferenceChecks
    {
        [MenuItem("Calculus Lab/Validation/Check scenes and prefab scripts")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before validating saved assets.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var previousScenes = EditorSceneManager.GetSceneManagerSetup();
            var report = new StringBuilder();
            int objectsChecked = 0;
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();

            try
            {
                foreach (string path in scenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    foreach (var root in scene.GetRootGameObjects())
                        objectsChecked += CheckHierarchy(root, path);
                    report.AppendLine("SCENE PASS " + path);
                }

                foreach (string path in prefabPaths)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try { objectsChecked += CheckHierarchy(root, path); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    report.AppendLine("PREFAB PASS " + path);
                }

                MissionFeedbackChecks.Validate();
                MissionPracticeChecks.Validate();
                MathNotationChecks.Validate();
                report.AppendLine($"PASS: {scenePaths.Length} scenes; {prefabPaths.Length} prefabs; {objectsChecked} GameObjects; no missing script components.");
                string output = Environment.GetEnvironmentVariable("CALCULUS_CODE_QA_OUTPUT");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, "unity-reference-checks.txt"), report.ToString());
                }
            }
            finally
            {
                // Batch startup can have an unnamed scene, which cannot be restored by path.
                if (previousScenes.Any(scene => !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("PROJECT_REFERENCE_CHECKS_PASS / " + scenePaths.Length + " scenes / " + prefabPaths.Length + " prefabs");
        }

        static int CheckHierarchy(GameObject root, string assetPath)
        {
            var objects = root.GetComponentsInChildren<Transform>(true);
            foreach (var item in objects)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                if (missing != 0)
                    throw new InvalidOperationException($"{assetPath}: {item.name} has {missing} missing script components.");
            }
            return objects.Length;
        }
    }
}
#endif
