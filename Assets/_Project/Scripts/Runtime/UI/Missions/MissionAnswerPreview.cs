using System.Linq;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        readonly int[] wireDraftTargets={-1,-1,-1};
        public bool ShowingCaseOverview=>topic==2&&caseOverview;
        public string CurrentAnswerPreview
        {
            get
            {
                if(Game==null||Game.Ready&&topic==1)return "";
                if(topic==1)
                {
                    var extra=Game.BridgeExtension;
                    if(lastBridgeQuestion!=(extra?.id??"D"+Game.Stage))return "";
                    if(extra!=null)return "f'(x) = "+(extra.choiceAnswer?AnswerPreview.Choice(extra.options,choice):AnswerPreview.Power(Value("BridgeA"),Value("BridgeB")));
                    int stage=Game.Stage;
                    if(stage==0)return "f'(x) = "+AnswerPreview.Power(coefficient?"4":"",exponent.ToString());
                    if(stage==7||stage==10)return "f'(x) = "+AnswerPreview.Choice(MissionRules.BridgeChoices(stage),choice);
                    if(stage==1)return "sqrt(x) = x^("+AnswerPreview.Value(Value("BridgeA"))+")";
                    if(stage==8)return "1/x^2 = x^("+AnswerPreview.Value(Value("BridgeA"))+")";
                    if(stage==6)return "d(7)/dx = "+AnswerPreview.Value(Value("BridgeA"));
                    if(stage==9)return "d(-2x)/dx = "+AnswerPreview.Value(Value("BridgeA"));
                    return (stage==4||stage==5?"Current term derivative:\n":"f'(x) = ")+AnswerPreview.Power(Value("BridgeA"),Value("BridgeB"));
                }
                if(caseOverview)return "";
                var calibration=Game.CaseCalibration;
                if(calibration!=null)return (Game.Module==3?"Definite integral = ":"F(x) = ")+AnswerPreview.Choice(calibration.options,lastCaseCalibration==calibration.id?calibrationChoice:-1);
                if(Game.Module==0)return "F(x) = x^("+exponent+") / "+divisor+(constant?" + C":"\nConstant: □");
                if(Game.Module==1)return AnswerPreview.Wires(Enumerable.Range(0,3).Select(i=>Game.MissionDone("I_W"+i)?i:wireDraftTargets[i]).ToArray(),Game.MissionDone("I_C"));
                if(Game.Module==2){int next=SequenceStep();return AnswerPreview.Sequence(next,next<4?order[next]:-1,Value("SequenceValue"));}
                return "Integral from 0 to 2:\n2x dx = "+release+(Game.MissionDone("I_GAUGE")?"\nF(2) - F(0) = 4 - 0 = 4":"");
            }
        }
        void ConnectWire(int source,int target)
        {
            if(Game.State!="Puzzle"||Game.Module!=1||source<0||source>=3||target<0||target>=MissionRules.WireOutputs.Length||Game.MissionDone("I_W"+source)||Game.CanvasSupportOpen)return;
            wireDraftTargets[source]=target;
            Game.MissionWire(source,target);
        }
    }
}
