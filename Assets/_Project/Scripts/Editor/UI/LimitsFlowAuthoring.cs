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
    public static partial class ReportFusionLayout
    {
        const string FlowArt="Assets/_Project/Art/UI/LimitsFlow/";
        const string FlowPrefab=Prefabs+"LimitsCalibrationFeedback.prefab";
        static string FlowEvidence=>LearningApi.Argument("--flow-evidence",Path.GetFullPath("../../qa/limits-flow-2026-10-24"));
        static void FlowLayout(GameObject root)
        {
            Index(root);roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");
            At("WorldHUD",24,88,796,94);At("WorldProgress",18,14,760,28);Regular("WorldProgress",21,Ink);
            At("WorldObjective",18,52,760,26);Regular("WorldObjective",17,Cyan);
            Text("WorldObjective","NEXT: Reach the portal console. Press E when in range.");
            Find("WorldPause").gameObject.SetActive(false);
            At("WorldPromptPanel",180,600,920,60);At("WorldPrompt",16,16,888,28);Regular("WorldPrompt",18,Ink,TextAlignmentOptions.Center);
            At("SummaryMetrics",62,285,1150,195);Regular("SummaryMetrics",24,Ink);
            var advice=Surface(Find("Summary"),"FlowReviewCard",62,491,1153,90,Panel);Rounded("FlowReviewCard");
            Label(advice.transform,"FlowReviewHeading","YOUR NEXT STEP",16,10,1120,20,13,Cyan);
            Label(advice.transform,"FlowReviewAdvice","Review unsuccessful submissions in Learning records, then practise the skill again.",16,33,1120,48,18,Ink);
        }
        static void CreateCalibrationPrefab(RenderTexture texture)
        {
            var overlay=Surface(null,"LimitsCalibrationFeedback",0,0,1280,720,Background);overlay.raycastTarget=true;
            var group=overlay.gameObject.AddComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=false;group.interactable=false;
            Label(overlay.transform,"FlowAppliedHeading","CALIBRATION APPLIED",30,27,1200,28,16,Cyan);
            var previewRect=Create(overlay.transform,"FlowPortalPreview",24,80,808,455);var raw=previewRect.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
            Label(overlay.transform,"FlowCalibration","CALIBRATION 1 / 5 COMPLETE",864,164,368,49,19,Cyan);
            Label(overlay.transform,"FlowCharge","1 / 5",858,228,374,127,96,Ink);
            Label(overlay.transform,"FlowChargeLabel","CALIBRATIONS RESTORED",864,364,368,33,17,Muted);
            for(int i=0;i<5;i++){Surface(overlay.transform,"FlowChargeSegment"+i,864+i*66,421,56,9,i==0?Cyan:Line);Rounded("FlowChargeSegment"+i);}
            Label(overlay.transform,"FlowNextTask","Next: Read the approaching value.",36,574,1200,64,28,Ink);
            Label(overlay.transform,"FlowReturnNote","Returning to the console...",36,659,1200,25,16,Muted);
            PrefabUtility.SaveAsPrefabAsset(overlay.gameObject,FlowPrefab);Object.DestroyImmediate(overlay.gameObject);
        }
        [MenuItem("Calculus Lab/Limits/Apply clearer guidance and charge feedback")]
        public static void AuthorLimitsFlow()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the open scene before authoring the Limits flow.");
            Directory.CreateDirectory(FlowArt);AssetDatabase.Refresh();
            var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(FlowArt+"PortalFeedback.renderTexture");
            if(texture==null){texture=new RenderTexture(1280,720,24){name="Portal feedback preview",antiAliasing=2};AssetDatabase.CreateAsset(texture,FlowArt+"PortalFeedback.renderTexture");}
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"LimitsUI.prefab");
            try{
                FlowLayout(ui);CreateCalibrationPrefab(texture);Index(ui);
                if(Find("CalibrationFeedback")!=null)Object.DestroyImmediate(Find("CalibrationFeedback").gameObject);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FlowPrefab),ui.scene);instance.transform.SetParent(Find("DesignFrame"),false);instance.name="CalibrationFeedback";instance.transform.SetAsLastSibling();instance.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"LimitsUI.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(ui);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var world=manager.world;
            FlowLayout(manager.menuOrLimitsUI.gameObject);
            foreach(var text in world.GetComponentsInChildren<TextMesh>(true)){
                if(text.text.Contains("EXAMINATION CONSOLE")){text.text="PORTAL CONSOLE";text.characterSize=.026f;text.fontSize=72;text.transform.position=world.station.position+new Vector3(0,1.3f,-1);text.color=new Color(.05f,.16f,.2f);EditorUtility.SetDirty(text);}
                else if(text.text.Contains("PORTAL EXAMINATION")){text.characterSize=.045f;text.fontSize=64;EditorUtility.SetDirty(text);}
            }
            var marker=(Transform)new SerializedObject(world).FindProperty("waypoint").objectReferenceValue;
            var material=AssetDatabase.LoadAssetAtPath<Material>(FlowArt+"ConsoleMarker.mat");if(material==null){material=new Material(Shader.Find("Unlit/Color")){color=new Color(.03f,.65f,.85f)};AssetDatabase.CreateAsset(material,FlowArt+"ConsoleMarker.mat");}marker.GetComponent<Renderer>().sharedMaterial=material;
            var cameraTransform=world.transform.Find("Portal feedback view");Camera camera;
            if(cameraTransform==null){camera=new GameObject("Portal feedback view",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(world.transform,false);}else camera=cameraTransform.GetComponent<Camera>();
            camera.transform.position=new Vector3(4.3f,3.9f,7);camera.transform.LookAt(world.exit.position);camera.fieldOfView=50;camera.nearClipPlane=.1f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;camera.targetTexture=texture;camera.enabled=false;camera.depth=-15;
            var flow=manager.GetComponent<LimitsFlowPresentation>();if(flow==null)flow=manager.gameObject.AddComponent<LimitsFlowPresentation>();manager.limitsFlow=flow;flow.gameManager=manager;flow.portalCamera=camera;
            flow.overlay=Find("CalibrationFeedback").gameObject;flow.cover=flow.overlay.GetComponent<CanvasGroup>();flow.portalPreview=Find("FlowPortalPreview").GetComponent<RawImage>();flow.charge=Find("FlowCharge").GetComponent<TMP_Text>();flow.calibration=Find("FlowCalibration").GetComponent<TMP_Text>();flow.nextTask=Find("FlowNextTask").GetComponent<TMP_Text>();flow.worldObjective=Find("WorldObjective").GetComponent<TMP_Text>();flow.summaryAdvice=Find("FlowReviewAdvice").GetComponent<TMP_Text>();
            flow.chargeSegments=Enumerable.Range(0,5).Select(i=>Find("FlowChargeSegment"+i).GetComponent<Image>()).ToArray();
            var sections=new SerializedObject(world).FindProperty("portalSections");flow.ringSections=Enumerable.Range(0,sections.arraySize).Select(i=>(Renderer)sections.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            foreach(var component in manager.menuOrLimitsUI.GetComponentsInChildren<Component>(true))if(component!=null)SaveReference(component);
            foreach(var node in manager.menuOrLimitsUI.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);
            SaveReference(manager);SaveReference(flow);SaveReference(camera);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            ValidateLimitsFlow();ManualEditingMigration.Validate();EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");
            Debug.Log("LIMITS_FLOW_AUTHORED_PASS");
        }
        public static void ValidateLimitsFlow()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var flow=manager.limitsFlow;
            if(flow==null||flow.gameManager!=manager||flow.portalCamera==null||flow.portalPreview.texture!=flow.portalCamera.targetTexture||flow.ringSections.Length!=40||flow.chargeSegments.Length!=5||flow.summaryAdvice==null||flow.overlay.activeSelf||flow.portalCamera.enabled||flow.cover.blocksRaycasts)throw new Exception("Save the idle Limits flow references before Play.");
            Directory.CreateDirectory(FlowEvidence);File.WriteAllText(Path.Combine(FlowEvidence,"saved-flow.txt"),"PASS: saved portal camera, overlay, forty ring sections, five charge segments, next-task text and review advice; idle camera/input released.");
            Debug.Log("LIMITS_FLOW_REFERENCES_PASS");
        }
        public static void AuthorLimitsFlowAndBuild(){AuthorLimitsFlow();ManualEditingMigration.BuildQA();}
        public static void ValidateLimitsFlowAndScenes(){ValidateLimitsFlow();ManualEditingMigration.Validate();OpenLimitsFlow();}
        [MenuItem("Calculus Lab/Limits/Open saved flow")]
        public static void OpenLimitsFlow()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");
            Selection.activeGameObject=Object.FindFirstObjectByType<GameManager>().gameObject;
        }
    }
}
#endif
