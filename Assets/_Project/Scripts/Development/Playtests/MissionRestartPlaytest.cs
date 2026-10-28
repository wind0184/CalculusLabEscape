#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator MissionRestartRunChecks()
        {
            yield return Click("HomeStart");yield return Click("Mission1");yield return new WaitForSeconds(.4f);
            Check(!Game.CanvasRestartMission()&&Game.State=="Brief","Cannot restart before starting a mission");yield return Click("BriefStart");yield return Inspect();
            yield return Click("SupportOpen");yield return Click("SupportTier1");yield return Click("SupportClose");
            yield return Click("BridgeChip");yield return Click("CoefficientSlot");yield return Click("ExponentMinus");yield return Click("RepairSubmit");yield return Click("RepairContinue");yield return SolveBridgeExtensions();yield return Inspect();Input("BridgeA","1/7");
            yield return RestartFromPuzzleChecks("bridge");
            Check(Game.Stage==1&&Game.BridgeParts==1&&Control<TMP_InputField>("BridgeA").text=="","Resumed bridge restores accepted work without carrying unsaved draft");
            yield return SaveReturn();yield return Click("HomeStart");yield return Click("Mission2");yield return new WaitForSeconds(.4f);yield return Click("BriefStart");yield return Inspect();yield return Click("Module0");
            yield return Click("SupportOpen");yield return Click("SupportTier1");yield return Click("SupportClose");
            yield return Click("DialExponentPlus");for(int i=0;i<4;i++)yield return Click("DivisorPlus");yield return Click("InsertC");yield return Click("DialSubmit");yield return Click("Module1");yield return Click("WireSource0");
            yield return RestartFromPuzzleChecks("case");
            Check(Game.ModulesComplete==1&&Game.MissionDone("I_DIAL")&&Mathf.Abs(Mathf.DeltaAngle(CasePart("Exponent needle").localEulerAngles.y,200))<1,"Resumed case restores its completed dial physically");
            Check(Chapter4Store.Read().Count==2&&Chapter4Store.Read().All(s=>s.status=="InProgress"),"Restart retained exactly the two original unfinished runs");yield return Shot("case-old-run-resumed");
        }
        IEnumerator RestartFromPuzzleChecks(string prefix)
        {
            int topic=Game.Topic;var old=Game.Session;int attempts=old.attempts.Count,hearts=old.heartsUsed,complete=old.completed.Count;
            Check(complete==(topic==1?2:3)&&hearts==1,"Restart fixture preserves guided work plus the required independent calibrations and one used heart");
            yield return Click("PuzzlePause");yield return Click("PauseRestart");
            Check(Game.State=="Paused"&&Control<Transform>("RestartModal").gameObject.activeInHierarchy,"Restart opens a confirmation over the paused mission");
            Check(!Control<Button>("PauseResume").IsInteractable()&&!Control<Button>("PauseLeave").IsInteractable()&&!Control<Button>("PauseRestart").IsInteractable(),"Underlying pause buttons cannot be selected or submitted");
            Check(EventSystem.current.currentSelectedGameObject==Control<Button>("RestartCancel").gameObject,"Confirmation defaults keyboard focus to cancel");
            var pointer=Pointer(Control<RectTransform>("PauseResume"));var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()!=Control<Button>("PauseResume"),"Confirmation blocks pointer access to Resume");
            float elapsed=old.elapsed,remaining=Game.CaseRemaining;Quaternion needle=topic==2?CasePart("Pressure needle").rotation:Quaternion.identity;
            yield return new WaitForSeconds(.2f);Check(old.elapsed==elapsed&&Game.CaseRemaining==remaining,"Confirmation freezes active time and countdown");
            if(topic==2)Check(Game.World.CaseAudio.IsPaused&&Quaternion.Angle(needle,CasePart("Pressure needle").rotation)<.01f,"Confirmation freezes case audio and pressure needle");
            yield return Shot(prefix+"-restart-confirmation");yield return Key(UnityEngine.InputSystem.Key.Enter);
            Check(Game.State=="Paused"&&!Control<Transform>("RestartModal").gameObject.activeSelf&&Game.Session.id==old.id,"Enter on default cancel does not restart or resume");
            yield return Click("PauseRestart");yield return Key(UnityEngine.InputSystem.Key.Escape);Check(Game.State=="Paused"&&!Control<Transform>("RestartModal").gameObject.activeSelf,"Esc dismisses only the confirmation");
            yield return Click("PauseRestart");yield return Click("RestartCancel");yield return Click("PauseResume");
            Check(Game.State=="Puzzle"&&old.attempts.Count==attempts&&old.heartsUsed==hearts&&old.completed.Count==complete,"Cancel restores the current puzzle without changing assessed work");
            Check(topic==1?Control<TMP_InputField>("BridgeA").text=="1/7":Control<MissionSelectionVisual>("WireSource0").IsSelected,"Cancel preserves numeric draft or pending wire selection");
            yield return Click("PuzzlePause");yield return Click("PauseRestart");
            string originalFolder=Chapter4Store.IsolatedFolder,blocked=Path.Combine(folder,prefix+"-blocked-save");File.WriteAllText(blocked,"QA fixture");
            try
            {
                Chapter4Store.IsolatedFolder=blocked;yield return Click("RestartConfirm");
                Check(Game.State=="Paused"&&Game.Session.id==old.id&&Control<Transform>("RestartModal").gameObject.activeInHierarchy&&!string.IsNullOrEmpty(Chapter4Store.LastError),"Failed save retains the old run and confirmation");
                Check(Control<TMP_Text>("RestartError").text.Contains("Try again"),"Failed restart offers a readable save retry");yield return Shot(prefix+"-restart-save-retry");
            }
            finally{Chapter4Store.IsolatedFolder=originalFolder;}
            int count=Chapter4Store.Read().Count;ProductionCanvas.ResumeId="stale-restart-qa";yield return Click("RestartConfirm");yield return new WaitForSeconds(.4f);
            Check(Game.Topic==topic&&Game.State=="Brief"&&Game.Session.id!=old.id&&Game.Session.completed.Count==0&&Game.Session.attempts.Count==0&&Game.Session.heartsUsed==0&&Game.Session.elapsed==0,"Restart creates an empty fresh briefing with new timer and hearts");
            Check(ProductionCanvas.ResumeId==null&&Game.Session.guideSeen.Count==0&&Game.CaseRemaining==600,"Restart clears resume/guidance and resets case countdown");
            var saved=Chapter4Store.Read().First(s=>s.id==old.id);Check(JsonUtility.ToJson(saved)==JsonUtility.ToJson(old)&&Chapter4Store.Read().Count==count,"Prior run is saved exactly and briefing writes no extra record");
            if(topic==1)Check(Game.BridgeParts==0&&Game.World.bridge.All(t=>t.position.y<0),"Fresh bridge has no installed components");
            else
            {
                var sequence=Game.World.GetComponent<CaseShutdownSequence>();Check(!Game.World.MasterCoverOpen&&!Game.World.DeviceDeactivated&&Game.World.CaseAudio.CueCount==0&&sequence.PowerOffCount==0&&sequence.powerBars.All(r=>r.sharedMaterial==sequence.powerOn),"Fresh case resets cover, shutdown, audio and power");
            }
            yield return Shot(prefix+"-fresh-brief");yield return Click("BriefBack");yield return new WaitForSeconds(.3f);yield return Click("HomeContinue");yield return new WaitForSeconds(.4f);
            Check(Game.Session.id==old.id&&Game.Session.heartsUsed==hearts&&Game.Session.completed.Count==complete&&Game.Session.attempts.Count==attempts,"Home Continue can recover the retained original run");yield return Inspect();
        }
    }
}
#endif
