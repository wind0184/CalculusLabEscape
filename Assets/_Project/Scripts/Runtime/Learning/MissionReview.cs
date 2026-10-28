using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculusLabEscape
{
    public static class MissionReview
    {
        public static string Recommendation(Chapter4Session s)
        {
            if(s.MathematicalAttemptCount==0)return s.InvalidInputCount>0?"Use finite numbers or valid fractions, then submit a mathematical answer.":"Submit a step to start your review.";
            if(s.MathematicalIncorrectCount==0)return s.completed.Count<s.required?"Continue the unfinished steps.":s.AssistedCompletionCount>0?"Try an independent check without hints.":"Try a new example and explain each step.";
            switch(s.CommonError)
            {
                case "INPUT_FORMAT":return "Practise entering numbers and valid fractions.";
                case "DIFF_EXPONENT":case "NEGATIVE_POWER":return "Practise subtracting one from fractional and negative powers.";
                case "DIFF_COEFFICIENT":return "Practise multiplying coefficients by signed exponents.";
                case "ROOT_REWRITE":case "RECIPROCAL_REWRITE":case "EXPONENT_REWRITE":return "Practise rewriting roots and reciprocals as powers.";
                case "CONSTANT_DERIVATIVE":case "LINEAR_DERIVATIVE":return "Compare derivatives of constant and linear terms.";
                case "TERM_ASSEMBLY":return "Differentiate each term before combining the results.";
                case "INT_DIVISOR":return "Practise dividing by the raised exponent.";
                case "INT_EXPONENT":return "Practise raising fractional exponents by one.";
                case "MISSING_C":return "Check when an indefinite integral needs +C.";
                case "SEQUENCE_ORDER":case "SEQUENCE_OR_VALUE":return "Rehearse Rewrite > Raise > Divide > Add C.";
                case "DEFINITE_ACCUMULATION":return "Practise evaluating F(upper) - F(lower).";
                default:return s.topic==1?"Practise the Power Rule one term at a time.":"Differentiate antiderivatives to check each term.";
            }
        }
        static string Skill(Chapter4Session s,string name,params string[] steps)
        {
            var ids=new HashSet<string>(steps);int accepted=s.completed.Distinct().Count(ids.Contains);
            int first=s.attempts.Where(a=>ids.Contains(a.step)&&!Chapter4Session.IsInvalidInput(a)).GroupBy(a=>a.step).Count(g=>g.First().correct&&!s.IsAssisted(g.First()));
            int retries=s.attempts.Count(a=>ids.Contains(a.step)&&!a.correct&&!Chapter4Session.IsInvalidInput(a));
            int assisted=s.attempts.Count(a=>ids.Contains(a.step)&&a.correct&&s.IsAssisted(a));
            return name+": "+accepted+"/"+steps.Length+" accepted; "+first+" first try; "+retries+" retr"+(retries==1?"y":"ies")+(assisted>0?"; "+assisted+" assisted":"")+".";
        }
        public static string Summary(Chapter4Session s)
        {
            string next="THIS RUN / Next practice: "+Recommendation(s);
            if(s.topic==1)return next+"\n"+Skill(s,"Power Rule",new[]{"D0","D2","D3","D4","D5","D9"}.Concat(s.bridgeCurriculumVersion==1?new[]{"DX_BASIC_1","DX_BASIC_2","DX_BASIC_3","DX_BASIC_4","DX_BASIC_5"}:new string[0]).ToArray())+"\n"+Skill(s,"Rewriting powers","D1","D8")+"\n"+Skill(s,"Constants & assembly",new[]{"D6","D7","D10"}.Concat(s.bridgeCurriculumVersion==1?new[]{"DX_APPLY_2","DX_APPLY_3"}:new string[0]).ToArray());
            if(s.topic==2)return next+"\n"+Skill(s,"Power Rule & +C",new[]{"I_DIAL","I_SEQ_0","I_SEQ_1","I_SEQ_2","I_SEQ_3"}.Concat(s.integrationCurriculumVersion==1?new[]{"IX_DIAL1","IX_DIAL2","IX_SEQ1","IX_SEQ2"}:new string[0]).ToArray())+"\n"+Skill(s,"Term-by-term integration",new[]{"I_W0","I_W1","I_W2","I_C"}.Concat(s.integrationCurriculumVersion==1?new[]{"IX_WIRE1","IX_WIRE2"}:new string[0]).ToArray())+"\n"+Skill(s,"Definite integral",new[]{"I_GAUGE"}.Concat(s.integrationCurriculumVersion==1?new[]{"IX_BOUND1","IX_BOUND2"}:new string[0]).ToArray());
            return "";
        }
    }
}
