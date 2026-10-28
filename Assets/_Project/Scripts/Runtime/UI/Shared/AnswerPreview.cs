using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalculusLabEscape
{
    // Presentation of the learner's controls only. Never evaluates or awards an answer.
    public static class AnswerPreview
    {
        public static string Value(string value)=>string.IsNullOrWhiteSpace(value)?"□":value.Trim();
        public static string Choice(string[] options,int index)=>options!=null&&index>=0&&index<options.Length?options[index]:"□";
        public static string Power(string coefficient,string exponent)
        {
            string a=Value(coefficient),b=Value(exponent);
            if(a.Contains("/")||a.Contains(" "))a="("+a+")";
            return a+"x^("+b+")";
        }
        public static string Limits(LimitsQuestion question,LimitsAnswer answer)
        {
            string limit=string.IsNullOrWhiteSpace(question.approachPoint)?"Limit":"lim(x→"+question.approachPoint+") f(x)";
            switch(question.kind)
            {
                case LimitsAnswerKind.GraphMarker:return limit+" = "+answer.marker.ToString(CultureInfo.InvariantCulture);
                case LimitsAnswerKind.ThreeValues:return "Left-hand limit = "+Value(answer.values[0])+"\nRight-hand limit = "+Value(answer.values[1])+"\nTwo-sided limit = "+Value(answer.values[2]);
                case LimitsAnswerKind.LimitAndValue:return limit+" = "+Value(answer.values[0])+"\nf("+Value(question.approachPoint)+") = "+Value(answer.values[1]);
                case LimitsAnswerKind.Choice:return "Selected answer:\n"+Choice(question.options,answer.conclusion);
                case LimitsAnswerKind.Conclusion:
                    var reasons=new[]{"Left-hand limit","is not equal to","Right-hand limit"};
                    return "Conclusion: "+Choice(new[]{"Limit = 2","Limit = 5","Does not exist"},answer.conclusion)+"\nReason:\n"+string.Join(" ",Array.ConvertAll(answer.reason,n=>Choice(reasons,n)));
                default:return limit+" = "+Value(answer.values[0]);
            }
        }
        public static string Wires(int[] targets,bool commonConstant)
        {
            var terms=new List<string>();
            for(int i=0;i<3;i++)terms.Add(Choice(MissionRules.WireOutputs,targets[i]));
            string sum=terms[0];
            for(int i=1;i<terms.Count;i++)sum+=terms[i].StartsWith("-",StringComparison.Ordinal)?" - "+terms[i].Substring(1):" + "+terms[i];
            return "F(x) = "+sum+(commonConstant?" + C":"\nCommon constant: □");
        }
        public static string Sequence(int next,int card,string value)
        {
            if(next>=4)return "F(x) = (2/3)x^(3/2) + C";
            string heading="Step "+(next+1)+": "+Choice(MissionRules.SequenceCards,card)+"\n",draft=Value(value);
            // A misplaced operation is shown literally, without implying the expected operation.
            if(card!=next)return heading+"Entered value: "+draft;
            if(next==0)return heading+"sqrt(x) = x^("+draft+")";
            if(next==1)return heading+"1/2 + 1 = "+draft;
            if(next==2)return heading+"F(x) = "+Power(value,"3/2");
            return heading+"F(x) = (2/3)x^(3/2) + "+draft;
        }
    }
}
