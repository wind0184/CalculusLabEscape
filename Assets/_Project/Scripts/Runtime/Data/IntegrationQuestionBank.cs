using System;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    [Serializable] public sealed class IntegrationCalibration
    {
        public string id,title;
        public int module,correctOption;
        [TextArea(2,5)] public string prompt,hint,explanation;
        [TextArea(2,5)] public string ruleReminder,demonstration;
        public string[] options,wrongFeedback,errorCodes;
    }
    [CreateAssetMenu(menuName="Calculus Lab/Integration question bank")]
    public sealed class IntegrationQuestionBank:ScriptableObject
    {
        public IntegrationCalibration[] questions;
        public static IntegrationQuestionBank Load()=>Resources.Load<IntegrationQuestionBank>("Production/IntegrationQuestions");
    }
    public partial class Chapter4Game
    {
        IntegrationCalibration caseCalibration;
        int guidedReviewModule=-1;
        public bool CaseGuidedReview=>Topic==2&&guidedReviewModule==Module;
        public void ContinueCaseGuided()
        {
            if(State!="Puzzle"||supportModal||!CaseGuidedReview)return;
            guidedReviewModule=-1;caseCalibration=null;feedback="";
        }
        public bool ExtendedCase=>Topic==2&&Session!=null&&Session.integrationCurriculumVersion==1;
        public bool CaseModuleComplete(int module)
        {
            if(module<0||module>3||!Done(new[]{"I_DIAL","I_C","I_SEQ_3","I_GAUGE"}[module]))return false;
            return !ExtendedCase||IntegrationQuestionBank.Load().questions.Where(q=>q.module==module).All(q=>Done(q.id));
        }
        public IntegrationCalibration CaseCalibration
        {
            get
            {
                if(!ExtendedCase||CaseGuidedReview||!Done(new[]{"I_DIAL","I_C","I_SEQ_3","I_GAUGE"}[Module]))return null;
                if(caseCalibration==null||caseCalibration.module!=Module)caseCalibration=IntegrationQuestionBank.Load().questions.FirstOrDefault(q=>q.module==Module&&!Done(q.id));
                return caseCalibration;
            }
        }
        public void SubmitCaseCalibration(int option)
        {
            var q=CaseCalibration;if(q==null||!CanCase(Module)||Done(q.id)||option<0||option>2)return;
            int before=ModulesComplete;bool correct=option==q.correctOption;
            ValidateStep(q.id,q.prompt+" | "+q.options[option],correct,correct?"":q.errorCodes[option],correct?q.explanation:q.wrongFeedback[option]);showRetry=false;
            World.SyncCaseFeedback(this);World.CaseAttemptSound(correct,ModulesComplete>before,Module);
        }
        public void ContinueCaseCalibration()
        {
            if(State!="Puzzle"||supportModal||CaseCalibration==null||!Done(caseCalibration.id))return;
            caseCalibration=null;feedback="";
        }
        public bool ExtendedLimits=>Topic==0&&Session!=null&&Session.limitsCurriculumVersion==1;
        public LimitsQuestion LimitsExtension {get;private set;}
        LimitsQuestion PendingLimitsExtension(int stage)
        {
            if(!ExtendedLimits||stage<0||stage>=5)return null;
            var q=Resources.Load<LimitsQuestionBank>("Production/LimitsIndependentQuestions").questions[stage];return Done(q.id)?null:q;
        }
        bool ContinueLimitsExtension()
        {
            LimitsExtension=PendingLimitsExtension(Stage);if(LimitsExtension==null)return false;ResetInputs();return true;
        }
    }
}
