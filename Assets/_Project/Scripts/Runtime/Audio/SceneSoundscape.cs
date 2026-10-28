using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace CalculusLabEscape
{
    // All sources, clips and mixer routes are authored in the saved scene.
    [DefaultExecutionOrder(-600),DisallowMultipleComponent]
    public sealed class SceneSoundscape : MonoBehaviour
    {
        public static SceneSoundscape Active {get;private set;}
        public GameManager manager;
        public MissionSoundBank bank;
        public AudioSource ui,hint,feedback,robot,ambience,effects,completion;
        readonly Dictionary<string,int> counts=new Dictionary<string,int>();
        bool paused,riverPaused,riverStarted;
        bool quitScheduled;
        public int Count(string cue)=>counts.TryGetValue(cue,out int count)?count:0;
        void Awake(){Active=this;}
        public void Play(string id)
        {
            var cue=bank!=null?bank.Find(id):null;if(cue==null||cue.clip==null)return;
            bool isUI=id=="click"||id=="hint";
            if(!isUI&&manager!=null&&(manager.State=="Paused"||manager.CanvasSupportOpen))return;
            var target=id=="hint"?hint:id=="click"?ui:id=="correct"||id=="retry"?feedback:id=="robot"?robot:id=="completion"?completion:effects;
            if(target==null)return;
            target.outputAudioMixerGroup=cue.output;target.clip=cue.clip;
            // One short click voice: rapid input restarts it instead of stacking pitched tails.
            // Hint has its own saved source, so neither source interrupts other categories.
            if(id=="click"){target.volume=cue.volume;target.Play();}
            else if(isUI){target.volume=1;target.PlayOneShot(cue.clip,cue.volume);}
            else{target.volume=cue.volume;target.Play();}
            counts[id]=Count(id)+1;
        }
        public void StateChanged(string previous,string next)
        {
            if(manager==null)return;
            if(next=="Finishing"&&previous!="Finishing"&&manager.Topic!=1)Play(manager.Topic==0?"portal":"switch");
            if(previous=="Finishing"&&next=="Summary")Play("completion");
        }
        public void QuitAfterButtonSound(){if(!quitScheduled)StartCoroutine(QuitWhenClickFinishes());}
        IEnumerator QuitWhenClickFinishes()
        {
            quitScheduled=true;
            yield return new WaitForSecondsRealtime((bank.Find("click")?.clip.length??.18f)+.06f);
            Application.Quit();
        }
        void LateUpdate()
        {
            if(manager==null)return;
            bool hold=manager.State=="Paused"||manager.CanvasSupportOpen;
            if(hold!=paused){paused=hold;foreach(var source in new[]{feedback,effects,completion})if(source!=null){if(hold)source.Pause();else source.UnPause();}}
            bool river=manager.Session!=null&&manager.Topic==1&&(manager.State=="Explore"||manager.State=="Puzzle"||manager.State=="Finishing"||manager.State=="Paused");
            if(ambience==null)return;
            if(!river){if(riverStarted)ambience.Stop();riverStarted=riverPaused=false;return;}
            if(!riverStarted){ambience.Play();riverStarted=true;}
            if(hold!=riverPaused){riverPaused=hold;if(hold)ambience.Pause();else ambience.UnPause();}
            var cue=bank.Find("river");
            float gain=manager.PresentationBusy?.3f:manager.State=="Puzzle"?.45f:1;
            ambience.volume=Mathf.MoveTowards(ambience.volume,cue.volume*gain,Time.unscaledDeltaTime*.5f);
        }
        void OnDestroy(){if(Active==this)Active=null;}
    }
}
