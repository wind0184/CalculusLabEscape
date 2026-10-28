#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest:MonoBehaviour
    {
        string folder;int failures;bool finished;
        MissionCanvas View=>MissionCanvas.Active;
        Chapter4Game Game=>View!=null?View.Game:ProductionCanvas.Active?.Game;
        T Control<T>(string name)where T:Component=>View!=null?View.Control<T>(name):ProductionCanvas.Active.Control<T>(name);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--limits-expanded-qa")&&!Environment.GetCommandLineArgs().Contains("--missions-qa")&&!Environment.GetCommandLineArgs().Contains("--bridge-route-qa")&&!Environment.GetCommandLineArgs().Contains("--practice-restart-qa")&&!Environment.GetCommandLineArgs().Contains("--mission-restart-qa")&&!Environment.GetCommandLineArgs().Contains("--record-resume-qa"))return;
            string path=LearningApi.Argument("--qa-output");if(string.IsNullOrEmpty(path))throw new Exception("Missions QA needs --qa-output.");
            Directory.CreateDirectory(path);Chapter4Store.IsolatedFolder=Path.Combine(path,"sessions");
            var go=new GameObject("Mission Canvas QA");DontDestroyOnLoad(go);go.AddComponent<MissionsPlaytest>().folder=path;
            // Hidden QA players must keep synthetic input enabled after losing OS focus.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground=true;if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();if(Mouse.current==null)InputSystem.AddDevice<Mouse>();
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failures++;if(folder!=null)File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool value,string label){if(!value){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),"ASSERT: "+label+"\n");}}
        IEnumerator NextMissionCheck()
        {
            string id=Game.Session.id,metrics=Game.Session.Summary,evidence=JsonUtility.ToJson(Game.Session);int runCount=Chapter4Store.Read().Count;
            string realFolder=Chapter4Store.IsolatedFolder,blocked=Path.Combine(folder,"blocked-save-path");File.WriteAllText(blocked,"QA save failure fixture");
            try
            {
                Chapter4Store.IsolatedFolder=blocked;yield return Click("SummaryNext");
                Check(Game.Topic==1&&Game.State=="Summary"&&Game.Session.id==id&&!string.IsNullOrEmpty(Chapter4Store.LastError),"Failed save keeps the completed bridge and results page open");
                Check(Control<TMP_Text>("SummaryNextHint").text.Contains("Could not save"),"Save failure offers a retry without losing the result");
                yield return Shot("08d-next-save-retry");
            }
            finally{Chapter4Store.IsolatedFolder=realFolder;}
            ProductionCanvas.ResumeId="stale-qa-resume";
            yield return Click("SummaryNext");yield return new WaitForSeconds(.4f);
            Check(Game.Topic==2&&Game.State=="Brief"&&Game.Session.id!=id&&Game.Session.completed.Count==0&&Game.Session.attempts.Count==0&&Game.Session.heartsUsed==0,"Next starts a fresh Integration briefing with no carried-over answers or hearts");
            Check(ProductionCanvas.ResumeId==null&&Game.Session.guideSeen.Count==0,"Next clears stale resume state and provides fresh guidance");
            var saved=Chapter4Store.Read().First(s=>s.id==id);
            Check(saved.status=="Completed"&&saved.Summary==metrics&&JsonUtility.ToJson(saved)==evidence&&Chapter4Store.Read().Count==runCount,"Bridge result and all completed/partial practice remain saved; briefing creates no extra record");
            Check(!Game.CanvasNextMission()&&Game.State=="Brief","Next navigation cannot skip an unfinished mission");
            yield return Shot("08c-next-integration-brief");
        }
        IEnumerator CompletedMissionSelectionCheck()
        {
            string id=Game.Session.id,metrics=Game.Session.Summary;int count=Chapter4Store.Read().Count;
            yield return Click("SummaryNext");yield return new WaitForSeconds(.4f);
            Check(View==null&&Control<Transform>("Topics").gameObject.activeInHierarchy,"Completed Integration returns directly to mission selection");
            var saved=Chapter4Store.Read().First(s=>s.id==id);Check(saved.status=="Completed"&&saved.Summary==metrics&&Chapter4Store.Read().Count==count,"Choosing a mission preserves results and does not create a new attempt");
            yield return Shot("18c-completed-mission-selection");yield return Click("TopicsBack");yield return Click("HomeRecords");
        }
        IEnumerator RescuePresentationCheck()
        {
            float audioDeadline=Time.realtimeSinceStartup+1.5f;
            while(Game.World.CaseAudio.CueCount<3&&Time.realtimeSinceStartup<audioDeadline)yield return null;
            Check(Game.World.CaseAudio.CueCount==3,"After resume, only the three subsequent bridge lifts emit installation audio / actual="+Game.World.CaseAudio.CueCount);
            var sequence=Game.World.GetComponent<BridgeRescueSequence>();Check(sequence!=null,"Rescue rig is loaded from authored scene");
            if(sequence==null)yield break;
            int attempts=Game.Session.attempts.Count,hearts=Game.Session.heartsUsed,accepted=Game.Session.completed.Count;
            yield return Click("Rescue");
            while(Game.State=="Finishing"&&sequence.Progress<.27f)yield return null;
            Check(Game.State=="Finishing"&&Game.Session.status!="Completed","Results wait for the rescue animation");
            Check(sequence.calf.legs.Where((leg,i)=>Quaternion.Angle(leg.localRotation,sequence.calf.legRest[i])>2).Any(),"Calf legs swing while walking");
            yield return Shot("07a-calf-crossing");while(Game.State=="Finishing"&&sequence.Progress<.50f)yield return null;
            Check(Mathf.Abs(Game.World.calf.position.x)<.01f&&Game.World.calf.position.y>=sequence.deckHeight-.001f,"Calf crosses down the centre above the repaired deck");
            yield return Shot("07c-calf-on-deck");while(Game.State=="Finishing"&&sequence.Progress<.89f)yield return null;
            Check(Vector3.Distance(Game.World.calf.position,sequence.route[3].position)<.03f,"Calf reaches the authored reunion point");
            Check(Quaternion.Angle(sequence.mother.root.rotation,sequence.mother.homeRotation)>10,"Mother turns towards the arriving calf");
            Check(Game.State=="Finishing"&&Control<TMP_Text>("FinishingTitle").gameObject.activeInHierarchy&&Control<TMP_Text>("FinishingTitle").text=="CALF REUNITED","Reunion feedback is visibly shown before results");
            Check(sequence.calf.legs.Select((leg,i)=>Quaternion.Angle(leg.localRotation,sequence.calf.legRest[i])).All(angle=>angle<.01f),"Legs settle after arriving");
            yield return Shot("07d-calf-reunited");while(Game.State=="Finishing")yield return null;
            Check(Game.Session.attempts.Count==attempts&&Game.Session.heartsUsed==hearts&&Game.Session.completed.Count==accepted,"Cinematic does not change answers or support usage");
        }
        IEnumerator ShutdownPresentationCheck()
        {
            var sequence=Game.World.GetComponent<CaseShutdownSequence>();Check(sequence!=null,"Authored shutdown settings are loaded");if(sequence==null)yield break;
            int attempts=Game.Session.attempts.Count,accepted=Game.Session.completed.Count,hearts=Game.Session.heartsUsed;
            int cuesBeforeShutdown=Game.World.CaseAudio.CueCount;
            var lamps=Game.World.indicators.Select(r=>r.sharedMaterial).ToArray();
            Check(sequence.deviceStatus.text.Contains("MASTER ARMED")&&sequence.powerBars.All(r=>r.sharedMaterial==sequence.powerOn),"Cover open arms master while device power remains on");
            yield return Click("MasterActivate");while(Game.State=="Finishing"&&sequence.Progress<.12f)yield return null;
            Check(Game.World.CaseAudio.LastCue!="shutdown","Motor shutdown waits for the lever phase");Check(sequence.PowerOffCount==0&&Quaternion.Angle(sequence.lever.localRotation,sequence.leverRest)>10,"Master lever moves before power bars shut down");
            Check(Control<TMP_Text>("ShutdownPhase0").text.Contains("[NOW]"),"Switch phase visibly active");
            while(Game.State=="Finishing"&&sequence.Progress<sequence.soundStartProgress+.015f)yield return null;
            CheckAudio("shutdown");ImportedShutdownAudioCheck();
            while(Game.State=="Finishing"&&sequence.Progress<.47f)yield return null;
            Check(sequence.PowerOffCount==2&&Control<TMP_Text>("FinishingText").text.Contains("2/4 off"),"Physical and UI power counts agree");
            Check(Control<TMP_Text>("ShutdownPhase0").text.Contains("[OK]")&&Control<TMP_Text>("ShutdownPhase1").text.Contains("[NOW]"),"Shutdown advances to device power phase");
            yield return Shot("17a-shutdown-action");while(Game.State=="Finishing"&&sequence.Progress<.78f)yield return null;
            Check(sequence.PowerOffCount==4&&Control<TMP_Text>("FinishingTitle").text=="VERIFYING POWER"&&Game.Session.status=="InProgress","Power off is verified before saving completion");
            yield return Shot("17b-shutdown-verification");while(Game.State=="Finishing"&&sequence.Progress<.915f)yield return null;
            Check(Game.State=="Finishing"&&Control<TMP_Text>("FinishingTitle").gameObject.activeInHierarchy&&Control<TMP_Text>("FinishingTitle").text=="DEVICE DEACTIVATED","Device-safe feedback appears before the results page");
            Check(sequence.deviceStatus.text.Contains("DEVICE SAFE")&&Enumerable.Range(0,3).All(i=>Control<TMP_Text>("ShutdownPhase"+i).text.Contains("[OK]")),"Physical display and all stage labels confirm safe power off");
            Check(Game.World.indicators.Where((r,i)=>r.sharedMaterial!=lamps[i]).Count()==0,"Completed module lamps stay stable during device shutdown");
            Check(Game.World.CaseAudio.CueCount==cuesBeforeShutdown+1&&!Game.World.CaseAudio.GetComponent<AudioSource>().isPlaying,"Shutdown cue plays once and ends before the device-safe confirmation");
            yield return Shot("17c-device-powered-off");while(Game.State=="Finishing")yield return null;
            Check(Game.Session.attempts.Count==attempts&&Game.Session.completed.Count==accepted&&Game.Session.heartsUsed==hearts,"Shutdown does not change mathematical answers or support usage");
        }
        void CheckSuperscript(string name)
        {
            var label=(name=="PowerPreview"||name=="DialPreview")?View.GetComponent<ReportFusionView>().resultState:Control<TMP_Text>(name);label.ForceMeshUpdate();bool raised=false;
            for(int i=0;i<label.textInfo.characterCount-1;i++)
            {
                var x=label.textInfo.characterInfo[i];var exponent=label.textInfo.characterInfo[i+1];
                if(x.character=='x'&&exponent.baseLine>x.baseLine&&exponent.scale<x.scale&&exponent.scale>=x.scale*.65f)raised=true;
            }
            Check(raised&&label.text.Contains("<sup>")&&!label.GetParsedText().Contains("^"),"Actual smaller raised exponent: "+name);
        }
        void CheckPracticeMathLayouts()
        {
            string[] names={"PracticeQuestion","PracticeFeedback","PracticeResults","PracticeOption0Label","PracticeOption1Label","PracticeOption2Label"};
            var originals=names.ToDictionary(n=>n,n=>Control<MathText>(n).Source);
            bool active=Control<Transform>("PracticeResults").gameObject.activeSelf;
            Control<Transform>("PracticeResults").gameObject.SetActive(true);
            try
            {
                foreach(string focus in MissionPractice.Focuses)
                {
                    var questions=MissionPractice.Questions(focus);
                    foreach(var q in questions)
                    {
                        Control<MathText>("PracticeQuestion").SetSource(q.prompt);Control<MathText>("PracticeFeedback").SetSource("REVIEW / "+q.explanation);
                        for(int i=0;i<3;i++)Control<MathText>("PracticeOption"+i+"Label").SetSource("[YOUR CHOICE] "+q.options[i]);
                        Control<MathText>("PracticeResults").SetSource("0 / 3 correct on first submission\n\n"+string.Join("\n\n",questions.Select(question=>"[REVIEW] "+question.prompt+"\n"+question.explanation)));
                        foreach(string name in names){var label=Control<TMP_Text>(name);label.ForceMeshUpdate();Check(!label.isTextOverflowing,"All practice math fits: "+focus+" / "+name);}
                    }
                }
            }
            finally{foreach(string name in names)Control<MathText>(name).SetSource(originals[name]);Control<Transform>("PracticeResults").gameObject.SetActive(active);}
        }
        PointerEventData Pointer(RectTransform rect)
        {return new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};}
        IEnumerator Click(string name)
        {
            // HomeStart now plays the saved entrance Timeline before exposing topic buttons.
            float navigationDeadline=Time.realtimeSinceStartup+30;
            while(ScreenTransition.IsBusy||FindFirstObjectByType<MainMenuEntrance>()?.Busy==true||Game!=null&&Game.PresentationBusy)
            {if(Time.realtimeSinceStartup>navigationDeadline)throw new Exception("Navigation did not settle before "+name);yield return null;}
            if(name!="IntroductionContinue"&&name!="IntroductionSkip"&&View!=null&&Control<Transform>("IntroductionModal").gameObject.activeSelf)yield return IntroductionCheck();
            yield return null;Canvas.ForceUpdateCanvases();var b=Control<Button>(name);Check(b.gameObject.activeInHierarchy&&b.IsInteractable(),"Button available: "+name);
            var data=Pointer((RectTransform)b.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"Unobstructed pointer: "+name);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
            while(ScreenTransition.IsBusy||FindFirstObjectByType<MainMenuEntrance>()?.Busy==true||Game!=null&&Game.PresentationBusy)
            {if(Time.realtimeSinceStartup>navigationDeadline)throw new Exception("Navigation did not settle after "+name);yield return null;}
            if(name.StartsWith("SupportTier",StringComparison.Ordinal))
            {
                int tier=int.Parse(name.Substring("SupportTier".Length));var panel=Control<HintPanelSelection>("SupportModal");
                Check(panel.SelectedTier==tier&&panel.caption.text.Contains(new[]{"BASIC","RULE REMINDER","GUIDED EXAMPLE"}[tier]),"Visible hint caption follows the selected tier");
                for(int i=0;i<3;i++)
                {
                    var button=Control<Button>("SupportTier"+i);var ink=button.GetComponentInChildren<TMP_Text>().color;
                    Check(i==tier?button.GetComponent<Image>().color.grayscale>.35f&&ink.grayscale<.2f:button.GetComponent<Image>().color.grayscale<.2f&&ink.grayscale>.8f,"Only the current hint has the bright fill and contrasting dark text");
                    var border=button.GetComponent<Outline>();if(border!=null)Check(i==tier?border.effectColor.grayscale>.35f:border.effectColor.grayscale<.3f,"The previous hint does not retain a selected border");
                }
            }
            if(Game!=null&&Game.Topic==2&&(name=="DialSubmit"||name=="GaugeSubmit"||name=="CommonC"||name=="SequenceSubmit"))yield return SolveCaseCalibrations();
        }
        IEnumerator Drag(string from,string to)
        {
            yield return null;Canvas.ForceUpdateCanvases();var source=Control<Button>(from);var target=Control<Button>(to);Check(source.IsInteractable()&&target.IsInteractable(),"Drag controls enabled: "+from+" -> "+to);
            int sounds=SceneSoundscape.Active.Count("click");
            var data=Pointer((RectTransform)source.transform);data.pointerDrag=source.gameObject;ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.beginDragHandler);
            data.position=Pointer((RectTransform)target.transform).position;ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.dragHandler);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==target,"Drop target unobstructed: "+to);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.endDragHandler);Check(SceneSoundscape.Active.Count("click")==sounds+2,"Drag start and matching drop each emit one UI cue: "+from+" -> "+to);yield return null;
        }
        IEnumerator Key(Key key,Key? modifier=null)
        {InputSystem.QueueStateEvent(Keyboard.current,modifier.HasValue?new KeyboardState(modifier.Value,key):new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
        IEnumerator FocusInput(string name)
        {
            yield return null;Canvas.ForceUpdateCanvases();var field=Control<TMP_InputField>(name);
            var data=Pointer((RectTransform)field.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<TMP_InputField>()==field,"Input unobstructed: "+name);
            if(hits.Count>0){ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);}
            yield return null;yield return null;
            Check(field.isFocused&&EventSystem.current.currentSelectedGameObject==field.gameObject,"Input focused by pointer: "+name);
        }
        IEnumerator Inspect()
        {
            Check(Game.World.IsAuthored&&FindObjectsByType<Chapter4World>(FindObjectsSortMode.None).Length==1,"Play uses exactly one saved authored world without regenerating geometry");
                // Exercise the real movement/controller/colliders; only yaw is steered by QA.
                float deadline=Time.realtimeSinceStartup+8;
                bool movingCaptured=false;
                while(!Game.CanvasNearStation&&Time.realtimeSinceStartup<deadline)
                {
                    var delta=Game.World.station.position-Game.World.player.transform.position;delta.y=0;
                    Game.World.player.transform.rotation=Quaternion.LookRotation(delta);
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;
                    if(!movingCaptured&&Game.Session.guideMoved&&!Game.CanvasNearStation)
                    {
                        movingCaptured=true;
                        var movingHud=View.GetComponent<ExplorationHUD>();
                        Check(movingHud!=null&&movingHud.IsCompact,"Walking compacts guidance while preserving the world view");
                        Check(Control<TMP_Text>("Interaction state").text=="APPROACH","Distant device does not show a false interaction state");
                        yield return Shot("guide-moving-"+Game.Topic+"-"+Game.Stage);
                    }
                }
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
                Check(Game.CanvasNearStation,"Walked to active bridge device without teleporting: "+Game.Stage+" / position="+Game.World.player.transform.position+" / keyboard enabled="+Keyboard.current.enabled);
                Check(Control<TMP_Text>("WorldPrompt").text.Contains("[E]"),"Nearby bridge device shows its interaction prompt");
                var zones=Game.World.GetComponentsInChildren<MissionGuideZone>(true);
                Check(zones.Length==(Game.Topic==1?3:1)&&zones.All(z=>z.target!=null&&z.GetComponent<BoxCollider>().isTrigger),"Serialized non-solid guide zones exist for the devices");
                var zone=zones.First(z=>z.target==Game.World.station);
                Check(zone.Contains(Game.World.player.transform.position),"Real walk enters active device's guide region");
                if(!Game.Session.guideSkipped)Check(Control<TMP_Text>("WorldGuideTitle").text.Contains("READY TO INSPECT"),"Proximity advances the world guide");
                Check(Game.Session.guideMoved,"Real movement advances the saved navigation introduction");
                var hud=View.GetComponent<ExplorationHUD>();
                Check(hud!=null&&hud.IsCompact&&Control<TMP_Text>("Interaction state").text=="IN RANGE","Nearby device has a compact guide and a highlighted interaction prompt");
                Check(hud.progressSegments.All(segment=>!segment.raycastTarget),"Progress decoration cannot intercept player input");
                yield return Shot("guide-near-"+Game.Topic+"-"+Game.Stage);
                yield return Key(UnityEngine.InputSystem.Key.E);Check(Game.State=="Puzzle","E opens device after walking");
        }
        IEnumerator IntroductionCheck()
        {
            string key=View.onboarding.cards.First(c=>c.title==Control<TMP_Text>("IntroductionTitle").text).key;
            int attempts=Game.Session.attempts.Count,hearts=Game.Session.heartsUsed,completed=Game.Session.completed.Count,support=Game.Session.support.Count;
            float elapsed=Game.Session.elapsed,remaining=Game.CaseRemaining;
            Check(Game.CanvasSupportOpen&&Game.State=="Puzzle","First-use introduction pauses the current device");
            var data=Pointer(Control<RectTransform>("PuzzleWorld"));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()!=Control<Button>("PuzzleWorld"),"Introduction blocks underlying clicks");
            yield return new WaitForSeconds(.15f);yield return Key(UnityEngine.InputSystem.Key.Tab);yield return Key(UnityEngine.InputSystem.Key.Enter);
            Check(Game.Session.elapsed==elapsed&&Game.CaseRemaining==remaining,"Introduction pauses active time and countdown");
            if(Game.Topic==2)Check(Game.World.CaseAudio.IsPaused,"Introduction pauses device audio");
            Check(Game.Session.attempts.Count==attempts&&Game.Session.heartsUsed==hearts&&Game.Session.completed.Count==completed&&Game.Session.support.Count==support,"Introduction adds no assessed answer or charged support");
            if(key=="bridge-power"||key=="bridge-numbers"){string body=Control<TMP_Text>("IntroductionBody").GetParsedText();Check(body.Contains("Next calibration")&&body.Contains("all checks"),"Bridge introduction explains calibration before installation: "+key);Check(View.onboarding.Find(key).instructions==MissionOnboarding.Defaults().First(c=>c.key==key).instructions,"Saved and default introduction wording agree: "+key);}
            yield return Shot("introduction-"+key);
            if(key=="case-sequence")yield return Key(UnityEngine.InputSystem.Key.Escape);else yield return Click("IntroductionContinue");
            Check(!Control<Transform>("IntroductionModal").gameObject.activeSelf&&!Game.CanvasSupportOpen&&Game.Session.guideSeen.Contains(key),"Introduction returns to controls and remembers the viewed page: "+key);
            Check(Chapter4Store.Read().First(s=>s.id==Game.Session.id).guideSeen.Contains(key),"Viewed introduction is written to the actual session save");
        }
        IEnumerator SkipIntroductionCheck()
        {
            int attempts=Game.Session.attempts.Count,hearts=Game.Session.heartsUsed;
            yield return Click("ControlsOpen");yield return Click("IntroductionReplay");
            Check(Control<Transform>("IntroductionModal").gameObject.activeSelf,"An already-viewed introduction can be opened manually");
            yield return Click("IntroductionSkip");
            Check(Game.Session.guideSkipped&&Chapter4Store.Read().First(s=>s.id==Game.Session.id).guideSkipped,"Skip automatic introductions is saved");
            Check(Game.Session.attempts.Count==attempts&&Game.Session.heartsUsed==hearts,"Skipping/replaying an introduction leaves assessment unchanged");
        }
        void Input(string name,string value)
        {
            var field=Control<TMP_InputField>(name);Check(field.IsInteractable()&&field.gameObject.activeInHierarchy,"Input available: "+name);
            bool changed=field.text!=value&&!Game.CanvasAccepted&&!Game.CanvasSupportOpen;
            string evidence=JsonUtility.ToJson(Game.Session);field.text=value;
            if(changed){CheckFeedbackCleared("Typing a changed answer clears feedback: "+name);Check(JsonUtility.ToJson(Game.Session)==evidence,"Editing a draft preserves assessment evidence: "+name);}
        }
        void CheckFeedbackCleared(string label)
        {
            bool clear=string.IsNullOrEmpty(Game.CanvasFeedback)&&string.IsNullOrEmpty(Control<TMP_Text>("Feedback").text);
            Check(clear,label);File.AppendAllText(Path.Combine(folder,"feedback-checks.txt"),(clear?"PASS: ":"FAIL: ")+label+"\n");
        }
        Transform CasePart(string name)=>Game.World.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        void CheckAudio(string cue)
        {
            var audio=Game.World.CaseAudio;var source=audio.GetComponent<AudioSource>();
            Check(audio.LastCue==cue,"Integration sound event: "+cue);
            Check(source.clip!=null&&!source.ignoreListenerVolume&&!source.loop,"Cue obeys master volume and does not loop");
            if(source.clip!=null){var samples=new float[source.clip.samples*source.clip.channels];source.clip.GetData(samples,0);Check(samples.Any(s=>Mathf.Abs(s)>.01f)&&samples.All(s=>Mathf.Abs(s)<1),"Cue has bounded non-silent PCM: "+cue);}
        }
        IEnumerator Repair(string a,string b=null,bool keyboard=false)
        {
            Input("BridgeA",a);if(b!=null)Input("BridgeB",b);int before=Game.Session.attempts.Count;
            if(keyboard)
            {
                yield return FocusInput("BridgeA");yield return Key(UnityEngine.InputSystem.Key.Tab);
                Check(Control<TMP_InputField>(b==null?"BridgeA":"BridgeB").isFocused,"Tab skips hidden fields or moves to the second answer");
                if(b!=null)
                {
                    yield return Key(UnityEngine.InputSystem.Key.Tab,UnityEngine.InputSystem.Key.LeftShift);
                    Check(Control<TMP_InputField>("BridgeA").isFocused,"Shift+Tab returns to the first answer");
                    Check(Game.Session.attempts.Count==before&&Control<TMP_InputField>("BridgeA").text==a&&Control<TMP_InputField>("BridgeB").text==b,"Focus changes preserve values without submitting");
                    yield return Key(UnityEngine.InputSystem.Key.Tab);
                    yield return Shot("05e-keyboard-inputs");
                }
                yield return Key(UnityEngine.InputSystem.Key.Enter);
            }
            else yield return Click("RepairSubmit");
            Check(Game.CanvasAccepted&&Game.Session.attempts.Count==before+1,"Bridge step accepted exactly once: "+Game.Stage);
            if(keyboard)
            {
                yield return Key(UnityEngine.InputSystem.Key.Enter);
                Check(Game.Session.attempts.Count==before+1&&Game.CanvasAccepted,"Repeated Enter neither resubmits nor installs an accepted repair");
            }
            yield return Click("RepairContinue");yield return SolveBridgeExtensions();yield return MachineryCheck("installed stage "+Game.Stage);
        }
        void NextActionCheck(string fragment)
        {
            var label=Control<TMP_Text>("NextAction");label.ForceMeshUpdate();
            Check(label.text.Contains(fragment)&&!label.isTextOverflowing,"Visible next action: "+fragment);
        }
        IEnumerator ControlsGuideCheck(string shot,bool escape=false)
        {
            int attempts=Game.Session.attempts.Count,hearts=Game.Session.heartsUsed,completed=Game.Session.completed.Count;
            yield return Click("ControlsOpen");float elapsed=Game.Session.elapsed,remaining=Game.CaseRemaining;
            Check(Control<Transform>("ControlsModal").gameObject.activeInHierarchy,"Operation help opens");
            var body=Control<TMP_Text>("ControlsBody");body.ForceMeshUpdate();Check(!body.isTextOverflowing&&body.text.Contains("Operation help is free"),"Contextual operation help fits");
            var data=Pointer(Control<RectTransform>("PuzzleWorld"));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()!=Control<Button>("PuzzleWorld"),"Help blocks underlying world button");
            string machineryPose=MachineryPose();yield return new WaitForSeconds(.2f);
            Check(machineryPose==MachineryPose(),"Operation help freezes all repair machinery");
            Check(Game.Session.elapsed==elapsed&&Game.CaseRemaining==remaining,"Operation help pauses active time and countdown");
            if(Game.Topic==2)Check(Game.World.CaseAudio.IsPaused,"Operation help pauses case audio");yield return Key(UnityEngine.InputSystem.Key.Tab);yield return Key(UnityEngine.InputSystem.Key.Enter);Check(Game.Session.attempts.Count==attempts&&!Control<TMP_InputField>("BridgeA").isFocused&&!Control<TMP_InputField>("SequenceValue").isFocused,"Help blocks answer keyboard shortcuts");
            yield return Shot(shot);
            if(escape)yield return Key(UnityEngine.InputSystem.Key.Escape);else yield return Click("ControlsClose");
            Check(!Control<Transform>("ControlsModal").gameObject.activeSelf&&Game.State=="Puzzle","Operation help returns to current device");
            Check(Game.Session.attempts.Count==attempts&&Game.Session.heartsUsed==hearts&&Game.Session.completed.Count==completed,"Operation help is free and preserves assessed progress");
            float resumed=Game.Session.elapsed;yield return new WaitForSeconds(.1f);Check(Game.Session.elapsed>resumed,"Active time resumes after operation help");
        }
        IEnumerator Start()
        {
            StartCoroutine(Watchdog());yield return new WaitForSeconds(1);
            if(Environment.GetCommandLineArgs().Contains("--limits-expanded-qa")){yield return ExpandedLimitsChecks();finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: ten Limits questions, five paired calibrations; correct independent views, help, retries, no skipping or duplicate score, actual walking, mid-calibration save/resume and final portal activation.":"FAILURES: "+failures);Application.Quit(failures==0?0:1);yield break;}
            if(Environment.GetCommandLineArgs().Contains("--bridge-route-qa"))
            {
                yield return BridgeRouteChecks();finished=true;
                File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: real controller walks spawn, three station approaches, west/rear/east/river boundaries; visible rails align with stopping positions; closed rescue gate; active station E and locked station rejection; matching visible tree/rock collision; no falls or teleport; no answer/hearts/progress changes.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);yield break;
            }
            if(Environment.GetCommandLineArgs().Contains("--record-resume-qa"))
            {
                yield return SelectedRecordResumeChecks();finished=true;
                File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: actual partial Bridge/Case runs retained across restart; older selected Case/Bridge restored instead of newest run; IDs/answers/hearts and physical progress preserved; reading records does not write; disappeared/completed/foreign record blocked on fresh read; list refresh; completed record offers review; Limits checkpoint fixture restores same ID/stage; no extra run created.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);yield break;
            }
            if(Environment.GetCommandLineArgs().Contains("--mission-restart-qa"))
            {
                yield return MissionRestartRunChecks();finished=true;
                File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: both missions restart through actual Canvas; cancel/Esc/Enter preserve draft and paused run; underlying controls blocked; timers, pressure and audio paused; save failure retains confirmation and allows retry; old save unchanged; new briefing resets answers, hearts, guidance and physical device; stale resume cleared; old run resumes from Home Continue; no extra briefing record.\n":"FAILURES: "+failures);
                Application.Quit(failures==0?0:1);yield break;
            }
            if(Environment.GetCommandLineArgs().Contains("--practice-restart-qa"))
            {
                yield return RestartPracticeCheck();finished=true;
                File.WriteAllText(Path.Combine(folder,"restart-result.txt"),failures==0?"PASS: new player process; records entry; saved choice and answer lock; remaining questions; separate scores; Esc back to refreshed record; no new mission session.":"FAILURES: "+failures);
                Application.Quit(failures==0?0:1);yield break;
            }
            Check(LearningApi.LocalPlay&&!LearningApi.Authenticated&&!LearningApi.TestBypass,"Normal local mode without authentication or test bypass");
            Check(!Control<Transform>("Login").gameObject.activeInHierarchy,"Login is not the entry page");Check(FindFirstObjectByType<LearningApi>()==null,"No login service is instantiated");
            yield return Shot("01-local-menu");yield return Click("HomeStart");Check(Control<Button>("Mission1").interactable&&Control<Button>("Mission2").interactable,"Both missions available locally");yield return Shot("02-missions");
            yield return Click("Mission1");yield return new WaitForSeconds(.5f);yield return Shot("03-bridge-brief");yield return Click("BriefStart");
            Check(Control<TMP_Text>("WorldPrompt").text.Contains("TURN LEFT"),"Spawn guidance points left towards Power Core");
            yield return Key(UnityEngine.InputSystem.Key.E);Check(Game.State=="Explore","Distant E cannot open device");yield return Shot("03a-find-power-core");Check(Control<TMP_Text>("WorldGuideTitle").text=="GET MOVING","New mission starts with movement introduction");yield return Key(UnityEngine.InputSystem.Key.G);Check(!Control<Transform>("WorldGuide").gameObject.activeSelf,"G hides the world guide");yield return Key(UnityEngine.InputSystem.Key.G);Check(Control<Transform>("WorldGuide").gameObject.activeSelf,"G restores the world guide");yield return Inspect();
            NextActionCheck("Drag the chip");yield return ControlsGuideCheck("03b-bridge-controls",true);yield return Click("BridgeChip");Check(Control<MissionSelectionVisual>("BridgeChip").IsSelected&&Control<TMP_Text>("BridgeChipLabel").text.Contains("[X]"),"Chip click shows a selection marker");yield return Shot("03c-chip-selected");NextActionCheck("Chip selected");yield return Click("CoefficientSlot");NextActionCheck("Adjust the exponent");Check(!Control<MissionSelectionVisual>("BridgeChip").IsSelected,"Installed chip is no longer a pending selection");Check(Control<TMP_Text>("CoefficientSlotLabel").text.Contains("4"),"Coefficient click alternative works");yield return Drag("BridgeChip","CoefficientSlot");yield return Click("RepairSubmit");Check(Game.Session.attempts.Count==1&&!Game.CanvasAccepted&&Game.Session.heartsUsed==0,"Wrong repair preserves progress and hearts");Check(Game.Session.attempts.Last().error=="DIFF_EXPONENT"&&Game.CanvasFeedback.Contains("subtract one"),"Exponent retry targets the wrong control");CheckSuperscript("PowerPreview");yield return Shot("04-power-retry");
            yield return Click("ExponentMinus");CheckFeedbackCleared("Bridge exponent edit clears the old retry");Check(Game.Session.attempts.Count==1&&Game.Session.attempts.Last().feedback.Contains("subtract one")&&Game.Session.completed.Count==0&&Game.Session.heartsUsed==0,"Clearing feedback preserves original retry evidence and progress");yield return Shot("feedback-bridge-edited");yield return Click("RepairSubmit");Check(Game.Stage==0&&Game.CanvasAccepted,"Accepted repair waits for installation");yield return MachineryCheck("accepted answer before installation",false);NextActionCheck("Click Next calibration");yield return Shot("04b-install-reminder");yield return Click("RepairContinue");yield return SolveBridgeExtensions();Check(Game.Stage==1&&Game.BridgeParts==1&&Game.World.station==Game.World.devices[1],"Power regulator installed; converter becomes active");
            yield return ImportedBridgeAudioCheck();yield return new WaitForSeconds(1.1f);Check(Game.World.bridge[0].position.y>.07f&&Game.World.bridge[2].position.y<0,"Only installed bridge sections rise into place");
            Check(Control<TMP_Text>("WorldObjective").text.Contains("Regulator installed"),"World guidance explains installation and next station");yield return MachineryCheck("power online; builder locked",false);yield return MachineryPauseCheck();yield return Shot("04a-regulator-installed");yield return Inspect();
            yield return Click("SupportOpen");float elapsed=Game.Session.elapsed;string supportPose=MachineryPose();yield return new WaitForSeconds(.15f);Check(elapsed==Game.Session.elapsed,"Support pauses active time");Check(supportPose==MachineryPose(),"Support freezes repair machinery");yield return Click("SupportTier0");yield return Click("SupportTier1");yield return Click("SupportTier1");Check(Game.Session.heartsUsed==1,"Repeated visible hint charges once");yield return Shot("05-bridge-support");yield return Click("SupportClose");
            yield return ControlsGuideCheck("05d-number-controls");yield return Repair("1/2",keyboard:true);var installedMaterial=Game.World.bridge[0].GetComponent<Renderer>().sharedMaterial;Input("BridgeA","1/2");Input("BridgeB","1/2");int wrongAttempts=Game.Session.attempts.Count;yield return FocusInput("BridgeB");yield return Key(UnityEngine.InputSystem.Key.Enter);Check(!Game.CanvasAccepted&&Game.Session.attempts.Count==wrongAttempts+1,"Wrong root exponent rejected exactly once with Enter");yield return Key(UnityEngine.InputSystem.Key.Enter);Check(Game.Session.attempts.Count==wrongAttempts+1,"Repeated Enter does not duplicate an incorrect attempt without selecting an input again");Check(Game.CanvasFeedback.Contains("coefficient is correct")&&Game.Session.attempts.Last().feedback.Contains("Subtract one"),"Fractional-power feedback preserves the correct coefficient and is stored");CheckSuperscript("TaskPrompt");Check(Control<TMP_InputField>("BridgeB").text=="1/2"&&!Control<TMP_InputField>("BridgeB").richText,"Fraction input remains literal");yield return Shot("05c-specific-exponent-feedback");Check(Game.World.bridge[0].GetComponent<Renderer>().sharedMaterial==installedMaterial,"Wrong answer preserves the visual state of installed bridge sections");yield return Repair("1/2","-1/2",true);
            yield return SkipIntroductionCheck();string bridgeId=Game.Session.id;yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);Check(Game.Stage==3&&Game.Session.id==bridgeId&&Game.Session.heartsUsed==1,"Bridge save/resume preserves accepted steps and hearts");Check(Game.Session.guideSkipped&&Game.Session.guideSeen.Contains("bridge-power")&&Game.Session.guideSeen.Contains("bridge-numbers")&&!Control<Transform>("WorldGuide").gameObject.activeSelf,"Resume restores skipped guides and viewed introductions");yield return Key(UnityEngine.InputSystem.Key.Escape);yield return Click("GuidePreference");yield return Click("PauseResume");Check(!Game.Session.guideSkipped,"Pause setting re-enables automatic guides");
            ImportedBridgeResumeCheck();
            var locks=Game.World.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Converter lock")).OrderBy(r=>r.name).ToArray();Check(locks.Length==3&&locks[0].transform.position.y>locks[2].transform.position.y&&locks[1].sharedMaterial==locks[0].sharedMaterial,"Resume restores two accepted converter locks");yield return MachineryCheck("two channels restored without replaying installation",false);yield return Shot("05a-resumed-guidance");yield return Inspect();yield return Repair("-18","-7");Check(Game.World.station==Game.World.devices[2],"Bridge Builder becomes active");yield return new WaitForSeconds(1.1f);yield return MachineryCheck("bridge lift finished",false);yield return Shot("05b-component-installed");yield return Inspect();
            yield return Repair("5","4");yield return Repair("-6","-4");int attempts=Game.Session.attempts.Count;yield return Click("RepairSubmit");Check(Game.Session.attempts.Count==attempts,"Blank bridge answer is not an attempt");yield return FocusInput("BridgeA");yield return Key(UnityEngine.InputSystem.Key.Enter);Check(Game.Session.attempts.Count==attempts&&!Game.CanvasAccepted,"Blank Enter is not a scored attempt");yield return Repair("0");
            yield return ControlsGuideCheck("06a-choice-controls");yield return Click("BridgeChoice1");yield return Click("RepairSubmit");Check(!Game.CanvasAccepted,"Incorrect assembled anchor rejected");yield return Click("BridgeChoice0");yield return Shot("06-anchor");yield return Click("RepairSubmit");yield return Click("RepairContinue");yield return SolveBridgeExtensions();yield return Inspect();
            yield return Repair("-2");yield return Repair("-2");yield return Click("BridgeChoice0");yield return Click("RepairSubmit");yield return Click("RepairContinue");yield return SolveBridgeExtensions();Check(Game.Ready&&Game.BridgeParts==3&&Game.Session.completed.Count==18,"All 18 steps and three bridge components complete");NextActionCheck("Open crossing");yield return ControlsGuideCheck("07b-rescue-controls");yield return Shot("07-crossing-ready");yield return RescuePresentationCheck();Check(Game.Session.status=="Completed"&&Game.State=="Summary"&&Game.Session.attempts.Count==23,"Rescue animation completes and includes five retries across guided and independent tasks");Check(Control<TMP_Text>("SkillReview").text.Contains("Power Rule: 11/11 accepted; 8 first try; 3 retries"),"Bridge review groups actual attempts");Check(Control<TMP_Text>("SkillReview").text.Contains("subtracting one"),"Bridge practice focus matches repeated exponent errors");yield return Shot("08-bridge-summary");yield return PracticeCheck("derivatives",new[]{0,0,1},"08b");yield return NextMissionCheck();
            yield return Shot("09-case-brief");yield return Click("BriefStart");yield return Shot("09a-case-room");yield return Inspect();
            NextActionCheck("Open Exponent dial");yield return ControlsGuideCheck("10a-case-controls",true);Check(!Control<Button>("Module1").interactable&&!Control<Button>("MasterCover").interactable,"Other modules and master cover initially locked");CheckAudio("case");Game.World.LiftMasterCover();Check(!Game.World.MasterCoverOpen,"Physical cover is locked before all modules are complete");yield return new WaitForSeconds(.5f);yield return Shot("10-case-locked");yield return Click("ManualOpen");float remaining=Game.CaseRemaining;var heldLid=CasePart("Hinged case lid").rotation;yield return new WaitForSeconds(.2f);Check(Game.CaseRemaining==remaining&&Game.World.CaseAudio.IsPaused&&Quaternion.Angle(heldLid,CasePart("Hinged case lid").rotation)<.01f,"Manual pauses countdown, mechanical animation and audio");yield return Shot("11-manual");yield return Click("ManualClose");
            yield return Click("Module0");yield return ControlsGuideCheck("12b-dial-controls");var oldNeedle=CasePart("Exponent needle").rotation;yield return Click("DialExponentPlus");for(int i=0;i<4;i++)yield return Click("DivisorPlus");yield return new WaitForSeconds(.5f);Check(Quaternion.Angle(oldNeedle,CasePart("Exponent needle").rotation)>20,"World dial follows the draft exponent");yield return Click("DialSubmit");Check(!Game.MissionDone("I_DIAL"),"Missing C keeps exponent module incomplete");Check(Game.Session.attempts.Last().error=="MISSING_C"&&Game.CanvasFeedback.Contains("Both dials are correct"),"Dial feedback isolates missing C");yield return Shot("12a-missing-c-feedback");CheckAudio("retry");yield return Click("InsertC");CheckFeedbackCleared("Integration constant edit clears the old retry");CheckSuperscript("DialPreview");yield return Shot("12-exponent-dial");yield return Click("DialSubmit");CheckAudio("complete");Check(Control<Transform>("CaseOverview").gameObject.activeInHierarchy,"Finish dial returns directly to overview");Check(Control<Button>("Module3").interactable,"Guided module unlocks free module order");
            yield return Click("Module3");yield return ControlsGuideCheck("13a-gauge-controls");yield return Click("GaugeSubmit");Check(!Game.MissionDone("I_GAUGE"),"Insufficient release rejected");for(int i=0;i<4;i++)yield return Click("ReleasePlus");CheckFeedbackCleared("Pressure edit clears the old retry");yield return new WaitForSeconds(.4f);var unsafeNeedle=CasePart("Pressure needle").rotation;yield return Click("GaugeSubmit");Check(Game.ModulesComplete==2&&Game.World.indicators[3].sharedMaterial==Game.World.indicators[0].sharedMaterial&&Game.World.indicators[1].sharedMaterial!=Game.World.indicators[0].sharedMaterial,"Out-of-order module completion lights the correct device");yield return new WaitForSeconds(.5f);Check(Quaternion.Angle(unsafeNeedle,CasePart("Pressure needle").rotation)>20,"Accepted pressure release moves the physical gauge to safe");yield return Shot("13-pressure-safe");CheckAudio("release");
            string caseId=Game.Session.id;remaining=Game.CaseRemaining;yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);Check(Game.Session.id==caseId&&Game.ModulesComplete==2&&Mathf.Abs(Game.CaseRemaining-remaining)<2,"Case resume preserves solved modules and countdown");Check(Game.World.CaseAudio.CueCount==0,"Resume restores accepted work silently");yield return Inspect();yield return Click("Module0");Check(Control<MathText>("DialPreview").Source=="x^5 / 5 + C","Resumed completed dial shows its accepted values");NextActionCheck("Module stable");yield return Click("CaseBack");yield return Click("Module2");NextActionCheck("Place a card in slot 1");yield return ControlsGuideCheck("14a-sequence-controls");
            yield return Click("SupportOpen");yield return Click("SupportTier1");yield return Click("SupportTier2");yield return Click("SupportClose");yield return Click("SupportOpen");yield return Click("SupportTier1");Check(Game.Session.heartsUsed==3&&!Control<Button>("SupportTier2").interactable,"Three optional stronger supports consume three hearts");yield return Click("SupportTier0");yield return Click("SupportClose");
            yield return Drag("SequenceCard1","SequenceSlot0");Check(Control<MissionSelectionVisual>("SequenceCard1").IsSelected,"Dragging a card retains the same selection as clicking");yield return Shot("14b-card-selected");Input("SequenceValue","1/2");yield return Click("SequenceSubmit");Check(!Game.MissionDone("I_SEQ_0"),"Wrong procedure order rejected");
            for(int i=0;i<3;i++)yield return Drag("SequenceCard"+i,"SequenceSlot"+i);CheckFeedbackCleared("Placing a new procedure clears the old retry");yield return Click("SequenceCard3");yield return Click("SequenceSlot3");NextActionCheck("Slot 1 ready");
            foreach(string value in new[]{"1/2","3/2","2/3","C"}){Input("SequenceValue",value);int priorAttempts=Game.Session.attempts.Count,priorCompleted=Game.Session.completed.Count;yield return FocusInput("SequenceValue");yield return Key(value=="C"?UnityEngine.InputSystem.Key.NumpadEnter:UnityEngine.InputSystem.Key.Enter);Check(Game.Session.attempts.Count==priorAttempts+1&&Game.Session.completed.Count==priorCompleted+1,"Enter checks exactly one sequence step: "+value);Check(Control<TMP_InputField>("SequenceValue").text=="","Accepted sequence input clears for the next value");}
            yield return SolveCaseCalibrations();Check(Game.MissionDone("I_SEQ_3")&&!Control<Button>("SequenceSlot0").interactable,"Accepted procedure cards remain locked");yield return new WaitForSeconds(.5f);Check(Enumerable.Range(0,4).All(i=>CasePart("Procedure card "+i).position.y>1.90f),"Four accepted cards rise into their physical slots");yield return Shot("14-sequence-stable");Check(Control<Transform>("CaseOverview").gameObject.activeInHierarchy,"Finish sequence returns directly to overview");
            typeof(Chapter4Game).GetField("caseElapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Game,600f);yield return null;Check(Game.Session.safetyMode,"Countdown enters Safety Mode without failure");
            yield return Click("Module1");yield return ControlsGuideCheck("15a-wire-controls");float priorVolume=AudioListener.volume;AudioListener.volume=0;yield return Drag("WireSource0","WireTarget3");Check(!Game.MissionDone("I_W0")&&AudioListener.volume==0,"Wrong wire remains retryable with master volume muted");CheckAudio("retry");AudioListener.volume=priorVolume;yield return Drag("WireSource0","WireTarget0");CheckAudio("connect");yield return Drag("WireSource1","WireTarget1");
            yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);Check(Game.World.CaseAudio.CueCount==0,"Partial wire restore does not replay success sounds");yield return Inspect();yield return Click("Module1");Check(CasePart("Accepted wire 0").GetComponent<Renderer>().sharedMaterial==CasePart("Accepted wire 1").GetComponent<Renderer>().sharedMaterial&&CasePart("Accepted wire 0").GetComponent<Renderer>().sharedMaterial!=CasePart("Accepted wire 2").GetComponent<Renderer>().sharedMaterial,"Save/resume restores only the two accepted physical wires");yield return Click("WireSource2");Check(Control<MissionSelectionVisual>("WireSource2").IsSelected&&!Control<MissionSelectionVisual>("WireSource0").IsSelected,"Unaccepted wire source alone is selected");yield return Shot("15b-wire-selected");NextActionCheck("Source 3 selected");yield return Click("WireTarget2");NextActionCheck("Connect one common +C");Check(!Control<MissionSelectionVisual>("WireSource2").IsSelected,"Accepted wire clears its selection marker");
            Check(Game.ModulesComplete==3&&!Control<Button>("WireSource0").interactable,"Correct wires locked; common C still required");yield return new WaitForSeconds(.5f);CheckSuperscript("WireTarget0Label");yield return Shot("15-wires");yield return Click("CommonC");Check(Control<Transform>("CaseOverview").gameObject.activeInHierarchy,"Finish wires returns directly to overview");Check(Game.Ready&&Game.ModulesComplete==4&&Game.Session.status=="InProgress","Four completed modules unlock a separate final action");Game.Activate();Check(Game.State=="Puzzle"&&!Game.World.DeviceDeactivated,"Completion cannot bypass the physical master cover");yield return new WaitForSeconds(.5f);NextActionCheck("Lift master switch cover");yield return Shot("16-master-covered");yield return Click("MasterCover");CheckAudio("cover");yield return new WaitForSeconds(.5f);Check(Game.Session.status=="InProgress"&&Game.World.MasterCoverOpen&&Quaternion.Angle(CasePart("Master switch cover hinge").rotation,Quaternion.identity)>90,"Lifting physical cover does not deactivate device");NextActionCheck("Click Master Deactivation");yield return Shot("17-master-ready");yield return ShutdownPresentationCheck();Check(Game.Session.status=="Completed"&&Game.Session.completed.Count==18&&Game.Session.attempts.Count==23&&Game.World.DeviceDeactivated,"Master Deactivation completes the device after eighteen assessed steps");Check(CasePart("Device power bar 0").GetComponent<Renderer>().sharedMaterial==CasePart("Device power bar 3").GetComponent<Renderer>().sharedMaterial&&Quaternion.Angle(CasePart("Master switch lever").rotation,Quaternion.identity)>60,"Power bars shut down and final switch stays thrown");Check(Control<TMP_Text>("SkillReview").text.Contains("Power Rule & +C: 9/9 accepted; 6 first try; 3 retries")&&Control<TMP_Text>("SkillReview").text.Contains("Definite integral: 3/3 accepted; 2 first try; 1 retry"),"Integration review groups modules and retries correctly");yield return Shot("18-case-summary");yield return PracticeCheck("constant_c",new[]{2,1,1},"18b");
            yield return CompletedMissionSelectionCheck();yield return new WaitForSeconds(.4f);Check(Chapter4Store.Read().Count(s=>s.status=="Completed")==2,"Both mission results persisted locally");yield return Shot("19-local-records");yield return Click("Record0");Check(Control<TMP_Text>("DetailAttempts").text.Contains("THIS RUN")&&Control<TMP_Text>("DetailAttempts").text.Contains("Both dials are correct")&&!Control<TMP_Text>("DetailAttempts").text.Contains("MISSING_C"),"Reloaded records show the review and readable saved feedback");yield return Shot("20-attempt-evidence");Control<ScrollRect>("AttemptScroll").verticalNormalizedPosition=.65f;yield return null;yield return Shot("21-feedback-record-scroll");Check(Control<TMP_Text>("DetailAttempts").text.Contains("FOCUSED PRACTICE")&&Control<TMP_Text>("DetailAttempts").text.Contains("2 / 3 correct on first submission"),"Saved practice is visible separately in learning records");Control<ScrollRect>("AttemptScroll").verticalNormalizedPosition=0;yield return null;yield return Shot("22-practice-history");yield return RecordPracticeCheck();
            Check(FindFirstObjectByType<LearningApi>()==null,"Both full missions require no backend connection");finished=true;
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: direct Bridge-to-Integration transition; failed-save retry stays on results; fresh briefing and cleared resume state; completed Integration returns to topic selection; prior results and practice preserved; authored Integration shutdown; staged lever and power-off; physical/UI counts agree; safe state shown before results; module lamps and assessment records preserved; persisted calf rig; animated legs; centred deck crossing and foot height; mother turns; settled legs; visible reunion before results; unchanged attempts/hearts/completion; movement and device-zone guidance; free first-use introductions; underlying pointer and keyboard blocking; paused active time, countdown and audio; continue/Esc; saved viewed pages; manual replay; saved skip/resume/re-enable; contextual next actions; all operation guides fit; free guide overlay blocks underlying clicks, pauses timers/audio, closes by button/Esc and resumes time; normal local entry; both mission buttons; 18 differentiation steps and seven independent questions; 3 bridge components; bridge walking via Input System with colliders; device directions, distance and proximity; animated installed sections; retry preserves repair visuals; resumed converter locks; chip/card/wire drag raycasts; retry evidence; hints; pause; both save/resume flows; four integration modules in free order; countdown safety mode; physical module actions; silent partial-wire resume; restored dial values; audio PCM, volume and pause; gated physical cover; switch and power-down animation; local records; no login service.\nActual Unity Canvas render-target images. Player yaw steering and countdown acceleration are QA-only. Both missions walk with the real controller; no Integration teleport.\n":"FAILURES: "+failures);Application.Quit(failures==0?0:1);
        }
        IEnumerator PracticeCheck(string focus,int[] choices,string shot)
        {
            string metrics=Game.Session.Summary;float elapsed=Game.Session.elapsed;int attempts=Game.Session.attempts.Count;
            yield return Click("SummaryPractice");Check(Game.Session.practice.Last().focus==focus,"Practice targets the mission's common error");
            if(focus=="derivatives")CheckPracticeMathLayouts();
            Check(!Control<Button>("PracticeSubmit").interactable,"Practice requires an explicit selection");if(focus=="derivatives")CheckSuperscript("PracticeQuestion");yield return Shot(shot+"-question");
            for(int i=0;i<3;i++)
            {
                yield return Click("PracticeOption"+choices[i]);yield return Click("PracticeSubmit");
                Check(!Control<Button>("PracticeOption0").interactable&&!Control<Button>("PracticeSubmit").gameObject.activeInHierarchy,"Submitted answer is locked while explanation is visible");
                if(i==0)
                {
                    yield return Shot(shot+"-explanation");yield return Key(UnityEngine.InputSystem.Key.Escape);
                    Check(!Control<Transform>("PracticeModal").gameObject.activeSelf&&Game.State=="Summary","Escape returns to completed mission results");
                    yield return Click("SummaryPractice");Check(Game.Session.practice.Count==1&&Game.Session.practice[0].answers.Count==1,"Reopening preserves practice and submitted answer");
                }
                yield return Click("PracticeNext");
            }
            var run=Game.Session.practice.Last();Check(run.Complete&&run.position==3&&run.Result=="2 / 3 correct on first submission","Practice review scores original submissions only");
            Check(Game.Session.Summary==metrics&&Game.Session.attempts.Count==attempts&&Game.Session.elapsed==elapsed,"Practice preserves mission score, time and support evidence");
            var saved=Chapter4Store.Read().Single(s=>s.id==Game.Session.id);Check(saved.practice.Last().Complete&&saved.practice.Last().answers[1].response==run.answers[1].response,"Actual disk save retains practice choices");
            yield return Shot(shot+"-review");
            if(focus=="derivatives")
            {
                yield return Click("PracticeRepeat");Check(Game.Session.practice.Count==2&&Game.Session.practice[0].Complete&&Game.Session.practice[1].answers.Count==0,"Repeat creates a separate set and preserves previous results");
                yield return Click("PracticeOption2");yield return Click("PracticeSubmit");yield return Click("PracticeClose");yield return Click("SummaryPractice");
                Check(Game.Session.practice.Count==2&&Game.Session.practice[1].answers.Count==1,"Partially repeated set reopens without losing evidence");
                Check(Chapter4Store.Read().Single(s=>s.id==Game.Session.id).practice[1].answers.Count==1,"Partial practice is saved to disk");
            }
            yield return Click("PracticeClose");
        }
        IEnumerator RecordPracticeCheck()
        {
            var math=Control<MathText>("DetailAttempts");var label=Control<TMP_Text>("DetailAttempts");string original=math.Source;
            math.SetSource("<size=200>x^4</size> + </noparse> sqrt(x)");label.ForceMeshUpdate();
            Check(label.GetParsedText()=="<size=200>x4</size> + </noparse> \u221ax","Learner rich-text tags stay visible instead of changing formatting");
            Check(Enumerable.Range(0,label.textInfo.characterCount).Any(i=>label.textInfo.characterInfo[i].character=='\u221a'&&label.textInfo.characterInfo[i].textElement.unicode==0x221a),"Square root has a real font glyph");
            Check(Enumerable.Range(0,label.textInfo.characterCount).All(i=>label.textInfo.characterInfo[i].pointSize<=label.fontSize*1.41f),"Saved text cannot enlarge record typography");
            math.SetSource(original);label.ForceMeshUpdate();
            Check(math.Source==original&&Chapter4Store.Read().SelectMany(s=>s.attempts).All(a=>a.response==null||!a.response.Contains("<sup>")),"Presentation never writes TMP markup into saved responses");
            var integration=Chapter4Store.Read().First(s=>s.topic==2);string json=JsonUtility.ToJson(integration);
            Check(Control<TMP_Text>("DetailPracticeLabel").text=="View practice review","Completed record offers review");
            yield return Click("DetailPractice");Check(Control<TMP_Text>("PracticeResults").gameObject.activeInHierarchy,"Record opens completed practice review directly");
            yield return Key(UnityEngine.InputSystem.Key.Escape);Check(!Control<Transform>("PracticeModal").gameObject.activeSelf&&Control<Transform>("Detail").gameObject.activeInHierarchy,"Menu Escape closes practice to the same record");
            Check(JsonUtility.ToJson(Chapter4Store.Read().First(s=>s.id==integration.id))==json,"Read-only review does not rewrite mission or practice data");
            yield return Click("DetailBack");yield return Click("Record1");
            Check(Control<TMP_Text>("DetailPracticeLabel").text=="Continue practice / 1 of 3 submitted","Partial practice exposes continuation from another scene");yield return Shot("23-record-continue");
            var bridge=Chapter4Store.Read().First(s=>s.topic==1);string metrics=bridge.Summary;
            yield return Click("DetailPractice");Check(!Control<Button>("PracticeOption0").interactable&&Control<TMP_Text>("PracticeProgress").text.Contains("Question 1"),"Record restores the submitted first question");
            yield return Click("PracticeNext");yield return Click("PracticeOption0");yield return Click("PracticeSubmit");yield return Click("PracticeNext");yield return Click("PracticeOption1");yield return Click("PracticeSubmit");yield return Click("PracticeNext");
            Check(Control<TMP_Text>("PracticeResults").text.StartsWith("3 / 3 correct"),"Continued bridge practice combines old and new responses");yield return Shot("24-record-practice-complete");yield return Click("PracticeClose");
            bridge=Chapter4Store.Read().First(s=>s.id==bridge.id);Check(bridge.practice.Count==2&&bridge.practice[0].Result.StartsWith("2 / 3")&&bridge.practice[1].Complete&&bridge.Summary==metrics,"Continuation preserves earlier set and mission score");
            Check(Control<TMP_Text>("DetailPracticeLabel").text=="View practice review"&&Control<TMP_Text>("DetailAttempts").text.Contains("3 / 3 correct"),"Closing refreshes the current record immediately");
            yield return Click("DetailBack");yield return Click("Record0");yield return Click("DetailPractice");yield return Click("PracticeRepeat");
            string storeFolder=Chapter4Store.IsolatedFolder;string blocked=Path.Combine(folder,"blocked-practice-store");File.WriteAllText(blocked,"Controlled QA write failure");
            try
            {
                Chapter4Store.IsolatedFolder=blocked;
                yield return Click("PracticeOption0");yield return Click("PracticeSubmit");
                Check(Control<TMP_Text>("PracticeSaveStatus").text.Contains("Could not save"),"Practice exposes a failed disk write");
                yield return Click("PracticeClose");Check(Control<Transform>("PracticeModal").gameObject.activeSelf,"Failed save keeps the answer and panel open");yield return Shot("25a-save-retry");
            }
            finally{Chapter4Store.IsolatedFolder=storeFolder;}
            yield return Click("PracticeClose");Check(!Control<Transform>("PracticeModal").gameObject.activeSelf,"Back retries the write and closes after recovery");
            Check(Chapter4Store.Read().First(s=>s.topic==2).practice.Last().answers.Count==1,"Partial integration practice saved for the next player process");yield return Shot("25-ready-for-restart");
        }
        IEnumerator RestartPracticeCheck()
        {
            var saved=Chapter4Store.Read().First(s=>s.topic==2);string metrics=saved.Summary;
            Check(saved.practice.Count==2&&saved.practice.Last().answers.Count==1,"Fresh process reads the prior partial set");
            yield return Click("HomeRecords");yield return Click("Record0");yield return Click("DetailPractice");
            Check(Control<TMP_Text>("PracticeProgress").text.Contains("Question 1")&&!Control<Button>("PracticeOption0").interactable&&Control<TMP_Text>("PracticeOption0Label").text.Contains("YOUR CHOICE"),"Fresh process restores feedback and prevents changing submitted choice");yield return Shot("26-restarted-practice");
            yield return Click("PracticeNext");Check(!Control<Button>("PracticeSubmit").interactable,"New question requires a fresh selection after restart");
            yield return Click("PracticeOption0");yield return Click("PracticeSubmit");yield return Click("PracticeNext");yield return Click("PracticeOption1");yield return Click("PracticeSubmit");yield return Click("PracticeNext");
            Check(Control<TMP_Text>("PracticeResults").text.StartsWith("2 / 3 correct"),"Restarted practice includes the earlier incorrect answer");yield return Shot("27-restarted-review");
            yield return Key(UnityEngine.InputSystem.Key.Escape);Check(Control<Transform>("Detail").gameObject.activeInHierarchy&&!Control<Transform>("PracticeModal").gameObject.activeSelf,"Esc returns to learning record");
            saved=Chapter4Store.Read().First(s=>s.id==saved.id);Check(saved.Summary==metrics&&saved.practice.Count==2&&saved.practice.All(r=>r.Complete)&&Chapter4Store.Read().Count==2,"Restart continuation changes neither mission result nor session count");
            Check(Control<TMP_Text>("DetailPracticeLabel").text=="View practice review","Record updates action after resumed completion");
        }
        IEnumerator SaveReturn(){yield return Click("PuzzlePause");yield return Click("PauseLeave");yield return Click("LeaveConfirm");yield return new WaitForSeconds(.4f);}
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(180);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: timeout; inspect errors.txt.");Application.Quit(1);}}
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var root=View!=null?View.gameObject:ProductionCanvas.Active.gameObject;
            if(View!=null&&View.Game.World.topic==1)
            {
                var signals=View.Game.World.GetComponentsInChildren<RepairStationIndicator>(true);
                Check(signals.Length==3&&signals.All(s=>s.source!=null&&s.lights.Length>0&&s.lights.All(r=>r!=null&&r.sharedMaterial==s.source.sharedMaterial)),"Three machinery signals follow mission state after input/resume: "+name);
            }
            foreach(var label in root.GetComponentsInChildren<TMP_Text>())if(label.isTextOverflowing&&label.name!="DetailAttempts"&&label.GetComponentInParent<TMP_InputField>()==null)Check(false,"Text overflow in "+name+": "+label.name);
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var go=new GameObject("QA capture");var camera=go.AddComponent<Camera>();camera.enabled=false;
            var canvas=root.GetComponent<Canvas>();var mode=canvas.renderMode;var prior=canvas.worldCamera;float distance=canvas.planeDistance;var transforms=root.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();var active=RenderTexture.active;Texture2D pixels=null;
            try{
                if(View!=null){var preview=View.Game.World.GetComponentsInChildren<Camera>().FirstOrDefault(c=>c.targetTexture==View.Game.World.Preview);if(preview!=null)preview.Render();}
                if(View==null)foreach(var preview in FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.targetTexture!=null))preview.Render();
                if(Camera.main!=null){var old=Camera.main.targetTexture;Camera.main.targetTexture=target;Camera.main.Render();Camera.main.targetTexture=old;}
                camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=Screen.height*.5f;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.clearFlags=Camera.main!=null?CameraClearFlags.Depth:CameraClearFlags.SolidColor;camera.cullingMask=1<<31;
                foreach(var t in transforms)t.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();Check(pixels.GetPixels32().Where((_,i)=>i%97==0).Select(c=>(int)c.r*65536+c.g*256+c.b).Distinct().Take(32).Count()>=32,"Visible rendered content: "+name);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }finally{canvas.renderMode=mode;canvas.worldCamera=prior;canvas.planeDistance=distance;for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];RenderTexture.active=active;Destroy(go);if(pixels!=null)Destroy(pixels);RenderTexture.ReleaseTemporary(target);Canvas.ForceUpdateCanvases();}
            yield return null;
        }
    }
}
#endif
