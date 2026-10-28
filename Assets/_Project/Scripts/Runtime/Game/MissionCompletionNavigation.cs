using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        bool completionTransition;
        public bool CanvasNextMission()
        {
            if(completionTransition||State!="Summary"||Session==null||Session.status!="Completed"||(Topic!=1&&Topic!=2))return false;
            Save();if(!string.IsNullOrEmpty(Chapter4Store.LastError))return false;
            completionTransition=true;ProductionCanvas.ResumeId=null;
            if(Topic==1)GameManager.ChangeScene(Scenes[2]);
            else{Chapter4Frontend.ReturnPage="Topics";GameProgress.ReturnToMenu();}
            return true;
        }
    }
    public sealed partial class MissionCanvas
    {
        void RefreshCompletionNavigation()
        {
            if(Game.State=="Summary"&&!string.IsNullOrEmpty(Chapter4Store.LastError))Text("SaveStatus","Could not save. Keep this result open and try again.");
            Enabled("SummaryNext",Game.State=="Summary"&&Game.Session.status=="Completed");
            Text("SummaryNextLabel",topic==1?"Next: Integration":"Choose another mission");
            Text("SummaryNextHint",!string.IsNullOrEmpty(Chapter4Store.LastError)?"Could not save. Stay here and try again.":topic==1?"Starts a new Integration run. Existing results stay saved.":"Return to mission selection. Existing results stay saved.");
        }
    }
}
