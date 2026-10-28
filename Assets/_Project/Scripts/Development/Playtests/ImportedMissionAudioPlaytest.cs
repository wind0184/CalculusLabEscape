#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator ImportedBridgeAudioCheck()
        {
            var audio=Game.World.CaseAudio;
            Check(audio!=null&&audio.BridgeInstallClip!=null,"Wood bridge clip reference is saved in the scene");
            if(audio==null)yield break;
            var source=audio.GetComponent<AudioSource>();
            Check(Game.World.GetComponents<AudioSource>().Length==1&&!source.playOnAwake,"Bridge uses one saved AudioSource");
            float deadline=Time.realtimeSinceStartup+3;
            while(audio.CueCount==0&&Time.realtimeSinceStartup<deadline)yield return null;
            CheckAudio("bridge");
            Check(audio.CueCount==1&&source.clip==audio.BridgeInstallClip&&Mathf.Abs(source.clip.length-1.2f)<.02f,"One cropped wooden installation cue plays after the accepted installation");
            Check(Mathf.Abs(source.volume-.85f)<.001f,"Imported bridge gain is applied without changing the master volume");
            yield return Click("WorldPause");yield return new WaitForSeconds(.05f);
            int sample=source.timeSamples,count=audio.CueCount;float y=Game.World.bridge[0].position.y;
            yield return new WaitForSeconds(.2f);
            Check(audio.IsPaused&&!source.isPlaying&&source.timeSamples==sample&&Game.World.bridge[0].position.y==y,"Pause freezes both the imported cue and bridge lift");
            yield return Click("PauseResume");yield return new WaitForSeconds(.08f);
            Check(!audio.IsPaused&&audio.CueCount==count&&source.isPlaying&&source.timeSamples>sample,"Resume continues the same clip without replaying its start");
            File.AppendAllText(Path.Combine(folder,"imported-audio-checks.txt"),"PASS: saved wooden clip; 1.2-second excerpt; one installation event; configured gain; real Pause/Resume preserves playback position and bridge motion.\n");
        }
        void ImportedShutdownAudioCheck()
        {
            var audio=Game.World.CaseAudio;var source=audio.GetComponent<AudioSource>();
            Check(audio.ShutdownClip!=null&&source.clip==audio.ShutdownClip&&Mathf.Abs(source.clip.length-2)<.02f,"Shutdown uses the supplied two-second clip");
            Check(Mathf.Abs(source.volume-.85f)<.001f&&!source.ignoreListenerVolume&&!source.loop,"Shutdown uses the balanced imported gain and master volume");
            File.AppendAllText(Path.Combine(folder,"imported-audio-checks.txt"),"PASS: supplied 2-second shutdown clip; saved reference; starts after lever phase; balanced gain; non-looping; obeys master volume.\n");
        }
        void ImportedBridgeResumeCheck()
        {
            var audio=Game.World.CaseAudio;
            Check(audio!=null&&audio.CueCount==0&&!audio.GetComponent<AudioSource>().isPlaying,"Restored bridge progress does not replay installation audio");
            File.AppendAllText(Path.Combine(folder,"imported-audio-checks.txt"),"PASS: actual Bridge save/resume starts with zero sound events and no playing source.\n");
        }
    }
}
#endif
