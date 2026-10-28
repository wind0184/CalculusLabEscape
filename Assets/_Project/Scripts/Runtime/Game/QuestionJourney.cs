using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // The labels and panels are authored in the prefab, including their preview state.
    public sealed class QuestionJourney : MonoBehaviour
    {
        [SerializeField] TMP_Text caption, detail;
        [SerializeField] Image[] phases;
        [SerializeField] TMP_Text[] labels;
        public void Configure(TMP_Text heading, TMP_Text note, Image[] panels, TMP_Text[] texts)
        { caption=heading;detail=note;phases=panels;labels=texts; }
        public void Present(string heading,string note,string[] steps,int active)
        {
            caption.text=heading;detail.text=note;
            for(int i=0;i<3;i++)
            {
                labels[i].text=(i<active?"[OK] ":i==active?"> ":"")+steps[i];
                labels[i].color=i<active?new Color(.35f,.92f,.59f):i==active?new Color(.2f,.82f,.88f):new Color(.55f,.7f,.76f);
                phases[i].color=i==active?new Color(.055f,.18f,.22f):new Color(.045f,.085f,.11f);
            }
        }
        public static void Choice(Button button,bool selected,bool accepted)
        {
            var color=accepted&&selected?new Color(.35f,.92f,.59f):selected?new Color(.2f,.82f,.88f):new Color(.15f,.31f,.37f);
            button.GetComponent<Image>().color=selected?(accepted?new Color(.045f,.19f,.13f):new Color(.045f,.16f,.2f)):new Color(.055f,.09f,.12f);
            var border=button.GetComponent<Outline>();border.effectColor=color;border.effectDistance=new Vector2(selected?2:1,selected?-2:-1);
        }
    }
    public sealed partial class MissionCanvas
    {
        void RefreshQuestionJourney()
        {
            if(!nodes.ContainsKey("QuestionJourney"))return;
            var bridge=Game.BridgeExtension;var integral=Game.CaseCalibration;
            bool visible=topic==1?bridge!=null&&!Game.Ready:!caseOverview&&integral!=null;
            Show("QuestionJourney",visible);if(!visible)return;
            int total,done,index;string heading,next;
            if(topic==1)
            {
                var questions=DifferentiationQuestionBank.Load().questions.Where(q=>q.afterStage==Game.Stage).ToArray();
                total=questions.Length;done=questions.Count(q=>Game.MissionDone(q.id));index=System.Array.IndexOf(questions,bridge)+1;
                heading="INDEPENDENT CHECK "+index+" / "+total+"   |   POWER RULE";
                next=done==total?"All checks accepted. Install the component to apply the repair.":Game.CanvasAccepted?"Check accepted. Continue to the next calibration.":"Use exact numbers or fractions. Accepted work counts towards this component.";
                if(bridge.choiceAnswer&&!Game.CanvasAccepted)next="Select a derivative, then Test repair. You can revise your choice before checking.";
            }
            else
            {
                var questions=IntegrationQuestionBank.Load().questions.Where(q=>q.module==Game.Module).ToArray();
                total=questions.Length;done=questions.Count(q=>Game.MissionDone(q.id));index=System.Array.IndexOf(questions,integral)+1;
                heading="INDEPENDENT CHECK "+index+" / "+total+"   |   "+(Game.Module==3?"DEFINITE ACCUMULATION":"ANTIDERIVATIVES");
                next=done==total?"Module stable. Finish module to return to the device overview.":Game.MissionDone(integral.id)?"Check accepted. One more independent check completes this module.":"Select an expression, then Check calibration. A green lamp requires both checks.";
                for(int i=0;i<3;i++)QuestionJourney.Choice(Control<Button>("CalibrationOption"+i),calibrationChoice==i,Game.MissionDone(integral.id));
            }
            Control<QuestionJourney>("QuestionJourney").Present(heading,next,new[]{"01  GUIDED TASK","02  CHECKS "+done+" / "+total,topic==1?"03  INSTALL REPAIR":"03  MODULE STABLE"},done==total?2:1);
        }
    }
    public sealed partial class ProductionCanvas
    {
        void RefreshLimitsJourney()
        {
            if(!nodes.ContainsKey("QuestionJourney"))return;
            Show("QuestionJourney",game.ExtendedLimits);Show("PortalDescription",!game.ExtendedLimits);
            if(!game.ExtendedLimits)return;
            bool extra=game.LimitsExtension!=null,ready=extra&&game.CanvasAccepted;
            string note=ready?"Both answers accepted. Apply calibration to restore one gate segment.":extra?"Guided task accepted. Complete the independent check to unlock calibration.":"Complete both questions, then apply this calibration to restore the gate.";
            Control<QuestionJourney>("QuestionJourney").Present("CALIBRATION "+(game.Stage+1)+" / 5",note,new[]{"01  GUIDED TASK","02  INDEPENDENT CHECK","03  RESTORE GATE SEGMENT"},ready?2:extra?1:0);
        }
    }
}
