#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class AssessmentTrackingChecks
    {
        static void Check(bool value,string note){if(!value)throw new Exception("Assessment tracking: "+note);}
        static void Hint(Chapter4Session s,string id,int tier)
        {
            Check(s.Hint(id,tier>0),"Support available");s.support.Last().type=new[]{"BasicHint","RuleReminder","GuidedDemonstration"}[tier];
        }
        public static void Validate()
        {
            var input=new Chapter4Session{topic=0,required=10};
            foreach(string value in new[]{"abc","1/0","NaN","Infinity"})
                Check(!input.Submit("LX1",value,false,"INPUT_FORMAT"),"Invalid inputs never complete a question");
            Check(input.attempts.Count==4&&input.InvalidInputCount==4&&input.Accuracy<0&&input.IndependentAccuracy<0&&input.CommonError=="NONE"&&input.completed.Count==0,"Invalid input is retained outside both denominators and math-error counts");
            Check(input.attempts.All(a=>a.category=="InvalidInput"&&a.consequence=="INPUT_VALIDATION"),"Invalid records are labelled and have no mathematical consequence");
            input.Submit("LX1","11",true,"");Check(input.Accuracy==100&&input.IndependentAccuracy==100&&input.MathematicalAttemptCount==1&&input.IndependentCompletionCount==1,"Correct after invalid has 100 percent mathematical and independent accuracy");
            int count=input.attempts.Count;input.Submit("LX1","11",true,"");Check(input.attempts.Count==count,"Accepted submission cannot be counted twice");

            for(int tier=0;tier<3;tier++)
            {
                var s=new Chapter4Session{topic=1,required=18};Hint(s,"DX_BASIC_1",tier);s.Submit("DX_BASIC_1","70 | 9",true,"");
                Check(s.Accuracy==100&&s.IndependentAccuracy<0&&s.IndependentAttemptCount==0&&s.AssistedCompletionCount==1&&s.IndependentCompletionCount==0,"Each hint tier makes completion assisted");
                Check(s.attempts[0].category=="AssistedCompletion"&&s.attempts[0].hintTypes.Single()==s.support[0].type&&s.heartsUsed==(tier==0?0:1),"Free basic hint and paid hints retain exact type and cost");
                var loaded=JsonUtility.FromJson<Chapter4Session>(JsonUtility.ToJson(s));
                Check(loaded.AssistedCompletionCount==1&&loaded.IndependentAccuracy<0&&loaded.HintHistory.Contains(Chapter4Session.HintLabel(s.support[0].type))&&loaded.AttemptHistory.Contains("ASSISTED COMPLETION"),"Assistance survives saved JSON and appears in the record text");
            }
            var guided=new Chapter4Session{topic=1,required=18};guided.Submit("D0","4x^3",true,"");
            Check(guided.GuidedCompletionCount==1&&guided.IndependentAccuracy<0,"Guided tasks do not inflate Independent Check accuracy");
            Hint(guided,"DX_BASIC_1",0);guided.Submit("DX_BASIC_1","70 | 9",true,"");
            Check(guided.Accuracy==100&&guided.IndependentAccuracy<0&&guided.AssistedCompletionCount==1&&guided.GuidedCompletionCount==1&&!guided.Band.Contains("STRONG"),"Guided plus assisted work is not labelled strong independent understanding");

            var retry=new Chapter4Session{topic=0};retry.Submit("LX1","10",false,"NUMERICAL_APPROACH");retry.Submit("LX1","11",true,"");
            Check(retry.Accuracy==50&&retry.IndependentAccuracy==50&&retry.MathematicalIncorrectCount==1,"Real mathematical retries remain in both eligible denominators");
            var retryHelp=new Chapter4Session{topic=0};retryHelp.Submit("LX1","10",false,"NUMERICAL_APPROACH");Hint(retryHelp,"LX1",0);retryHelp.Submit("LX1","11",true,"");
            Check(retryHelp.Accuracy==50&&retryHelp.IndependentAccuracy==0&&retryHelp.AssistedCompletionCount==1,"Assisted recovery does not erase an earlier independent error");

            var other=new Chapter4Session{topic=1};Hint(other,"DX_BASIC_1",0);other.Submit("DX_BASIC_2","-4 | -5",true,"");
            Check(other.IndependentAccuracy==100&&other.IndependentCompletionCount==1,"Help on another question does not taint the submission");
            Hint(other,"DX_BASIC_2",1);Check(other.IndependentAccuracy==100&&other.IndependentCompletionCount==1&&!other.IsAssisted(other.attempts[0]),"A hint requested after acceptance cannot retroactively change completion");

            var legacy=new Chapter4Session{topic=1};
            legacy.attempts.Add(new LearningAttempt{step="DX_BASIC_1",correct=true,timestamp="2026-10-25T10:00:01.0000000Z",elapsed=1});legacy.completed.Add("DX_BASIC_1");
            legacy.support.Add(new LearningSupport{step="DX_BASIC_1",type="BasicHint",timestamp="2026-10-25T10:00:02.0000000Z",elapsed=2});
            Check(legacy.IndependentAccuracy==100&&!legacy.IsAssisted(legacy.attempts[0]),"Older saves use time evidence, excluding later hints");
            legacy.support.Add(new LearningSupport{step="DX_BASIC_1",type="RuleReminder",timestamp="2026-10-25T10:00:00.0000000Z",elapsed=0});
            Check(legacy.AssistedCompletionCount==1&&legacy.IndependentAccuracy<0&&legacy.HintTypesFor(legacy.attempts[0]).SequenceEqual(new[]{"RuleReminder"}),"Older saves recover only support that existed at submission");
            legacy.attempts.Add(new LearningAttempt{step="LX1",correct=false,error="INPUT_FORMAT"});Check(legacy.InvalidInputCount==1&&legacy.Accuracy==100,"Older format errors are excluded without a destructive migration");
            var oldModule=new Chapter4Session{topic=2};oldModule.support.Add(new LearningSupport{step="I_MODULE_1",type="BasicHint",elapsed=1});oldModule.attempts.Add(new LearningAttempt{step="I_W0",correct=true,elapsed=2});
            Check(oldModule.AssistedCompletionCount==1,"Legacy module-scoped hints remain associated with their module");

            var evidence=new Chapter4Session{topic=0,required=10};
            evidence.Submit("LX1","10",false,"NUMERICAL_APPROACH");
            Check(evidence.Band=="INDEPENDENT PRACTICE IN PROGRESS","No accepted independent checks cannot be labelled completed");
            evidence.Submit("LX1","11",true,"");
            Check(evidence.Band=="INDEPENDENT CHECK COMPLETED","One completed independent check gets a factual, provisional label");
            evidence.Submit("LX2","choice 0",true,"");
            Check(evidence.IndependentCompletionCount==2&&!evidence.Band.Contains("UNDERSTANDING"),"Three submissions on only two distinct independent questions do not meet the threshold");
            Hint(evidence,"LX3",0);evidence.Submit("LX3","3",true,"");
            Check(evidence.IndependentCompletionCount==2&&evidence.AssistedCompletionCount==1&&!evidence.Band.Contains("UNDERSTANDING"),"An assisted completion does not supply independent evidence");
            evidence.Submit("LX4","choice 1",true,"");
            Check(evidence.IndependentCompletionCount==3&&evidence.IndependentAccuracy==75&&evidence.Band=="DEVELOPING INDEPENDENT UNDERSTANDING","Three independent completions retain existing accuracy-based developing band");
            evidence.Submit("LX5","choice 2",true,"");
            Check(evidence.IndependentAccuracy==80&&evidence.Band=="STRONG INDEPENDENT UNDERSTANDING","The strong band still requires independent accuracy of at least 80 percent");
            var clean=new Chapter4Session{topic=0};foreach(string id in new[]{"LX1","LX2","LX3"})clean.Submit(id,"correct",true,"");
            Check(clean.IndependentCompletionCount==3&&clean.Band=="STRONG INDEPENDENT UNDERSTANDING","Three different unassisted correct checks satisfy the minimum boundary");
            Hint(clean,"LX3",1);Check(clean.Band=="STRONG INDEPENDENT UNDERSTANDING","Viewing a hint after acceptance cannot remove previously earned evidence");
            var cleanLoaded=JsonUtility.FromJson<Chapter4Session>(JsonUtility.ToJson(clean));
            Check(cleanLoaded.Band==clean.Band&&cleanLoaded.IndependentCompletionCount==3,"Evidence threshold and classification survive persistence");
            var weak=new Chapter4Session{topic=0};foreach(string id in new[]{"LX1","LX2","LX3"}){weak.Submit(id,"wrong",false,"NUMERICAL_APPROACH");weak.Submit(id,"correct",true,"");}
            Check(weak.IndependentCompletionCount==3&&weak.IndependentAccuracy==50&&weak.Band=="INDEPENDENT PRACTICE RECOMMENDED","Meeting the evidence threshold does not override low independent accuracy");

            FeedbackRevisionChecks.Validate();Debug.Log("ASSESSMENT_TRACKING_CHECKS_PASS / three distinct unassisted completions before proficiency bands; accuracy thresholds retained; format exclusion; all three hint tiers; genuine retries; snapshots; later hints; legacy timestamps; saved records; unchanged math, previews and practice rules");
        }
        public static void ValidateAndBuild(){Validate();ManualEditingMigration.BuildQA();}
    }
}
#endif
