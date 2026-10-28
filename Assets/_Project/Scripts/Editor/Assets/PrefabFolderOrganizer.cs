#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class PrefabFolderOrganizer
    {
        public const string Root = "Assets/_Project/Prefabs";
        static string Project => Path.GetDirectoryName(Application.dataPath);
        static string Evidence => Path.GetFullPath(Path.Combine(Project, "../../qa/prefab-organization-2026-10-23"));
        static string Request => Path.Combine(Project, "Library/PrefabFolderOrganizer.request");
        static string Status => Path.Combine(Evidence, "live-status.json");
        static double nextPoll;
        [Serializable] sealed class Result { public string state, message; }
        [Serializable] sealed class Move { public string source, destination, guid, sha256; }
        [Serializable] sealed class Snapshot { public string path, sha256; }
        [Serializable] sealed class Manifest { public Move[] moves; public Snapshot[] scenes; }
        [InitializeOnLoadMethod] static void Watch() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 2;
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try
            {
                Run();
                OpenFolder();
                File.WriteAllText(Status, JsonUtility.ToJson(new Result { state = "complete", message = "All prefabs moved and references verified." }));
            }
            catch (Exception error)
            {
                File.WriteAllText(Status, JsonUtility.ToJson(new Result { state = "failed", message = error.ToString() }));
                Debug.LogException(error);
            }
        }
        static string Hash(string path)
        {
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(Path.Combine(Project, path)))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public static void Run()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Evidence, "move-manifest.json")));
            foreach (var move in manifest.moves)
            {
                if (!move.destination.StartsWith(Root + "/", StringComparison.Ordinal)) throw new Exception("Destination outside prefab folder");
                if (AssetDatabase.AssetPathToGUID(move.source) != move.guid) throw new Exception("Source GUID changed: " + move.source);
                if (Hash(move.source) != move.sha256) throw new Exception("Source prefab changed: " + move.source);
                if (File.Exists(Path.Combine(Project, move.destination))) throw new Exception("Existing destination: " + move.destination);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Project, move.destination)));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new StringBuilder();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var move in manifest.moves)
                {
                    string error = AssetDatabase.MoveAsset(move.source, move.destination);
                    if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                    report.AppendLine(move.source + " -> " + move.destination);
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var move in manifest.moves)
            {
                if (AssetDatabase.AssetPathToGUID(move.destination) != move.guid) throw new Exception("GUID changed: " + move.destination);
                if (Hash(move.destination) != move.sha256) throw new Exception("Prefab contents changed: " + move.destination);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(move.destination);
                if (prefab == null) throw new Exception("Cannot load prefab: " + move.destination);
                foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) != 0)
                        throw new Exception("Missing script: " + move.destination + " / " + node.name);
            }
            string[] allPrefabs = Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories);
            if (allPrefabs.Length != manifest.moves.Length || allPrefabs.Any(p => !Path.GetFullPath(p).StartsWith(Path.GetFullPath(Path.Combine(Project, Root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Prefabs remain outside the central folder");
            foreach (string name in new[] { "MainMenuUI", "LimitsUI", "DifferentiationUI", "IntegrationUI" })
            {
                var loaded = Resources.Load<GameObject>("Production/" + name);
                if (loaded == null || AssetDatabase.GetAssetPath(loaded) != Root + "/Resources/Production/" + name + ".prefab")
                    throw new Exception("Runtime UI resource failed: " + name);
            }
            foreach (string name in new[] { "LimitsQuestions", "LimitsIndependentQuestions", "DifferentiationQuestions", "IntegrationQuestions", "MissionIntroductions" })
                if (Resources.Load("Production/" + name) == null) throw new Exception("Data resource unavailable: " + name);
            foreach (var scene in manifest.scenes)
                if (Hash(scene.path) != scene.sha256) throw new Exception("Scene changed: " + scene.path);
            var robot = AssetDatabase.LoadAssetAtPath<GameObject>(LaboratoryAIImport.PrefabPath).GetComponent<LaboratoryAIPreview>();
            if (robot == null || robot.clips.Length != 9 || robot.clips.Any(c => c == null) || robot.clips[3].name != "Think")
                throw new Exception("Robot clip reference failed");
            File.WriteAllText(Path.Combine(Evidence, "moves.txt"), report.ToString());
            File.WriteAllText(Path.Combine(Evidence, "validation.txt"),
                "PASS: " + manifest.moves.Length + " prefabs moved under " + Root + ".\n" +
                "PASS: Every prefab retains its GUID and exact file contents.\n" +
                "PASS: All prefabs load without missing scripts.\n" +
                "PASS: All four production UI prefabs load through their original Resources keys.\n" +
                "PASS: Five question/introduction data resources remain available.\n" +
                "PASS: Robot retains all nine clips, including the current Think.\n" +
                "PASS: All " + manifest.scenes.Length + " scene files are unchanged.\n");
            Debug.Log("PREFAB_FOLDER_ORGANIZATION_PASS");
        }

        [MenuItem("Calculus Lab/Assets/Open prefab folder")]
        public static void OpenFolder()
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}
#endif
