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
        static void AddCaseShutdownUI(GameObject root)
        {
            var nodes=root.GetComponentsInChildren<Transform>(true);var card=nodes.First(t=>t.name=="FinishingCard") as RectTransform;
            if(nodes.Any(t=>t.name=="ShutdownPhase0"))return;
            if(Mathf.Approximately(card.sizeDelta.y,146))
            {
                card.anchoredPosition=new Vector2(165,-525);card.sizeDelta=new Vector2(950,150);
                var title=nodes.First(t=>t.name=="FinishingTitle").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition=new Vector2(27,-14);title.rectTransform.sizeDelta=new Vector2(899,45);title.fontSize=30;
                var body=nodes.First(t=>t.name=="FinishingText").GetComponent<TMP_Text>();body.rectTransform.anchoredPosition=new Vector2(28,-62);body.rectTransform.sizeDelta=new Vector2(895,32);body.fontSize=21;
            }
            for(int i=0;i<3;i++)Text(card,"ShutdownPhase"+i,"[WAIT] "+new[]{"Master switch","Power off","Device safe"}[i],28+i*300,108,290,27,17,Muted);
        }
        [MenuItem("Calculus Lab/Production/Upgrade Integration shutdown presentation")]
        public static void UpgradeCaseShutdown()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[2]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();
            var renderers=world.GetComponentsInChildren<Renderer>(true);var positions=renderers.Select(r=>r.transform.localToWorldMatrix).ToArray();
            var colliders=world.GetComponentsInChildren<Collider>(true);string data=JsonUtility.ToJson(world);
            var sequence=world.GetComponent<CaseShutdownSequence>();
            if(sequence==null)
            {
                sequence=world.gameObject.AddComponent<CaseShutdownSequence>();var parts=world.GetComponentsInChildren<Transform>(true);
                sequence.lever=world.exit;sequence.leverRest=world.exit.localRotation;
                sequence.powerBars=Enumerable.Range(0,4).Select(i=>parts.First(t=>t.name=="Device power bar "+i).GetComponent<Renderer>()).ToArray();
                sequence.powerOn=sequence.powerBars[0].sharedMaterial;sequence.powerOff=parts.First(t=>t.name=="Master switch housing").GetComponent<Renderer>().sharedMaterial;
                var parent=RescueNode(world.transform.Find("Containment device"),"Shutdown sequence",Vector3.zero);
                sequence.cameraPose=RescueNode(parent,"Shutdown camera",new Vector3(0,4.9f,-3.8f));sequence.cameraTarget=RescueNode(parent,"Camera target",new Vector3(0,1.5f,.8f));
                var label=parts.First(t=>t.name=="DIAL").GetComponent<TextMesh>();
                sequence.deviceStatus=RescueNode(parent,"Device status display",new Vector3(0,1.19f,.049f)).gameObject.AddComponent<TextMesh>();
                sequence.deviceStatus.font=label.font;sequence.deviceStatus.fontSize=48;sequence.deviceStatus.characterSize=.030f;sequence.deviceStatus.anchor=TextAnchor.MiddleCenter;sequence.deviceStatus.alignment=TextAlignment.Center;
                sequence.deviceStatus.GetComponent<Renderer>().sharedMaterial=label.GetComponent<Renderer>().sharedMaterial;
                sequence.SetReadiness(0,false);
                var camera=world.player.GetComponentInChildren<Camera>();sequence.playerCameraHomePosition=camera.transform.localPosition;sequence.playerCameraHomeRotation=camera.transform.localRotation;sequence.playerCameraHomeMask=camera.cullingMask;
            }
            for(int i=0;i<renderers.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(renderers[i].transform.localToWorldMatrix[j]-positions[i][j])>.0001f)throw new Exception("Shutdown upgrade moved existing art");
            if(colliders.Any(c=>c==null)||world.GetComponentsInChildren<Collider>(true).Length!=colliders.Length||JsonUtility.ToJson(world)!=data)throw new Exception("Shutdown upgrade changed colliders/world references");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            string path=PrefabsPath+"/IntegrationUI.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try{AddCaseShutdownUI(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();ValidateCaseShutdown();Debug.Log("CASE_SHUTDOWN_UPGRADE_PASS");
        }
        public static void ValidateCaseShutdown()
        {
            string scenePath=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[2]+".unity";EditorSceneManager.OpenScene(scenePath);
            var world=Object.FindFirstObjectByType<Chapter4World>();var sequence=world.GetComponent<CaseShutdownSequence>();
            if(sequence==null||sequence.powerBars.Length!=4||sequence.deviceStatus==null||world.GetComponentsInChildren<CaseShutdownSequence>().Length!=1)throw new Exception("Missing/duplicate persisted shutdown settings");
            world.EditorCaseCompletion(4,true);var completed=world.indicators.Select(r=>r.sharedMaterial).ToArray();
            int previous=0;
            for(int i=0;i<=100;i++)
            {
                sequence.Sample(i/100f);if(sequence.PowerOffCount<previous)throw new Exception("Device power increases during shutdown");previous=sequence.PowerOffCount;
                if(world.indicators.Where((r,n)=>r.sharedMaterial!=completed[n]).Any())throw new Exception("Shutdown changed completed module evidence");
            }
            if(previous!=4||!sequence.Safe)throw new Exception("Device fails to reach safe power-off state");
            sequence.ResetPose();if(sequence.PowerOffCount!=0||sequence.lever.localRotation!=sequence.leverRest)throw new Exception("Reset cannot restore the starting device state");
            EditorSceneManager.OpenScene(scenePath);
            SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-shutdown"));EditorSceneManager.OpenScene(scenePath);
            File.AppendAllText(Path.GetFullPath("../../qa/editor-shutdown/result.txt"),"PASS: unique saved shutdown component; 101 samples power off in order; completed module lamps preserved; starting lever and power reset; no gameplay session.\n");
        }
        public static void UpgradeCaseShutdownAndBuild(){UpgradeCaseShutdown();Build();}
    }
    [CustomEditor(typeof(CaseShutdownSequence))]
    public sealed class CaseShutdownInspector:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();if(Application.isPlaying)return;var sequence=(CaseShutdownSequence)target;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Shutdown preview (no Play)",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();float t=EditorGUILayout.Slider("Progress",sequence.Progress,0,1);
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RegisterFullObjectHierarchyUndo(sequence.gameObject,"Preview device shutdown");var world=sequence.GetComponent<Chapter4World>();world.EditorOpenCase(true);world.EditorCaseCompletion(4,true);sequence.Sample(t);
                EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
            }
            if(GUILayout.Button("Reset device and camera poses"))
            {Undo.RegisterFullObjectHierarchyUndo(sequence.gameObject,"Reset shutdown preview");sequence.ResetPose();var world=sequence.GetComponent<Chapter4World>();world.EditorCaseCompletion(0);world.EditorOpenCase(false);EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);}
        }
    }
}
#endif
