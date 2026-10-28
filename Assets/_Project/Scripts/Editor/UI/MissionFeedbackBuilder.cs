#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void AddSkillReview(Transform summary)
        {
            var card=Panel(summary,"SkillReviewCard",54,477,1170,119,White);
            Text(card.transform,"SkillReview","",20,12,1130,95,18,Ink);
        }
        [MenuItem("Calculus Lab/Production/Upgrade mission learning review")]
        public static void UpgradeMissionFeedback()
        {
            foreach(var name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var labels=root.GetComponentsInChildren<TMP_Text>(true);font=labels.First().font;
                    var metrics=labels.First(t=>t.name=="SummaryMetrics");metrics.rectTransform.anchoredPosition=new Vector2(57,-237);metrics.rectTransform.sizeDelta=new Vector2(1155,210);metrics.fontSize=22;
                    if(!labels.Any(t=>t.name=="SkillReview"))AddSkillReview(root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Summary"));
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("MISSION_REVIEW_PREFABS_UPDATED");
        }
        public static void UpgradeFeedbackAndBuild(){UpgradeMissionFeedback();Build();}
    }
}
#endif
