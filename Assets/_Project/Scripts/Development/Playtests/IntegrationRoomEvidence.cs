#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CalculusLabEscape
{
    // Observes the normal UI-driven mission playtest. It never changes answers or progress.
    [DefaultExecutionOrder(10000)]
    public sealed class IntegrationRoomEvidence : MonoBehaviour
    {
        string folder;
        readonly HashSet<int> moduleCounts = new HashSet<int>();
        readonly List<string> failures = new List<string>();
        IntegrationRoomPresentation previous;
        bool wasPaused, safeSeen, closedLidSeen, openedLidSeen;
        float previousTime;
        int pausedFrames, frames;
        MaterialPropertyBlock block;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--integration-room-evidence") < 0) return;
            var go = new GameObject("Integration presentation observer"); DontDestroyOnLoad(go);
            go.AddComponent<IntegrationRoomEvidence>().folder = LearningApi.Argument("--qa-output");
        }
        void Fail(string reason) { if (!failures.Contains(reason)) { failures.Add(reason); Debug.LogError(reason); } }
        void LateUpdate()
        {
            var game = GameManager.Instance;
            if (game == null || game.world == null || game.Session == null || game.Topic != 2 || game.State == "Brief" || ScreenTransition.IsBusy) return;
            var presentation = game.world.GetComponent<IntegrationRoomPresentation>();
            if (presentation == null) { Fail("Saved Integration presentation missing at runtime"); return; }
            frames++; moduleCounts.Add(game.ModulesComplete);
            bool paused = game.State == "Paused" || game.CanvasSupportOpen;
            if (previous == presentation && paused && wasPaused)
            { pausedFrames++; if (Mathf.Abs(previousTime - presentation.MotionTime) > .0001f) Fail("Room animation advanced while paused/help was open"); }
            if (!paused)
            {
                if (presentation.CompletedCount != game.ModulesComplete) Fail("Room module count differs from assessed module completion");
                for (int i = 0; i < 4; i++)
                {
                    if (block == null) block = new MaterialPropertyBlock();
                    var lamp = presentation.circuits[i].statusLamps[0]; lamp.GetPropertyBlock(block);
                    Color expected = game.CaseModuleComplete(i) ? presentation.complete : presentation.pending;
                    if (Vector4.Distance(block.GetColor("_Color"), expected) > .001f) Fail("Room status lamp differs from module " + i);
                }
                if (presentation.guideLid != null)
                {
                    float angle = Quaternion.Angle(presentation.guideLid.localRotation, Quaternion.identity);
                    if (angle < 25) { closedLidSeen = true; if (presentation.lidGuide.enabled) Fail("Closed lid exposes underside guide lettering"); }
                    if (angle > 80) { openedLidSeen = true; if (!presentation.lidGuide.enabled) Fail("Opened lid hides its guide"); }
                }
                var shutdown = game.world.GetComponent<CaseShutdownSequence>();
                if (shutdown.Progress >= .91f)
                { safeSeen = true; if (!presentation.PoweredOff || presentation.coreLight.intensity > .001f || presentation.CompletedCount != 4) Fail("Final shutdown fails to extinguish the core and preserve four calibrated modules"); }
            }
            previous = presentation; wasPaused = paused; previousTime = presentation.MotionTime;
        }
        void OnApplicationQuit()
        {
            if (!safeSeen) Fail("Did not observe final safe shutdown");
            if (!closedLidSeen || !openedLidSeen) Fail("Did not observe both physical lid states");
            if (pausedFrames == 0 || moduleCounts.Count < 5) Fail("Incomplete pause/module progression coverage");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "integration-room-result.txt"), failures.Count == 0
                ? "PASS: " + frames + " observed frames; all five module counts; out-of-order completion and save/resume status mapping; " + pausedFrames + " frozen help/pause frames; closed/open lid lettering; core power off and four accepted modules preserved."
                : string.Join("\n", failures));
        }
    }
}
#endif
