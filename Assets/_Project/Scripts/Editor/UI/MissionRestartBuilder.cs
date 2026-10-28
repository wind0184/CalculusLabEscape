#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void AddMissionRestart(GameObject root)
        {
            var nodes=root.GetComponentsInChildren<Transform>(true);if(nodes.Any(t=>t.name=="RestartModal"))return;
            var pause=nodes.First(t=>t.name=="Pause");if(pause.GetComponent<CanvasGroup>()==null)pause.gameObject.AddComponent<CanvasGroup>();
            Button(pause,"PauseRestart","Restart this mission",235,644,795,40);
            var frame=nodes.First(t=>t.name=="DesignFrame");var modal=Panel(frame,"RestartModal",0,0,1280,720,new Color(.01f,.025f,.04f,.96f));
            var card=Panel(modal.transform,"RestartCard",235,190,811,354,Paper);
            Text(card.transform,"RestartTitle","Start a separate run?",29,28,750,58,32,Ink,true);
            Text(card.transform,"RestartBody","Your current run will be saved as a separate record.\nA fresh run starts at the briefing with empty answers,\n3 Support Hearts and a new timer.\nYour earlier scores and practice stay in learning records.",30,105,747,117,22,Muted);
            Text(card.transform,"RestartError","",30,231,747,40,18,new Color(1f,.74f,.33f));
            Button(card.transform,"RestartCancel","Cancel / stay paused",30,285,347,47);Button(card.transform,"RestartConfirm","Save & start new run",436,285,347,47,true);
            modal.gameObject.SetActive(false);
        }
        [MenuItem("Calculus Lab/Production/Upgrade mission restart confirmation")]
        public static void UpgradeMissionRestart()
        {
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    font=root.GetComponentsInChildren<TMP_Text>(true).First().font;var rects=root.GetComponentsInChildren<RectTransform>(true);var poses=rects.Select(r=>r.anchoredPosition).ToArray();var sizes=rects.Select(r=>r.sizeDelta).ToArray();
                    AddMissionRestart(root);AddMissionRestart(root);
                    if(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="RestartModal")!=1)throw new Exception("Duplicate restart modal");
                    for(int i=0;i<rects.Length;i++)if(rects[i].anchoredPosition!=poses[i]||rects[i].sizeDelta!=sizes[i])throw new Exception("Restart upgrade moved existing UI");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-mission-restart"));EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");Debug.Log("MISSION_RESTART_UPGRADE_PASS");
        }
        public static void UpgradeMissionRestartAndBuild(){UpgradeMissionRestart();Build();}
    }
}
#endif
