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
        [MenuItem("Calculus Lab/Questions/Polish question journey UI")]
        public static void PolishQuestionDetails()
        {
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI","LimitsUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
                    if(root.GetComponentsInChildren<QuestionJourney>(true).Length==0)
                    {
                        bool limits=name=="LimitsUI";
                        var parent=root.GetComponentsInChildren<Transform>(true).First(t=>t.name==(limits?"QuestionBody":"Puzzle"));
                        var group=Rect(parent,"QuestionJourney",0,0,1280,720);
                        var heading=Text(group,"JourneyHeading",limits?"CALIBRATION 1 / 5":"INDEPENDENT CHECK 1 / 2",limits?64:429,limits?478:171,limits?302:780,22,14,Accent,true);
                        var detail=Text(group,"JourneyDetail",limits?"Complete both questions, then apply this calibration to charge the portal.":"Select an answer, then check the calibration.",limits?64:449,limits?608:540,limits?302:750,limits?54:29,limits?14:15,Muted);
                        if(limits){heading.gameObject.SetActive(false);detail.rectTransform.anchoredPosition=new Vector2(64,-587);detail.rectTransform.sizeDelta=new Vector2(302,37);detail.fontSize=12;}
                        var panels=new Image[3];var labels=new TMP_Text[3];
                        for(int i=0;i<3;i++)
                        {
                            panels[i]=Panel(group,"JourneyPhase"+i,limits?64:429+i*263,limits?494+i*30:268,limits?302:250,25);
                            panels[i].raycastTarget=false;
                            labels[i]=Text(panels[i].transform,"JourneyPhaseLabel"+i,new[]{"01  GUIDED TASK","02  INDEPENDENT CHECK","03  APPLY REPAIR"}[i],9,3,limits?284:232,19,13,Muted,true);
                        }
                        group.gameObject.AddComponent<QuestionJourney>().Configure(heading,detail,panels,labels);
                        group.gameObject.SetActive(false);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("QUESTION_DETAIL_PREFABS_PASS");
        }
        public static void PolishQuestionDetailsAndBuild(){PolishQuestionDetails();PolishQuestionDetails();BuildDelivery();Debug.Log("QUESTION_DETAIL_DELIVERY_PASS");}
    }
}
#endif
