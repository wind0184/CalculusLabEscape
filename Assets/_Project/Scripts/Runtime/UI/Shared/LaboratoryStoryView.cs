using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    // Narrative presentation reads the current profile's existing records; it never writes progress.
    public sealed class LaboratoryStoryView:MonoBehaviour
    {
        public GameManager manager;
        public TMP_Text summaryTitle,storyStatus;
        public string missionTitle;
        string lastSnapshot;
        public static int CompletedTopics(IEnumerable<Chapter4Session> rows)
        {
            return rows.Where(s=>s!=null&&s.topic>=0&&s.topic<3&&s.status=="Completed"&&s.Completion>=100).Select(s=>s.topic).Distinct().Count();
        }
        void LateUpdate()
        {
            if(manager==null||manager.Session==null||manager.State!="Summary"){lastSnapshot=null;return;}
            string key=manager.Session.id+"|"+Chapter4Store.Profile+"|"+Chapter4Store.LastError;
            if(lastSnapshot==key)return;lastSnapshot=key;RefreshFromRecords();
        }
        public void RefreshFromRecords()=>Present(CompletedTopics(Chapter4Store.Read()));
        public void Present(int completed)
        {
            completed=Mathf.Clamp(completed,0,3);
            summaryTitle.text=completed==3?"The laboratory is stable.":missionTitle;
            storyStatus.text=completed==3?"ALL THREE SIMULATIONS RESTORED  /  The lab is back online. Thank you, analyst.":"SYSTEM RECOVERY  "+completed+" / 3  /  Restore the remaining simulations to bring the laboratory back online.";
        }
    }
}
