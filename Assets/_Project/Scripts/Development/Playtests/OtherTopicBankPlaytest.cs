#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator SolveCaseCalibrations()
        {
            if(Game.Topic==2&&Game.CaseGuidedReview)
            {
                int attempts=Game.Session.attempts.Count,completed=Game.Session.completed.Count;
                string note=Game.CanvasFeedback;int module=Game.Module;
                Check(Game.CaseCalibration==null&&note.StartsWith("CORRECT")&&Control<Transform>("CalibrationNext").gameObject.activeInHierarchy,"Guided success is visible before independent calibration");
                Check(View.CurrentAnswerPreview.Contains("C")||module==3,"Accepted guided result remains in preview");
                NextActionCheck("Continue to independent check");
                foreach(string name in new[]{"TaskTitle","TaskPrompt","Feedback","CalibrationNextLabel"})
                {var label=Control<TMP_Text>(name);label.ForceMeshUpdate();Check(!label.isTextOverflowing,"Guided success text fits: "+name);}
                yield return new WaitForSeconds(.4f);
                Check(Game.CaseGuidedReview&&Game.CanvasFeedback==note,"Guided success waits for the learner to continue");
                yield return Click("CaseBack");yield return Click("Module"+module);
                Check(Game.CaseGuidedReview&&Game.CanvasFeedback==note,"Returning from overview preserves the unacknowledged guided result");
                yield return Shot("guided-success-module-"+module);
                yield return Click("CalibrationNext");
                Check(!Game.CaseGuidedReview&&Game.CaseCalibration!=null&&Game.Session.attempts.Count==attempts&&Game.Session.completed.Count==completed,"Continue opens independent question without rescoring");
            }
            while(Game.Topic==2&&Game.CaseCalibration!=null)
            {
                yield return null;var q=Game.CaseCalibration;int beforeModules=Game.ModulesComplete;CheckFeedbackCleared("New Integration calibration has no previous verdict: "+q.id);if(q.id=="IX_DIAL1")yield return Shot("feedback-integration-new-question");
                Check(Game.ExtendedCase&&Game.Session.required==18&&Control<MathText>("TaskPrompt").Source==q.prompt,"Native Case calibration shown: "+q.id);
                Game.ContinueCaseCalibration();Check(Game.CaseCalibration?.id==q.id,"Unanswered calibration cannot be skipped");
                Game.World.LiftMasterCover();Game.Activate();Check(!Game.World.MasterCoverOpen&&Game.State=="Puzzle","Master action stays locked during calibration");
                if(q.module==0)Check(!Game.MissionSelectModule(1),"Dial base answer alone cannot unlock the other modules");
                if(q.id=="IX_DIAL1")
                {
                    yield return ControlsGuideCheck("case-calibration-help");yield return Click("CalibrationOption2");yield return Click("CalibrationSubmit");
                    Check(!Game.MissionDone(q.id)&&Game.Session.attempts.Last().error=="MISSING_C"&&Game.CanvasFeedback.Contains("needs +C"),"Calibration retry diagnoses missing arbitrary constant");
                    Check(Control<TMP_Text>("JourneyPhaseLabel1").text.StartsWith(">"),"Retry keeps independent checks active");yield return Shot("case-calibration-retry");
                }
                int option=q.id=="IX_DIAL1"?1:q.id=="IX_DIAL2"?2:q.id=="IX_WIRE1"?0:q.id=="IX_WIRE2"?1:q.id=="IX_SEQ1"?0:q.id=="IX_SEQ2"?2:q.id=="IX_BOUND1"?2:1;
                yield return Click("CalibrationOption"+option);CheckFeedbackCleared("Changing calibration choice clears prior feedback: "+q.id);int attempts=Game.Session.attempts.Count;yield return Click("CalibrationSubmit");
                Check(Game.MissionDone(q.id)&&Game.Session.attempts.Count==attempts+1,"Calibration accepted once: "+q.id);Game.SubmitCaseCalibration(option);Check(Game.Session.attempts.Count==attempts+1,"Accepted calibration cannot be resubmitted");
                Check(Game.ModulesComplete==beforeModules+(q.id.EndsWith("2",StringComparison.Ordinal)?1:0),"Module becomes stable only after both independent answers: "+q.id);
                Check(Control<TMP_Text>("JourneyPhaseLabel0").text.StartsWith("[OK]")&&Control<TMP_Text>("JourneyPhaseLabel1").text.Contains(q.id.EndsWith("2",StringComparison.Ordinal)?"2 / 2":"1 / 2"),"Journey reflects accepted module questions");
                var selected=Control<UnityEngine.UI.Outline>("CalibrationOption"+option).effectColor;Check(selected.g>selected.b&&selected.g>selected.r,"Accepted selection is visibly green");
                for(int i=0;i<3;i++){var label=Control<TMP_Text>("CalibrationOption"+i+"Label");label.ForceMeshUpdate();Check(label.GetComponent<MathText>()!=null&&!label.GetParsedText().Contains("^")&&!label.GetParsedText().Contains("sqrt("),"Calibration options render authored mathematical notation: "+q.id);}
                yield return Shot("case-bank-"+q.id);
                if(q.id=="IX_DIAL1")
                {
                    string id=Game.Session.id;int accepted=Game.Session.completed.Count;yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);
                    Check(Game.Session.id==id&&Game.Session.completed.Count==accepted&&Game.ModulesComplete==0&&Game.World.CaseAudio.CueCount==0,"Partial calibration resumes silently without granting module credit");
                    yield return Inspect();yield return Click("Module0");Check(Game.CaseCalibration?.id=="IX_DIAL2","Resume restores the next unfinished dial calibration");continue;
                }
                bool finished=Game.CaseModuleComplete(Game.Module);int scored=Game.Session.attempts.Count,completed=Game.Session.completed.Count,hearts=Game.Session.heartsUsed;
                yield return Click("CalibrationNext");
                bool overview=Control<Transform>("CaseOverview").gameObject.activeInHierarchy;
                Check(overview==finished,"Next calibration stays in module; Finish module returns to overview: "+q.id);
                Check(Game.State=="Puzzle"&&Game.Session.attempts.Count==scored&&Game.Session.completed.Count==completed&&Game.Session.heartsUsed==hearts,"Calibration navigation preserves accepted work and assessment evidence");
                File.AppendAllText(Path.Combine(folder,"navigation-checks.txt"),(overview==finished?"PASS: ":"FAIL: ")+q.id+" -> "+(overview?"overview":"next question")+"\n");
                if(finished){Check(Game.CaseCalibration==null&&!Control<Transform>("CaseModule").gameObject.activeInHierarchy&&!Control<Transform>("CaseBack").gameObject.activeInHierarchy,"Finished module closes directly without a redundant back click");yield return Shot("finish-module-"+q.module+"-overview");}
                else Check(Game.CaseCalibration!=null&&!Game.MissionDone(Game.CaseCalibration.id),"Next calibration opens an unfinished question");
            }
        }
        IEnumerator EnterLimitsConsole()
        {
            yield return RouteWalk(new Vector3(0,0,-1));yield return Key(UnityEngine.InputSystem.Key.E);Check(Game.State=="Puzzle","Limits console opens after real walking");
        }
        IEnumerator ExpandedLimitsChecks()
        {
            yield return Click("HomeStart");yield return Click("Mission0");yield return new WaitForSeconds(.5f);yield return Click("BriefStart");yield return EnterLimitsConsole();
            Check(Game.ExtendedLimits&&Game.Session.required==10,"New Limits run requires ten questions across five calibrations");
            for(int stage=0;stage<5;stage++)
            {
                Check(Game.Stage==stage&&Game.LimitsExtension==null,"Guided task precedes its independent check");
                if(stage==0)Control<TMP_InputField>("Answer0").text="5";
                if(stage==1)Control<UnityEngine.UI.Slider>("Marker").value=4;
                if(stage==2)for(int i=0;i<3;i++)Control<TMP_InputField>("Answer"+i).text="3";
                if(stage==3){Control<TMP_InputField>("Answer0").text="4";Control<TMP_InputField>("Answer1").text="7";}
                if(stage==4){yield return Click("Conclusion2");for(int i=0;i<3;i++){yield return Click("ReasonCard"+i);yield return Click("ReasonSlot"+i);}}
                yield return Click("Submit");Check(Game.CanvasAccepted,"Guided Limits answer accepted");yield return Click("Continue");
                Check(Game.Stage==stage&&Game.LimitsExtension?.id=="LX"+(stage+1),"Independent check gates portal charge");CheckFeedbackCleared("New Limits check has no previous verdict");
                Game.CanvasContinue();Game.Activate();Check(Game.Stage==stage&&Game.State=="Puzzle"&&!Game.Ready,"Blank independent answer cannot charge or activate portal");
                if(stage==0)
                {
                    Control<TMP_InputField>("Answer0").text="10";yield return Click("Submit");Check(!Game.CanvasAccepted&&Game.Session.heartsUsed==0,"Wrong independent limit preserves calibration and hearts");
                    Check(!Game.CanvasFeedback.Contains("11")&&!Game.CanvasFeedback.Contains("12 - 1"),"Limits retry does not disclose the answer from the screenshot");yield return Shot("limits-retry-without-answer");
                    yield return Click("PuzzleSupport");
                    Check(Control<HintPanelSelection>("SupportModal").SelectedTier==-1,"Opening support shows a neutral choice before buying any hint");
                    float time=Game.Session.elapsed;yield return new WaitForSeconds(.15f);yield return Click("SupportTier0");
                    Check(Control<HintPanelSelection>("SupportModal").SelectedTier==0&&Control<TMP_Text>("HintContentLabel").text.Contains("BASIC"),"Limits hint caption and selected tier match the visible content");
                    Check(Game.Session.elapsed==time&&Game.Session.support.Last().step=="LX1"&&Game.Session.heartsUsed==0,"Limits independent support uses correct ID and pauses time for free");yield return Click("SupportClose");
                }
                if(stage==2)
                {
                    string id=Game.Session.id;int count=Game.Session.completed.Count;yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);
                    Check(Game.Session.id==id&&Game.Stage==2&&Game.LimitsExtension?.id=="LX3"&&Game.Session.completed.Count==count,"Limits mid-calibration resume retains question and 40 percent charge");yield return EnterLimitsConsole();
                }
                if(stage==0){string evidence=JsonUtility.ToJson(Game.Session);Control<TMP_InputField>("Answer0").text="11";CheckFeedbackCleared("Limits numeric edit clears the old retry");Check(JsonUtility.ToJson(Game.Session)==evidence,"Limits feedback clearing preserves retry history and progress");yield return Shot("feedback-limits-edited");}
                else if(stage==2)Control<TMP_InputField>("Answer0").text="3";
                else
                {
                    if(stage==4){yield return Click("Conclusion0");yield return Click("Submit");Check(!Game.CanvasAccepted,"Unequal-side independent retry remains open");}
                    yield return Click("Conclusion"+(stage==1?0:stage==3?1:2));CheckFeedbackCleared("Limits conclusion edit clears prior feedback");
                }
                yield return Click("Submit");Check(Game.CanvasAccepted,"Independent Limits answer accepted");
                Check(Control<TMP_Text>("JourneyPhaseLabel2").text.StartsWith(">")&&Control<TMP_Text>("JourneyDetail").text.Contains("Apply calibration"),"Accepted pair shows pending application while charge is unchanged");
                if(stage==0||stage==3)CheckSuperscript("QuestionPrompt");
                int attempts=Game.Session.attempts.Count;Game.CanvasSubmit(Game.LimitsExtension,new LimitsAnswer());Check(attempts==Game.Session.attempts.Count,"Accepted independent limit cannot be scored twice");
                yield return Shot("limits-bank-LX"+(stage+1));yield return Click("Continue");Check(Game.Stage==stage+1&&Game.Session.completed.Count==2*(stage+1),"Each completed pair restores one portal section");
            }
            yield return new WaitForSeconds(.7f);yield return Shot("limits-bank-ready");Check(Game.Ready&&Game.Session.completed.Count==10&&Game.Session.Completion==100,"All ten answers required before portal activation");
            yield return Click("ActivatePortal");while(Game.State=="Finishing")yield return null;yield return Shot("limits-bank-summary");
            Check(Game.Session.status=="Completed"&&Game.Session.attempts.Count==12&&Game.Session.heartsUsed==0&&Chapter4Store.Read().Count==1,"Limits saves ten answers, two retries and free support in one completed run");
            string sessionId=Game.Session.id,metrics=Game.Session.Summary;
            Check(Control<TMP_Text>("SummaryReplayLabel").text=="Replay mission","Limits labels replay separately from focused practice");
            yield return Click("SummaryPractice");
            Check(Control<Transform>("PracticeModal").gameObject.activeInHierarchy&&Game.State=="Summary"&&Game.Session.Summary==metrics,"Limits summary opens focused practice without restarting or changing mainline metrics");
            yield return Shot("limits-summary-practice-entry");
            yield return Click("PracticeClose");
            Check(!Control<Transform>("PracticeModal").gameObject.activeSelf&&Game.State=="Summary"&&Game.Session.Summary==metrics,"Closing Limits practice returns to the same completed result");
            var statistics=Control<TMP_Text>("SummaryMetrics");statistics.ForceMeshUpdate();
            var practiceButton=Control<RectTransform>("SummaryPractice");
            Check(statistics.transform.TransformPoint(statistics.textBounds.min).y>=practiceButton.TransformPoint(new Vector3(0,practiceButton.rect.yMax,0)).y,"Limits practice button stays below every rendered statistics line");
            yield return Shot("limits-summary-practice-return");
            yield return Click("SummaryRecords");yield return Click("Record0");
            Check(Control<Transform>("DetailPractice").gameObject.activeInHierarchy,"Completed Limits record offers focused practice");
            yield return Click("DetailPractice");CheckPracticeMathLayouts();
            for(int i=0;i<3;i++)
            {
                var saved=Chapter4Store.Read().Single(s=>s.id==sessionId);var run=saved.practice.Last();
                int answer=MissionPractice.Questions(run.focus,run.questionSetVersion)[i].correct;
                yield return Click("PracticeOption"+answer);yield return Click("PracticeSubmit");yield return Click("PracticeNext");
            }
            yield return Shot("limits-focused-practice-review");yield return Click("PracticeClose");
            var final=Chapter4Store.Read().Single(s=>s.id==sessionId);
            Check(final.practice.Last().Complete&&final.practice.Last().Result.StartsWith("3 / 3")&&final.practice.Last().questionSetVersion==1&&final.Summary==metrics,"Limits practice saves revised questions separately without changing mission score");
        }
    }
}
#endif
