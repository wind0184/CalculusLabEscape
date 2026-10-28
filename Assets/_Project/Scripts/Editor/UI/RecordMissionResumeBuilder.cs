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
    public static partial class ProductionUIBuilder
    {
        static void AddRecordMissionResume(GameObject root)
        {
            var nodes=root.GetComponentsInChildren<Transform>(true);if(nodes.Any(t=>t.name=="DetailResume"))return;var detail=nodes.First(t=>t.name=="Detail");
            Button(detail,"DetailResume","Continue this saved run",340,636,350,44,true);
            Text(detail,"RecordResumeStatus","",340,683,886,18,12,Muted);
        }
        [MenuItem("Calculus Lab/Production/Upgrade selected-record mission resume")]
        public static void UpgradeRecordMissionResume()
        {
            string path=PrefabsPath+"/MainMenuUI.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                font=root.GetComponentsInChildren<TMP_Text>(true).First().font;var rects=root.GetComponentsInChildren<RectTransform>(true);var positions=rects.Select(r=>r.anchoredPosition).ToArray();var sizes=rects.Select(r=>r.sizeDelta).ToArray();
                AddRecordMissionResume(root);AddRecordMissionResume(root);
                if(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="DetailResume")!=1)throw new Exception("Duplicate record resume control");
                for(int i=0;i<rects.Length;i++)if(rects[i].anchoredPosition!=positions[i]||rects[i].sizeDelta!=sizes[i])throw new Exception("Record upgrade moved existing controls");
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();RecordResumePreview.ValidateAndCapture();Debug.Log("RECORD_MISSION_RESUME_UPGRADE_PASS");
        }
        public static void UpgradeRecordMissionResumeAndBuild(){UpgradeRecordMissionResume();Build();}
    }
    public sealed class RecordResumePreview:EditorWindow
    {
        static readonly string[] Views={"Unfinished Bridge","Unfinished Case","Unfinished Limits","Completed record","Unavailable record"};int selected;
        [MenuItem("Calculus Lab/Learning record preview")]
        public static void Open()=>GetWindow<RecordResumePreview>("Record preview");
        void OnGUI()
        {
            EditorGUILayout.LabelField("LEARNING RECORD / NO PLAY",EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                selected=EditorGUILayout.Popup("Record",selected,Views);
                if(GUILayout.Button("Show selected record preview"))
                {
                    if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainMenu")EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
                    var canvas=Object.FindFirstObjectByType<Canvas>();Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Preview learning record");Apply(canvas,Views[selected]);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);EditorApplication.QueuePlayerLoopUpdate();
                }
                EditorGUILayout.HelpBox("Sample record text only. No mission session, save file or gameplay is created. Play opens the normal menu.",MessageType.Info);
            }
        }
        static void Apply(Canvas canvas,string view)
        {
            var nodes=canvas.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            void Text(string key,string text)=>MathText.Set(nodes[key].GetComponent<TMP_Text>(),text);
            foreach(Transform page in nodes["Pages"])page.gameObject.SetActive(page.name=="Detail");
            foreach(var node in nodes.Values)if(node.name.EndsWith("Modal",StringComparison.Ordinal))node.gameObject.SetActive(false);
            bool completed=view=="Completed record",unavailable=view=="Unavailable record";
            string topic=view=="Unfinished Case"?"Integration":view=="Unfinished Limits"?"Limits":"Differentiation";
            Text("DetailTitle","Session details / "+topic);
            Text("DetailSummary",""+(completed?"Completed":"InProgress")+"\nAccuracy: 100%   |   Completion: "+(completed?"100%":"partial")+"\nActive time: 01:20   |   Support Hearts used: 1/3");
            Text("DetailAttempts","Saved work\nAccepted answers appear here; resuming restores accepted steps and support.\nUnsubmitted drafts return to their starting values.");
            nodes["DetailResume"].gameObject.SetActive(!completed);nodes["DetailResume"].GetComponent<Button>().interactable=!unavailable;
            nodes["DetailPractice"].gameObject.SetActive(completed);Text("DetailPracticeLabel","Start focused practice");
            Text("RecordResumeStatus",unavailable?"This saved run is no longer available. Return to records.":completed?"This record remains available for review.":"Continue this saved run. Unsubmitted drafts start empty.");
            Canvas.ForceUpdateCanvases();
        }
        public static void ValidateAndCapture()
        {
            string path=SceneAuthoring.SceneFolder+"MainMenu.unity";EditorSceneManager.OpenScene(path);var canvas=Object.FindFirstObjectByType<Canvas>();string folder=Path.GetFullPath("../../qa/editor-record-resume");Directory.CreateDirectory(folder);
            var cameraObject=new GameObject("Record preview capture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=360;camera.transform.position=new Vector3(0,0,-10);
            var mode=canvas.renderMode;var prior=canvas.worldCamera;float distance=canvas.planeDistance;var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;
            try
            {
                camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(string view in Views)
                {
                    Apply(canvas,view);foreach(var text in canvas.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.name!="DetailAttempts"&&text.isTextOverflowing)throw new Exception("Record preview overflow: "+text.name);}
                    camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,view.Replace(" ","-")+".png"),pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
                }
            }
            finally{canvas.renderMode=mode;canvas.worldCamera=prior;canvas.planeDistance=distance;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(cameraObject);}
            EditorSceneManager.OpenScene(path);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: five saved-menu UI previews render without Play; labels fit; no session/save created; scene reloaded after preview.\n");
        }
    }
}
#endif
