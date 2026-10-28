#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    // Opt-in observer for the existing UI-driven playtests. No input or scores are changed.
    [DefaultExecutionOrder(12000)]
    public sealed class AnswerPreviewEvidence:MonoBehaviour
    {
        string folder;
        readonly HashSet<string> seen=new HashSet<string>(),failures=new HashSet<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--answer-preview-evidence"))return;
            var go=new GameObject("Answer preview evidence");DontDestroyOnLoad(go);
            go.AddComponent<AnswerPreviewEvidence>().folder=LearningApi.Argument("--qa-output");
        }
        void Fail(string message){if(failures.Add(message))Debug.LogError("Answer preview evidence: "+message);}
        void LateUpdate()
        {
            var game=GameManager.Instance;
            if(game==null||game.Session==null||game.State!="Puzzle"||ScreenTransition.IsBusy)return;
            var view=game.GetComponentInChildren<ReportFusionView>(true)??FindFirstObjectByType<ReportFusionView>();
            if(view==null){Fail("Missing saved report view");return;}
            var mission=view.mission;
            if(mission!=null&&mission.ShowingCaseOverview)
            {
                if(view.resultHeading.text!="DEVICE STATUS")Fail("Overview is labelled as an answer preview");
                return;
            }
            if(game.Ready&&game.Topic!=2)return;
            string expected=mission!=null?mission.CurrentAnswerPreview:view.limits.CurrentAnswerPreview;
            if(!view.resultState.gameObject.activeInHierarchy)Fail("Preview hidden during an active question");
            var math=view.resultState.GetComponent<MathText>();
            if(math==null||math.Source!=expected)Fail("Visible preview differs from the current controls");
            view.resultState.ForceMeshUpdate();
            if(view.resultState.isTextOverflowing)Fail("Result text overflows: "+expected);
            var prompt=mission!=null?mission.Control<TMP_Text>("TaskPrompt"):view.limits.Control<TMP_Text>("QuestionPrompt");
            if(prompt.gameObject.activeInHierarchy){prompt.ForceMeshUpdate();if(prompt.isTextOverflowing)Fail("Question text overflows: "+prompt.text);}
            string id=game.Topic==0?(game.LimitsExtension?.id??"L"+(game.Stage+1)):game.Topic==1?(game.BridgeExtension?.id??"D"+game.Stage):game.CaseCalibration?.id??"module-"+game.Module;
            string state=game.CanvasAccepted?"accepted":string.IsNullOrEmpty(game.CanvasFeedback)?"draft":"feedback";
            string key=game.Topic+" / "+id+" / "+state;
            if(seen.Add(key)&&!string.IsNullOrEmpty(folder))File.AppendAllText(Path.Combine(folder,"preview-states.txt"),key+"\n"+expected+"\n\n");
        }
        void OnApplicationQuit()
        {
            if(!string.IsNullOrEmpty(folder))File.WriteAllText(Path.Combine(folder,"preview-result.txt"),(failures.Count==0?"PASS":"FAIL")+" / "+seen.Count+" question states observed; "+failures.Count+" preview failures.");
        }
    }
}
#endif
