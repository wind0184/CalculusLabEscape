using TMPro;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        int calibrationChoice=-1;
        string lastCaseCalibration;
        void ContinueCalibration()
        {
            if(Game.CaseGuidedReview){Game.ContinueCaseGuided();return;}
            var q=Game.CaseCalibration;
            if(Game.State!="Puzzle"||Game.CanvasSupportOpen||q==null||!Game.MissionDone(q.id))return;
            bool finished=Game.CaseModuleComplete(Game.Module);
            Game.ContinueCaseCalibration();
            if(finished){caseOverview=true;EndDrag();}
        }
        bool RefreshCaseCalibration()
        {
            if(!caseOverview&&Game.CaseGuidedReview)
            {
                Show("CaseCalibration",true);
                for(int i=0;i<4;i++)Show("ModuleControls"+i,false);
                for(int i=0;i<3;i++)Show("CalibrationOption"+i,false);
                Show("CalibrationSubmit",false);Show("CalibrationNext",true);
                Text("TaskTitle","GUIDED TASK ACCEPTED");
                Text("TaskPrompt","Your guided calculation is saved. Review the result below, then continue to the independent check.");
                Text("CalibrationNextLabel","Continue to independent check");
                return true;
            }
            var q=Game.CaseCalibration;bool active=!caseOverview&&q!=null;
            if(!nodes.ContainsKey("CaseCalibration"))return false;
            Show("CaseCalibration",active);if(!active)return false;
            if(lastCaseCalibration!=q.id){lastCaseCalibration=q.id;calibrationChoice=-1;Game.CanvasClearFeedback();EndDrag();}
            for(int i=0;i<4;i++)Show("ModuleControls"+i,false);
            bool done=Game.MissionDone(q.id);
            Text("TaskTitle",q.title);Text("TaskPrompt",q.prompt);
            for(int i=0;i<3;i++){Show("CalibrationOption"+i,true);Text("CalibrationOption"+i+"Label",(calibrationChoice==i?"[X] ":"[ ] ")+q.options[i]);Enabled("CalibrationOption"+i,!done);}
            Show("CalibrationSubmit",!done);Enabled("CalibrationSubmit",calibrationChoice>=0);
            Show("CalibrationNext",done);Text("CalibrationNextLabel",Game.CaseModuleComplete(Game.Module)?"Finish module":"Next calibration");
            return true;
        }
    }
}
