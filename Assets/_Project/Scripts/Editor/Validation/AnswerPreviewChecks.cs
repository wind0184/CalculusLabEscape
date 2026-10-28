#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class AnswerPreviewChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Answer preview: "+message);}
        public static void Validate()
        {
            var bank=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsQuestions.asset");
            var a=new LimitsAnswer();
            Check(AnswerPreview.Limits(bank.questions[0],a)=="lim(x→3) f(x) = □","Empty limit input stays empty");
            a.values[0]="-9";Check(AnswerPreview.Limits(bank.questions[0],a).EndsWith("= -9"),"Wrong input is echoed, not replaced with 5");
            a.values=new[]{"2","5","DNE"};Check(AnswerPreview.Limits(bank.questions[2],a).Contains("Left-hand limit = 2\nRight-hand limit = 5\nTwo-sided limit = DNE"),"All three fields keep their meanings");
            Check(AnswerPreview.Limits(bank.questions[3],a).Contains("f(2) = 5"),"Point value is distinct from limit");
            a.conclusion=0;a.reason=new[]{2,-1,0};Check(AnswerPreview.Limits(bank.questions[4],a).Contains("Conclusion: Limit = 2\nReason:\nRight-hand limit □ Left-hand limit"),"Wrong/partial reason order stays literal");
            var independent=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsIndependentQuestions.asset");
            Check(AnswerPreview.Limits(independent.questions[1],new LimitsAnswer{conclusion=1}).EndsWith("Limit = 9"),"Choice echoes selected distractor");
            Check(AnswerPreview.Power("1/2","-1/2")=="(1/2)x^(-1/2)","Fractional coefficient is grouped");
            Check(AnswerPreview.Power("","")=="□x^(□)","Missing controls are not answered automatically");
            Check(AnswerPreview.Wires(new[]{0,1,2},false)=="F(x) = x^3 + 2x^2 - 5x\nCommon constant: □","Accepted terms do not imply a connected C");
            Check(AnswerPreview.Wires(new[]{3,-1,2},true)=="F(x) = 3x^3 + □ - 5x + C","Wrong wire targets and empty sockets are visible");
            Check(AnswerPreview.Sequence(0,1,"1/2")=="Step 1: Raise\nEntered value: 1/2","Wrong operation does not masquerade as Rewrite");
            Check(!AnswerPreview.Sequence(2,2,"").Contains("2/3"),"Future coefficient is not disclosed");
            Check(AnswerPreview.Sequence(2,2,"3/2").Contains("(3/2)x^(3/2)"),"Wrong sequence coefficient remains visible");
            Check(AnswerPreview.Sequence(4,-1,"")=="F(x) = (2/3)x^(3/2) + C","Completed sequence retains assembled result");
            Check(MathNotation.Format("x^(□)").Contains("<sup>"),"Blank exponent uses the same typesetting as entered exponents");
            Check(MathNotation.Format("sqrt(x^3)")=="√(x<sup><size=140%>3</size></sup>)","A root containing a power renders correctly");
            MathNotationChecks.Validate();MissionFeedbackChecks.Validate();
            foreach(string name in new[]{"LimitsUI","DifferentiationUI","IntegrationUI"})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Production/"+name+".prefab");
                var view=root.GetComponent<ReportFusionView>();
                Check(view.resultState!=null&&view.resultHeading!=null&&view.resultState.GetComponent<MathText>()!=null,"Saved preview references: "+name);
                Check(name=="LimitsUI"?view.limits!=null:view.mission!=null,"Saved input presenter: "+name);
                Check(root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="PowerPreview"||t.name=="DialPreview").All(t=>!t.gameObject.activeSelf),"Old result labels cannot overlap common preview: "+name);
                if(name!="LimitsUI")
                {
                    var wires=root.GetComponentInChildren<MissionConnections>(true);
                    Check(wires.sources.Length==3&&wires.targets.Length==5&&wires.sources.All(r=>r!=null)&&wires.targets.All(r=>r!=null),"Wires use saved socket references: "+name);
                }
            }
            Debug.Log("ANSWER_PREVIEW_CHECKS_PASS / blank, wrong, partial, selected and accepted answers; saved UI references; unchanged assessment rules");
        }
    }
}
#endif
