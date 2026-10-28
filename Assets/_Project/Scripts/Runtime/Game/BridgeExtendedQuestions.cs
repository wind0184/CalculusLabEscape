using System.Linq;

namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        DifferentiationQuestion bridgeExtension;
        public DifferentiationQuestion BridgeExtension=>bridgeExtension;
        public bool ExtendedBridge=>Topic==1&&Session!=null&&Session.bridgeCurriculumVersion==1;
        DifferentiationQuestion NextBridgeExtension(int stage)=>ExtendedBridge?DifferentiationQuestionBank.Load()?.Next(stage,Session.completed):null;
        bool ContinueBridgeExtension()
        {
            bridgeExtension=NextBridgeExtension(Stage);
            if(bridgeExtension==null)return false;
            ResetInputs();return true;
        }
        void SubmitBridgeExtension(string first,string second,int option)
        {
            var q=bridgeExtension;
            if(q.choiceAnswer)
            {
                if(option<0||option>=q.options.Length){feedback="Select a derivative before testing the repair.";return;}
                bool correct=option==q.correctOption;
                accepted=ValidateStep(q.id,q.prompt+" | "+q.options[option],correct,"TERM_ASSEMBLY",correct?q.explanation:q.optionFeedback[option]);
            }
            else
            {
                if(string.IsNullOrWhiteSpace(first)||string.IsNullOrWhiteSpace(second)){feedback="Enter the new coefficient and exponent. Fractions use /.";return;}
                bool coefficient=NumericAnswer(first,ParseExpected(q.coefficient)),power=NumericAnswer(second,ParseExpected(q.exponent));
                bool format=MissionFeedback.NumberFormat(first)&&MissionFeedback.NumberFormat(second);
                string code=!format?"INPUT_FORMAT":coefficient?"DIFF_EXPONENT":power?"DIFF_COEFFICIENT":"POWER_RULE";
                string note=MissionFeedback.BridgeIndependent(q,first,second).message;
                accepted=ValidateStep(q.id,q.prompt+" | coefficient="+first+"; exponent="+second,coefficient&&power,code,coefficient&&power?q.explanation:note);
            }
            showRetry=false;
        }
        static float ParseExpected(string text)
        {
            var parts=text.Split('/');float value=float.Parse(parts[0],System.Globalization.CultureInfo.InvariantCulture);
            return parts.Length==2?value/float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture):value;
        }
        string ExtensionHint(int tier)
        {
            var q=bridgeExtension;
            return tier==2?q.demonstration:tier==1?q.ruleReminder:q.basicHint;
        }
    }
}
