#if UNITY_EDITOR
using System;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class MissionFeedbackChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Learning feedback: "+message);}
        public static void Validate()
        {
            Check(MissionFeedback.Bridge(2,"1/2","1/2",false,0,0).code=="DIFF_EXPONENT","Correct coefficient with wrong fractional exponent");
            Check(MissionFeedback.Bridge(3,"18","-7",false,0,0).code=="DIFF_COEFFICIENT","Sign error isolated from correct negative exponent");
            Check(MissionFeedback.Bridge(3,"18","-5",false,0,0).code=="POWER_RULE","Both controls need correction");
            Check(MissionFeedback.Bridge(2,"2/4","-2/4",false,0,0).code=="","Equivalent fractions remain correct");
            foreach(string invalid in new[]{"NaN","Infinity","1/0","apple","1e38/1e-38"})Check(MissionFeedback.Bridge(3,invalid,"-7",false,0,0).code=="INPUT_FORMAT","Invalid format is not a conceptual coefficient diagnosis: "+invalid);
            Check(MissionFeedback.Bridge(6,"7","",false,0,0).code=="CONSTANT_DERIVATIVE","Constant diagnosis");
            Check(MissionFeedback.Bridge(8,"2","",false,0,0).code=="RECIPROCAL_REWRITE","Reciprocal rewrite diagnosis");
            Check(MissionFeedback.Dial(4,1,false).code=="INT_EXPONENT","Dial diagnoses exponent first");
            Check(MissionFeedback.Dial(5,1,false).code=="INT_DIVISOR","Then diagnoses divisor");
            Check(MissionFeedback.Dial(5,5,false).code=="MISSING_C","C alone missing");
            Check(MissionFeedback.Dial(5,5,true).code=="","Accepted dial retains result explanation");
            Check(MissionFeedback.Wire(0,3).code=="INT_DIVISOR"&&MissionFeedback.Wire(0,3).message.Contains("9x^2"),"Wire feedback checks chosen output by differentiation");
            Check(MissionFeedback.Wire(1,4).message.Contains("8x"),"Second distractor has correct derivative");
            Check(MissionFeedback.Sequence(0,1,"1/2").code=="SEQUENCE_ORDER","Correct value cannot hide wrong operation");
            Check(MissionFeedback.Sequence(1,1,"1/2").code=="INT_EXPONENT","Correct card with wrong value");
            Check(MissionFeedback.Sequence(2,2,"3/2").code=="INT_DIVISOR","Dividing by a fractional exponent");
            Check(MissionFeedback.Sequence(3,3,"0").code=="MISSING_C"&&MissionFeedback.Sequence(3,3,"c").code=="","Constant chip accepts either case");
            var session=new Chapter4Session{topic=1,required=11};
            Check(MissionReview.Summary(session).Contains("0/6 accepted; 0 first try; 0 retries"),"Empty review does not infer mastery");
            session.Submit("D0","4x^4",false,"DIFF_EXPONENT");session.Submit("D0","4x^3",true,"");session.Submit("D1","1/2",true,"");
            session.attempts[0].feedback="Subtract one from the original exponent.";
            Check(MissionReview.Summary(session).Contains("1/6 accepted; 0 first try; 1 retry"),"Retry and first-try counts differ");
            Check(MissionReview.Summary(session).Contains("1/2 accepted; 1 first try; 0 retries"),"Skill boundaries do not mix steps");
            Check(MissionReview.Recommendation(session).Contains("subtracting one"),"Recommendation comes from actual error evidence");
            var loaded=JsonUtility.FromJson<Chapter4Session>(JsonUtility.ToJson(session));
            Check(loaded.attempts[0].feedback==session.attempts[0].feedback&&MissionReview.Summary(loaded)==MissionReview.Summary(session),"Feedback and review survive JSON round trip");
            var old=JsonUtility.FromJson<Chapter4Session>("{\"topic\":2,\"required\":10,\"attempts\":[{\"step\":\"I_DIAL\",\"correct\":false,\"error\":\"INTEGRATION_POWER_OR_C\"}],\"completed\":[]}");
            Check(old.attempts[0].feedback==null&&MissionFeedback.ErrorLabel(old.CommonError)=="Integration Power Rule / +C"&&MissionReview.Summary(old).Contains("0/5 accepted"),"Older records need no feedback-field migration");
            Debug.Log("MISSION_FEEDBACK_CHECKS_PASS / diagnosis, fractions, invalid input, recommendations, evidence and backward-compatible records");
        }
    }
}
#endif
