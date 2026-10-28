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

namespace CalculusLabEscape
{
    // Native output-history snapshots stay in the QA folder; no OS or microphone capture.
    public sealed class UIAudioOverlapPlaytest:MonoBehaviour
    {
        string folder;int failures;bool finished;float lastPeak,singlePeak;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--ui-overlap-qa"))return;
            string path=LearningApi.Argument("--qa-output");Directory.CreateDirectory(path);Chapter4Store.IsolatedFolder=Path.Combine(path,"sessions");
            var root=new GameObject("UI sound overlap QA");DontDestroyOnLoad(root);root.AddComponent<UIAudioOverlapPlaytest>().folder=path;Application.runInBackground=true;
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool value,string text){File.AppendAllText(Path.Combine(folder,"checks.txt"),(value?"PASS: ":"FAIL: ")+text+"\n");if(!value)failures++;}
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(120);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: timeout");Application.Quit(2);}}
        void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        IEnumerator Capture(string name,Action<int> input,int frames=60)
        {
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))source.Stop();
            var samples=new List<float>();var window=new float[4096];
            // The first call allocates Unity's history buffer; let it fill before measuring.
            AudioListener.GetOutputData(window,0);yield return new WaitForSecondsRealtime(.12f);
            for(int frame=0;frame<frames;frame++){
                input(frame);yield return new WaitForSecondsRealtime(1f/60);
                AudioListener.GetOutputData(window,0);samples.AddRange(window);
            }
            lastPeak=samples.Count==0?0:samples.Max(value=>Mathf.Abs(value));
            Check(samples.Count>0&&lastPeak>1e-5f,"Recorded non-silent output: "+name);
            File.AppendAllText(Path.Combine(folder,"levels.csv"),name+","+lastPeak.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+samples.Count+"\n");
            // Consecutive 4096-sample history windows may overlap; this is analysis data, not a WAV.
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,name+".windows.f32"))))foreach(float value in samples)writer.Write(value);
        }
        IEnumerator Start()
        {
            StartCoroutine(Watchdog());yield return new WaitForSecondsRealtime(.8f);File.WriteAllText(Path.Combine(folder,"levels.csv"),"Scenario,Unclamped peak,Samples\n");
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return new WaitForSecondsRealtime(.5f);
            var audio=SceneSoundscape.Active;
            yield return Capture("single-click",frame=>{if(frame==0)audio.Play("click");});singlePeak=lastPeak;
            yield return Capture("six-same-frame-clicks",frame=>{if(frame==0)for(int n=0;n<6;n++)audio.Play("click");});
            bool baseline=Environment.GetCommandLineArgs().Contains("--capture-ui-baseline");
            File.AppendAllText(Path.Combine(folder,"checks.txt"),"MEASURED: six-click/single-click peak ratio="+(lastPeak/singlePeak)+"\n");
            if(!baseline)Check(lastPeak<=singlePeak*1.05f,"Six immediate clicks do not stack UI voices or increase peak level");
            yield return Capture("rapid-clicks",frame=>{if(frame<18&&frame%3==0)audio.Play("click");});
            File.AppendAllText(Path.Combine(folder,"checks.txt"),"MEASURED: rapid-click/single-click peak ratio="+(lastPeak/singlePeak)+"\n");
            if(!baseline)Check(lastPeak<=singlePeak*1.05f,"Rapid clicks keep the same single-click peak");
            foreach(string cue in new[]{"hint","correct","retry","portal","completion"}){
                var other=cue=="hint"?audio.hint:cue=="correct"||cue=="retry"?audio.feedback:cue=="completion"?audio.completion:audio.effects;
                int before=audio.Count(cue);bool concurrent=false;
                yield return Capture(cue+"-and-rapid-clicks",frame=>{
                    if(frame==0)audio.Play(cue);
                    if(frame<18&&frame%3==0)audio.Play("click");
                    if(frame==3)concurrent=other.isPlaying&&audio.ui.isPlaying&&other.clip==audio.bank.Find(cue).clip;
                });
                Check(concurrent&&audio.Count(cue)==before+1,"Rapid clicks preserve the separate "+cue+" sound without replaying it");
            }
            for(int topic=0;topic<3;topic++){
                yield return SceneManager.LoadSceneAsync(Chapter4Game.Scenes[topic]);yield return new WaitForSecondsRealtime(.6f);
                var game=GameManager.Instance;game.StartSession();var chat=game.robotDialogue;var controller=game.world.player.GetComponent<CharacterController>();
                if(controller!=null)controller.enabled=false;game.world.player.transform.position=chat.guide.position+Vector3.back*1.9f;game.world.player.transform.rotation=Quaternion.LookRotation(Vector3.forward);if(controller!=null)controller.enabled=true;
                yield return null;Check(chat.CanTalk,"Robot audio fixture can open actual conversation: "+topic);
                audio=SceneSoundscape.Active;int robots=audio.Count("robot"),clicks=audio.Count("click"),attempts=game.Session.attempts.Count;
                var button=game.world.topic==0?game.menuOrLimitsUI.Control<Button>("DialogueTopic0"):game.missionUI.Control<Button>("DialogueTopic0");
                yield return Capture("mission-"+(topic+1)+"-robot-and-topic-clicks",frame=>{if(frame==0)chat.Open();if(frame>=3&&frame<=18&&frame%3==0)Click(button);},120);
                Check(audio.Count("robot")==robots+1&&audio.Count("click")==clicks+6,"What should I do emits six UI cues and does not replay robot greeting: "+topic);
                Check(game.Session.attempts.Count==attempts&&game.Session.support.Count==0&&chat.IsOpen,"Rapid topic clicks preserve conversation and assessment state: "+topic);
                Check(!audio.robot.isPlaying,"Original robot greeting finishes normally: "+topic);chat.Close();
                if(topic==2){
                    game.OpenPuzzle();yield return null;
                    var intro=game.missionUI.Control<Button>("IntroductionContinue");if(intro.gameObject.activeInHierarchy)Click(intro);
                    Click(game.missionUI.Control<Button>("Module0"));yield return null;
                    if(intro.gameObject.activeInHierarchy)Click(intro);yield return null;
                    int uiBefore=audio.Count("click"),physicalBefore=game.World.CaseAudio.CueCount,attemptsBefore=game.Session.attempts.Count;
                    foreach(string name in new[]{"DialExponentPlus","DialExponentMinus","DivisorPlus","DivisorMinus"}){
                        var control=game.missionUI.Control<Button>(name);Check(control.IsActive()&&control.IsInteractable(),"Native dial control is available: "+name);Click(control);yield return null;
                    }
                    Check(audio.Count("click")==uiBefore+4&&game.World.CaseAudio.CueCount==physicalBefore,"Four actual dial edits emit one shared UI sound each and no duplicate physical tick");
                    Check(game.Session.attempts.Count==attemptsBefore,"Dial edits do not submit mathematical attempts");
                }
            }
            string scope=baseline?"Baseline captured; no single-voice peak assertions.":"Single/burst/rapid clicks retain one UI voice.";
            finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: "+scope+" Native output-history measurements; concurrent Hint, feedback and effects; actual What should I do clicks during all three robot greetings; no repeated greeting or assessment changes.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);
        }
    }
}
#endif
