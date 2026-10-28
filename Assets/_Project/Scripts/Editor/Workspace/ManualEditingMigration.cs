#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // One-time conversion of the existing saved assets. Ordinary Play/build does
    // not regenerate the scene, refresh its references, or rewrite its UI events.
    public static class ManualEditingMigration
    {
        const string PrefabRoot="Assets/_Project/Prefabs/Resources/Production/";
        static readonly string[] Names={"MainMenuUI","LimitsUI","DifferentiationUI","IntegrationUI"};
        static string Evidence=>string.IsNullOrEmpty(LearningApi.Argument("--manual-editing-evidence"))?Path.GetFullPath("../../qa/manual-editing-2026-10-23"):LearningApi.Argument("--manual-editing-evidence");
        static string Request=>Path.GetFullPath("Library/ManualEditing.request");
        static double nextPoll;
        [Serializable] sealed class Status { public string state,message; }
        static readonly List<string> report=new List<string>();

        [InitializeOnLoadMethod] static void Watch()=>EditorApplication.update+=Poll;
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll)return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            string command=File.ReadAllText(Request).Trim();File.Delete(Request);
            Directory.CreateDirectory(Evidence);
            WriteStatus("running",command);
            try
            {
                if(command=="convert")Run();
                else if(command=="build")BuildQA();
                else if(command=="validate")Validate();
                else if(command=="capture")Capture();
                else if(command=="open")OpenLearningScene();
                else if(command=="report-preview")ReportFusionLayout.OpenConceptPreview();
                else if(command=="transition-validate")ScreenTransitionAuthoring.Validate();
                else if(command=="transition-settings")ScreenTransitionAuthoring.OpenSettings();
                else if(command=="timeline-validate")ReportFusionLayout.ValidateEntranceTimeline();
                else if(command=="timeline-open")ReportFusionLayout.OpenEntranceTimeline();
                else if(command=="observatory-open")ReportFusionLayout.OpenObservatory();
                else if(command=="observatory-validate")ReportFusionLayout.ValidateObservatory();
                else if(command=="dialogue-validate")ReportFusionLayout.ValidateRobotDialogue();
                else if(command=="dialogue-open")ReportFusionLayout.OpenRobotDialoguePreview();
                else if(command=="welcome-validate")ReportFusionLayout.ValidateMenuWelcome();
                else if(command=="welcome-open")ReportFusionLayout.OpenMenuWelcome();
                else if(command=="story-validate")ReportFusionLayout.ValidateLaboratoryStory();
                else if(command=="story-open")ReportFusionLayout.OpenMainMenu();
                else if(command=="brief-video-validate")ReportFusionLayout.ValidateLimitsBriefingVideo();
                else if(command=="brief-video-open")ReportFusionLayout.OpenLimitsBriefingVideo();
                else throw new InvalidOperationException("Unknown manual editing request: "+command);
                WriteStatus("complete",command);
            }
            catch(Exception error){WriteStatus("failed",error.ToString());Debug.LogException(error);}
        }
        static void WriteStatus(string state,string message)=>File.WriteAllText(Path.Combine(Evidence,"live-status.json"),JsonUtility.ToJson(new Status{state=state,message=message},true));

        [MenuItem("Calculus Lab/Manual editing/Open learning scene (start here)")]
        public static void OpenLearningScene()
        {
            RequireCleanScenes();
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level02_Differentiation.unity");
            Selection.activeGameObject=Objects<GameManager>(scene).Single().gameObject;
        }

        [MenuItem("Calculus Lab/Manual editing/Validate scene references and buttons")]
        public static void Validate()
        {
            report.Clear();
            foreach(string name in Names)
            {
                var root=PrefabUtility.LoadPrefabContents(PrefabRoot+name+".prefab");
                try{ValidateCanvas(root);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            RequireCleanScenes();
            try
            {
                foreach(string path in ScenePaths())
                {
                    var scene=EditorSceneManager.OpenScene(path);
                    var manager=Objects<GameManager>(scene).Single();
                    if(Objects<Chapter4Game>(scene).Length!=1||manager.Session!=null)throw new Exception("Game logic must be saved on the idle Game Manager before Play: "+path);
                    if(manager.eventSystem==null)throw new Exception("Assign Event System: "+path);
                    if(manager.world!=null)
                    {
                        var reportView=(manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject).GetComponent<ReportFusionView>();
                        if(reportView!=null&&(reportView.gameManager!=manager||reportView.scenePreview==null||reportView.scenePreview.texture!=manager.world.Preview))throw new Exception("Report view must retain its saved Game Manager and preview texture after reopening: "+path);
                    }
                    if(path.EndsWith("MainMenu.unity"))
                    {if(manager.menuOrLimitsUI==null||!manager.menuOrLimitsUI.isMenu)throw new Exception("Main menu UI missing");}
                    else
                    {
                        if(manager.world==null||!manager.world.IsAuthored)throw new Exception("Saved world missing: "+path);
                        if(manager.world.gameManager!=manager||(int)manager.mission!=manager.world.topic)throw new Exception("World must reference its saved Game Manager: "+path);
                        if(manager.world.GetComponent<Chapter4Game>()!=null)throw new Exception("Game logic must be on Game Manager, not the map: "+path);
                        var input=manager.world.player.GetComponent<PlayerInteraction>();
                        if(input==null||input.scene!=manager||Math.Abs(input.interactionDistance-3.8f)>.001f)throw new Exception("Player interaction reference missing");
                        var player=new SerializedObject(manager.world.player);
                        if(player.FindProperty("playerCamera").objectReferenceValue==null)throw new Exception("Player camera not assigned");
                        if(manager.world.topic==0&&manager.menuOrLimitsUI==null||manager.world.topic>0&&manager.missionUI==null)throw new Exception("Mission UI missing");
                    }
                    foreach(var ui in Objects<ProductionCanvas>(scene))ValidateCanvas(ui.gameObject);
                    foreach(var ui in Objects<MissionCanvas>(scene))ValidateCanvas(ui.gameObject);
                    if(Objects<MenuHierarchyController>(scene).Any(x=>x.enabled)||Objects<CyberLabSceneBuilder>(scene).Any(x=>x.enabled)||Objects<DifferentiationSceneBuilder>(scene).Any(x=>x.enabled)||Objects<IntegrationSceneBuilder>(scene).Any(x=>x.enabled))
                        throw new Exception("Legacy scene bootstrap still active: "+path);
                    foreach(var root in scene.GetRootGameObjects())foreach(var node in root.GetComponentsInChildren<Transform>(true))
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)>0)throw new Exception("Missing script: "+node.name);
                    report.Add("PASS scene: "+path+" / saved Game Manager owns idle rules / saved map, UI and player references / no runtime generator bootstrap");
                    if(manager.world!=null)report.Add("SAVED MAP: "+path+" / "+manager.world.GetComponentsInChildren<Transform>(true).Length+" objects / "+manager.world.GetComponentsInChildren<Renderer>(true).Length+" renderers / "+manager.world.GetComponentsInChildren<Collider>(true).Length+" colliders, all before Play");
                }
            }
            finally{RestoreSetup(setup);}
            Directory.CreateDirectory(Evidence);File.WriteAllLines(Path.Combine(Evidence,"validation.txt"),report);
            Debug.Log("MANUAL_EDITING_REFERENCES_PASS");
        }

        public static void Run()
        {
            RequireCleanScenes();
            report.Clear();Directory.CreateDirectory(Evidence);
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string name in Names)
                {
                    string path=PrefabRoot+name+".prefab";
                    var root=PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        string before=VisualSignature(root);
                        ConfigureCanvas(root);
                        if(VisualSignature(root)!=before)throw new Exception("UI appearance changed: "+name);
                        PrefabUtility.SaveAsPrefabAsset(root,path);
                        report.Add("PASS visual layout/material/text unchanged: "+path);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                foreach(string path in ScenePaths())
                {
                    var scene=EditorSceneManager.OpenScene(path);
                    var world=Objects<Chapter4World>(scene).FirstOrDefault();
                    var manager=Objects<GameManager>(scene).FirstOrDefault();
                    if(manager==null)manager=new GameObject("Game Manager").AddComponent<GameManager>();
                    manager.world=world;
                    manager.mission=world==null?GameManager.Mission.MainMenu:(GameManager.Mission)world.topic;
                    if(world!=null)world.gameManager=manager;
                    manager.eventSystem=Objects<EventSystem>(scene).FirstOrDefault();
                    if(manager.eventSystem==null)manager.eventSystem=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
                    manager.menuOrLimitsUI=Objects<ProductionCanvas>(scene).FirstOrDefault();
                    manager.missionUI=Objects<MissionCanvas>(scene).FirstOrDefault();
                    if(manager.menuOrLimitsUI!=null){manager.menuOrLimitsUI.sceneController=manager;ConfigureCanvas(manager.menuOrLimitsUI.gameObject);}
                    if(manager.missionUI!=null){manager.missionUI.sceneController=manager;ConfigureCanvas(manager.missionUI.gameObject);}
                    if(world!=null)
                    {
                        var input=world.player.GetComponent<PlayerInteraction>();
                        if(input==null)input=world.player.gameObject.AddComponent<PlayerInteraction>();
                        input.scene=manager;
                        var player=new SerializedObject(world.player);
                        player.FindProperty("playerCamera").objectReferenceValue=world.player.GetComponentInChildren<Camera>(true);player.ApplyModifiedPropertiesWithoutUndo();
                    }
                    // Retain old reference objects, but the saved scene controller owns startup.
                    foreach(var old in Objects<CyberLabSceneBuilder>(scene))old.enabled=false;
                    foreach(var old in Objects<DifferentiationSceneBuilder>(scene))old.enabled=false;
                    foreach(var old in Objects<IntegrationSceneBuilder>(scene))old.enabled=false;
                    foreach(var old in Objects<MenuHierarchyController>(scene))
                    {
                        old.enabled=false;
                        foreach(Transform child in old.transform)
                            if(child.GetComponentInChildren<ProductionCanvas>(true)==null)child.gameObject.SetActive(false);
                        var image=old.GetComponent<Image>();if(image!=null)image.enabled=false;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    report.Add("Converted scene: "+path);
                }
                MovePlayerScript();
                AssetDatabase.SaveAssets();
                File.WriteAllLines(Path.Combine(Evidence,"conversion.txt"),report);
            }
            finally{RestoreSetup(setup);}
            Validate();
        }

        // Used by the old optional UI generator as well, so regenerated base controls
        // receive the same Inspector events before being saved.
        public static void ConfigureCanvas(GameObject root)
        {
            var production=root.GetComponent<ProductionCanvas>();var mission=root.GetComponent<MissionCanvas>();
            if(production==null&&mission==null)return;
            var references=root.GetComponent<UIReferenceMap>();if(references==null)references=root.AddComponent<UIReferenceMap>();
            var saved=new Dictionary<string,Transform>();
            foreach(var element in references.elements)if(element!=null&&element.target!=null&&!saved.ContainsKey(element.id))saved.Add(element.id,element.target);
            var mapped=new HashSet<Transform>(saved.Values);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
                if(!mapped.Contains(node)&&!saved.ContainsKey(node.name)){saved.Add(node.name,node);mapped.Add(node);}
            references.elements=saved.Select(pair=>new UIReferenceMap.Element{id=pair.Key,target=pair.Value}).ToArray();
            if(production!=null)production.uiReferences=references;else mission.uiReferences=references;
            var entries=production!=null?ManualButtonBindings.ProductionCanvas:ManualButtonBindings.MissionCanvas;
            Object receiver=production!=null?(Object)production:mission;
            foreach(var entry in entries)
            {
                if(!saved.TryGetValue(entry.button,out var node)||!node.TryGetComponent<Button>(out var button))continue;
                if(HasCall(button.onClick,receiver,entry.method))continue;
                var method=receiver.GetType().GetMethod(entry.method);
                if(method==null)throw new Exception("Missing button method: "+entry.method);
                if(entry.indexed)UnityEventTools.AddIntPersistentListener(button.onClick,(UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>),receiver,method),entry.argument);
                else UnityEventTools.AddPersistentListener(button.onClick,(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),receiver,method));
            }
            if(production!=null)
            {
                if(production.isMenu)
                {
                    FloatEvent(saved,"VolumeSlider",production.OnVolumeChanged);
                    StringEvent(saved,"ProfileInput",production.OnProfileEdited,true);
                }
                else
                {
                    FloatEvent(saved,"Probe",production.OnProbeChanged);FloatEvent(saved,"Marker",production.OnMarkerChanged);
                    StringEvent(saved,"Answer0",production.OnAnswer0Changed);StringEvent(saved,"Answer1",production.OnAnswer1Changed);StringEvent(saved,"Answer2",production.OnAnswer2Changed);
                }
            }
            else
            {
                foreach(string key in new[]{"BridgeA","BridgeB","SequenceValue"})StringEvent(saved,key,mission.OnAnswerEdited);
                SubmitEvent(saved,"BridgeA",mission.OnBridgeASubmitted);SubmitEvent(saved,"BridgeB",mission.OnBridgeBSubmitted);SubmitEvent(saved,"SequenceValue",mission.OnSequenceValueSubmitted);
            }
            EditorUtility.SetDirty(root);EditorUtility.SetDirty(references);EditorUtility.SetDirty(receiver);
        }
        static bool HasCall(UnityEventBase value,Object receiver,string method)
        {for(int i=0;i<value.GetPersistentEventCount();i++)if(value.GetPersistentTarget(i)==receiver&&value.GetPersistentMethodName(i)==method)return true;return false;}
        static void FloatEvent(Dictionary<string,Transform> nodes,string key,UnityAction<float> action)
        {if(nodes.TryGetValue(key,out var node)&&node.TryGetComponent<Slider>(out var slider)&&!HasCall(slider.onValueChanged,(Object)action.Target,action.Method.Name))UnityEventTools.AddPersistentListener(slider.onValueChanged,action);}
        static void StringEvent(Dictionary<string,Transform> nodes,string key,UnityAction<string> action,bool endEdit=false)
        {
            if(!nodes.TryGetValue(key,out var node)||!node.TryGetComponent<TMP_InputField>(out var input))return;
            UnityEvent<string> value=endEdit?input.onEndEdit:input.onValueChanged;
            if(!HasCall(value,(Object)action.Target,action.Method.Name))UnityEventTools.AddPersistentListener(value,action);
        }
        static void SubmitEvent(Dictionary<string,Transform> nodes,string key,UnityAction<string> action)
        {if(nodes.TryGetValue(key,out var node)&&node.TryGetComponent<TMP_InputField>(out var input)&&!HasCall(input.onSubmit,(Object)action.Target,action.Method.Name))UnityEventTools.AddPersistentListener(input.onSubmit,action);}

        static void ValidateCanvas(GameObject root)
        {
            var production=root.GetComponent<ProductionCanvas>();var mission=root.GetComponent<MissionCanvas>();
            if(production==null&&mission==null)return;
            var references=production!=null?production.uiReferences:mission.uiReferences;
            if(references==null)throw new Exception("UI Reference Map missing: "+root.name);
            var nodes=new Dictionary<string,Transform>();references.CopyTo(nodes);
            var entries=production!=null?ManualButtonBindings.ProductionCanvas:ManualButtonBindings.MissionCanvas;
            Object receiver=production!=null?(Object)production:mission;
            int wired=0;
            foreach(var entry in entries)
            {
                if(!nodes.TryGetValue(entry.button,out var node)||!node.TryGetComponent<Button>(out var button))continue;
                if(!HasCall(button.onClick,receiver,entry.method))throw new Exception("Inspector On Click missing: "+root.name+" / "+entry.button);
                if(receiver.GetType().GetMethod(entry.method)==null)throw new Exception("Missing action: "+entry.method);
                wired++;
            }
            var unbound=root.GetComponentsInChildren<Button>(true).Where(b=>b.onClick.GetPersistentEventCount()==0).Select(b=>b.name).ToArray();
            if(unbound.Length>0)throw new Exception("Unbound buttons on "+root.name+": "+string.Join(", ",unbound));
            foreach(var node in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)>0)throw new Exception("Missing script: "+node.name);
            report.Add("PASS UI: "+root.name+" / "+nodes.Count+" saved references / "+wired+" persistent button events");
        }
        static T[] Objects<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).ToArray();
        static string[] ScenePaths()=>new[]{"MainMenu"}.Concat(Chapter4Game.Scenes).Select(name=>SceneAuthoring.SceneFolder+name+".unity").ToArray();
        static void RestoreSetup(SceneSetup[] setup)
        {
            if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
        }
        static void RequireCleanScenes()
        {for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save your open scene before conversion/validation: "+SceneManager.GetSceneAt(i).path);}
        static void MovePlayerScript()
        {
            const string from="Assets/_Project/Scripts/Legacy/Prototype/Gameplay/LabPlayerController.cs",to="Assets/_Project/Scripts/Runtime/Player/LabPlayerController.cs";
            if(!File.Exists(from))return;
            string guid=AssetDatabase.AssetPathToGUID(from);string error=AssetDatabase.MoveAsset(from,to);
            if(!string.IsNullOrEmpty(error)||AssetDatabase.AssetPathToGUID(to)!=guid)throw new Exception("Could not preserve player script GUID: "+error);
        }
        static string VisualSignature(GameObject root)
        {
            var text=new StringBuilder();
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))
                text.Append(rect.name).Append('|').Append(rect.localPosition.ToString("R")).Append(rect.localRotation.ToString("R")).Append(rect.localScale.ToString("R"))
                    .Append(rect.anchorMin.ToString("R")).Append(rect.anchorMax.ToString("R")).Append(rect.pivot.ToString("R")).Append(rect.sizeDelta.ToString("R"));
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                text.Append(graphic.name).Append('|').Append(graphic.color.ToString("R")).Append('|').Append(AssetDatabase.GetAssetPath(graphic.material));
                if(graphic is TMP_Text label)text.Append(label.text).Append(label.fontSize.ToString("R")).Append(AssetDatabase.GetAssetPath(label.font));
            }
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }
        public static void BuildQA()
        {
            Validate();
            string path=Path.GetFullPath("Builds/ManualEditingQA/LearningLab.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ScenePaths(),locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Manual editing QA build failed: "+result.summary.result);
            File.WriteAllText(Path.Combine(Evidence,"build.txt"),"PASS: development player built from the four converted scenes.\n"+path);
        }
        public static void BuildAndCapture(){BuildQA();Capture();}
        public static void SaveManagersAndVerify(){Run();BuildAndCapture();}
        public static void Capture()
        {
            RequireCleanScenes();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for(int topic=0;topic<3;topic++)
                {
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                    var world=Objects<Chapter4World>(scene).Single();var canvas=Objects<Canvas>(scene).First(c=>c.GetComponent<ProductionCanvas>()!=null||c.GetComponent<MissionCanvas>()!=null);
                    string view=topic==0?"Brief":topic==1?"Power Core":"Case overview";
                    SceneAuthoringWindow.ApplyView(world,canvas,view);
                    SceneAuthoring.Capture(world,canvas,Path.Combine(Evidence,"scene-"+topic+".png"),false);
                    EditorSceneManager.OpenScene(scene.path);
                }
            }
            finally{RestoreSetup(setup);}
        }
    }
}
#endif
