#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Behaviour checks for the editing promises, in addition to the existing
    // complete mission playtests. Uses isolated saves and an opt-in player flag.
    public sealed class ManualEditingPlaytest : MonoBehaviour
    {
        string folder;
        int failures;
        readonly List<string> results=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--manual-editing-qa"))return;
            string path=LearningApi.Argument("--qa-output");
            if(string.IsNullOrEmpty(path))throw new Exception("Manual editing QA requires an output folder");
            Directory.CreateDirectory(path);Chapter4Store.IsolatedFolder=Path.Combine(path,"sessions");
            var go=new GameObject("Manual editing behaviour checks");DontDestroyOnLoad(go);
            var test=go.AddComponent<ManualEditingPlaytest>();test.folder=path;
            Application.runInBackground=true;
        }
        void Check(bool success,string message)
        {results.Add((success?"PASS: ":"FAIL: ")+message);if(!success)failures++;File.WriteAllLines(Path.Combine(folder,"checks.txt"),results);}
        void Awake()=>Application.logMessageReceived+=Log;
        void OnDestroy()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error){failures++;if(folder!=null)File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        IEnumerator Start()
        {
            yield return null;yield return null;
            var menu=ProductionCanvas.Active;
            Check(menu!=null&&menu.isMenu,"MainMenu starts from the saved scene UI");
            var start=menu.Control<Button>("HomeStart");
            start.onClick.SetPersistentListenerState(0,UnityEventCallState.Off);
            start.onClick.Invoke();
            Check(menu.Control<Transform>("Home").gameObject.activeInHierarchy,"Disabling the Inspector On Click disables the action; no hidden runtime listener");
            start.onClick.SetPersistentListenerState(0,UnityEventCallState.RuntimeOnly);
            start.name="Student renamed Start button";
            menu.Control<Transform>("Topics").name="Student renamed mission page";
            start.onClick.Invoke();
            Check(menu.Control<Transform>("Topics").gameObject.activeInHierarchy,"Renaming the UI button and page preserves saved references and navigation");
            var slider=menu.Control<Slider>("VolumeSlider");float original=AudioListener.volume;
            slider.onValueChanged.SetPersistentListenerState(0,UnityEventCallState.Off);slider.value=original>.5f?.25f:.75f;
            Check(Mathf.Approximately(AudioListener.volume,original),"Slider behaviour is also owned by the saved Inspector event");
            slider.onValueChanged.SetPersistentListenerState(0,UnityEventCallState.RuntimeOnly);slider.value=original;
            menu.Control<Button>("Mission1").onClick.Invoke();
            yield return null;yield return null;yield return null;
            var manager=FindFirstObjectByType<GameManager>();var view=MissionCanvas.Active;var game=view.Game;
            Check(manager!=null&&manager.Game==game&&manager.world==game.World&&manager.missionUI==view,"Saved Game Manager connects the actual world, UI, and live game");
            Check(game==manager&&GameManager.Instance==manager&&manager.world.gameManager==manager&&manager.world.GetComponent<Chapter4Game>()==null,"The saved Game Manager itself owns game state; no rules component is added to the map after Play");
            Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<Chapter4World>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<MissionCanvas>(FindObjectsSortMode.None).Length==1,"Startup creates no duplicate managers, worlds, or UI");
            Check(game.World.player.GetComponent<PlayerInteraction>().scene==manager&&game.ExternalPlayerInput,"Player Interaction uses the assigned scene controller");
            var title=view.Control<RectTransform>("TaskTitle");Vector2 titlePosition=title.anchoredPosition+new Vector2(13,-7);title.anchoredPosition=titlePosition;
            view.Control<Button>("BriefStart").onClick.Invoke();
            yield return null;
            if(game.CanvasSupportOpen)view.HandleEscape();
            var hud=view.GetComponent<ExplorationHUD>();hud.Present(false,false,0);
            Vector2 objectivePosition=hud.objectivePanel.anchoredPosition+new Vector2(31,9);hud.objectivePanel.anchoredPosition=objectivePosition;
            Vector2 guidePosition=hud.guidePanel.anchoredPosition+new Vector2(42,18);hud.guidePanel.anchoredPosition=guidePosition;
            hud.Present(true,true,1);
            Check(hud.objectivePanel.anchoredPosition==objectivePosition,"HUD refresh preserves manually edited fixed panel positions");
            Check(hud.guidePanel.anchoredPosition==guidePosition+hud.compactGuideOffset,"Guide folding follows the edited position using the relative offset");
            hud.Present(false,false,0);
            Check(hud.guidePanel.anchoredPosition==guidePosition,"Unfolding restores the student's edited guide position");
            game.OpenPuzzle();yield return null;
            if(game.CanvasSupportOpen)view.HandleEscape();
            Check(title.anchoredPosition==titlePosition,"Opening/refreshing the question preserves its authored RectTransform");
            var reportView=view.GetComponent<ReportFusionView>();
            if(reportView!=null&&reportView.useConceptLayout)
            {
                int hearts=game.Session.heartsUsed,attempts=game.Session.attempts.Count;
                view.Control<Button>("ReportNotesOpen").onClick.Invoke();
                Check(reportView.gameManager==manager&&reportView.ScratchpadOpen&&game.CanvasSupportOpen,"The report view retains its saved manager and its Scratchpad button opens a blocking notes panel");
                reportView.scratchpad.text="Working: d(x^4)/dx = 4x^3";
                view.HandleEscape();
                Check(!reportView.ScratchpadOpen&&!game.CanvasSupportOpen&&reportView.scratchpad.text.Contains("4x^3"),"Escape closes the scratchpad and preserves working notes");
                Check(game.Session.heartsUsed==hearts&&game.Session.attempts.Count==attempts,"Working notes consume no Support Hearts and submit no assessment attempt");
            }
            var submit=view.Control<Button>("RepairSubmit");submit.name="Student renamed repair button";
            view.Control<Button>("BridgeChip").onClick.Invoke();view.Control<Button>("CoefficientSlot").onClick.Invoke();
            view.Control<Button>("ExponentMinus").onClick.Invoke();
            int before=game.Session.attempts.Count;submit.onClick.Invoke();
            Check(game.CanvasAccepted&&game.Session.attempts.Count==before+1,"Saved chip/slot/exponent/submit events accept the correct answer exactly once after renaming");
            submit.onClick.Invoke();Check(game.Session.attempts.Count==before+1,"A repeated submit does not add another attempt");
            view.Control<Button>("RepairContinue").onClick.Invoke();
            Check(game.BridgeParts==0&&game.BridgeExtension?.id=="DX_BASIC_1","Continue opens the independent calibration before installing the regulator");
            view.Control<TMPro.TMP_InputField>("BridgeA").text="70";
            view.Control<TMPro.TMP_InputField>("BridgeB").text="9";
            submit.onClick.Invoke();view.Control<Button>("RepairContinue").onClick.Invoke();
            Check(game.BridgeParts==1&&game.State=="Explore","Saved Continue event applies the accepted regulator and returns to exploration");
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: Inspector events, disabled-listener behaviour, stable UI renaming, explicit scene/player references, no duplicate startup, editable HUD/layout, and exactly-once answer/installation.":"FAILURES: "+failures);
            Application.Quit(failures==0?0:1);
        }
    }
}
#endif
