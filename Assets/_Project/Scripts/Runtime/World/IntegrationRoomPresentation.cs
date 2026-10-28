using System;
using UnityEngine;

namespace CalculusLabEscape
{
    /// <summary>Animates saved room art from the existing Integration session; owns no gameplay progress.</summary>
    [DisallowMultipleComponent]
    public sealed class IntegrationRoomPresentation : MonoBehaviour
    {
        [Serializable]
        public sealed class Circuit
        {
            public Renderer[] statusLamps;
            public Renderer[] powerLamps;
            public TextMesh label;
        }

        public Chapter4World world;
        public Circuit[] circuits = new Circuit[4];
        public Transform[] flywheels;
        public Renderer[] coreBands;
        public Light coreLight;
        public TextMesh roomStatus;
        public Renderer approachMarker;
        public Transform guideLid;
        public Renderer lidGuide;
        public Color pending = new Color(.9f, .60f, .25f);
        public Color complete = new Color(.28f, .94f, .69f);
        public Color powered = new Color(.24f, .75f, .87f);
        public Color off = new Color(.075f, .13f, .16f);
        public float MotionTime { get; private set; }
        public int CompletedCount { get; private set; }
        public bool PoweredOff { get; private set; }
        readonly bool[] accepted = new bool[4];
        MaterialPropertyBlock block;
        static readonly int ColourId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly string[] Names = { "01 / POWER RULE", "02 / TERM MATCHING", "03 / PROCEDURE", "04 / ACCUMULATION" };

        void Start() { Sample(accepted, 0, 0); if (approachMarker != null) approachMarker.enabled = true; if (lidGuide != null) lidGuide.enabled = false; }

        void LateUpdate()
        {
            var game = world != null ? world.Game : null;
            if (game == null || game.Session == null) return;
            if (approachMarker != null) approachMarker.enabled = game.State == "Explore";
            if (lidGuide != null && guideLid != null) lidGuide.enabled = Quaternion.Angle(guideLid.localRotation, Quaternion.identity) > 55;
            if (game.State == "Paused" || game.State == "Brief" || game.CanvasSupportOpen || game.PresentationBusy || ScreenTransition.IsBusy) return;
            if (game.State != "Summary") MotionTime += Time.deltaTime;
            for (int i = 0; i < 4; i++) accepted[i] = game.CaseModuleComplete(i);
            var sequence = world.GetComponent<CaseShutdownSequence>();
            Sample(accepted, sequence != null ? sequence.Progress : 0, MotionTime);
        }

        void Tint(Renderer renderer, Color colour, float emission)
        {
            if (renderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColourId, colour);
            block.SetColor(EmissionId, colour * emission);
            renderer.SetPropertyBlock(block);
        }

        // This is also used by the Editor's deterministic, no-Play previews.
        public void Sample(bool[] modules, float shutdown, float seconds)
        {
            CompletedCount = 0;
            PoweredOff = shutdown >= .91f;
            for (int i = 0; i < circuits.Length; i++)
            {
                var circuit = circuits[i];
                if (circuit == null) continue;
                bool done = modules != null && i < modules.Length && modules[i];
                if (done) CompletedCount++;
                Color status = done ? complete : pending;
                foreach (var lamp in circuit.statusLamps) Tint(lamp, status, done ? 1.1f : .55f);
                bool cut = shutdown >= .30f + i * .14f;
                foreach (var lamp in circuit.powerLamps) Tint(lamp, cut ? off : powered, cut ? 0 : .8f);
                if (circuit.label != null)
                {
                    string text = Names[i] + "\n" + (done ? "CALIBRATED" : "AWAITING CALIBRATION");
                    if (circuit.label.text != text) circuit.label.text = text;
                    circuit.label.color = done ? complete : new Color(.73f, .84f, .86f);
                }
            }
            float energy = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.24f, .86f, shutdown));
            float breathing = .94f + .06f * Mathf.Sin(seconds * 1.6f);
            foreach (var band in coreBands) Tint(band, Color.Lerp(off, powered, energy), energy * breathing * 1.25f);
            if (coreLight != null) coreLight.intensity = energy * breathing * 2.2f;
            for (int i = 0; i < flywheels.Length; i++)
                if (flywheels[i] != null) flywheels[i].localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 1 : -1) * seconds * 10f, 0);
            if (roomStatus != null)
            {
                string status = PoweredOff ? "DEVICE SAFE\nPOWER OFF" : shutdown > 0 ? "CONTROLLED SHUTDOWN\nVERIFYING POWER" :
                    CompletedCount == 4 ? "4 / 4 MODULES CALIBRATED\nMASTER CONTROL READY" : CompletedCount + " / 4 MODULES CALIBRATED\nCONTAINMENT CONTROL";
                if (roomStatus.text != status) roomStatus.text = status;
                roomStatus.color = PoweredOff || CompletedCount == 4 ? complete : new Color(.72f, .88f, .91f);
            }
        }
    }
}
