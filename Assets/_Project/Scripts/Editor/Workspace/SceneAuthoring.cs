#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class SceneAuthoring
    {
        public const string SceneFolder="Assets/_Project/Scenes/";
        const string AssetFolder="Assets/_Project/Art/AuthoredWorlds";

        // One-time migration. Existing authored worlds and subsequent hand edits are preserved.
        public static void BakeMissingWorlds()
        {
            for(int topic=0;topic<3;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                if(Object.FindFirstObjectByType<Chapter4World>()!=null)continue;
                var world=Chapter4World.CreateAuthored(topic);
                string folder=AssetFolder+"/"+Chapter4Game.Scenes[topic];Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                void Persist(Object value)
                {
                    if(value==null||AssetDatabase.Contains(value)||!(value is Material||value is Mesh||value is RenderTexture))return;
                    string safe=string.Concat(value.name.Select(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'?c:'_'));
                    string extension=value is Material?".mat":value is RenderTexture?".renderTexture":".asset";
                    AssetDatabase.CreateAsset(value,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+safe+extension));
                }
                foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)Persist(material);
                foreach(var filter in world.GetComponentsInChildren<MeshFilter>(true))Persist(filter.sharedMesh);
                // Include inactive-state materials which are not yet assigned to any renderer.
                var serialized=new SerializedObject(world);var property=serialized.GetIterator();
                while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)Persist(property.objectReferenceValue);
                AssetDatabase.SaveAssets();
                var canvas=Object.FindFirstObjectByType<Canvas>();
                if(topic>0)SceneAuthoringWindow.ApplyView(world,canvas,topic==1?"Power Core":"Case overview");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("AUTHORED_WORLD_SAVED / "+scene.path+" / "+world.GetComponentsInChildren<Renderer>(true).Length+" renderers");
            }
            ValidateAndCapture();
            EditorSceneManager.OpenScene(SceneFolder+Chapter4Game.Scenes[1]+".unity");
        }

        public static void BakeAndBuild(){BakeMissingWorlds();ProductionUIBuilder.Build();}

        public static void ValidateAndCapture()=>ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-authoring"));
        public static void ValidateAndCaptureTo(string evidence)
        {
            Directory.CreateDirectory(evidence);
            for(int topic=0;topic<3;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var worlds=Object.FindObjectsByType<Chapter4World>(FindObjectsSortMode.None);
                if(Application.isPlaying||worlds.Length!=1||!worlds[0].IsAuthored)throw new Exception("Expected one persisted Edit Mode world: "+scene.path);
                var world=worlds[0];
                if(world.player==null||world.station==null||world.Preview==null||!AssetDatabase.Contains(world.Preview))throw new Exception("Missing authored world references");
                foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
                    foreach(var material in renderer.sharedMaterials)if(material==null||!EditorUtility.IsPersistent(material))throw new Exception("Unsaved material: "+renderer.name);
                foreach(var filter in world.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh==null||!EditorUtility.IsPersistent(filter.sharedMesh))throw new Exception("Unsaved mesh: "+filter.name);
                if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Editor preview must not start a gameplay session");
                var canvas=Object.FindFirstObjectByType<Canvas>();
                SceneAuthoringWindow.ApplyView(world,canvas,"World");Capture(world,canvas,Path.Combine(evidence,topic+"-world.png"),true);
                foreach(string view in topic==0?LimitsEditorPreview.Views:topic==1?SceneAuthoringWindow.BridgeViews:SceneAuthoringWindow.CaseViews)
                {
                    SceneAuthoringWindow.ApplyView(world,canvas,view);Canvas.ForceUpdateCanvases();
                    foreach(var label in canvas.GetComponentsInChildren<TMP_Text>())
                    {label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Editor preview text overflow: "+view+" / "+label.name);}
                    string filename=string.Concat(view.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'-':c)).Replace(" ","-");
                    Capture(world,canvas,Path.Combine(evidence,topic+"-"+filename+".png"),false);
                }
                // Reopen from disk: preview switches used for evidence are not baked into gameplay.
                EditorSceneManager.OpenScene(scene.path);
            }
            File.WriteAllText(Path.Combine(evidence,"result.txt"),"PASS: three serialized worlds reload in Edit Mode; persistent materials, meshes and preview textures; mission UI views render without Play; no Chapter4Game/session created; preview text fits.\n");
            Debug.Log("EDITOR_AUTHORING_CHECKS_PASS");
        }

        public static void Capture(Chapter4World world,Canvas canvas,string path,bool environment)
        {
            var go=new GameObject("Editor evidence camera");var camera=go.AddComponent<Camera>();camera.enabled=false;
            var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;
            var mode=canvas.renderMode;var prior=canvas.worldCamera;float distance=canvas.planeDistance;Texture2D pixels=null;
            try
            {
                camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.targetTexture=target;
                camera.transform.position=environment?(world.topic==1?new Vector3(13,12,-1):new Vector3(8,6,-5)):new Vector3(0,0,-10);
                camera.transform.LookAt(environment?(world.topic==1?new Vector3(0,0,7):new Vector3(0,1,4)):Vector3.zero);
                if(!environment&&canvas.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="World"))
                {var playerCamera=world.player.GetComponentInChildren<Camera>();camera.transform.SetPositionAndRotation(playerCamera.transform.position,playerCamera.transform.rotation);}
                if(!environment){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
                world.transform.Find("Storyboard view").GetComponent<Camera>().Render();Canvas.ForceUpdateCanvases();camera.Render();
                if(!environment)EditorPreviewRepair.ValidateRenderedGraphics(canvas);
                RenderTexture.active=target;
                pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{canvas.renderMode=mode;canvas.worldCamera=prior;canvas.planeDistance=distance;RenderTexture.active=active;Object.DestroyImmediate(go);if(pixels!=null)Object.DestroyImmediate(pixels);RenderTexture.ReleaseTemporary(target);}
        }
    }

    public sealed partial class SceneAuthoringWindow:EditorWindow
    {
        static readonly string[] ExtraViews={"Pause","Restart confirmation","Support","Controls help","Save & return","Practice question","Practice explanation","Practice review","Independent check","Check accepted"};
        public static readonly string[] BridgeViews=new[]{"Brief","Power Core","Chip selected","Number inputs","Expression choices","Rescue","Rescue crossing","Rescue reunion","Summary","Movement guide","Device guide"}.Concat(ExtraViews).Concat(MissionOnboarding.PreviewViews(1)).ToArray();
        public static readonly string[] CaseViews=new[]{"Brief","Case overview","Exponent dial","Wire matching","Wire selected","Symbol sequence","Card selected","Pressure gauge","Three modules complete","Modules complete / cover closed","Master deactivation","Device powering down","Device powered off","Manual","Summary","Movement guide","Device guide"}.Concat(ExtraViews).Concat(MissionOnboarding.PreviewViews(2)).ToArray();
        Vector2 scroll;
        int selectedView,lastTopic=-1;
        [MenuItem("Calculus Lab/Scene & UI Workspace")]
        public static void Open()=>GetWindow<SceneAuthoringWindow>("Scene & UI");
        public static void OpenBridgeWorkspace()
        {OpenScene(1);Open();Selection.activeGameObject=Object.FindFirstObjectByType<Chapter4World>()?.gameObject;}
        void OnGUI()
        {
            EditorGUILayout.LabelField("EDIT WITHOUT PLAY",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scene view: select and edit real scene objects. Game view: inspect the selected UI screen. Preview text is sample data; Play starts a fresh mission normally.",MessageType.Info);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            try{DrawWorkspace();}finally{EditorGUILayout.EndScrollView();}
        }
        void DrawWorkspace()
        {
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                for(int i=0;i<3;i++){int topic=i;if(GUILayout.Button("Open "+Chapter4Game.Scenes[i]))OpenScene(topic);}
                var world=Object.FindFirstObjectByType<Chapter4World>();var canvas=Object.FindFirstObjectByType<Canvas>();
                if(world==null||canvas==null)return;
                GUILayout.Space(10);
                if(GUILayout.Button("3D world / hide UI"))Preview(world,canvas,"World");
                if(GUILayout.Button("Select world in Hierarchy")){Selection.activeGameObject=world.gameObject;FrameWorld(world);}
                if(GUILayout.Button("Select UI in Hierarchy"))Selection.activeGameObject=canvas.gameObject;
                GUILayout.Space(10);EditorGUILayout.LabelField("Find a device",EditorStyles.boldLabel);
                if(world.topic==1)
                {
                    for(int i=0;i<world.devices.Count;i++){int index=i;if(GUILayout.Button(Chapter4World.RepairDeviceNames[i]))SelectAndFrame(world.devices[index].parent);}
                    if(GUILayout.Button("Calf"))SelectAndFrame(world.calf);
                    if(GUILayout.Button("Mother"))SelectAndFrame(world.mother);
                    if(world.GetComponent<BridgeRescueSequence>()!=null&&GUILayout.Button("Rescue preview settings"))Selection.activeObject=world.GetComponent<BridgeRescueSequence>();
                }
                else if(world.topic==2)
                {
                    foreach(var device in world.devices)if(GUILayout.Button(device.parent.name))SelectAndFrame(device.parent);
                    var master=world.transform.Find("Containment device/Master deactivation");if(master!=null&&GUILayout.Button("Master deactivation"))SelectAndFrame(master);
                    if(world.GetComponent<CaseShutdownSequence>()!=null&&GUILayout.Button("Shutdown preview settings"))Selection.activeObject=world.GetComponent<CaseShutdownSequence>();
                }
                else if(GUILayout.Button("Examination console"))SelectAndFrame(world.station);
                var zones=world.transform.Find("Guide zones");if(zones!=null&&GUILayout.Button("Guide zones"))SelectAndFrame(zones);
                GUILayout.Space(10);EditorGUILayout.LabelField("UI preview",EditorStyles.boldLabel);
                var views=world.topic==1?BridgeViews:world.topic==2?CaseViews:LimitsEditorPreview.Views;
                if(lastTopic!=world.topic){selectedView=0;lastTopic=world.topic;}
                EditorGUI.BeginChangeCheck();selectedView=EditorGUILayout.Popup("Screen",Mathf.Clamp(selectedView,0,views.Length-1),views);
                if(EditorGUI.EndChangeCheck())Preview(world,canvas,views[selectedView]);
                if(GUILayout.Button("Show selected UI"))Preview(world,canvas,views[selectedView]);
                EditorGUILayout.HelpBox("Save scene changes with Ctrl+S. UI layout overrides belong to this scene; use the UI prefab for shared layout edits. Animated parts still follow gameplay when running.",MessageType.None);
            }
        }
        static void OpenScene(int topic)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();if(world!=null)FrameWorld(world);
        }
        static void FrameWorld(Chapter4World world)
        {if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(world.topic==1?new Vector3(0,1,7):new Vector3(0,2,3),Quaternion.Euler(35,-30,0),world.topic==1?22:16);}
        static void Preview(Chapter4World world,Canvas canvas,string view)
        {
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Preview mission UI");
            if(world.GetComponent<BridgeRescueSequence>()!=null||world.GetComponent<CaseShutdownSequence>()!=null)Undo.RegisterFullObjectHierarchyUndo(world.gameObject,"Preview completion poses");
            var lid=world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Hinged case lid");if(lid!=null)Undo.RecordObject(lid,"Preview case lid");
            var preview=world.transform.Find("Storyboard view");Undo.RecordObject(preview,"Preview device camera");Undo.RecordObject(preview.GetComponent<Camera>(),"Preview device camera");
            ApplyView(world,canvas,view);EditorSceneManager.MarkSceneDirty(world.gameObject.scene);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void ApplyView(Chapter4World world,Canvas canvas,string view)
        {
            var pauseGroup=canvas.GetComponentsInChildren<UnityEngine.CanvasGroup>(true).FirstOrDefault(g=>g.name=="Pause");if(pauseGroup!=null)pauseGroup.interactable=true;
            if(view=="Restart confirmation")
            {
                ApplyView(world,canvas,"Pause");canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name=="RestartModal").gameObject.SetActive(true);pauseGroup.interactable=false;return;
            }
            var shutdown=world.GetComponent<CaseShutdownSequence>();
            if(shutdown!=null){shutdown.ResetPose();world.EditorCaseCompletion(0);}
            if(view=="Three modules complete"||view=="Modules complete / cover closed"||view=="Device powering down"||view=="Device powered off")
            {
                ApplyView(world,canvas,"Case overview");
                var all=canvas.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                void Label(string name,string value)=>MathText.Set(all[name].GetComponent<TMP_Text>(),value);
                bool isFinishing=view.StartsWith("Device ",StringComparison.Ordinal);int completedCount=view=="Three modules complete"?3:4;
                world.EditorCaseCompletion(completedCount,isFinishing);
                Label("MissionProgress","Modules: "+completedCount+"/4");Label("SecondaryProgress","Accepted steps: "+(completedCount==4?18:15)+"/18");
                string[] labelNames={"Exponent dial","Wire matching","Symbol sequence","Pressure gauge"};
                for(int i=0;i<4;i++){Label("Module"+i+"Label",(i<completedCount?"[OK] ":"")+labelNames[i]);Label("ModuleState"+i,i<completedCount?"COMPLETE":"Inspect and complete this module");all["Module"+i].GetComponent<UnityEngine.UI.Button>().interactable=true;}
                all["MasterCover"].GetComponent<UnityEngine.UI.Button>().interactable=completedCount==4;
                Label("MasterCoverLabel",completedCount==4?"Lift master switch cover":"[LOCKED] Complete all four modules");all["MasterCoverLabel"].GetComponent<TMP_Text>().color=completedCount==4?new Color(.025f,.045f,.065f):new Color(.62f,.73f,.78f);
                Label("MasterStatus",completedCount==4?"All four modules complete. Device power is still ON. Lift the cover, then deactivate.":"Modules 3/4 complete. Master Deactivation stays locked until all four are complete.");
                Label("NextAction","NEXT ACTION\n"+(completedCount==4?"Lift the master switch cover.":"Complete the remaining module."));
                if(isFinishing)
                {
                    foreach(var finishingPage in all.Values.Where(t=>t.parent!=null&&t.parent.name=="Pages"))finishingPage.gameObject.SetActive(finishingPage.name=="Finishing");all["Backdrop"].gameObject.SetActive(false);
                    shutdown.Sample(view=="Device powered off"?.96f:.5f);Label("FinishingTitle",shutdown.Title);Label("FinishingText",shutdown.Description);
                    for(int i=0;i<3;i++)Label("ShutdownPhase"+i,shutdown.PhaseLabel(i));
                }
                return;
            }
            var rescueSequence=world.GetComponent<BridgeRescueSequence>();
            if(rescueSequence!=null){rescueSequence.ResetPose();world.EditorRescueState(false);}
            if(view=="Rescue crossing"||view=="Rescue reunion")
            {
                ApplyView(world,canvas,"Rescue");
                foreach(var previewPage in canvas.GetComponentsInChildren<Transform>(true).Where(t=>t.parent!=null&&t.parent.name=="Pages"))previewPage.gameObject.SetActive(previewPage.name=="Finishing");
                canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Backdrop").gameObject.SetActive(false);
                bool reunited=view=="Rescue reunion";world.EditorRescueState(true);rescueSequence.Sample(reunited?.96f:.45f);
                MathText.Set(canvas.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="FinishingTitle"),reunited?"CALF REUNITED":"BRIDGE RESTORED");
                MathText.Set(canvas.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="FinishingText"),reunited?"The calf has reached its mother. Rescue complete.":"The calf is crossing to its mother.");
                return;
            }
            var nodes=canvas.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            void Show(string name,bool active){if(nodes.TryGetValue(name,out var t))t.gameObject.SetActive(active);}
            void Text(string name,string text){if(nodes.TryGetValue(name,out var t)&&t.TryGetComponent<TMP_Text>(out var label))MathText.Set(label,text);}
            Show("QuestionJourney",false);Show("CaseCalibration",false);
            for(int i=0;i<3;i++)foreach(string prefix in new[]{"BridgeChoice","CalibrationOption"})
                if(nodes.TryGetValue(prefix+i,out var option))QuestionJourney.Choice(option.GetComponent<Button>(),false,false);
            canvas.enabled=view!="World";world.EditorOpenCase(world.topic==2&&view!="World"&&view!="Brief"&&view!="Movement guide"&&view!="Device guide");
            foreach(var t in nodes.Values)if(t.name.EndsWith("Modal",StringComparison.Ordinal)||t.name=="DragGhost")t.gameObject.SetActive(false);
            foreach(var visual in canvas.GetComponentsInChildren<MissionSelectionVisual>(true))visual.SetSelected(false);
            bool practice=view.StartsWith("Practice ",StringComparison.Ordinal);
            bool worldGuide=view=="Movement guide"||view=="Device guide",introduction=view.StartsWith("Guide - ",StringComparison.Ordinal);
            string page=worldGuide?"World":view=="Brief"?"Brief":view=="Summary"||practice?"Summary":view=="Pause"||view=="Save & return"?"Pause":"Puzzle";
            foreach(Transform t in nodes["Pages"])t.gameObject.SetActive(t.name==page);
            if(world.topic==0){LimitsEditorPreview.Apply(world,canvas,view);return;}
            string content=view=="Support"||view=="Controls help"?(world.topic==1?"Power Core":"Exponent dial"):view=="Master deactivation"?"Case overview":view;
            if(view=="Chip selected")content="Power Core";
            if(view=="Wire selected")content="Wire matching";
            if(view=="Card selected")content="Symbol sequence";
            if(introduction)content=view.Substring("Guide - ".Length);
            string introductionScreen=content;
            if(content=="Master deactivation")content="Case overview";
            bool bridge=world.topic==1,overview=content=="Case overview",rescue=content=="Rescue";
            Show("Backdrop",!worldGuide);Show("WorldGuide",worldGuide);Show("BridgeBody",bridge&&!rescue);Show("BridgeReady",bridge&&rescue);Show("CaseOverview",!bridge&&overview);Show("CaseModule",!bridge&&!overview);
            Show("TaskTitle",!overview&&!rescue);Show("TaskPrompt",!overview&&!rescue);Show("Feedback",!overview&&!rescue);
            Show("PowerControls",content=="Power Core");Show("BridgeInputs",content=="Number inputs");Show("BridgeOptions",content=="Expression choices");Show("BridgeBGroup",true);
            Show("RepairSubmit",true);Show("RepairContinue",false);Show("CaseBack",!bridge&&!overview);Show("ManualOpen",!bridge);Show("SupportOpen",!overview&&!rescue);Show("MasterCover",true);Show("MasterActivate",false);
            string[] modules={"Exponent dial","Wire matching","Symbol sequence","Pressure gauge"};
            for(int i=0;i<4;i++)Show("ModuleControls"+i,content==modules[i]);
            if(!bridge)world.FocusCase(Array.IndexOf(modules,content),true);
            Text("LiveStatus","");Text("SaveStatus","");Text("Feedback","");
            Text("GuidePreferenceLabel","First-use guides: ON / click to hide");
            Text("WorldProgress",bridge?"Bridge repair: 0/3":"Modules: 0/4");Text("WorldObjective",bridge?"Find Power Core to repair the first component.":"Complete all four modules and deactivate the device.");
            Text("WorldPrompt",view=="Device guide"?"[E] INSPECT DEVICE":bridge?"FOLLOW THE CYAN MARKER / POWER CORE":"FOLLOW THE CYAN MARKER / CONTAINMENT CASE");
            Text("WorldGuideTitle",view=="Device guide"?"READY TO INSPECT / "+(bridge?"Power Core":"Containment case"):"GET MOVING");
            Text("WorldGuideBody",view=="Device guide"?"Press E to inspect. Your mouse becomes available for the device controls.":"WASD or arrow keys move. Mouse looks around. Walk towards the cyan device marker.");
            canvas.GetComponent<ExplorationHUD>()?.Present(view=="Device guide",view=="Device guide",0);
            if(introduction)
            {
                var card=canvas.GetComponent<MissionCanvas>().onboarding.cards.First(c=>c.screen==introductionScreen&&c.key.StartsWith(bridge?"bridge-":"case-",StringComparison.Ordinal));
                Text("IntroductionTitle",card.title);Text("IntroductionBody",card.instructions);Show("IntroductionModal",true);
            }
            Text("MissionProgress",bridge?"Bridge repair: 0/3":"Modules: 0/4");Text("SecondaryProgress","Accepted steps: 0/18");Text("EnvironmentStatus",bridge?"Water: LOW":"Countdown: 10:00");
            for(int i=0;i<3;i++)Text("Component"+i,"[  ] "+new[]{"Power regulator","Bridge component","Bridge anchor"}[i]);
            Text("NextAction",bridge?rescue?"NEXT ACTION\nClick Open crossing to rescue the calf.":content=="Number inputs"?"NEXT ACTION\nEnter both values, then click Test repair.":content=="Expression choices"?"NEXT ACTION\nSelect an expression, then click Test repair.":"NEXT ACTION\nDrag the chip to its slot, or click chip then slot.":overview?"NEXT ACTION\nOpen Exponent dial to unlock the other modules.":"NEXT ACTION\nUse this module's controls, then check your answer.");
            Text("TaskTitle",content);Text("TaskPrompt",content=="Power Core"?MissionRules.BridgePrompts[0]:content=="Number inputs"?MissionRules.BridgePrompts[2]:content=="Expression choices"?MissionRules.BridgePrompts[7]:content=="Exponent dial"?"Integrate x^4. Raise the exponent, divide by the new exponent and insert one +C.":content=="Wire matching"?"Integrate 3x^2 + 4x - 5. Drag each source to a socket, or click a source then a target.":content=="Symbol sequence"?"Integrate sqrt(x). Place the procedure cards in order, then enter each step's value.":"Evaluate the integral from 0 to 2 of 2x dx. Set the release amount; no +C is needed.");
            Text("ExponentValue","4");Text("PowerPreview","?x^4");Text("BridgeALabel","Coefficient / result");
            Text("BridgeChipLabel","4");Text("CoefficientSlotLabel","Coefficient slot");
            for(int i=0;i<3;i++)Text("WireSource"+i+"Label",MissionRules.WireInputs[i]);
            for(int i=0;i<4;i++)Text("SequenceCard"+i+"Label",MissionRules.SequenceCards[i]);
            string selectedName=view=="Chip selected"?"BridgeChip":view=="Wire selected"?"WireSource0":view=="Card selected"?"SequenceCard0":null;
            if(selectedName!=null)
            {
                nodes[selectedName].GetComponent<MissionSelectionVisual>().SetSelected(true);
                Text(selectedName+"Label","[X] "+(view=="Chip selected"?"4":view=="Wire selected"?MissionRules.WireInputs[0]:MissionRules.SequenceCards[0]));
                Text("NextAction","NEXT ACTION\n"+(view=="Chip selected"?"Chip selected. Click the coefficient slot.":view=="Wire selected"?"Source 1 selected. Click a matching socket to check it.":"Rewrite card selected. Click a numbered slot to place it."));
            }
            for(int i=0;i<3;i++)Text("BridgeChoice"+i+"Label","[ ] "+MissionRules.BridgeChoices(7)[i]);
            for(int i=0;i<4;i++){Text("Module"+i+"Label",(i==0?"":"[LOCKED] ")+modules[i]);Text("ModuleState"+i,i==0?"Inspect and complete this module":"Complete Exponent Dial first");Text("SequenceSlot"+i+"Label",(i+1)+" / Place card");}
            if(!bridge){nodes["MasterCover"].GetComponent<Button>().interactable=false;Text("MasterCoverLabel","[LOCKED] Complete all four modules");nodes["MasterCoverLabel"].GetComponent<TMP_Text>().color=new Color(.62f,.73f,.78f);}
            Text("MasterStatus","Master Deactivation unlocks when all four modules are complete.");Text("DialExponentValue","4");Text("DivisorValue","1");Text("DialPreview","x^4 / 1");Text("SequenceValueLabel","Step 1: rewritten exponent");Text("ReleaseValue","0");Text("GaugeState","PRESSURE ACTIVE / adjust the release");
            Text("SummaryMetrics","Accuracy: 80%   |   Completion: 100%\nActive time: 05:00   |   Support Hearts used: 1/3");Text("SkillReview","THIS RUN\nKnowledge-point feedback and practice recommendations appear here after play.");
            Show("ManualModal",view=="Manual");
            Show("SupportModal",view=="Support");Text("SupportHearts","Support Hearts: 3/3");Text("SupportText","Basic hint\nReview the requested operation, then adjust the answer controls. Stronger support is optional and costs one heart.");
            Show("LeaveModal",view=="Save & return");
            Show("ControlsModal",view=="Controls help");Text("ControlsTitle",bridge?"Bridge controls":"Case controls");Text("ControlsBody","1. Adjust the answer controls described in the question.\n2. Click the check button and read the feedback.\n3. Apply an accepted repair, or return to Case overview after a stable module.\n\nDrag and drop also supports clicking a source, then its target.\nOperation help is free and pauses active time and environmental pressure.");
            if(view=="Master deactivation"||introductionScreen=="Master deactivation")
            {
                world.EditorCaseCompletion(4,true);
                Text("MissionProgress","Modules: 4/4");Text("SecondaryProgress","Accepted steps: 18/18");
                for(int i=0;i<4;i++){Text("Module"+i+"Label","[OK] "+modules[i]);Text("ModuleState"+i,"STABLE");}
                Show("MasterCover",false);Show("MasterActivate",true);Text("MasterStatus","All four modules complete. Click Master Deactivation to shut down the device.");Text("NextAction","NEXT ACTION\nClick Master Deactivation to shut down the device.");
            }
            Show("PracticeModal",practice);
            if(practice)
            {
                string focus=bridge?"derivatives":"integration";var questions=MissionPractice.Questions(focus);var question=questions[0];
                bool review=view=="Practice review",answered=view=="Practice explanation";
                Text("PracticeTitle","Focused practice / "+MissionPractice.Title(focus));Text("PracticeProgress",review?"Three-question review. Mission score unchanged.":"Question 1 / 3. Mission score unchanged.");Text("PracticeSaveStatus","");
                Show("PracticeQuestion",!review);Text("PracticeQuestion",question.prompt);Show("PracticeResults",review);
                for(int i=0;i<3;i++){Show("PracticeOption"+i,!review);Text("PracticeOption"+i+"Label",(answered&&i==question.correct?"[ANSWER] ":"")+question.options[i]);}
                Show("PracticeSubmit",!review&&!answered);Show("PracticeNext",!review&&answered);Show("PracticeRepeat",review);
                Text("PracticeCloseLabel","Back to mission results");Text("PracticeFeedback",review?"You can repeat this same set for guided review. Each run is saved separately.":answered?"CORRECT / "+question.explanation:"Each question is scored once. The explanation appears after you submit.");
                Text("PracticeResults","3 / 3 correct on first submission\n\n"+string.Join("\n\n",questions.Select(q=>"[OK] "+q.prompt+"\n"+q.explanation)));
            }
            PreviewIndependentCheck(world,nodes,view);
            if(nodes.TryGetValue("ScenePreview",out var preview))preview.GetComponent<RawImage>().texture=world.Preview;
            world.transform.Find("Storyboard view").GetComponent<Camera>().Render();Canvas.ForceUpdateCanvases();
            // Record changes on prefab instances so scene preview state survives an editor restart.
            foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach(var t in nodes.Values)if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        static void SelectAndFrame(Transform target)
        {if(target==null)return;Selection.activeGameObject=target.gameObject;EditorGUIUtility.PingObject(target.gameObject);if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();}
    }
    [CustomEditor(typeof(MathText))]
    public sealed class MathTextInspector:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();DrawDefaultInspector();
            if(EditorGUI.EndChangeCheck())
            {
                var math=(MathText)target;Undo.RecordObject(math.GetComponent<TMP_Text>(),"Preview mathematical text");
                math.SetSource(math.Source);EditorUtility.SetDirty(math.GetComponent<TMP_Text>());
            }
        }
    }
}
#endif
