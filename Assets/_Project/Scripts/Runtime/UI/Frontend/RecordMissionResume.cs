using System;
using System.Globalization;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public static class RecordMissionResume
    {
        static string pendingId,pendingProfile,loadFailure;
        public static void Request(Chapter4Session s,string profile){pendingId=s.id;pendingProfile=profile;loadFailure=null;}
        public static string TakeLoadFailure(){string message=loadFailure;loadFailure=null;return message;}
        public static bool RestorePending(Chapter4Game game)
        {
            if(string.IsNullOrEmpty(pendingId))return false;
            string id=pendingId;pendingId=null;Chapter4Store.Profile=pendingProfile;pendingProfile=null;ProductionCanvas.ResumeId=null;
            var saved=Chapter4Store.Read().FirstOrDefault(s=>s.id==id);
            if(CanResume(saved,Chapter4Store.Profile)&&saved.topic==game.Topic)
            {if(game.Topic==0)game.CanvasResume(saved);else game.MissionResume(saved);}
            else{loadFailure="Saved run could not be restored. Choose another record.";Chapter4Frontend.ReturnPage="Dashboard";GameProgress.ReturnToMenu();}
            return true;
        }
        public static bool CanResume(Chapter4Session s,string profile)=>s!=null&&s.status=="InProgress"&&s.ownerId==0&&s.profile==profile&&s.topic>=0&&s.topic<Chapter4Game.Scenes.Length&&Guid.TryParseExact(s.id,"N",out _);
        public static string StartedLabel(string value)=>DateTime.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var started)?started.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"):"Start time unavailable";
    }
    public sealed partial class ProductionCanvas
    {
        bool recordMissionTransition;
        void UpdateRecordMissionResume(Chapter4Session session)
        {
            bool available=LearningApi.LocalPlay&&!teacherRecords&&RecordMissionResume.CanResume(session,Chapter4Store.Profile);
            Show("DetailResume",available);Control<Button>("DetailResume").interactable=available;
            Text("RecordResumeStatus",available?"Continue this saved run. Unsubmitted drafts start empty.":"This record remains available for review.");
        }
        void ResumeSelectedMission()
        {
            if(recordMissionTransition||!isMenu||page!="Detail"||recordPracticeOpen||!LearningApi.LocalPlay||teacherRecords)return;
            ResumeId=null;var saved=Chapter4Store.Read().FirstOrDefault(s=>s.id==selectedRecordId);
            if(!RecordMissionResume.CanResume(saved,Chapter4Store.Profile))
            {
                Control<Button>("DetailResume").interactable=false;
                Text("RecordResumeStatus",saved!=null&&saved.status=="Completed"?"This run is already complete. Return to records to review it.":"This saved run is no longer available. Return to records.");
                return;
            }
            recordMissionTransition=true;ResumeId=saved.id;RecordMissionResume.Request(saved,Chapter4Store.Profile);GameManager.ChangeScene(Chapter4Game.Scenes[saved.topic]);
        }
    }
}
