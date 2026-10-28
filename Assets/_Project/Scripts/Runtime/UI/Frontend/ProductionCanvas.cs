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
    // Binds an editable prefab. No controls are constructed or positioned at runtime.
    public sealed partial class ProductionCanvas : MonoBehaviour
    {
        public bool isMenu;
        public LimitsQuestionBank questionBank;
        [Header("Editable scene/prefab references")]
        public UIReferenceMap uiReferences;
        public GameManager sceneController;
        public static ProductionCanvas Active { get; private set; }
        public static string ResumeId;
        public static bool LegacyQA => LearningApi.TestBypass&&Environment.GetCommandLineArgs().Contains("--chapter4-qa");
        readonly Dictionary<string, Transform> nodes = new Dictionary<string, Transform>();
        Chapter4Game game;
        LimitsAnswer answer = new LimitsAnswer();
        string page, lastState, modal, beforePause = "Explore";
        int lastStage = -1, selectedCard = -1, recordPage;
        bool lastAccepted;
        string lastLimitsQuestion;
        LimitsQuestion CurrentLimitsQuestion=>game.LimitsExtension??questionBank.questions[Mathf.Min(game.Stage,4)];
        List<Chapter4Session> records = new List<Chapter4Session>();
        readonly Dictionary<int,string> purchasedSupport = new Dictionary<int,string>();
        public Chapter4Game Game => game;
        public string CurrentAnswerPreview=>game==null||game.Ready?"":AnswerPreview.Limits(CurrentLimitsQuestion,lastLimitsQuestion==CurrentLimitsQuestion.id?answer:new LimitsAnswer());
        public T Control<T>(string name) where T : Component => nodes[name].GetComponent<T>();
        void Text(string name, string value) { MathText.Set(Control<TMP_Text>(name),value); }
        void Show(string name, bool visible) { nodes[name].gameObject.SetActive(visible); }
        public static bool Install(bool menu)
        {
            if(LegacyQA)return false;
            if(FindFirstObjectByType<ProductionCanvas>()==null)
                throw new InvalidOperationException("Save the menu/Limits UI prefab in the scene before Play.");
            return true;
        }
        void Awake()
        {
            if (LegacyQA) { gameObject.SetActive(false); return; }
            Active = this;
            GetComponent<Canvas>().enabled=true;
            if(uiReferences==null)throw new InvalidOperationException("Assign UI References on ProductionCanvas before playing.");
            uiReferences.CopyTo(nodes);
            if (EventSystem.current == null&&(sceneController==null||sceneController.eventSystem==null))
                new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if(isMenu&&LearningApi.LocalPlay)OpenPage("Home");
        }
        void OnDestroy() { if (Active == this) Active = null; }
        void Start()
        {
            if (isMenu) SetupMenu(); else SetupLimits();
        }
        void OpenPage(string next)
        {
            if(isMenu&&!LearningApi.LocalPlay)
            {
                if(!LearningApi.Authenticated)next="Login";
                else if(LearningApi.Account.role=="Teacher"&&(next=="Home"||next=="Topics"))next="Teacher";
                else if(LearningApi.Account.role=="Student"&&(next=="Teacher"||next=="Students"))next="Home";
            }
            var target=nodes[next];
            foreach (Transform child in nodes["Pages"]) child.gameObject.SetActive(child==target);
            page = next; EventSystem.current?.SetSelectedGameObject(null);
        }
        void SetupMenu()
        {
            SetupAccountControls();
            SetupPracticeRecords();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Chapter4Store.Profile = !LearningApi.LocalPlay&&LearningApi.Account!=null?LearningApi.Account.username:Chapter4Store.IsolatedRun ? "Canvas QA" : "Local player";
            Control<TMP_InputField>("ProfileInput").text = Chapter4Store.Profile;
            AudioListener.volume = PlayerPrefs.GetFloat("C4Volume", .75f);
            
            Control<Slider>("VolumeSlider").SetValueWithoutNotify(AudioListener.volume);
            
            if(!LearningApi.LocalPlay){StartCoroutine(RestoreAccountMenu());return;}
            SaveProfile();
            bool dashboard = Chapter4Frontend.ReturnPage == "Dashboard",topics=Chapter4Frontend.ReturnPage=="Topics";
            Chapter4Frontend.ReturnPage = "Home";
            if (dashboard) RefreshRecords(); OpenPage(dashboard ? "Records" : topics?"Topics":"Home");
            string restoreFailure=RecordMissionResume.TakeLoadFailure();if(!string.IsNullOrEmpty(restoreFailure))Text("RecordsOverview",restoreFailure);
        }
        bool SaveProfile()
        {
            if(!LearningApi.LocalPlay)
            {
                if(!LearningApi.Student){OpenPage("Login");return false;}
                UpdateAccountHome();return true;
            }
            string value = Control<TMP_InputField>("ProfileInput").text.Trim();
            if (value.Length == 0) { Text("HomeStatus", "Enter a local learner name to begin."); return false; }
            Chapter4Store.Profile = value;
            var pending = Chapter4Store.Read().FirstOrDefault(s => s.status == "InProgress");
            Control<Button>("HomeContinue").interactable = pending != null;
            Text("HomeContinueLabel",pending==null?"No session to continue":"Continue "+new[]{"Limits","Differentiation","Integration"}[pending.topic]);
            Text("HomeStatus", pending == null ? "Choose a mission to begin. No account is needed." : "Accepted steps: " + pending.completed.Count + "/"+pending.required+". Your progress is saved locally.");
            return true;
        }
        void ResumeLatest()
        {
            var pending = Chapter4Store.Read().FirstOrDefault(s => s.status == "InProgress");
            if (pending == null) return;
            ResumeId = pending.id; GameManager.ChangeScene(Chapter4Game.Scenes[pending.topic]);
        }
        void RefreshRecords()
        {
            if(!teacherRecords)records = Chapter4Store.Read();
            recordPage = Mathf.Clamp(recordPage, 0, Mathf.Max(0, (records.Count - 1) / 5));
            int attempts = records.Sum(s => s.MathematicalAttemptCount), correct = records.Sum(s => s.MathematicalCorrectCount),independent=records.Sum(s=>s.IndependentAttemptCount),independentCorrect=records.Sum(s=>s.IndependentCorrectCount);
            Text("RecordsOverview", (teacherRecords?"Teacher results":Chapter4Store.Profile) + " / " + records.Count(s => s.status == "Completed") + " completed / Math " + Chapter4Session.Percent(attempts==0?-1:100f*correct/attempts)+" / Independent "+Chapter4Session.Percent(independent==0?-1:100f*independentCorrect/independent));
            for (int i = 0; i < 5; i++)
            {
                int index = recordPage * 5 + i; Show("Record" + i, index < records.Count);
                if (index < records.Count) { var s = records[index]; Text("Record" + i + "Label", (teacherRecords?s.profile+" / ":"") + new[] { "LIMITS", "DIFFERENTIATION", "INTEGRATION" }[Mathf.Clamp(s.topic, 0, 2)] + " / " + s.status + "\n" + RecordMissionResume.StartedLabel(s.started) + " | Math " + (s.Accuracy < 0 ? "--" : s.Accuracy.ToString("0") + "%") + " | Independent "+(s.IndependentAccuracy<0?"--":s.IndependentAccuracy.ToString("0")+"%")+" | Completion " + s.Completion.ToString("0") + "%"); }
            }
            Text("RecordsPage", records.Count == 0 ? "No records yet. Start a mission to create your first session." : "Page " + (recordPage + 1) + " / " + ((records.Count + 4) / 5));
            Control<Button>("RecordsPrevious").interactable = recordPage > 0;
            Control<Button>("RecordsNext").interactable = (recordPage + 1) * 5 < records.Count;
        }
        void OpenRecord(int index)
        {
            if (index >= records.Count) return; var s = records[index];
            UpdatePracticeRecord(s);UpdateRecordMissionResume(s);
            Text("DetailTitle","Session details / "+new[]{"Limits","Differentiation","Integration"}[Mathf.Clamp(s.topic,0,2)]);
            Text("DetailSummary", (teacherRecords?"Student: "+s.profile+"\n":"")+s.Summary);
            string review=s.topic>0?MissionReview.Summary(s)+"\n\nATTEMPT HISTORY\n\n":"";
            string detail=review+s.AttemptHistory+"\n\n"+s.HintHistory;
            Text("DetailAttempts",detail+MissionPractice.History(s));
            Control<ScrollRect>("AttemptScroll").verticalNormalizedPosition = 1; OpenPage("Detail");
        }
        void SetupLimits()
        {
            if(game==null&&sceneController!=null)game=sceneController.Game;
            if(game==null)game=FindFirstObjectByType<Chapter4Game>();
            if (game == null || questionBank == null || questionBank.questions.Length != 5) { Debug.LogError("Limits Canvas needs a game and five configured questions."); return; }
            game.EnableCanvas();
            SetupPracticeRecords();
            if(!LearningApi.LocalPlay&&!string.IsNullOrEmpty(LearningApi.ContentJson)){questionBank=Instantiate(questionBank);questionBank.questions=JsonUtility.FromJson<LimitsContent>(LearningApi.ContentJson).questions;}
             
            
            if(RecordMissionResume.RestorePending(game)){if(game!=null)RefreshLimits();return;}
            if (!string.IsNullOrEmpty(ResumeId)) { var s = Chapter4Store.Read().FirstOrDefault(row => row.id == ResumeId); ResumeId = null; game.CanvasResume(s); }
            RefreshLimits();
        }
        public void SetSceneGame(Chapter4Game sceneGame)=>game=sceneGame;
        void Update()
        {
            if(isMenu&&recordPracticeOpen&&UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame){recordPractice.Close();return;}
            if (isMenu || game == null) return;
            if (lastState != game.State || lastStage != game.Stage || lastAccepted != game.CanvasAccepted||lastLimitsQuestion!=CurrentLimitsQuestion.id) RefreshLimits();
            Text("LiveStatus", "Support Hearts  " + (3 - game.Session.heartsUsed) + "/3     |     Active time  " + TimeSpan.FromSeconds(game.Session.elapsed).ToString(@"mm\:ss"));
            Text("SaveStatus", !string.IsNullOrEmpty(Chapter4Store.LastError)?Chapter4Store.LastError:LearningApi.LocalPlay?"":LearningApi.SyncStatus);
            Control<TMP_Text>("SaveStatus").color=!string.IsNullOrEmpty(Chapter4Store.LastError)||LearningApi.SyncStatus.Contains("pending")?new Color(1f,.74f,.33f):new Color(.55f,.7f,.76f);
            if(modal=="Leave"&&!string.IsNullOrEmpty(Chapter4Store.LastError))Text("LeaveText",Chapter4Store.LastError+"\nYour session is still open. Try saving again.");
            if (game.State == "Explore") Text("WorldPrompt", game.CanvasNearStation ? "[E]  " + (game.Ready ? "OPEN ACTIVATION CONTROLS" : "EXAMINE CONSOLE") : "Walk towards the examination console.  WASD move / Mouse look");
        }
        void RefreshLimits()
        {
            bool newQuestion = lastLimitsQuestion!=CurrentLimitsQuestion.id;lastLimitsQuestion=CurrentLimitsQuestion.id;
            lastState = game.State; lastStage = game.Stage; lastAccepted = game.CanvasAccepted;
            Show("Backdrop",game.State!="Explore"&&game.State!="Finishing");
            OpenPage(game.State == "Finishing" ? "Finishing" : game.State == "Paused" ? "Pause" : game.State == "Explore" ? "World" : game.State);
            foreach (var diagram in GetComponentsInChildren<LimitsDiagram>(true)) if (diagram.portal) { diagram.charge = game.Stage; diagram.SetVerticesDirty(); }
            Text("WorldProgress", "OBSERVATORY CALIBRATION   " + game.Stage + " / 5 RESTORED");
            Text("ChargeLabel", game.Stage + " / 5");
            if (game.State == "Summary") { Text("SummaryMetrics", game.Session.Summary);Show("SummaryPractice",LearningApi.LocalPlay); }
            if (game.State != "Puzzle") return;
            Show("QuestionBody", !game.Ready); Show("ReadyBody", game.Ready);
            if (game.Ready) return;
            var q = CurrentLimitsQuestion;bool extra=game.LimitsExtension!=null;
            if(nodes.ContainsKey("IndependentGiven")){Show("IndependentGiven",extra&&q.kind==LimitsAnswerKind.Number);Text("IndependentGiven",q.display??"");}
            Text("QuestionNumber", "CALIBRATION " + (game.Stage + 1) + " / 5"+(extra?" / INDEPENDENT CHECK":" / GUIDED TASK")); Text("QuestionTitle", q.title); Text("QuestionPrompt", q.prompt);
            if (newQuestion)
            {
                answer = new LimitsAnswer(); selectedCard = -1;
                for (int i = 0; i < 3; i++) Control<TMP_InputField>("Answer" + i).SetTextWithoutNotify("");
                Control<Slider>("Probe").SetValueWithoutNotify(1); Control<Slider>("Marker").SetValueWithoutNotify(1);
            }
            Show("NumberInputs", q.kind != LimitsAnswerKind.Conclusion && q.kind != LimitsAnswerKind.GraphMarker&&q.kind!=LimitsAnswerKind.Choice);
            for (int i = 0; i < 3; i++) Show("AnswerGroup" + i, i < q.expected.Length);
            string[] labels = q.kind == LimitsAnswerKind.ThreeValues ? new[] { "Left-hand limit", "Right-hand limit", "Two-sided limit" } : q.kind == LimitsAnswerKind.LimitAndValue ? new[] { "Limit as x approaches 2", "Function value f(2)", "" } : new[] { "Approached value", "", "" };
            for (int i = 0; i < 3; i++) Text("AnswerLabel" + i, labels[i]);
            bool graph = q.kind == LimitsAnswerKind.GraphMarker || q.kind == LimitsAnswerKind.LimitAndValue;
            Show("GraphPanel", graph&&!extra); Show("GraphSliders", q.kind == LimitsAnswerKind.GraphMarker&&!extra);
            Show("NumberTable", q.kind == LimitsAnswerKind.Number&&!extra); Show("Piecewise", q.kind == LimitsAnswerKind.ThreeValues&&!extra); Show("ReasonPanel", q.kind == LimitsAnswerKind.Conclusion||q.kind==LimitsAnswerKind.Choice);
            foreach(string name in new[]{"ReasonHint","ReasonPrompt"})if(nodes.ContainsKey(name))Show(name,q.kind!=LimitsAnswerKind.Choice);
            for(int i=0;i<3;i++){Show("ReasonCard"+i,q.kind!=LimitsAnswerKind.Choice);Show("ReasonSlot"+i,q.kind!=LimitsAnswerKind.Choice);}
            var plot = Control<LimitsDiagram>("Graph"); plot.filledPoint = q.kind == LimitsAnswerKind.LimitAndValue; plot.showProbe = !plot.filledPoint;
            RefreshGraph(); RefreshReason();
            Text("Feedback", game.CanvasFeedback);
            Control<TMP_Text>("Feedback").color=game.CanvasAccepted?new Color(.35f,.92f,.59f):new Color(1f,.74f,.33f);
            Show("Submit", !game.CanvasAccepted); Show("Continue", game.CanvasAccepted);
            Text("ContinueLabel",game.ExtendedLimits&&game.LimitsExtension==null?"Independent check":"Apply calibration");
            Control<CanvasGroup>("AnswerControls").interactable = !game.CanvasAccepted;
            RefreshLimitsJourney();
        }
        void RefreshGraph()
        {
            var graph = Control<LimitsDiagram>("Graph"); graph.probe = Control<Slider>("Probe").value; graph.marker = answer.marker; graph.SetVerticesDirty();
            Text("ProbeLabel", "Probe x = " + graph.probe.ToString("0.00") + (Mathf.Abs(graph.probe - 2) < .015f ? " (open point)" : "")); Text("MarkerLabel", "Answer marker y = " + answer.marker);
        }
        void RefreshReason()
        {
            string[] conclusions = CurrentLimitsQuestion.kind==LimitsAnswerKind.Choice?CurrentLimitsQuestion.options:new[]{ "Limit = 2", "Limit = 5", "Does not exist" }, reasons = { "Left-hand limit", "is not equal to", "Right-hand limit" };
            for (int i = 0; i < 3; i++)
            {
                Text("Conclusion" + i + "Label", (answer.conclusion == i ? "[X] " : "[ ] ") + conclusions[i]);
                QuestionJourney.Choice(Control<Button>("Conclusion"+i),answer.conclusion==i,game.CanvasAccepted);
                Text("ReasonCard" + i + "Label", (selectedCard == i ? "> " : "") + reasons[i]);
                Text("ReasonSlot" + i + "Label", answer.reason[i] < 0 ? "Place card " + (i + 1) : reasons[answer.reason[i]]);
            }
        }
        void Submit() { game.CanvasSubmit(CurrentLimitsQuestion, answer); RefreshLimits(); }
        void LimitsAnswerChanged()
        {
            game.CanvasAnswerChanged();Text("Feedback",game.CanvasFeedback);
        }
        void OpenSupport() { purchasedSupport.Clear(); game.CanvasOpenSupport(true); modal = "Support"; Show("SupportModal", true);Control<HintPanelSelection>("SupportModal").Select(-1); Text("SupportText", "Choose a level of support. Only the two stronger options use a heart."); UpdateSupport(); }
        void UpdateSupport() { Text("SupportHearts", "Support Hearts remaining: " + (3 - game.Session.heartsUsed)); for (int i = 1; i < 3; i++) Control<Button>("SupportTier" + i).interactable = game.Session.heartsUsed < 3||purchasedSupport.ContainsKey(i); }
        void UseSupport(int tier) { if(tier<0||tier>2||tier>0&&game.Session.heartsUsed>=3&&!purchasedSupport.ContainsKey(tier))return;if(!purchasedSupport.TryGetValue(tier,out var hint)){hint=game.CanvasHint(CurrentLimitsQuestion, tier);purchasedSupport[tier]=hint;} Text("SupportText", hint);Control<HintPanelSelection>("SupportModal").Select(tier); UpdateSupport(); }
        void CloseSupport() { game.CanvasOpenSupport(false); modal = null; Show("SupportModal", false); }
        void Pause() { if(game.PresentationBusy)return;beforePause = game.State; game.SetState("Paused"); }
        void ResumePause() { game.SetState(beforePause); }
        public void HandleEscape()
        {
            if(game!=null&&game.PresentationBusy)return;
            if(isMenu&&recordPracticeOpen){recordPractice.Close();return;}
            if (isMenu) { OpenPage("Home"); return; }
            if (modal == "Support") { CloseSupport(); return; }
            if (modal == "Practice") { recordPractice.Close(); return; }
            if (modal == "Leave") { modal = null; Show("LeaveModal", false); return; }
            if (game.State == "Paused") ResumePause();
            else if (game.State == "Puzzle" || game.State == "Explore") Pause();
        }
    }
}
