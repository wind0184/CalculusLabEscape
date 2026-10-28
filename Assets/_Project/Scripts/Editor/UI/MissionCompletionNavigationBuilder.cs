#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void AddCompletionNavigation(GameObject root,int topic)
        {
            var nodes=root.GetComponentsInChildren<Transform>(true);if(nodes.Any(t=>t.name=="SummaryNext"))return;
            var summary=nodes.First(t=>t.name=="Summary");
            Button(summary,"SummaryNext",topic==1?"Next: Integration":"Choose another mission",720,416,498,44,true);
            Text(summary,"SummaryNextHint",topic==1?"Starts a new Integration run. Existing results stay saved.":"Return to mission selection. Existing results stay saved.",720,461,498,16,12,Muted);
        }
        [MenuItem("Calculus Lab/Production/Upgrade mission completion navigation")]
        public static void UpgradeCompletionNavigation()
        {
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
                    var existing=root.GetComponentsInChildren<RectTransform>(true);var positions=existing.Select(t=>t.anchoredPosition).ToArray();var sizes=existing.Select(t=>t.sizeDelta).ToArray();
                    AddCompletionNavigation(root,name=="DifferentiationUI"?1:2);AddCompletionNavigation(root,name=="DifferentiationUI"?1:2);
                    if(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="SummaryNext")!=1)throw new Exception("Duplicate completion navigation");
                    for(int i=0;i<existing.Length;i++)if(existing[i].anchoredPosition!=positions[i]||existing[i].sizeDelta!=sizes[i])throw new Exception("Navigation upgrade moved existing UI");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-completion-navigation"));
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");Debug.Log("MISSION_COMPLETION_NAVIGATION_UPGRADE_PASS");
        }
        public static void UpgradeCompletionNavigationAndBuild(){UpgradeCompletionNavigation();Build();}
    }
}
#endif
