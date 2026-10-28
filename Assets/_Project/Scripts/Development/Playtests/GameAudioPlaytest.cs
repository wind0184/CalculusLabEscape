#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed class GameAudioPlaytest:MonoBehaviour
    {
        string folder;int failures;bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--game-audio-qa"))return;
            string path=LearningApi.Argument("--qa-output");Directory.CreateDirectory(path);Chapter4Store.IsolatedFolder=Path.Combine(path,"sessions");
            var root=new GameObject("Audio QA");DontDestroyOnLoad(root);root.AddComponent<GameAudioPlaytest>().folder=path;Application.runInBackground=true;
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool value,string label){File.AppendAllText(Path.Combine(folder,"checks.txt"),(value?"PASS: ":"FAIL: ")+label+"\n");if(!value)failures++;}
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(90);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: timed out");Application.Quit(2);}}
        float OutputRms(){var data=new float[4096];AudioListener.GetOutputData(data,0);return Mathf.Sqrt(data.Sum(x=>x*x)/data.Length);}
        IEnumerator Start()
        {
            StartCoroutine(Watchdog());yield return new WaitForSecondsRealtime(1);
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                yield return SceneManager.LoadSceneAsync(name);yield return new WaitForSecondsRealtime(.6f);
                var audio=SceneSoundscape.Active;var game=GameManager.Instance;
                Check(audio!=null&&audio.manager==game&&audio.bank.cues.Length==12,name+": saved soundscape and 12 imported cues");
                if(audio==null)continue;
                foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b.name=="SupportOpen"||b.name=="PuzzleSupport"||b.name=="ManualOpen"))Check(button.GetComponent<ButtonAudio>().cue=="hint",name+": support/manual opening uses the Hint mixer track");
                foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))Check(source.outputAudioMixerGroup!=null&&source.outputAudioMixerGroup.audioMixer==audio.bank.mixer&&!source.ignoreListenerVolume,name+": routed "+source.name);
                foreach(string cue in new[]{"click","hint","correct","retry","robot","portal","completion"}){
                    int before=audio.Count(cue);audio.Play(cue);yield return null;
                    var source=cue=="hint"?audio.hint:cue=="click"?audio.ui:cue=="correct"||cue=="retry"?audio.feedback:cue=="robot"?audio.robot:cue=="completion"?audio.completion:audio.effects;
                    Check(audio.Count(cue)==before+1&&source.clip==audio.bank.Find(cue).clip&&source.outputAudioMixerGroup==audio.bank.Find(cue).output,name+": "+cue+" uses its imported clip and separate mixer channel");
                }
                foreach(var source in new[]{audio.ui,audio.hint,audio.feedback,audio.robot,audio.effects,audio.completion})source.Stop();
                if(name=="MainMenu"){
                    var mixer=audio.bank.mixer;
                    foreach(var group in mixer.FindMatchingGroups("Master")){
                        string parameter=group.name+"Volume";float original;Check(mixer.GetFloat(parameter,out original),"Exposed fader "+parameter);
                        Check(mixer.SetFloat(parameter,-18)&&mixer.GetFloat(parameter,out var changed)&&Mathf.Abs(changed+18)<.01f,"Adjustable fader "+parameter);mixer.SetFloat(parameter,original);
                    }
                    var correct=audio.bank.Find("correct");audio.feedback.clip=correct.clip;audio.feedback.outputAudioMixerGroup=correct.output;audio.feedback.loop=true;audio.feedback.Play();
                    // Unity allocates its output-history buffer on the first read.
                    OutputRms();yield return new WaitForSecondsRealtime(.3f);float audible=OutputRms();mixer.GetFloat("CorrectVolume",out float gain);mixer.SetFloat("CorrectVolume",-80);
                    yield return new WaitForSecondsRealtime(.3f);float muted=OutputRms();mixer.SetFloat("CorrectVolume",gain);audio.feedback.Stop();audio.feedback.loop=false;
                    File.AppendAllText(Path.Combine(folder,"checks.txt"),"MEASURED: mixed output RMS normal="+audible+", Correct fader -80 dB="+muted+", listeners="+FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length+", listenerVolume="+AudioListener.volume+", listenerPause="+AudioListener.pause+", driver="+AudioSettings.driverCapabilities+"\n");
                    Check(audible>1e-6f&&muted<audible*.03f,"Mixer fader attenuates actual rendered audio output");
                    var button=ProductionCanvas.Active.Control<Button>("HomeStart");int clicks=audio.Count("click");button.onClick.Invoke();yield return null;
                    Check(audio.Count("click")==clicks+1,"Saved button audio fires before page navigation hides its button");
                    continue;
                }
                game.StartSession();yield return new WaitForSecondsRealtime(.2f);
                Check(game.Topic!=1||audio.ambience.isPlaying&&audio.ambience.loop&&audio.ambience.clip==audio.bank.Find("river").clip,"River loops only during bridge gameplay");
                int good=audio.Count("correct"),retry=audio.Count("retry");
                game.ValidateStep("QA_FORMAT","abc",false,"INPUT_FORMAT","Format check");Check(audio.Count("retry")==retry,"Invalid input has no mathematical retry sound");
                game.ValidateStep("QA_RETRY","0",false,"QA_WRONG","Mathematical check");Check(audio.Count("retry")==retry+1,"Mathematical retry emits one soft feedback cue");
                game.ValidateStep("QA_CORRECT","1",true,"","Accepted");Check(audio.Count("correct")==good+1,"Accepted mathematical response emits one correct cue");
                audio.effects.clip=audio.bank.Find("portal").clip;audio.effects.Play();yield return new WaitForSecondsRealtime(.1f);game.SetState("Paused");yield return new WaitForSecondsRealtime(.08f);
                int sample=audio.effects.timeSamples;yield return new WaitForSecondsRealtime(.15f);
                Check(!audio.effects.isPlaying&&audio.effects.timeSamples==sample&&(game.Topic!=1||!audio.ambience.isPlaying),"Pause holds effects and river without restarting");
                game.SetState("Explore");yield return new WaitForSecondsRealtime(.1f);Check(audio.effects.isPlaying&&audio.effects.timeSamples>sample,"Resume continues effects from saved playback position");
                game.OpenPuzzle();game.CanvasOpenSupport(true);yield return new WaitForSecondsRealtime(.1f);
                int hints=audio.Count("hint");audio.Play("hint");Check(audio.Count("hint")==hints+1&&audio.hint.isPlaying,"Hint cue remains audible while support pauses gameplay audio");
                game.CanvasOpenSupport(false);game.SetState("Explore");
                if(game.World.CaseAudio!=null){
                    yield return null;game.World.CaseAudio.GetComponent<AudioSource>().Stop();foreach(string cue in game.Topic==1?new[]{"bridge"}:new[]{"tick","case","cover","connect","retry","release","shutdown"}){
                        game.World.CaseAudio.Play(cue);yield return null;var source=game.World.CaseAudio.GetComponent<AudioSource>();
                        string id=cue=="tick"?"click":cue=="case"||cue=="cover"?"switch":cue=="connect"?"correct":cue=="release"?"pressure":cue;
                        Check(source.clip==audio.bank.Find(id).clip&&source.outputAudioMixerGroup==audio.bank.Find(id).output,"Physical "+cue+" uses saved clip and correct mixer group");source.Stop();
                    }
                }
                game.SetState("Finishing");yield return null;int complete=audio.Count("completion");game.SetState("Summary");yield return null;game.SetState("Summary");
                Check(audio.Count("completion")==complete+1,"Completion fanfare occurs once on finishing-to-results transition");
            }
            finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: all saved audio routes, exposed mixer controls, rendered fader attenuation, buttons, feedback, invalid-input silence, pause/resume, river, hint, physical clips and completion.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);
        }
    }
}
#endif
