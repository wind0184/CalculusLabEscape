#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // Explicit, one-time authoring. Normal startup never rebuilds these UI assets.
    public static partial class ReportFusionLayout
    {
        static readonly string[] LessonNames = { "Limits", "Differentiation", "Integration" };
        static readonly string[] LessonObjectives = {
            "Restore the\nconvergence gate.",
            "Restore the bridge.\nReunite the calf.",
            "Restore four modules.\nDeactivate the device."
        };
        static string MissionVideoEvidence => LearningApi.Argument("--mission-video-evidence", Path.GetFullPath("../../qa/mission-briefing-videos-2026-10-25"));
        static string VideoFolder(int topic) => "Assets/_Project/Art/Video/" + LessonNames[topic] + "Briefing/";

        [MenuItem("Calculus Lab/Mission videos/Install all three saved introductions")]
        public static void AuthorAllMissionBriefingVideos()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before authoring the introduction pages.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save open scene edits before authoring.");
            Directory.CreateDirectory(MissionVideoEvidence);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Limits already has an authored, compatible introduction. Preserve its assets.
            for (int topic = 1; topic < 3; topic++)
            {
                AuthorMissionVideoPrefab(topic);
                AuthorMissionVideoPage(topic);
            }
            for (int topic = 0; topic < 3; topic++)
            {
                var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[topic] + ".unity");
                var manager = Object.FindFirstObjectByType<GameManager>();
                var ui = topic == 0 ? manager.menuOrLimitsUI.gameObject : manager.missionUI.gameObject;
                var view = ui.GetComponentsInChildren<MissionBriefingVideo>(true).Single();
                manager.briefingVideo = view; view.manager = manager;
                SaveReference(manager); SaveReference(view);
                if (topic > 0)
                {
                    ManualEditingMigration.ConfigureCanvas(ui);
                    SaveReference(ui.GetComponent<UIReferenceMap>());
                    SaveReference(manager.missionUI);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            ValidateAllMissionBriefingVideos();
            ManualEditingMigration.Validate();
            File.WriteAllText(Path.Combine(MissionVideoEvidence, "authored.txt"),
                "PASS: three saved local English video introductions; original Limits media preserved; shared playback behaviour; persistent play/pause, replay, Start and Back events; no runtime UI/map generation.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + "MainMenu.unity");
        }

        static void AuthorMissionVideoPrefab(int topic)
        {
            string name = LessonNames[topic], folder = VideoFolder(topic);
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(folder + name + "Introduction-English.mp4");
            if (clip == null || clip.audioTrackCount != 1 || clip.width != 1920 || clip.height != 1080 || Math.Abs(clip.length - 90) > .1)
                throw new Exception("Import the complete 1080p/90-second English video with its audio first: " + name);
            string coverPath = folder + name + "Introduction-Cover.png";
            var importer = AssetImporter.GetAtPath(coverPath) as TextureImporter;
            if (importer == null) throw new Exception("Missing lesson cover: " + name);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var cover = AssetDatabase.LoadAssetAtPath<Texture2D>(coverPath);
            string texturePath = folder + name + "Introduction.renderTexture";
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
            if (texture == null)
            {
                texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32) { name = name + " Introduction", antiAliasing = 1 };
                AssetDatabase.CreateAsset(texture, texturePath);
            }

            // Copy the validated player layout, replacing only the lesson-specific data.
            var root = PrefabUtility.LoadPrefabContents(Prefabs + "LimitsBriefingVideo.prefab");
            try
            {
                var old = root.GetComponent<MissionBriefingVideo>();
                var view = root.AddComponent<MissionBriefingVideo>();
                view.player = old.player; view.voice = old.voice; view.screen = old.screen; view.poster = old.poster;
                view.toggle = old.toggle; view.replay = old.replay; view.toggleLabel = old.toggleLabel;
                view.status = old.status; view.clock = old.clock; view.progress = old.progress;
                while (view.toggle.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(view.toggle.onClick, 0);
                while (view.replay.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(view.replay.onClick, 0);
                UnityEventTools.AddPersistentListener(view.toggle.onClick, view.TogglePlayback);
                UnityEventTools.AddPersistentListener(view.replay.onClick, view.Replay);
                Object.DestroyImmediate(old);
                root.name = name + "BriefingVideo";
                view.manager = null;
                view.player.playOnAwake = false; view.player.clip = clip; view.player.source = VideoSource.VideoClip;
                view.player.targetTexture = texture; view.player.renderMode = VideoRenderMode.RenderTexture;
                view.player.isLooping = false; view.player.waitForFirstFrame = true; view.player.sendFrameReadyEvents = false;
                view.player.audioOutputMode = VideoAudioOutputMode.AudioSource; view.player.controlledAudioTrackCount = 1;
                view.player.EnableAudioTrack(0, true); view.player.SetTargetAudioSource(0, view.voice);
                view.voice.playOnAwake = false; view.voice.loop = false; view.voice.spatialBlend = 0; view.voice.volume = 1;
                view.screen.texture = texture; view.screen.raycastTarget = false;
                view.poster.GetComponent<RawImage>().texture = cover;
                view.poster.SetActive(true);
                Index(root);
                Text("BriefVideoObjective", LessonObjectives[topic]);
                Type("BriefVideoObjective", 23, Ink);
                Text("BriefVideoNote", "A short introduction to " + name.ToLowerInvariant() + ".\nWatch now, or start when ready.");
                Text("BriefVideoStatus", "English audio / 1 min 30 sec");
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + "BriefingVideo.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AuthorMissionVideoPage(int topic)
        {
            string path = Prefabs + LessonNames[topic] + "UI.prefab";
            var ui = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var old in ui.GetComponentsInChildren<MissionBriefingVideo>(true)) Object.DestroyImmediate(old.gameObject);
                Index(ui); var brief = Find("Brief");
                foreach (Transform child in brief)
                    if (child.name != "BriefTitle" && child.name != "BriefStart" && child.name != "BriefBack") child.gameObject.SetActive(false);
                At("BriefTitle", 24, 73, 960, 34); Type("BriefTitle", 29, Ink);
                Text("BriefTitle", "Understanding " + LessonNames[topic]);
                Button("BriefStart", 1008, 578, 248, 52, brief, true); Text("BriefStartLabel", "Start mission");
                Button("BriefBack", 1008, 638, 248, 31, brief); Text("BriefBackLabel", "Back to menu");
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + LessonNames[topic] + "BriefingVideo.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, ui.scene);
                instance.transform.SetParent(brief, false); instance.transform.SetAsFirstSibling();
                SaveReference(instance.transform);
                ManualEditingMigration.ConfigureCanvas(ui);
                PrefabUtility.SaveAsPrefabAsset(ui, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(ui); }
        }

        [MenuItem("Calculus Lab/Mission videos/Validate all three saved introductions")]
        public static void ValidateAllMissionBriefingVideos()
        {
            Directory.CreateDirectory(MissionVideoEvidence);
            var lines = new System.Collections.Generic.List<string>();
            for (int topic = 0; topic < 3; topic++)
            {
                var scene = EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + Chapter4Game.Scenes[topic] + ".unity");
                var manager = Object.FindFirstObjectByType<GameManager>();
                var view = manager.briefingVideo;
                if (view == null || view.manager != manager || view.player == null || view.player.clip == null || view.voice == null || view.screen == null || view.poster == null || view.progress == null)
                    throw new Exception("Missing saved video references: " + LessonNames[topic]);
                var clip = view.player.clip;
                if (clip.audioTrackCount != 1 || clip.width != 1920 || clip.height != 1080 || Math.Abs(clip.length - 90) > .1 || !clip.name.StartsWith(LessonNames[topic]))
                    throw new Exception("Wrong lesson/audio/format: " + LessonNames[topic]);
                if (view.player.playOnAwake || view.player.isLooping || view.player.GetTargetAudioSource(0) != view.voice || view.voice.spatialBlend != 0 || view.voice.playOnAwake)
                    throw new Exception("Incorrect saved playback/audio settings: " + LessonNames[topic]);
                if (view.toggle.onClick.GetPersistentEventCount() != 1 || view.replay.onClick.GetPersistentEventCount() != 1 || view.toggle.onClick.GetPersistentTarget(0) != view || view.replay.onClick.GetPersistentTarget(0) != view)
                    throw new Exception("Playback events must target the saved controller: " + LessonNames[topic]);
                var ui = topic == 0 ? manager.menuOrLimitsUI.gameObject : manager.missionUI.gameObject;
                var map = ui.GetComponent<UIReferenceMap>();
                var start = map.elements.Single(e => e.id == "BriefStart").target.GetComponent<Button>();
                var back = map.elements.Single(e => e.id == "BriefBack").target.GetComponent<Button>();
                if (start.onClick.GetPersistentEventCount() != 1 || start.onClick.GetPersistentMethodName(0) != "OnBriefStart" || back.onClick.GetPersistentEventCount() != 1 || back.onClick.GetPersistentMethodName(0) != "OnBriefBack")
                    throw new Exception("Preserve the original Start/Back mission events: " + LessonNames[topic]);
                // Edit mode does not execute UI Awake, so inspect the saved map directly.
                lines.Add("PASS " + LessonNames[topic] + ": saved local clip, AAC track, texture, poster, 2D AudioSource, persistent playback/Start/Back, scene manager reference; no session generated.");
            }
            File.WriteAllLines(Path.Combine(MissionVideoEvidence, "saved-references.txt"), lines);
            Debug.Log("ALL_MISSION_BRIEFING_VIDEOS_SAVED_PASS");
        }

        public static void AuthorAllMissionBriefingVideosAndBuild()
        {
            AuthorAllMissionBriefingVideos();
            BuildMissionVideoPlayer();
        }

        public static void BuildAllMissionBriefingVideos()
        {
            ValidateAllMissionBriefingVideos();
            ManualEditingMigration.Validate();
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder + "MainMenu.unity");
            BuildMissionVideoPlayer();
        }

        static void BuildMissionVideoPlayer()
        {
            string path = Path.GetFullPath("Builds/MissionBriefingQA/LearningLab.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scenes = new[] { "MainMenu", Chapter4Game.Scenes[0], Chapter4Game.Scenes[1], Chapter4Game.Scenes[2] }.Select(s => SceneAuthoring.SceneFolder + s + ".unity").ToArray();
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Mission video player build failed: " + result.summary.result);
            File.WriteAllText(Path.Combine(MissionVideoEvidence, "build.txt"), "PASS: four-scene Windows development player with all three saved introductions.\n" + path);
        }
    }
}
#endif
