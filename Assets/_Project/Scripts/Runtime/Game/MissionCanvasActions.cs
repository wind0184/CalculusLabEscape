using System;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        public bool MissionDone(string id)=>Done(id);
        public int BridgeParts=>Stage>=8?3:Stage>=4?2:Stage>=1?1:0;
        public float CaseRemaining=>Mathf.Max(0,600-caseElapsed);
        public bool PressureHeld=>pressurePaused||Session.safetyMode;
        public void MissionRepair(string first,string second,bool chip,int exponent,int option)
        {
            if(Topic!=1||State!="Puzzle"||Ready||accepted||supportModal)return;
            if(bridgeExtension!=null){SubmitBridgeExtension(first,second,option);return;}
            var result=MissionRules.Bridge(Stage,first,second,chip,exponent,option);
            if(!result.complete){feedback="Complete the answer controls before testing the repair.";return;}
            var note=MissionFeedback.Bridge(Stage,first,second,chip,exponent,option);
            accepted=ValidateStep("D"+Stage,result.response,result.correct,note.code,note.message);showRetry=false;
        }
        public bool MissionSelectModule(int module)
        {
            if(Topic!=2||State!="Puzzle"||module<0||module>3||module>0&&!CaseModuleComplete(0)||supportModal)return false;
            Module=module;caseCalibration=null;ResetInputs();
            if(CaseGuidedReview)
            {
                string step=new[]{"I_DIAL","I_C","I_SEQ_3","I_GAUGE"}[Module];
                feedback="CORRECT  /  "+Session.attempts.Last(a=>a.step==step&&a.correct).feedback;
            }
            return true;
        }
        bool CanCase(int module)=>Topic==2&&State=="Puzzle"&&Module==module&&!supportModal&&(module==0||CaseModuleComplete(0));
        bool CaseStep(string id,string response,bool valid,string error,string explanation)
        {
            int before=ModulesComplete;bool result=ValidateStep(id,response,valid,error,explanation,false);showRetry=false;
            if(result&&ExtendedCase&&id==new[]{"I_DIAL","I_C","I_SEQ_3","I_GAUGE"}[Module])guidedReviewModule=Module;
            World.SyncCaseFeedback(this);if(result||error!="INPUT_FORMAT")World.CaseAttemptSound(result,ModulesComplete>before,Module);return result;
        }
        public void MissionDial(int exponent,int divisor,bool constant)
        {
            if(!CanCase(0)||Done("I_DIAL"))return;
            var note=MissionFeedback.Dial(exponent,divisor,constant);
            CaseStep("I_DIAL",exponent+" | "+divisor+" | "+(constant?"C":"missing C"),exponent==5&&divisor==5&&constant,note.code,note.message);
        }
        public void MissionWire(int source,int target)
        {
            if(!CanCase(1)||source<0||source>2||target<0||target>4||Done("I_W"+source))return;
            var note=MissionFeedback.Wire(source,target);
            CaseStep("I_W"+source,MissionRules.WireInputs[source]+" -> "+MissionRules.WireOutputs[target],source==target,note.code,note.message);
        }
        public void MissionCommonConstant()
        {
            if(!CanCase(1)||Done("I_C")||!Done("I_W0")||!Done("I_W1")||!Done("I_W2"))return;
            CaseStep("I_C","C",true,"MISSING_C","One common +C gives x^3 + 2x^2 - 5x + C.");
        }
        public void MissionSequence(int cardIndex,string value)
        {
            if(!CanCase(2))return;int next=0;while(next<4&&Done("I_SEQ_"+next))next++;
            if(next==4)return;
            if(cardIndex<0||string.IsNullOrWhiteSpace(value)){feedback="Place a card in the current slot and enter its value.";return;}
            var note=MissionFeedback.Sequence(next,cardIndex,value);
            CaseStep("I_SEQ_"+next,"card="+cardIndex+";value="+value,cardIndex==next&&MissionRules.SequenceValue(next,value),note.code,note.message);
        }
        public void MissionGauge(int release)
        {
            if(!CanCase(3)||Done("I_GAUGE"))return;
            CaseStep("I_GAUGE",release.ToString(),release==4,"DEFINITE_ACCUMULATION",release==4?"F(x) = x^2. F(2) - F(0) = 4; no +C is needed.":release<4?"Too little release. Evaluate x^2 at 2, then subtract its value at 0.":"Too much release. The safety valve remains locked; compute F(2) - F(0).");
        }
        public string MissionHint(int tier,int selectedSource=-1)
        {
            if(!supportModal||tier<0||tier>2||Ready)return "Return to an active repair or module for support.";
            var calibration=Topic==2?CaseCalibration:null;
            int sequenceStep=Enumerable.Range(0,4).Count(n=>Done("I_SEQ_"+n));
            int wireStep=selectedSource>=0&&selectedSource<3&&!Done("I_W"+selectedSource)?selectedSource:Enumerable.Range(0,3).FirstOrDefault(n=>!Done("I_W"+n));
            bool allWires=Enumerable.Range(0,3).All(n=>Done("I_W"+n));
            string step=Topic==1?(bridgeExtension?.id??"D"+Stage):calibration?.id??(Module==0?"I_DIAL":Module==1?(allWires?"I_C":"I_W"+wireStep):Module==3?"I_GAUGE":"I_SEQ_"+System.Math.Min(sequenceStep,3));
            if(!Session.Hint(step,tier>0))return "No Support Hearts remain. Basic hints and the manual are free.";
            Session.support.Last().type=tier==2?"GuidedDemonstration":tier==1?"RuleReminder":"BasicHint";
            if(tier>0)pressurePaused=true;Save();
            if(bridgeExtension!=null)return ExtensionHint(tier);
            if(calibration!=null)return tier==2?calibration.demonstration:tier==1?calibration.ruleReminder:calibration.hint;
            return Topic==1?MissionHints.Bridge(Stage,tier):MissionHints.Integration(Module,Module==1?wireStep:sequenceStep,allWires,tier);
        }
        public void MissionResume(Chapter4Session session)
        {
            if(session==null||session.topic!=Topic||session.profile!=Chapter4Store.Profile||session.status!="InProgress")return;
            Session=session;Stage=0;canvasSessionStarted=true;pressurePaused=session.pressureHeld;caseElapsed=session.environmentElapsed;
            caseCalibration=null;guidedReviewModule=-1;
            if(Topic==1){while(Stage<11&&Done("D"+Stage)&&NextBridgeExtension(Stage)==null)Stage++;World.Restore(Stage>=11?4:BridgeParts);World.SetBridgeStage(Stage);World.station=World.devices[Stage==0?0:Stage<4?1:2];if(World.water!=null)World.water.position=new Vector3(0,session.waterHeight,9.5f);}
            else{World.SyncCaseFeedback(this,true);}
            ResetInputs();saveAt=Session.elapsed+10;SetState("Explore");
            bridgeExtension=Topic==1&&Done("D"+Stage)?NextBridgeExtension(Stage):null;
        }
    }
}
