#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator SelectedRecordResumeChecks()
        {
            yield return MissionRestartRunChecks();string oldCase=Game.Session.id;
            yield return Click("PuzzlePause");yield return Click("PauseRestart");yield return Click("RestartConfirm");yield return new WaitForSeconds(.4f);yield return Click("BriefStart");yield return Key(UnityEngine.InputSystem.Key.Escape);yield return Click("PauseLeave");yield return Click("LeaveConfirm");yield return new WaitForSeconds(.4f);
            Check(Chapter4Store.Read().Count==3,"Fixture contains original bridge/case plus a newer empty case run");
            var bridge=Chapter4Store.Read().Single(s=>s.topic==1);var selectedCase=Chapter4Store.Read().Single(s=>s.id==oldCase);string json=JsonUtility.ToJson(selectedCase);
            yield return Click("HomeRecords");Check(Control<TMP_Text>("Record0Label").text.Contains(RecordMissionResume.StartedLabel(Chapter4Store.Read()[0].started)),"Record row includes readable local start time");
            yield return Click("Record1");Check(Control<Button>("DetailResume").IsInteractable()&&!Control<Transform>("DetailPractice").gameObject.activeSelf,"Unfinished selected record offers mission continuation");
            Check(JsonUtility.ToJson(Chapter4Store.Read().Single(s=>s.id==oldCase))==json,"Opening record details does not rewrite saved work");yield return Shot("selected-older-case");
            yield return Click("DetailResume");yield return new WaitForSeconds(.4f);
            Check(Game.Session.id==oldCase&&Game.ModulesComplete==1&&Game.Session.heartsUsed==1&&Game.World.CaseOpened,"Selected older case restores its own accepted dial and hearts");Check(Chapter4Store.Read().Count==3,"Resume does not create a new record");yield return Inspect();Check(Mathf.Abs(Mathf.DeltaAngle(CasePart("Exponent needle").localEulerAngles.y,200))<1,"Selected record restores physical dial");yield return Click("Module0");Check(Control<TMP_Text>("DialExponentValue").text=="5"&&Control<TMP_Text>("DivisorValue").text=="5"&&Control<TMP_Text>("InsertCLabel").text.Contains("inserted"),"Selected record also restores accepted dial values in UI");yield return Shot("older-case-resumed");yield return SaveReturn();
            yield return Click("HomeRecords");yield return Click("Record2");yield return Click("DetailResume");yield return new WaitForSeconds(.4f);
            Check(Game.Session.id==bridge.id&&Game.Stage==1&&Game.BridgeParts==1&&Game.Session.heartsUsed==1,"Older bridge record restores its own component and support");yield return Inspect();yield return Shot("older-bridge-resumed");yield return SaveReturn();yield return Click("HomeRecords");
            var newest=Chapter4Store.Read()[0];string path=Path.Combine(Chapter4Store.Folder,newest.id+".json"),original=File.ReadAllText(path),held=Path.Combine(folder,"held-record.fixture");
            yield return Click("Record0");File.Move(path,held);
            try
            {
                ProductionCanvas.ResumeId="stale-record-qa";yield return Click("DetailResume");
                Check(View==null&&Control<Transform>("Detail").gameObject.activeInHierarchy&&!Control<Button>("DetailResume").IsInteractable()&&ProductionCanvas.ResumeId==null,"Disappeared record stays on details and clears stale resume ID");Check(Control<TMP_Text>("RecordResumeStatus").text.Contains("no longer available"),"Missing-record feedback gives a way back");yield return Shot("missing-record-blocked");
                yield return Click("DetailBack");Check(!Control<Transform>("Record2").gameObject.activeSelf,"Back refreshes the list after a record disappears");
            }
            finally{File.Move(held,path);}
            yield return Click("RecordsBack");yield return Click("HomeRecords");yield return Click("Record0");
            try
            {
                newest.status="Completed";Chapter4Store.WriteCache(newest);yield return Click("DetailResume");
                Check(View==null&&!Control<Button>("DetailResume").IsInteractable()&&Control<TMP_Text>("RecordResumeStatus").text.Contains("already complete"),"Record completed after opening cannot be resumed");
                yield return Click("DetailBack");yield return Click("Record0");Check(!Control<Transform>("DetailResume").gameObject.activeSelf,"Completed record hides mission continuation");yield return Shot("completed-record-review-only");
            }
            finally{File.WriteAllText(path,original);}
            yield return Click("DetailBack");yield return Click("Record0");
            try
            {
                var foreign=JsonUtility.FromJson<Chapter4Session>(original);foreign.profile="Another local learner";Chapter4Store.WriteCache(foreign);yield return Click("DetailResume");Check(View==null&&!Control<Button>("DetailResume").IsInteractable(),"A record whose owner changed is not resumed");
            }
            finally{File.WriteAllText(path,original);}
            yield return Click("DetailBack");
            yield return Click("Record0");bool moved=false;
            UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode> intercept=(scene,mode)=>
            {if(!moved&&scene.name==Chapter4Game.Scenes[newest.topic]){File.Move(path,held);moved=true;}};
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=intercept;
            try
            {
                yield return Click("DetailResume");yield return new WaitForSeconds(.4f);
                Check(moved&&View==null&&Control<Transform>("Records").gameObject.activeInHierarchy&&ProductionCanvas.ResumeId==null,"A record lost during scene loading returns to records instead of a fresh mission");
                Check(Control<TMP_Text>("RecordsOverview").text.Contains("could not be restored"),"Post-load failure gives a readable recovery message");yield return Shot("record-lost-during-load");
            }
            finally{UnityEngine.SceneManagement.SceneManager.sceneLoaded-=intercept;if(moved)File.Move(held,path);}
            // Check the existing Limits route with an explicitly isolated checkpoint fixture.
            var limits=new Chapter4Session{profile=Chapter4Store.Profile,topic=0,required=5,elapsed=32};limits.Submit("L1","5",true,"");Chapter4Store.Save(limits);
            yield return Click("RecordsBack");yield return Click("HomeRecords");yield return Click("Record0");yield return Click("DetailResume");yield return new WaitForSeconds(.4f);
            Check(Game.Topic==0&&Game.Session.id==limits.id&&Game.Stage==1&&Game.State=="Explore"&&Game.Session.completed.Count==1,"Selected Limits checkpoint uses the existing stage-resume route");Check(Chapter4Store.Read().Count==4,"Limits continuation also creates no extra session");yield return Shot("limits-checkpoint-resumed");
        }
    }
}
#endif
