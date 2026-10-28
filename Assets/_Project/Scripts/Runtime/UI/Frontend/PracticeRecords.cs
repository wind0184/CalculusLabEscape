using System.Linq;
using TMPro;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed partial class ProductionCanvas
    {
        PracticePanel recordPractice;
        string selectedRecordId;
        bool recordPracticeOpen;
        void SetupPracticeRecords()
        {
            recordPractice=new PracticePanel(Text,Show,(name,enabled)=>Control<Button>(name).interactable=enabled,CloseRecordPractice,isMenu?"Back to learning record":"Back to mission results");
            
        }
        void UpdatePracticeRecord(Chapter4Session s)
        {
            selectedRecordId=s.id;
            bool available=LearningApi.LocalPlay&&!teacherRecords&&MissionPractice.CanOpenRecord(s,Chapter4Store.Profile);
            Show("DetailPractice",available);
            if(available)Text("DetailPracticeLabel",MissionPractice.RecordAction(s));
        }
        void CloseRecordPractice()
        {
            recordPracticeOpen=false;Show("PracticeModal",false);
            if(!isMenu){modal=null;game.CanvasOpenSupport(false);return;}
            RefreshRecords();int index=records.FindIndex(s=>s.id==selectedRecordId);
            if(index>=0)OpenRecord(index);else OpenPage("Records");
        }
    }
}
