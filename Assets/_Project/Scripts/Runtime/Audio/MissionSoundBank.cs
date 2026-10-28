using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CalculusLabEscape
{
    [CreateAssetMenu(menuName="Calculus Lab/Mission Sound Bank")]
    public sealed class MissionSoundBank : ScriptableObject
    {
        [Serializable] public sealed class Cue
        {
            public string id;
            public AudioClip clip;
            public AudioMixerGroup output;
            [Range(0,1)] public float volume=.85f;
        }
        public AudioMixer mixer;
        public Cue[] cues=Array.Empty<Cue>();
        public Cue Find(string id)=>Array.Find(cues,c=>c.id==id);
    }
}
