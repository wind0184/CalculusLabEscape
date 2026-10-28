#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class FeedbackRevisionChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception("Feedback revision: "+message);}
        public static void Validate()
        {
            foreach(string name in new[]{"LimitsQuestions","LimitsIndependentQuestions"})
            foreach(var q in AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/"+name+".asset").questions)
            {
                var wrong=new LimitsAnswer{values=new[]{"999","999","999"},marker=1,conclusion=q.kind==LimitsAnswerKind.Choice?(q.correctOption+1)%3:0,reason=new[]{2,1,0}};
                Check(!LimitsRules.Evaluate(q,wrong).correct,"Incorrect fixture must be rejected: "+q.id);
                string retry=MissionFeedback.Limits(q,wrong).message;
                Check(retry!=q.explanation&&!retry.Contains("= 11")&&!retry.Contains("limit is 5")&&!retry.Contains("Limit 3"),"Retry must give process guidance: "+q.id);
                Check(q.demonstration.StartsWith("Similar example:")&&q.demonstration!=q.explanation,"Limits examples differ from the current solution: "+q.id);
                var correct=new LimitsAnswer{marker=q.kind==LimitsAnswerKind.GraphMarker?(int)q.expected[0]:1,conclusion=q.kind==LimitsAnswerKind.Choice?q.correctOption:2,reason=new[]{0,1,2}};
                for(int i=0;i<q.expected.Length;i++)correct.values[i]=q.expected[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
                Check(LimitsRules.Evaluate(q,correct).correct&&MissionFeedback.Limits(q,correct).message==q.explanation,"Correct answers retain full explanations: "+q.id);
                if(q.kind==LimitsAnswerKind.Number){wrong.values[0]="1/0";Check(MissionFeedback.Limits(q,wrong).code=="INPUT_FORMAT","Invalid number diagnosed separately: "+q.id);}
            }
            var guided=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsQuestions.asset").questions;
            Check(MissionFeedback.Limits(guided[2],new LimitsAnswer{values=new[]{"3","0","0"}}).message.StartsWith("The left-hand value is correct"),"Partial one-sided work retained");
            Check(MissionFeedback.Limits(guided[3],new LimitsAnswer{values=new[]{"4","0",""}}).message.StartsWith("The limit is correct"),"Partial limit/point work retained");
            foreach(var q in DifferentiationQuestionBank.Load().questions)
            {
                Check(!string.IsNullOrEmpty(q.basicHint)&&q.ruleReminder==MissionHints.DerivativeRuleFor(q.id)&&q.demonstration.StartsWith("Similar example:")&&!q.demonstration.Contains(q.explanation),"Independent derivative support is authored separately: "+q.id);
                if(q.id=="DX_BASIC_3")Check(q.basicHint.Contains("fractional power")&&!q.basicHint.Contains("m/k")&&q.ruleReminder.Contains("root index")&&!q.basicHint.Contains("3/2")&&!q.ruleReminder.Contains("3/2"),"Root Basic Hint prompts a rewrite; Rule Reminder explains the rule without supplying the current answer");
                if(q.id=="DX_APPLY_2")Check(q.ruleReminder.Contains("x^(p-q)")&&q.ruleReminder.Contains("Split")&&!q.ruleReminder.Contains(q.rewrite),"Fraction support teaches splitting and exponent subtraction without the current rewrite");
                if(q.id=="DX_APPLY_3")Check(q.ruleReminder.Contains("every numerator term")&&q.ruleReminder.Contains("numerical coefficient")&&q.ruleReminder.Contains("Subtract the denominator exponent")&&!q.ruleReminder.Contains(q.rewrite),"Fraction support includes the whole denominator, coefficients and exponent subtraction without the current rewrite");
                if(!q.choiceAnswer)Check(!MissionFeedback.BridgeIndependent(q,"0","0").message.Contains(q.rewrite),"Incorrect derivative does not reveal its rewrite: "+q.id);
            }
            foreach(var q in IntegrationQuestionBank.Load().questions)
                Check(!string.IsNullOrEmpty(q.hint)&&!string.IsNullOrEmpty(q.ruleReminder)&&q.demonstration.StartsWith("Similar example:")&&q.demonstration!=q.explanation,"Independent integral support uses another problem: "+q.id);
            Check(MissionHints.Integration(2,3,false,0).Contains("general indefinite integral")&&!MissionHints.Integration(2,3,false,0).Contains("root"),"Add-constant hint matches current step");
            Check(MissionHints.Integration(1,0,true,0).Contains("All three term connections"),"Common-constant hint follows accepted wires");
            Check(Enumerable.Range(0,4).Select(i=>MissionHints.Integration(2,i,false,0)).Distinct().Count()==4,"Four Sequence stages have distinct basic hints");
            var session=new Chapter4Session{topic=0};Check(MissionPractice.Focus(session)=="limits_nearby","Limits default focus");
            session.Submit("L4","wrong",false,"LIMIT_VS_FUNCTION_VALUE");Check(MissionPractice.Focus(session)=="limits_points","Limits point-value targeting");
            session.attempts.Clear();session.Submit("L5","wrong",false,"UNEQUAL_SIDES_DNE");Check(MissionPractice.Focus(session)=="limits_sides","Limits one-sided targeting");
            Check(MissionPractice.Focuses.Sum(f=>MissionPractice.Questions(f).Length)==30,"Thirty questions across ten practice sets");
            var oldPractice=new PracticeRun{focus="integration"};oldPractice.Submit(1);
            var newPractice=new PracticeRun{focus="integration",questionSetVersion=1};newPractice.Submit(1);
            Check(oldPractice.answers[0].prompt.Contains("6x^2")&&newPractice.answers[0].prompt.Contains("10x^4"),"Old saved practice retains its original questions; new practice uses the revised set");
            Check(MissionPractice.Questions("integration")[0].options[1]=="2x^5 + C"&&MissionPractice.Questions("bounds")[0].options[1]=="24"&&MissionPractice.Questions("bounds")[1].options[2]=="16","Replacement practice answers");
            MissionFeedbackChecks.Validate();MissionPracticeChecks.Validate();AnswerPreviewChecks.Validate();
            Debug.Log("FEEDBACK_REVISION_CHECKS_PASS / 10 Limits retries; partial diagnostics; 25 authored support sets; contextual guided hints; 30 practice questions; existing fraction, score and preview checks");
        }
        public static void ApplyAndBuild(){FeedbackRevisionAuthoring.Apply();Validate();ManualEditingMigration.BuildQA();}
    }
}
#endif
