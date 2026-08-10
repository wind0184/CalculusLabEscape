#if UNITY_EDITOR
using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class PrototypeSceneGenerator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Level01_Limits.unity";

        [MenuItem("Calculus Lab/Generate Level 01 Prototype")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/_Project/Materials");
            Directory.CreateDirectory("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level 01 - Limits Lab");
            var builder = root.AddComponent<CyberLabSceneBuilder>();

            SerializedObject serialized = new SerializedObject(builder);
            serialized.FindProperty("floorMaterial").objectReferenceValue = Material("Floor", new Color(.025f, .035f, .08f), 0f);
            serialized.FindProperty("wallMaterial").objectReferenceValue = Material("Wall", new Color(.04f, .055f, .12f), .25f);
            serialized.FindProperty("cyanMaterial").objectReferenceValue = Material("NeonCyan", new Color(.02f, .75f, .95f), .45f);
            serialized.FindProperty("magentaMaterial").objectReferenceValue = Material("NeonMagenta", new Color(.9f, .04f, .65f), .4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated prototype scene: {ScenePath}");
        }

        public static void GenerateBatch()
        {
            Generate();
            EditorApplication.Exit(0);
        }

        public static void BuildWindowsBatch()
        {
            Generate();
            Directory.CreateDirectory("Builds/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/CalculusLabEscape_v1.0.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Prototype build failed: {report.summary.result}");
            Debug.Log($"Prototype build succeeded: {report.summary.outputPath}");
            EditorApplication.Exit(0);
        }

        private static Material Material(string name, Color color, float metallic)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name, color = color };
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", .75f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
#endif
