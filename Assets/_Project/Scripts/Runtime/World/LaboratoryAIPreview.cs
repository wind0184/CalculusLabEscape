using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    [DisallowMultipleComponent]
    public sealed class LaboratoryAIPreview : MonoBehaviour
    {
        public static readonly string[] MotionNames = { "Idle", "Welcome", "Explain", "Think", "Encourage", "Remind", "Cruise", "TravelForward", "TurnLeft" };
        public GameObject model;
        public AnimationClip[] clips = new AnimationClip[9];
        public Light hoverLight;
        [Range(0, 8)] public int motionIndex = 2;
        [Min(0)] public float previewTime = 1.8f;
        public bool playing = true;
        public bool cycleAll;
        public TMP_Text motionLabel;
        public TMP_Text timeLabel;
        public TMP_Text stateLabel;
        public Color CurrentColour { get; private set; }
        readonly List<TintTarget> tints = new List<TintTarget>();
        MaterialPropertyBlock properties;

        struct TintTarget
        {
            public Renderer renderer;
            public int slot;
            public float normalStrength, greenStrength;
        }

        void OnEnable() { RebuildTintTargets(); Sample(motionIndex, previewTime); }
        void Update()
        {
            if (Application.isPlaying && playing) Advance(Time.unscaledDeltaTime);
        }
        public void PlayIndex(int index)
        {
            playing = true;
            Sample(index, 0);
        }
        public void TogglePlayback() { playing = !playing; }
        public void ToggleCycle() { cycleAll = !cycleAll; }
        public void Advance(float seconds)
        {
            if (!playing || clips == null || clips.Length == 0) return;
            int index = Mathf.Clamp(motionIndex, 0, clips.Length - 1);
            var clip = clips[index];
            if (clip == null || clip.length <= 0) return;
            float next = previewTime + Mathf.Max(0, seconds);
            if (next >= clip.length)
            {
                next %= clip.length;
                if (cycleAll) index = (index + 1) % clips.Length;
            }
            Sample(index, next);
        }
        public void RebuildTintTargets()
        {
            tints.Clear();
            if (model == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material == null) continue;
                    float normal = 0, green = 0;
                    switch (material.name.Replace('_', ' '))
                    {
                        case "AI Cyan emission": normal = 1.7f; green = 1.5f; break;
                        case "AI Soft cyan": normal = .9f; green = 1f; break;
                        case "AI Hover bright cyan": normal = 2.8f; green = 2f; break;
                        case "AI Hover blue radial fade": normal = 1f; green = 1f; break;
                    }
                    if (normal > 0) tints.Add(new TintTarget { renderer = renderer, slot = slot, normalStrength = normal, greenStrength = green });
                }
            }
        }
        public void Sample(int index, float seconds)
        {
            if (model == null || clips == null || clips.Length == 0 || !gameObject.scene.IsValid()) return;
            motionIndex = Mathf.Clamp(index, 0, clips.Length - 1);
            var clip = clips[motionIndex];
            if (clip == null) return;
            previewTime = Mathf.Clamp(seconds, 0, clip.length);
            clip.SampleAnimation(model, previewTime);
            float phase = clip.length > 0 ? previewTime / clip.length : 0;
            int state = phase >= .13f && phase <= .84f ? (motionIndex == 4 ? 1 : motionIndex == 5 ? 2 : 0) : 0;
            CurrentColour = state == 1 ? new Color(.002f, 1f, .008f) : state == 2 ? new Color(1f, .43f, .025f) : new Color(.004f, .52f, 1f);
            if (tints.Count == 0) RebuildTintTargets();
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (var target in tints)
            {
                if (target.renderer == null) continue;
                target.renderer.GetPropertyBlock(properties, target.slot);
                properties.SetColor("_Color", CurrentColour * (state == 1 ? target.greenStrength : target.normalStrength));
                target.renderer.SetPropertyBlock(properties, target.slot);
            }
            if (hoverLight != null) hoverLight.color = CurrentColour;
            if (motionLabel != null) motionLabel.text = MotionNames[motionIndex].ToUpperInvariant();
            if (timeLabel != null) timeLabel.text = previewTime.ToString("0.00") + " / " + clip.length.ToString("0.00") + " s";
            if (stateLabel != null) stateLabel.text = state == 1 ? "ENCOURAGEMENT / GREEN" : state == 2 ? "GENTLE REMINDER / AMBER" : "LABORATORY GUIDE / CYAN";
        }
        public Transform Part(string name)
        {
            if (model == null) return null;
            foreach (var part in model.GetComponentsInChildren<Transform>(true)) if (part.name == name) return part;
            return null;
        }
    }
}
