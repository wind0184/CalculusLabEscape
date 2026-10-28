using System.Collections.Generic;
using UnityEngine;

namespace CalculusLabEscape
{
    // Imported mission clips are saved in the scene. Other short cues retain their original synthesis.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MissionAudio : MonoBehaviour
    {
        [Header("Imported mission sounds")]
        [SerializeField] AudioClip bridgeInstallClip,shutdownClip;
        [SerializeField,Range(0,1)] float importedClipVolume=.85f;
        public MissionSoundBank soundBank;
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        AudioSource source;
        bool paused;
        public string LastCue {get;private set;}
        public int CueCount {get;private set;}
        public bool IsPaused=>paused;
        public AudioClip BridgeInstallClip=>bridgeInstallClip;
        public AudioClip ShutdownClip=>shutdownClip;
        void Awake()
        {
            source=GetComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.32f;
            clips.Add("tick",Tone("Dial click",.055f,650,480));
            clips.Add("case",Tone("Case latch",.20f,220,330));
            clips.Add("cover",Tone("Cover latch",.15f,330,500));
            clips.Add("connect",Tone("Connection accepted",.22f,520,780));
            clips.Add("step",Tone("Procedure slot locked",.16f,380,570));
            clips.Add("release",Release());
            clips.Add("complete",Tone("Module stable",.42f,440,660,880));
            clips.Add("retry",Tone("Try again",.22f,230,190));
            clips.Add("shutdown",shutdownClip!=null?shutdownClip:Tone("Power down",1.15f,600,440,330,220,110));
            if(bridgeInstallClip!=null)clips.Add("bridge",bridgeInstallClip);
        }
        static AudioClip Tone(string name,float duration,params float[] notes)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*duration)];
            double phase=0;float segment=duration/notes.Length;
            for(int i=0;i<samples.Length;i++)
            {
                float time=(float)i/rate;int note=Mathf.Min(notes.Length-1,(int)(time/segment));float local=time-note*segment;
                float envelope=Mathf.Min(1,local/.008f)*Mathf.Clamp01((segment-local)/.025f);
                phase+=2*System.Math.PI*notes[note]/rate;
                samples[i]=(float)System.Math.Sin(phase)*envelope*.42f;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip Release()
        {
            const int rate=22050;var samples=new float[rate/2];var noise=new System.Random(47);float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/samples.Length;filtered=Mathf.Lerp(filtered,(float)noise.NextDouble()*2-1,.2f);
                samples[i]=filtered*.65f*Mathf.Min(1,t*40)*(1-t)*(1-t);
            }
            var clip=AudioClip.Create("Pressure release",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        public void Play(string cue)
        {
            if(paused||!clips.TryGetValue(cue,out var clip))return;
            // Dial taps must not cut off a result or shutdown cue.
            if(cue=="tick"&&source.isPlaying)return;
            string id=cue=="tick"?"click":cue=="case"||cue=="cover"?"switch":cue=="release"?"pressure":cue=="connect"||cue=="step"||cue=="complete"?"correct":cue;
            var imported=soundBank!=null?soundBank.Find(id):null;
            if(imported!=null){clip=imported.clip;source.outputAudioMixerGroup=imported.output;}
            LastCue=cue;CueCount++;source.volume=clip==bridgeInstallClip||clip==shutdownClip?importedClipVolume:imported!=null?imported.volume:.32f;source.clip=clip;source.Play();
        }
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;if(value)source.Pause();else source.UnPause();
        }
        void OnDestroy(){if(source!=null)source.Stop();foreach(var clip in clips.Values)if(clip!=bridgeInstallClip&&clip!=shutdownClip)Destroy(clip);}
    }
}
