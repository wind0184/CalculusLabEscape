#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        public static void AddLimitsSummaryPractice(GameObject root)
        {
            var nodes=root.GetComponentsInChildren<Transform>(true);
            font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
            var summary=nodes.Single(t=>t.name=="Summary");
            if(!nodes.Any(t=>t.name=="SummaryPractice"))Button(summary,"SummaryPractice","Practise this focus / 3 questions",57,416,610,44,true);
            var practice=summary.GetComponentsInChildren<Button>(true).Single(t=>t.name=="SummaryPractice");
            var rect=(RectTransform)practice.transform;rect.anchoredPosition=new Vector2(57,-456);rect.sizeDelta=new Vector2(610,34);
            var image=practice.GetComponent<UnityEngine.UI.Image>();var reference=summary.GetComponentsInChildren<Button>(true).Single(t=>t.name=="SummaryRecords").GetComponent<UnityEngine.UI.Image>();
            image.sprite=reference.sprite;image.type=reference.type;
            var label=practice.GetComponentInChildren<TMP_Text>(true);label.fontSize=label.fontSizeMin=label.fontSizeMax=18;label.enableAutoSizing=false;label.fontStyle=FontStyles.Normal;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(10,3);label.rectTransform.offsetMax=new Vector2(-10,-3);
            if(!nodes.Any(t=>t.name=="PracticeModal"))AddPracticeModal(nodes.Single(t=>t.name=="DesignFrame"));
            summary.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="SummaryReplayLabel").text="Replay mission";
            ConfigureMathText(root);
        }
        static void AddMissionPractice(Transform frame,Transform summary)
        {
            Button(summary,"SummaryPractice","Practise this focus / 3 questions",57,416,610,44,true);
            summary.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="SummaryReplayLabel").text="Replay mission";
            AddPracticeModal(frame);
        }
        static void AddPracticeModal(Transform frame)
        {
            var modal=Panel(frame,"PracticeModal",0,0,1280,720,Paper);
            var p=modal.transform;
            Text(p,"PracticeTitle","Focused practice",55,107,1170,60,31,Ink,true);
            Text(p,"PracticeProgress","",57,178,1165,51,20,Muted);
            Text(p,"PracticeQuestion","",57,250,1165,65,29,Ink,true);
            for(int i=0;i<3;i++)Button(p,"PracticeOption"+i,"",57,333+i*66,1165,52);
            Text(p,"PracticeFeedback","",57,544,1165,74,21,Accent);
            Text(p,"PracticeResults","",57,250,1165,275,21,Ink);
            Text(p,"PracticeSaveStatus","",57,80,1165,24,15,new Color(1f,.74f,.33f));
            Button(p,"PracticeClose","Back to mission results",57,641,395,51);
            Button(p,"PracticeSubmit","Check answer",827,641,395,51,true);
            Button(p,"PracticeNext","Next question",827,641,395,51,true);
            Button(p,"PracticeRepeat","Repeat this set",827,641,395,51,true);
            modal.gameObject.SetActive(false);
        }
        [MenuItem("Calculus Lab/Production/Upgrade focused practice")]
        public static void UpgradeMissionPractice()
        {
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var transforms=root.GetComponentsInChildren<Transform>(true);font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
                    if(!transforms.Any(t=>t.name=="PracticeModal"))AddMissionPractice(transforms.First(t=>t.name=="DesignFrame"),transforms.First(t=>t.name=="Summary"));
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("MISSION_PRACTICE_PREFABS_UPDATED");
        }
        public static void UpgradePracticeAndBuild(){UpgradeMissionPractice();Build();}
        static void AddRecordPractice(Transform frame,Transform detail)
        {
            Button(detail,"DetailPractice","Continue focused practice",716,636,510,44,true);
            AddPracticeModal(frame);
        }
        [MenuItem("Calculus Lab/Production/Upgrade practice records")]
        public static void UpgradePracticeRecords()
        {
            string path=PrefabsPath+"/MainMenuUI.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var transforms=root.GetComponentsInChildren<Transform>(true);font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
                if(!transforms.Any(t=>t.name=="DetailPractice"))AddRecordPractice(transforms.First(t=>t.name=="DesignFrame"),transforms.First(t=>t.name=="Detail"));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("PRACTICE_RECORDS_PREFAB_UPDATED");
        }
        public static void UpgradePracticeRecordsAndBuild(){UpgradePracticeRecords();Build();}
    }
}
#endif
