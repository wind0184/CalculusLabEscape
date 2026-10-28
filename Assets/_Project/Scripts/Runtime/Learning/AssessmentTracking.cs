using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalculusLabEscape
{
    public sealed partial class Chapter4Session
    {
        public static bool IsInvalidInput(LearningAttempt attempt)=>!attempt.correct&&attempt.error=="INPUT_FORMAT";
        public static bool IsIndependentQuestion(string id)=>!string.IsNullOrEmpty(id)&&(id.StartsWith("LX",StringComparison.Ordinal)||id.StartsWith("DX_",StringComparison.Ordinal)||id.StartsWith("IX_",StringComparison.Ordinal));
        public static string Percent(float value)=>value<0?"Not yet assessed":value.ToString("0",CultureInfo.InvariantCulture)+"%";
        public static string FeedbackText(bool correct,string error,string explanation)=>(!correct&&error=="INPUT_FORMAT"?"INVALID INPUT  /  ":correct?"CORRECT  /  ":"TRY AGAIN  /  ")+explanation;
        static string LegacyModule(string step)=>step=="I_DIAL"||step.StartsWith("IX_DIAL",StringComparison.Ordinal)?"I_MODULE_0":step.StartsWith("I_W",StringComparison.Ordinal)||step=="I_C"||step.StartsWith("IX_WIRE",StringComparison.Ordinal)?"I_MODULE_1":step.StartsWith("I_SEQ_",StringComparison.Ordinal)||step.StartsWith("IX_SEQ",StringComparison.Ordinal)?"I_MODULE_2":step=="I_GAUGE"||step.StartsWith("IX_BOUND",StringComparison.Ordinal)?"I_MODULE_3":null;
        IEnumerable<LearningSupport> SupportForStep(string step)=>(support??new List<LearningSupport>()).Where(s=>s.step==step||s.step==LegacyModule(step??""));
        static bool Before(LearningSupport hint,LearningAttempt attempt)
        {
            if(DateTimeOffset.TryParse(hint.timestamp,CultureInfo.InvariantCulture,DateTimeStyles.None,out var h)&&DateTimeOffset.TryParse(attempt.timestamp,CultureInfo.InvariantCulture,DateTimeStyles.None,out var a))return h<=a;
            return hint.elapsed<=attempt.elapsed;
        }
        public string[] HintTypesFor(LearningAttempt attempt)
        {
            // Snapshot support at submission. Older saves derive it from their
            // existing step and time evidence without rewriting the saved data.
            if(attempt.assessmentVersion>=1)return attempt.hintTypes??Array.Empty<string>();
            return SupportForStep(attempt.step).Where(s=>Before(s,attempt)).Select(s=>s.type).Where(t=>!string.IsNullOrEmpty(t)).Distinct().ToArray();
        }
        public bool IsAssisted(LearningAttempt attempt)=>attempt.assessmentVersion>=1?attempt.assisted:HintTypesFor(attempt).Length>0;
        public string CategoryFor(LearningAttempt attempt)=>IsInvalidInput(attempt)?"InvalidInput":!attempt.correct?"Incorrect":IsAssisted(attempt)?"AssistedCompletion":IsIndependentQuestion(attempt.step)?"IndependentCompletion":"GuidedCompletion";
        public string AttemptLabel(LearningAttempt attempt)
        {
            switch(CategoryFor(attempt))
            {
                case "InvalidInput":return "INVALID INPUT";
                case "AssistedCompletion":return "ASSISTED COMPLETION";
                case "IndependentCompletion":return "INDEPENDENT COMPLETION";
                case "GuidedCompletion":return "GUIDED COMPLETION";
                default:return "INCORRECT";
            }
        }
        public static string HintLabel(string type)=>type=="BasicHint"?"Basic Hint":type=="RuleReminder"?"Rule Reminder":type=="GuidedDemonstration"?"Guided Demonstration":type;
        public string HintHistory=>support==null||support.Count==0?"HINT HISTORY\nNo hints used.":"HINT HISTORY\n"+string.Join("\n",support.Select(h=>h.step+" / "+HintLabel(h.type)+" / "+(h.cost==0?"Free":h.cost+" Support Heart")+" / "+TimeSpan.FromSeconds(h.elapsed).ToString(@"mm\:ss")));
        public string AttemptHistory=>attempts.Count==0?"No submissions recorded in this session.":string.Join("\n\n",attempts.Select(a=>a.number+". "+a.step+"   "+AttemptLabel(a)+"\n"+a.response
            +(IsInvalidInput(a)?"\nExcluded from both Accuracy measures.":!a.correct?"\n"+MissionFeedback.ErrorLabel(a.error):"")
            +(HintTypesFor(a).Length>0?"\nSupport used: "+string.Join(", ",HintTypesFor(a).Select(HintLabel)):"")
            +(string.IsNullOrEmpty(a.feedback)?"":"\nFeedback: "+a.feedback)));
        public int MathematicalAttemptCount=>attempts.Count(a=>!IsInvalidInput(a));
        public int MathematicalCorrectCount=>attempts.Count(a=>a.correct);
        public int MathematicalIncorrectCount=>attempts.Count(a=>!a.correct&&!IsInvalidInput(a));
        public int InvalidInputCount=>attempts.Count(IsInvalidInput);
        public int IndependentAttemptCount=>attempts.Count(a=>!IsInvalidInput(a)&&IsIndependentQuestion(a.step)&&!IsAssisted(a));
        public int IndependentCorrectCount=>attempts.Count(a=>a.correct&&IsIndependentQuestion(a.step)&&!IsAssisted(a));
        public float IndependentAccuracy=>IndependentAttemptCount==0?-1:100f*IndependentCorrectCount/IndependentAttemptCount;
        public int IndependentCompletionCount=>attempts.Where(a=>a.correct&&IsIndependentQuestion(a.step)&&!IsAssisted(a)).Select(a=>a.step).Distinct().Count();
        public int AssistedCompletionCount=>attempts.Where(a=>a.correct&&IsAssisted(a)).Select(a=>a.step).Distinct().Count();
        public int GuidedCompletionCount=>attempts.Where(a=>a.correct&&!IsIndependentQuestion(a.step)&&!IsAssisted(a)).Select(a=>a.step).Distinct().Count();
    }
}
