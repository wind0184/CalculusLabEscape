using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    // The same read-only recovery state used by mission results, shown on the lobby wall.
    public sealed class LaboratoryStoryBoards:MonoBehaviour
    {
        public TMP_Text heading,mainSystem,simulations,plan;
        string lastProfile;
        void LateUpdate()
        {
            if(lastProfile==Chapter4Store.Profile)return;lastProfile=Chapter4Store.Profile;RefreshFromRecords();
        }
        public void RefreshFromRecords()
        {
            int completed=LaboratoryStoryView.CompletedTopics(Chapter4Store.Read());bool stable=completed==3;
            heading.text=stable?"LAB ONLINE":"SYSTEM FAULT";heading.color=stable?new Color(.48f,.85f,.9f):new Color(.945f,.733f,.459f);
            mainSystem.text=stable?"MAIN AI ONLINE":"AI CORRUPTED";
            simulations.text=stable?"Simulations stable":completed==0?"3 unstable simulations":completed+" / 3 restored";
            plan.text=stable?"LAB RESTORED":"RESTORE THE LAB";
        }
    }
}
