using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        public bool CanvasRestartMission()
        {
            if(completionTransition||State!="Paused"||!canvasSessionStarted||Session==null||Session.status!="InProgress"||(Topic!=1&&Topic!=2))return false;
            Save();if(!string.IsNullOrEmpty(Chapter4Store.LastError))return false;
            completionTransition=true;ProductionCanvas.ResumeId=null;GameManager.ChangeScene(Scenes[Topic]);return true;
        }
    }
    public sealed partial class MissionCanvas
    {
        bool restartSaveFailed;
        void SetupMissionRestart()
        {
            Control<UnityEngine.CanvasGroup>("Pause").interactable=true;
            
        }
        void RefreshMissionRestart()
        {
            if(Game.State=="Paused"&&!string.IsNullOrEmpty(Chapter4Store.LastError))Text("SaveStatus","Could not save. Your current run is still open.");
            Text("RestartError",restartSaveFailed?"Could not save. Your current run is still open. Try again.":"");
        }
    }
}
