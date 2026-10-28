#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CalculusLabEscape
{
    // Opt-in player QA only; real profiles and saved runs are never touched.
    public sealed class MissionBriefingVideoPlaytest : MonoBehaviour
    {
        string folder;
        int failures;
        bool finished;
        readonly List<string> checks = new List<string>();
        GameManager Manager => GameManager.Instance;
        static readonly string[] Names = { "Limits", "Differentiation", "Integration" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--mission-briefing-video-qa")) return;
            var path = LearningApi.Argument("--qa-output");
            if (string.IsNullOrEmpty(path)) throw new Exception("Video QA requires --qa-output.");
            Directory.CreateDirectory(path);
            Chapter4Store.IsolatedFolder = Path.Combine(path, "sessions");
            var go = new GameObject("Three mission video QA");
            DontDestroyOnLoad(go); go.AddComponent<MissionBriefingVideoPlaytest>().folder = path;
            Application.runInBackground = true;
        }

        void Awake() => Application.logMessageReceived += Log;
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Log(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            failures++;
            if (folder != null) File.AppendAllText(Path.Combine(folder, "errors.txt"), message + "\n" + stack + "\n");
        }
        void Check(bool ok, string label)
        {
            checks.Add((ok ? "PASS: " : "FAIL: ") + label);
            if (!ok) failures++;
            File.WriteAllLines(Path.Combine(folder, "checks.txt"), checks);
        }
        IEnumerator Until(Func<bool> condition, string label, float seconds = 20)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            if (!condition()) throw new Exception("Video QA timeout: " + label);
        }
        IEnumerator Settled(string scene)
        {
            yield return Until(() => SceneManager.GetActiveScene().name == scene && !ScreenTransition.IsBusy, "scene " + scene);
            yield return null;
        }
        Button Control(string id)
        {
            if (Manager.missionUI != null) return Manager.missionUI.Control<Button>(id);
            return Manager.menuOrLimitsUI.Control<Button>(id);
        }
        IEnumerator Click(Button button, string label)
        {
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            bool reachable = button.isActiveAndEnabled && button.interactable && hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button;
            Check(reachable, "Pointer reaches " + label);
            if (!reachable) throw new Exception("Blocked video flow button: " + label);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
        }
        IEnumerator Select(int topic)
        {
            yield return Settled("MainMenu");
            yield return Click(Control("HomeStart"), "HomeStart");
            yield return Until(() => Control("Mission" + topic).isActiveAndEnabled && (Manager.menuEntrance == null || !Manager.menuEntrance.Busy), "topic selector");
            yield return Click(Control("Mission" + topic), "Mission" + topic);
            yield return Settled(Chapter4Game.Scenes[topic]);
            Check(Manager.Topic == topic && Manager.State == "Brief", Names[topic] + " selection enters Brief");
        }

        IEnumerator Start()
        {
            StartCoroutine(Watchdog());
            yield return null;
            yield return Settled("MainMenu");
            Chapter4Store.Profile = "Video QA";
            for (int topic = 0; topic < 3; topic++)
            {
                yield return Select(topic);
                var view = Manager.briefingVideo;
                Check(view != null && view.player.clip.name.StartsWith(Names[topic]), Names[topic] + " uses its saved local English clip");
                int count = Chapter4Store.Read().Count;
                float elapsed = Manager.Session.elapsed;
                yield return Until(() => view.IsReady && view.HasFrame || view.Failed, Names[topic] + " first decoded frame", 30);
                Check(!view.Failed && view.player.isPlaying && !view.poster.activeSelf, Names[topic] + " video decodes and automatically plays");
                Check(view.player.audioTrackCount == 1 && view.player.GetTargetAudioSource(0) == view.voice && view.voice.spatialBlend == 0, Names[topic] + " embedded English audio routes to its 2D AudioSource");
                float peak = 0; var samples = new float[1024];
                float audioDeadline = Time.realtimeSinceStartup + 3;
                while (Time.realtimeSinceStartup < audioDeadline)
                {
                    view.voice.GetOutputData(samples, 0);
                    foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                    yield return null;
                }
                Check(peak > .0001f && peak < 1, Names[topic] + " actual decoded narration PCM is audible and unclipped (peak " + peak.ToString("F5") + ")");
                Capture((topic + 1) + "-" + Names[topic] + "-video");
                foreach (var text in view.GetComponentsInChildren<TMP_Text>())
                    Check(!text.isTextOverflowing, Names[topic] + " player text fits: " + text.name);
                yield return Click(view.toggle, Names[topic] + " pause");
                double paused = view.player.time;
                yield return new WaitForSecondsRealtime(.6f);
                Check(!view.player.isPlaying && Math.Abs(view.player.time - paused) < .1, Names[topic] + " pause holds picture and playback time");
                yield return Click(view.toggle, Names[topic] + " play");
                yield return new WaitForSecondsRealtime(.5f);
                Check(view.player.isPlaying && view.player.time > paused + .2, Names[topic] + " play resumes");
                yield return Click(view.replay, Names[topic] + " replay");
                yield return new WaitForSecondsRealtime(.5f);
                Check(view.player.isPlaying && view.player.time < 2, Names[topic] + " replay restarts the same lesson");
                view.player.time = 88;
                yield return Until(() => view.Finished, Names[topic] + " actual end callback", 15);
                long last = view.player.frame;
                yield return new WaitForSecondsRealtime(.6f);
                Check(!view.player.isPlaying && view.player.frame == last && last >= 2690 && Manager.State == "Brief", Names[topic] + " completion holds the final frame and waits for Start");
                Capture((topic + 1) + "-" + Names[topic] + "-complete");
                Check(Manager.Session.elapsed == elapsed && Manager.Session.attempts.Count == 0 && Manager.Session.completed.Count == 0 && Manager.Session.heartsUsed == 0 && Chapter4Store.Read().Count == count, Names[topic] + " introduction does not start assessment time or save an attempt");
                yield return Click(Control("BriefStart"), Names[topic] + " Start mission");
                yield return new WaitForSecondsRealtime(.4f);
                Check(Manager.State == "Explore" && Manager.Session.elapsed > elapsed && !view.player.isPlaying && !view.player.isPrepared && !view.voice.isPlaying, Names[topic] + " Start enters the correct level and stops video/audio");
                Manager.CanvasSaveAndLeave();
                yield return Settled("MainMenu");

                // Return without starting must not create another assessment record.
                yield return Select(topic);
                view = Manager.briefingVideo;
                count = Chapter4Store.Read().Count;
                yield return Until(() => view.IsReady && view.HasFrame || view.Failed, "back test first frame", 30);
                yield return Click(Control("BriefBack"), Names[topic] + " Back to menu");
                if (view != null)
                {
                    Check(!view.IsReady && view.voice.mute, Names[topic] + " Back immediately mutes narration and blocks queued callbacks");
                    yield return new WaitForSecondsRealtime(.2f);
                    Check(!view.player.isPlaying && !view.voice.isPlaying, Names[topic] + " Back stops native video/audio before scene unload");
                }
                yield return Settled("MainMenu");
                Check(Chapter4Store.Read().Count == count, Names[topic] + " Back before Start leaves assessment records unchanged");

                yield return Select(topic);
                view = Manager.briefingVideo;
                yield return Until(() => view.IsReady && view.HasFrame || view.Failed, "early Start frame", 30);
                yield return Click(Control("BriefStart"), Names[topic] + " Start during playback");
                yield return new WaitForSecondsRealtime(.4f);
                Check(Manager.State == "Explore" && !view.IsReady && !view.player.isPlaying && !view.voice.isPlaying && view.voice.mute, Names[topic] + " Start is available during playback and cannot restart late audio");
                Manager.CanvasSaveAndLeave();
                yield return Settled("MainMenu");

                // Existing record resumes continue gameplay rather than replaying an introduction.
                var saved = Chapter4Store.Read().Last(s => s.topic == topic && s.status == "InProgress");
                RecordMissionResume.Request(saved, saved.profile);
                GameManager.ChangeScene(Chapter4Game.Scenes[topic]);
                yield return Settled(Chapter4Game.Scenes[topic]);
                yield return new WaitForSecondsRealtime(.5f);
                view = Manager.briefingVideo;
                Check(Manager.State == "Explore" && Manager.Session.id == saved.id && !view.gameObject.activeInHierarchy && !view.player.isPlaying && !view.player.isPrepared && !view.voice.isPlaying, Names[topic] + " saved run bypasses the introduction without late audio");
                Manager.CanvasSaveAndLeave();
                yield return Settled("MainMenu");
            }
            finished = true;
            File.WriteAllText(Path.Combine(folder, "result.txt"), failures == 0 ? "PASS: three actual menu-to-video-to-Start-to-level routes; local decoded frames and narration PCM; pause/play/replay; final hold; Back; resume; no assessment during video." : "FAILURES: " + failures);
            Application.Quit(failures == 0 ? 0 : 1);
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(240);
            if (finished) yield break;
            File.WriteAllText(Path.Combine(folder, "result.txt"), "FAIL: video QA timeout; inspect player.log and checks.txt.");
            Application.Quit(1);
        }

        void Capture(string name)
        {
            var roots = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.isActiveAndEnabled).ToArray();
            var modes = roots.Select(c => c.renderMode).ToArray(); var cameras = roots.Select(c => c.worldCamera).ToArray(); var distances = roots.Select(c => c.planeDistance).ToArray();
            var transforms = roots.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray(); var layers = transforms.Select(t => t.gameObject.layer).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24); var prior = RenderTexture.active;
            var go = new GameObject("Video QA camera"); var camera = go.AddComponent<Camera>(); camera.enabled = false; Texture2D pixels = null;
            try
            {
                if (Camera.main != null) { var old = Camera.main.targetTexture; Camera.main.targetTexture = target; Camera.main.Render(); Camera.main.targetTexture = old; }
                camera.targetTexture = target; camera.orthographic = true; camera.orthographicSize = Screen.height * .5f; camera.nearClipPlane = .1f; camera.farClipPlane = 10; camera.cullingMask = 1 << 31;
                camera.clearFlags = Camera.main == null ? CameraClearFlags.SolidColor : CameraClearFlags.Depth; camera.backgroundColor = new Color(.035f, .08f, .105f);
                foreach (var t in transforms) t.gameObject.layer = 31;
                foreach (var canvas in roots) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < roots.Length; i++) { roots[i].renderMode = modes[i]; roots[i].worldCamera = cameras[i]; roots[i].planeDistance = distances[i]; }
                for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
                RenderTexture.active = prior; Destroy(go); if (pixels != null) Destroy(pixels); RenderTexture.ReleaseTemporary(target); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
#endif
