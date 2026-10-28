using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas : MonoBehaviour
    {
        public int topic=1;
        [Header("Editable scene/prefab references")]
        public UIReferenceMap uiReferences;
        public GameManager sceneController;
        public static MissionCanvas Active {get;private set;}
        public Chapter4Game Game {get;private set;}
        readonly Dictionary<string,Transform> nodes=new Dictionary<string,Transform>();
        readonly Dictionary<int,string> hints=new Dictionary<int,string>();
        string priorState="Explore",modal;
        bool caseOverview=true,coefficient,constant,cover;
        int exponent=4,divisor=1,release,choice=-1,selectedWire=-1,selectedCard=-1,lastStage=-1;
        int[] order={-1,-1,-1,-1};
        string summarySession;
        public int DragKind {get;private set;}=-1;
        public int DragValue {get;private set;}=-1;
        public bool HasPowerCoefficient=>coefficient;
        public T Control<T>(string name)where T:Component=>nodes[name].GetComponent<T>();
        void Text(string n,string v)=>MathText.Set(Control<TMP_Text>(n),v);
        void Show(string n,bool value)=>nodes[n].gameObject.SetActive(value);
        string AnswerDraft()=>coefficient+"|"+exponent+"|"+divisor+"|"+constant+"|"+release+"|"+choice+"|"+calibrationChoice;
        void RefreshFeedback()
        {
            Text("Feedback",Game.CanvasFeedback);
            Control<TMP_Text>("Feedback").color=Game.CanvasFeedback.StartsWith("CORRECT")?new Color(.35f,.92f,.59f):new Color(1f,.74f,.33f);
        }
        void Enabled(string n,bool value)=>Control<Button>(n).interactable=value;
        public static void Install(int topic)
        {
            if(ProductionCanvas.LegacyQA)return;
            if(FindFirstObjectByType<MissionCanvas>()==null)
                throw new InvalidOperationException("Save the mission UI prefab in the scene before Play.");
        }
        void Awake()
        {
            if(ProductionCanvas.LegacyQA){gameObject.SetActive(false);return;}
            Active=this;
            if(uiReferences==null)throw new InvalidOperationException("Assign UI References on MissionCanvas before playing.");
            uiReferences.CopyTo(nodes);
            GetComponent<Canvas>().enabled=true;
            foreach(var visual in GetComponentsInChildren<MissionSelectionVisual>(true))visual.SetSelected(false);
            foreach(var entry in nodes)if(entry.Key.EndsWith("Modal",StringComparison.Ordinal)||entry.Key=="DragGhost")entry.Value.gameObject.SetActive(false);
            if(EventSystem.current==null&&(sceneController==null||sceneController.eventSystem==null))new GameObject("UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        }
        void OnDestroy(){if(Active==this)Active=null;}
        void Start()
        {
            if(Game==null&&sceneController!=null)Game=sceneController.Game;
            if(Game==null)Game=FindFirstObjectByType<Chapter4Game>();
            if(Game==null)return;Game.EnableCanvas();
            Control<RawImage>("ScenePreview").texture=Game.World.Preview;
            
            
            SetupPractice();
            SetupMissionRestart();
            SetupOnboarding();
            
            if(RecordMissionResume.RestorePending(Game)){if(Game==null||Game.State!="Explore")return;for(int i=0;i<4;i++)if(Game.MissionDone("I_SEQ_"+i))order[i]=i;}
            else if(!string.IsNullOrEmpty(ProductionCanvas.ResumeId)){var row=Chapter4Store.Read().FirstOrDefault(s=>s.id==ProductionCanvas.ResumeId);ProductionCanvas.ResumeId=null;Game.MissionResume(row);for(int i=0;i<4;i++)if(Game.MissionDone("I_SEQ_"+i))order[i]=i;}
            if(topic==2){if(Game.MissionDone("I_DIAL")){exponent=divisor=5;constant=true;}if(Game.MissionDone("I_GAUGE"))release=4;}
            Refresh();
        }
        public void SetSceneGame(Chapter4Game game)=>Game=game;
        string Value(string name)=>Control<TMP_InputField>(name).text;
        string lastBridgeQuestion;
        void Update(){if(Game!=null){Refresh();RefreshOnboarding();HandleAnswerKeyboard();}}
        void Pause(){if(Game==null||Game.PresentationBusy||ScreenTransition.IsBusy)return;priorState=Game.State;Game.SetState("Paused");}
        void CloseModal(){if(modal!=null)Show(modal+"Modal",false);if(modal=="Restart"){Control<CanvasGroup>("Pause").interactable=true;EventSystem.current?.SetSelectedGameObject(Control<Button>("PauseRestart").gameObject);}modal=null;Game.CanvasOpenSupport(false);}
        public void HandleEscape()
        {
            var reportView=GetComponent<ReportFusionView>();if(reportView!=null&&reportView.ScratchpadOpen){reportView.CloseScratchpad();return;}
            if(modal=="Introduction"){FinishIntroduction(false);return;}
            if(modal=="Practice"){practicePanel.Close();return;}
            if(modal!=null){CloseModal();return;}
            if(Game.State=="Paused")Game.SetState(priorState);else if(Game.State=="Explore"||Game.State=="Puzzle")Pause();
        }
        int SequenceStep(){int n=0;while(n<4&&Game.MissionDone("I_SEQ_"+n))n++;return n;}
        void PlaceCard(int card,int slot)
        {
            if(card<0||Game.MissionDone("I_SEQ_"+slot))return;
            for(int i=0;i<4;i++)if(order[i]==card&&Game.MissionDone("I_SEQ_"+i))return;
            if(order[slot]==card)return;
            Game.CanvasAnswerChanged();
            for(int i=0;i<4;i++)if(order[i]==card)order[i]=-1;order[slot]=card;
        }
        public void BeginSelection(int kind,int value)
        {
            if(DragKind!=kind||DragValue!=value)Game.CanvasAnswerChanged();
            DragKind=kind;DragValue=value;
            if(kind==1)selectedWire=value;
            if(kind==2)selectedCard=value;
        }
        void SelectionVisual(string name,bool selected)=>Control<MissionSelectionVisual>(name).SetSelected(selected);
        public void Drag(int kind,int value,PointerEventData data)
        {
            BeginSelection(kind,value);Show("DragGhost",true);Text("DragLabel",kind==0?"4":kind==1?MissionRules.WireInputs[value]:MissionRules.SequenceCards[value]);
            var frame=Control<RectTransform>("DesignFrame");RectTransformUtility.ScreenPointToLocalPointInRectangle(frame,data.position,data.pressEventCamera,out var point);
            Control<RectTransform>("DragGhost").anchoredPosition=point+new Vector2(15,-15);
        }
        public void EndDrag(){Show("DragGhost",false);DragKind=DragValue=-1;}
        public void Drop(int kind,int target)
        {
            if(Game.State!="Puzzle"||modal!=null||kind!=DragKind)return;
            if(kind==0&&!Game.CanvasAccepted&&!coefficient){coefficient=true;Game.CanvasAnswerChanged();}
            if(kind==1)ConnectWire(DragValue,target);
            if(kind==2)PlaceCard(DragValue,target);
            Refresh();
        }
        void Refresh()
        {
            if(Game==null)return;string state=Game.State;
            var visiblePage=nodes[state=="Paused"?"Pause":state=="Explore"?"World":state];
            foreach(Transform child in nodes["Pages"])child.gameObject.SetActive(child==visiblePage);
            Show("Backdrop",state!="Explore"&&state!="Finishing");
            Text("LiveStatus","Support Hearts "+(3-Game.Session.heartsUsed)+"/3   |   Active time "+TimeSpan.FromSeconds(Game.Session.elapsed).ToString(@"mm\:ss"));
            Text("SaveStatus",Chapter4Store.LastError);
            string progress=topic==1?"Bridge components: "+Game.BridgeParts+"/3  |  Checks: "+Game.Session.completed.Count+"/"+Game.Session.required:"Modules: "+Game.ModulesComplete+"/4";
            string objective=topic==1?(Game.Ready?"Crossing secured. Return to Bridge Builder to rescue the calf.":Game.Stage>=8?"Anchor installed. Stabilise the crossing at Bridge Builder.":Game.Stage>=4?"Component installed. Next: assemble the anchor at Bridge Builder.":Game.Stage>=1?"Regulator installed. Next: unlock the Converter Box.":"Find Power Core to repair the first component. Follow the cyan marker."):"Complete all four modules and deactivate the device.";
            Text("WorldProgress",progress);Text("WorldObjective",objective);Text("WorldPrompt",topic==1?Game.World.RepairNavigation(Game.CanvasNearStation):Game.CanvasNearStation?"[E]  OPEN DEVICE":"FOLLOW THE CYAN MARKER / CONTAINMENT CASE");
            Control<TMP_Text>("Crosshair").color=Game.CanvasNearStation?new Color(.2f,.82f,.88f):Color.white;
            Text("MissionProgress",progress);
            Text("EnvironmentStatus",topic==1?"Water: "+(Game.PressureHeld?"HELD":Game.Stage>=8?"RISING":"LOW"):Game.Session.safetyMode?"SAFETY MODE / continue at your pace":"Countdown: "+TimeSpan.FromSeconds(Game.CaseRemaining).ToString(@"mm\:ss"));
            Text("SummaryMetrics",Game.Session.Summary);
            RefreshCompletionNavigation();
            RefreshMissionRestart();
            if(state=="Summary"&&summarySession!=Game.Session.id){summarySession=Game.Session.id;Text("SkillReview",MissionReview.Summary(Game.Session));}
            if(topic==1&&state=="Finishing")
            {
                bool reunited=Game.World.TryGetComponent<BridgeRescueSequence>(out var rescue)&&rescue.Progress>=.87f;
                Text("FinishingTitle",reunited?"CALF REUNITED":"BRIDGE RESTORED");
                Text("FinishingText",reunited?"The calf has reached its mother. Rescue complete.":"The calf is crossing to its mother.");
            }
            if(topic==2&&state=="Finishing")
            {
                if(Game.World.TryGetComponent<CaseShutdownSequence>(out var shutdown))
                {Text("FinishingTitle",shutdown.Title);Text("FinishingText",shutdown.Description);for(int i=0;i<3;i++)Text("ShutdownPhase"+i,shutdown.PhaseLabel(i));}
                else{Text("FinishingTitle","DEACTIVATING DEVICE");Text("FinishingText","Powering down. Results are saved when the device is safe.");}
            }
            Text("SupportHearts","Support Hearts: "+(3-Game.Session.heartsUsed)+"/3");for(int i=1;i<3;i++)Enabled("SupportTier"+i,Game.Session.heartsUsed<3||hints.ContainsKey(i));
            Show("ManualOpen",topic==2);Show("SupportOpen",!Game.Ready&&(topic==1||!caseOverview));Enabled("SupportOpen",!Game.Ready&&(topic==1||!caseOverview));
            if(state!="Puzzle")return;
            Show("BridgeBody",topic==1&&!Game.Ready);Show("BridgeReady",topic==1&&Game.Ready);
            Show("CaseOverview",topic==2&&caseOverview);Show("CaseModule",topic==2&&!caseOverview);Show("CaseBack",topic==2&&!caseOverview);
            bool taskVisible=topic==1?!Game.Ready:!caseOverview;
            Show("TaskTitle",taskVisible);Show("TaskPrompt",taskVisible);Show("Feedback",taskVisible);
            if(topic==1)RefreshBridge();else RefreshCase();
            RefreshFeedback();
            RefreshQuestionJourney();
            RefreshControlsGuide();
        }
        void RefreshBridge()
        {
            for(int i=0;i<3;i++){Text("Component"+i,(Game.BridgeParts>i?"[OK] ":"[  ] ")+new[]{"Power regulator","Bridge component","Bridge anchor"}[i]);Control<TMP_Text>("Component"+i).color=Game.BridgeParts>i?new Color(.35f,.92f,.59f):new Color(.55f,.7f,.76f);}
            Text("SecondaryProgress","Accepted steps: "+Game.Session.completed.Count+"/"+Game.Session.required);
            if(Game.Ready)return;
            var extra=Game.BridgeExtension;string key=extra?.id??("D"+Game.Stage);
            if(lastBridgeQuestion!=key){lastBridgeQuestion=key;lastStage=Game.Stage;coefficient=false;exponent=4;choice=-1;Control<TMP_InputField>("BridgeA").SetTextWithoutNotify("");Control<TMP_InputField>("BridgeB").SetTextWithoutNotify("");EndDrag();}
            Text("TaskTitle",extra?.title??MissionRules.BridgeTitles[Game.Stage]);Text("TaskPrompt",extra?.prompt??MissionRules.BridgePrompts[Game.Stage]);
            bool options=extra!=null?extra.choiceAnswer:Game.Stage==7||Game.Stage==10;
            Show("PowerControls",extra==null&&Game.Stage==0);Show("BridgeInputs",(extra!=null||Game.Stage!=0)&&!options);Show("BridgeOptions",options);
            Text("CoefficientSlotLabel",coefficient?"Coefficient: 4":"Coefficient slot");Text("ExponentValue",exponent.ToString());Text("PowerPreview",(coefficient?"4":"?")+"x^"+exponent);
            bool chipSelected=DragKind==0&&!coefficient&&!Game.CanvasAccepted;
            SelectionVisual("BridgeChip",chipSelected);Text("BridgeChipLabel",chipSelected?"[X] 4":"4");
            Text("BridgeALabel",extra!=null?"Coefficient":Game.Stage==1||Game.Stage==8?"Exponent":Game.Stage==6||Game.Stage==9?"Constant derivative":"Coefficient");Show("BridgeBGroup",extra!=null?!options:MissionRules.BridgePair(Game.Stage));
            for(int i=0;i<3;i++)
            {
                Text("BridgeChoice"+i+"Label",(choice==i?"[X] ":"[ ] ")+(extra?.choiceAnswer==true?extra.options[i]:MissionRules.BridgeChoices(Game.Stage)[i]));
                QuestionJourney.Choice(Control<Button>("BridgeChoice"+i),choice==i,Game.CanvasAccepted);
            }
            Show("RepairSubmit",!Game.CanvasAccepted);Show("RepairContinue",Game.CanvasAccepted);
            Text("RepairContinueLabel",Game.Stage==0?"Install regulator":Game.Stage==3?"Install component":Game.Stage==7?"Install anchor":"Place / continue");
            if(Game.ExtendedBridge&&DifferentiationQuestionBank.Load().Next(Game.Stage,Game.Session.completed)!=null)Text("RepairContinueLabel","Next calibration");
            Control<CanvasGroup>("BridgeAnswers").interactable=!Game.CanvasAccepted;
        }
        void RefreshCase()
        {
            Game.World.FocusCase(caseOverview?-1:Game.Module);Game.World.SetCaseDraft(exponent,divisor,release);
            Text("SecondaryProgress","Accepted steps: "+Game.Session.completed.Count+"/"+Game.Session.required);
            string[] ids={"I_DIAL","I_C","I_SEQ_3","I_GAUGE"},names={"Exponent dial","Wire matching","Symbol sequence","Pressure gauge"};
            Text("CaseGuidance",caseOverview?"LIVE DEVICE VIEW\nGreen lamps mark stable work.\nPower stays on until shutdown.":"LIVE DEVICE VIEW\n"+names[Game.Module]+"\n"+(Game.CaseModuleComplete(Game.Module)?"Module stable / work saved":Game.CaseCalibration!=null?"Independent calibration":"Controls update the device."));
            for(int i=0;i<4;i++){bool done=Game.CaseModuleComplete(i);Text("Module"+i+"Label",(done?"[OK] ":i>0&&!Game.CaseModuleComplete(0)?"[LOCKED] ":"")+names[i]);Enabled("Module"+i,i==0||Game.CaseModuleComplete(0));Text("ModuleState"+i,done?"STABLE":i>0&&!Game.CaseModuleComplete(0)?"Complete Exponent Dial first":"Complete controls and calibration");Control<TMP_Text>("ModuleState"+i).color=done?new Color(.35f,.92f,.59f):new Color(.55f,.7f,.76f);}
            Enabled("MasterCover",Game.Ready);Show("MasterCover",!cover);Show("MasterActivate",cover);Enabled("MasterActivate",cover&&Game.Ready);
            Text("MasterCoverLabel",Game.Ready?"Lift master switch cover":"[LOCKED] Complete all four modules");
            Control<TMP_Text>("MasterCoverLabel").color=Game.Ready?new Color(.025f,.045f,.065f):new Color(.62f,.73f,.78f);
            Text("MasterStatus",Game.Ready?(cover?"All four modules complete. Cover lifted. Click Master Deactivation to shut down the device.":"All four modules complete. Device power is still ON. Lift the cover, then deactivate."):"Modules "+Game.ModulesComplete+"/4 complete. Master Deactivation stays locked until all four are complete.");
            for(int i=0;i<4;i++)Show("ModuleControls"+i,Game.Module==i);
            if(RefreshCaseCalibration())return;
            if(caseOverview)return;
            Text("TaskTitle",names[Game.Module]);Text("TaskPrompt",new[]{"Integrate x^4. Raise the exponent, divide by the new exponent and insert one +C.","Integrate 3x^2 + 4x - 5. Connect each term to its integral, then add one common +C.","For x >= 0, integrate sqrt(x). Order the procedure cards and enter each step's value.","Evaluate the integral from 0 to 2 of 2x dx. Set the release amount; no +C is needed."}[Game.Module]);
            Text("DialExponentValue",exponent.ToString());Text("DivisorValue",divisor.ToString());Text("InsertCLabel",constant?"+C inserted":"Insert +C");Text("DialPreview","x^"+exponent+" / "+divisor+(constant?" + C":""));
            Enabled("DialSubmit",!Game.MissionDone("I_DIAL"));Control<CanvasGroup>("DialAnswers").interactable=!Game.MissionDone("I_DIAL");
            var connections=Control<MissionConnections>("WireConnections");
            for(int i=0;i<3;i++){bool done=Game.MissionDone("I_W"+i);connections.locked[i]=done;Text("WireSource"+i+"Label",(done?"[OK] ":selectedWire==i?"[X] ":"")+MissionRules.WireInputs[i]);Enabled("WireSource"+i,!done);SelectionVisual("WireSource"+i,!done&&selectedWire==i);}
            connections.SetVerticesDirty();Enabled("CommonC",Enumerable.Range(0,3).All(i=>Game.MissionDone("I_W"+i))&&!Game.MissionDone("I_C"));
            Text("CommonCLabel",Game.MissionDone("I_C")?"Common +C connected":"Connect one common +C");
            int next=SequenceStep();for(int i=0;i<4;i++)
            {
                bool done=Game.MissionDone("I_SEQ_"+i),used=Enumerable.Range(0,4).Any(n=>order[n]==i&&Game.MissionDone("I_SEQ_"+n));
                Enabled("SequenceCard"+i,!used);Enabled("SequenceSlot"+i,!done);
                bool selected=!used&&selectedCard==i;SelectionVisual("SequenceCard"+i,selected);
                Text("SequenceCard"+i+"Label",(selected?"[X] ":"")+MissionRules.SequenceCards[i]);
                Text("SequenceSlot"+i+"Label",(done?"[OK] ":(i+1)+" / ")+(order[i]<0?"Place card":MissionRules.SequenceCards[order[i]]));
            }
            Text("SequenceValueLabel",next==4?"Output: (2/3)x^(3/2) + C":"Step "+(next+1)+": "+new[]{"rewritten exponent","raised exponent","new coefficient","constant chip"}[Mathf.Min(next,3)]);Enabled("SequenceSubmit",next<4);Control<TMP_InputField>("SequenceValue").interactable=next<4;
            Text("ReleaseValue",release.ToString());bool safe=Game.MissionDone("I_GAUGE");Enabled("GaugeSubmit",!safe);Control<CanvasGroup>("GaugeAnswers").interactable=!safe;
            Text("GaugeState",safe?"PRESSURE SAFE":Game.PressureHeld?"PRESSURE HELD / take your time":"PRESSURE ACTIVE / adjust the release");Control<Slider>("Pressure").SetValueWithoutNotify(safe?.12f:Game.PressureHeld?.65f:Mathf.Clamp01(.35f+(600-Game.CaseRemaining)/900));
        }
    }
}
