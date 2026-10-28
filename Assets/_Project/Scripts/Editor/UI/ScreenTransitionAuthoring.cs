#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class ScreenTransitionAuthoring
    {
        const string Folder="Assets/_Project/Prefabs/Resources/Production/";
        const string MaterialPath="Assets/_Project/Art/UI/Transitions/ScreenTransition.mat";
        static string Evidence=>LearningApi.Argument("--transition-evidence",Path.GetFullPath("../../qa/screen-transitions-2026-10-23"));
        static string[] Scenes=>new[]{"MainMenu"}.Concat(Chapter4Game.Scenes).Select(n=>SceneAuthoring.SceneFolder+n+".unity").ToArray();
        static void AddEntrances(GameObject root)
        {
            var pages=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Pages");
            foreach(Transform page in pages)if(page.name!="World"&&page.name!="Finishing")AddEntrance(page.gameObject,.16f);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
                if(new[]{"SupportModal","ControlsModal","ManualModal","IntroductionModal","ReportScratchpadModal"}.Contains(node.name))AddEntrance(node.gameObject,.14f);
        }
        static void AddEntrance(GameObject node,float seconds)
        {
            var group=node.GetComponent<CanvasGroup>()??node.AddComponent<CanvasGroup>();
            var animation=node.GetComponent<PageEntrance>()??node.AddComponent<PageEntrance>();animation.canvasGroup=group;animation.seconds=seconds;
        }
        public static void ApplyAndBuild()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your scene before adding transitions.");
            Directory.CreateDirectory(Evidence);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var shader=Shader.Find("CalculusLabEscape/UI/ScreenTransition");if(shader==null||!shader.isSupported)throw new Exception("Transition shader did not compile.");
                var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if(material==null){material=new Material(shader){name="ScreenTransition"};AssetDatabase.CreateAsset(material,MaterialPath);}
                var root=new GameObject("Screen Transition",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=32000;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var shield=root.GetComponent<CanvasGroup>();shield.blocksRaycasts=false;
                var cover=new GameObject("Transition Cover",typeof(RectTransform),typeof(Image));cover.transform.SetParent(root.transform,false);
                var rect=cover.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var image=cover.GetComponent<Image>();image.material=material;image.color=Color.white;image.raycastTarget=false;
                var transition=root.AddComponent<ScreenTransition>();transition.overlay=image;transition.inputShield=shield;transition.transitionMaterial=material;
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"ScreenTransition.prefab");Object.DestroyImmediate(root);
                foreach(string name in new[]{"MainMenu","Limits","Differentiation","Integration"})
                {
                    string path=Folder+name+"UI.prefab";var ui=PrefabUtility.LoadPrefabContents(path);
                    try{AddEntrances(ui);PrefabUtility.SaveAsPrefabAsset(ui,path);}finally{PrefabUtility.UnloadPrefabContents(ui);}
                }
                foreach(string path in Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();
                    if(Object.FindFirstObjectByType<ScreenTransition>()!=null)throw new Exception("A transition is already saved in "+path);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);manager.screenTransition=instance.GetComponent<ScreenTransition>();EditorUtility.SetDirty(manager);
                    var ui=manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject;AddEntrances(ui);
                    foreach(var component in ui.GetComponentsInChildren<PageEntrance>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();Validate();ManualEditingMigration.Validate();ManualEditingMigration.BuildQA();
            }
            finally{Restore(setup);}
        }
        public static void Validate()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your open scene before validating another scene.");
            Directory.CreateDirectory(Evidence);var setup=EditorSceneManager.GetSceneManagerSetup();var lines=new System.Collections.Generic.List<string>();
            try
            {
                foreach(string path in Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var transitions=Object.FindObjectsByType<ScreenTransition>(FindObjectsSortMode.None);
                    if(transitions.Length!=1||manager.screenTransition!=transitions[0])throw new Exception("Assign the one saved transition to Game Manager: "+path);
                    var transition=transitions[0];if(transition.overlay==null||transition.transitionMaterial==null||transition.inputShield==null||transition.inputShield.blocksRaycasts)throw new Exception("Incomplete or blocking idle overlay: "+path);
                    if(transition.GetComponent<Canvas>().sortingOrder!=32000)throw new Exception("Transition Canvas must render over all UI.");
                    var ui=manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject;
                    if(ui.GetComponentsInChildren<PageEntrance>(true).Any(p=>p.canvasGroup==null))throw new Exception("Missing page group.");
                    lines.Add("PASS: "+path+" / one saved overlay / manager reference / idle input clear / page and modal fades authored before Play");
                }
                File.WriteAllLines(Path.Combine(Evidence,"saved-transition-checks.txt"),lines);
            }
            finally{Restore(setup);}
        }
        static void Restore(SceneSetup[] setup)
        {
            if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
        }
        public static void ValidateAndBuild(){Validate();ManualEditingMigration.BuildQA();}
        [MenuItem("Calculus Lab/Manual editing/Select screen transition settings")]
        public static void OpenSettings()
        {
            var transition=Object.FindFirstObjectByType<ScreenTransition>();if(transition==null)return;Selection.activeGameObject=transition.gameObject;
        }
    }
}
#endif
