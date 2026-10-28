#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Isolated event audit: gameplay listeners are temporarily disabled, then restored.
    public sealed class ButtonSoundCoveragePlaytest:MonoBehaviour
    {
        string folder;int failures,total;bool finished,expectQuit;float quitRequested,minimumExitPlayback;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--button-sound-qa")&&!Environment.GetCommandLineArgs().Contains("--button-exit-qa"))return;
            string folder=LearningApi.Argument("--qa-output");Directory.CreateDirectory(folder);Chapter4Store.IsolatedFolder=Path.Combine(folder,"sessions");
            var root=new GameObject("Button audio event QA");DontDestroyOnLoad(root);root.AddComponent<ButtonSoundCoveragePlaytest>().folder=folder;Application.runInBackground=true;
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void OnApplicationQuit(){if(expectQuit){Check(Time.realtimeSinceStartup-quitRequested>=minimumExitPlayback,"Exit waits until the button sound can finish");finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: native Exit/Confirm buttons; actual click PCM; quit delayed until playback finishes.\n":"FAILURES: "+failures);}}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool value,string label){File.AppendAllText(Path.Combine(folder,"checks.txt"),(value?"PASS: ":"FAIL: ")+label+"\n");if(!value)failures++;}
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(90);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: timeout");Application.Quit(2);}}
        float Rms(){var samples=new float[4096];AudioListener.GetOutputData(samples,0);return Mathf.Sqrt(samples.Sum(s=>s*s)/samples.Length);}
        IEnumerator Start()
        {
            StartCoroutine(Watchdog());yield return new WaitForSecondsRealtime(.8f);
            if(Environment.GetCommandLineArgs().Contains("--button-exit-qa")){
                yield return SceneManager.LoadSceneAsync("MainMenu");yield return new WaitForSecondsRealtime(.6f);
                var ui=ProductionCanvas.Active;var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(ui.Control<Button>("HomeExit").gameObject,pointer,ExecuteEvents.pointerClickHandler);
                Rms();yield return new WaitForSecondsRealtime(.2f);expectQuit=true;minimumExitPlayback=SceneSoundscape.Active.bank.Find("click").clip.length;quitRequested=Time.realtimeSinceStartup;int clicks=SceneSoundscape.Active.Count("click");
                ExecuteEvents.Execute(ui.Control<Button>("ExitConfirm").gameObject,pointer,ExecuteEvents.pointerClickHandler);Check(SceneSoundscape.Active.Count("click")==clicks+1,"Exit Confirm emits its click before requesting quit");
                yield return new WaitForSecondsRealtime(Mathf.Min(.06f,minimumExitPlayback*.65f));Check(Rms()>1e-5f,"Exit Confirm click is rendered before application closes");
                yield return new WaitForSecondsRealtime(.5f);Check(false,"Exit request should have closed the player");Application.Quit(1);yield break;
            }
            File.WriteAllText(Path.Combine(folder,"button-events.csv"),"Scene,Button,Pointer sound,Keyboard sound,Disabled silent\n");
            foreach(string scene in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                yield return SceneManager.LoadSceneAsync(scene);yield return new WaitForSecondsRealtime(.6f);
                var audio=SceneSoundscape.Active;var game=GameManager.Instance;
                foreach(var presenter in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(presenter is GameManager||presenter is ProductionCanvas||presenter is MissionCanvas||presenter is MissionBriefingVideo||presenter is RobotDialogue||presenter is ReportFusionView)presenter.enabled=false;
                foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))source.Stop();
                string progress=game.Session!=null?JsonUtility.ToJson(game.Session):"";
                var groups=FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include,FindObjectsSortMode.None);var previousGroups=groups.Select(g=>g.interactable).ToArray();foreach(var group in groups)group.interactable=true;
                var buttons=FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(b=>b.name).ToArray();
                foreach(var button in buttons){
                    var sound=button.GetComponent<ButtonAudio>();Check(sound!=null,scene+"/"+button.name+": saved audio component");if(sound==null)continue;
                    var parents=new List<GameObject>();var prior=new List<bool>();for(var parent=button.transform;parent!=null;parent=parent.parent){parents.Add(parent.gameObject);prior.Add(parent.gameObject.activeSelf);}
                    for(int i=parents.Count-1;i>=0;i--)parents[i].SetActive(true);
                    bool interactable=button.interactable;button.interactable=true;
                    var states=new UnityEventCallState[button.onClick.GetPersistentEventCount()];int call=-1;
                    for(int i=0;i<states.Length;i++){states[i]=button.onClick.GetPersistentListenerState(i);if(button.onClick.GetPersistentTarget(i)==sound&&button.onClick.GetPersistentMethodName(i)=="Play")call=i;else button.onClick.SetPersistentListenerState(i,UnityEventCallState.Off);}
                    int before=audio.Count(sound.cue);var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);bool pointerSound=audio.Count(sound.cue)==before+1;
                    ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);bool keyboardSound=audio.Count(sound.cue)==before+2;
                    button.interactable=false;ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);bool disabled=audio.Count(sound.cue)==before+2;
                    Check(call==0&&pointerSound&&keyboardSound&&disabled,scene+"/"+button.name+": pointer, keyboard, rapid consecutive clicks, disabled silence");
                    File.AppendAllText(Path.Combine(folder,"button-events.csv"),$"{scene},{button.name},{pointerSound},{keyboardSound},{disabled}\n");total++;
                    audio.ui.Stop();audio.hint.Stop();
                    button.interactable=interactable;for(int i=0;i<states.Length;i++)button.onClick.SetPersistentListenerState(i,states[i]);
                    for(int i=0;i<parents.Count;i++)parents[i].SetActive(prior[i]);
                }
                for(int i=0;i<groups.Length;i++)groups[i].interactable=previousGroups[i];
                Check((game.Session!=null?JsonUtility.ToJson(game.Session):"")==progress,scene+": event sweep does not submit answers or change assessment records");
                int clicks=audio.Count("click"),hints=audio.Count("hint");audio.Play("hint");audio.Play("click");audio.Play("click");
                Check(audio.Count("click")==clicks+2&&audio.Count("hint")==hints+1&&audio.ui.isPlaying&&audio.hint.isPlaying&&audio.ui.outputAudioMixerGroup!=audio.hint.outputAudioMixerGroup,"Two immediate click requests are handled while Hint keeps its separate source and mixer route");
                audio.ui.Stop();audio.hint.Stop();
                // Native event playback rendered through the scene's actual AudioListener.
                var candidate=buttons.First(b=>b.GetComponent<ButtonAudio>().cue=="click");var enabledParents=new List<GameObject>();
                for(var parent=candidate.transform;parent!=null;parent=parent.parent)if(!parent.gameObject.activeSelf){parent.gameObject.SetActive(true);enabledParents.Add(parent.gameObject);}
                candidate.interactable=true;foreach(var group in candidate.GetComponentsInParent<CanvasGroup>(true))group.interactable=true;
                for(int i=0;i<candidate.onClick.GetPersistentEventCount();i++)if(candidate.onClick.GetPersistentTarget(i)!=candidate.GetComponent<ButtonAudio>())candidate.onClick.SetPersistentListenerState(i,UnityEventCallState.Off);
                Rms();yield return new WaitForSecondsRealtime(.12f);ExecuteEvents.Execute(candidate.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return new WaitForSecondsRealtime(.06f);float rms=Rms();Check(rms>1e-5f,scene+": actual button event renders audible PCM; RMS="+rms);
                foreach(var node in enabledParents)node.SetActive(false);
            }
            finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: "+total+" saved buttons; native pointer and keyboard sound events; rapid clicks; disabled silence; no assessment changes; actual rendered button audio in all four scenes; separate Hint playback.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);
        }
    }
}
#endif
