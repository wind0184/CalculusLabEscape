#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator SolveBridgeExtensions()
        {
            while(Game.Topic==1&&Game.BridgeExtension!=null)
            {
                yield return null;
                if(Control<Transform>("IntroductionModal").gameObject.activeSelf)yield return IntroductionCheck();
                var q=Game.BridgeExtension;int stage=Game.Stage,parts=Game.BridgeParts;CheckFeedbackCleared("New independent Bridge question has no previous verdict: "+q.id);
                Check(Game.Session.bridgeCurriculumVersion==1&&Game.Session.required==18,"New Bridge has 18 assessed steps across twelve expression groups");
                Check(Control<MathText>("TaskPrompt").Source==q.prompt,"Real Canvas shows independent question "+q.id);
                Game.CanvasContinue();Check(Game.Stage==stage&&Game.BridgeExtension==q,"Unanswered independent task cannot install its component");
                if(q.id=="DX_BASIC_3")
                {
                    Input("BridgeA","3/2");Input("BridgeB","3/2");yield return Click("RepairSubmit");
                    Check(!Game.CanvasAccepted&&Game.CanvasFeedback.Contains("coefficient is correct"),"Fractional-power retry diagnoses correct coefficient and wrong exponent");
                    string id=Game.Session.id;int attempts=Game.Session.attempts.Count,completed=Game.Session.completed.Count,hearts=Game.Session.heartsUsed;
                    yield return SaveReturn();yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);
                    Check(Game.Stage==stage&&Game.BridgeExtension?.id==q.id&&Game.Session.id==id&&Game.Session.attempts.Count==attempts&&Game.Session.completed.Count==completed&&Game.Session.heartsUsed==hearts,"Resume restores unfinished independent question and its evidence without skipping its gate");
                    yield return Inspect();Check(!Game.CanvasAccepted&&Control<TMPro.TMP_InputField>("BridgeA").text=="","Resumed independent task restores clean answer controls");
                }
                if(q.choiceAnswer)
                {
                    if(q.id=="DX_APPLY_2"){yield return Click("BridgeChoice0");yield return Click("RepairSubmit");Check(!Game.CanvasAccepted&&Game.CanvasFeedback.Contains("positive"),"Application retry explains product of two negatives");}
                    yield return Click(q.id=="DX_APPLY_2"?"BridgeChoice1":"BridgeChoice2");CheckFeedbackCleared("Changing derivative choice clears prior feedback");
                }
                else
                {
                    string first=q.id=="DX_BASIC_1"?"70":q.id=="DX_BASIC_2"?"-4":q.id=="DX_BASIC_3"?"3/2":q.id=="DX_BASIC_4"?"-5":"16";
                    string second=q.id=="DX_BASIC_1"?"9":q.id=="DX_BASIC_2"?"-5":q.id=="DX_BASIC_3"?"1/2":q.id=="DX_BASIC_4"?"-5":"-9";
                    Input("BridgeA",first);Input("BridgeB",second);
                }
                int before=Game.Session.attempts.Count;yield return Click("RepairSubmit");
                Check(Game.CanvasAccepted&&Game.Session.completed.Contains(q.id)&&Game.Session.attempts.Count==before+1,"Independent answer scored once: "+q.id);
                Game.MissionRepair("0","0",true,4,0);
                Check(Game.Session.attempts.Count==before+1,"Accepted independent answer cannot be submitted twice: "+q.id);
                Check(Game.Stage==stage&&Game.BridgeParts==parts,"Correct answer waits for install / continue: "+q.id);
                Check(Control<TMPro.TMP_Text>("JourneyPhaseLabel0").text.StartsWith("[OK]")&&Control<TMPro.TMP_Text>("JourneyHeading").text.Contains("INDEPENDENT CHECK"),"Bridge journey distinguishes checks from installation");
                yield return Shot("pdf-bank-"+q.id);yield return Click("RepairContinue");
            }
        }
    }
}
#endif
