#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class RepairStationMotionBuilder
    {
        const string GroupName="Repair station machinery";
        static string Project=>Path.GetDirectoryName(Application.dataPath);
        static string Request=>Path.Combine(Project,"Library/CodexRepairStationMotion.request");
        static string Status=>Path.Combine(Project,"Library/CodexRepairStationMotion.status");
        static string Evidence=>Path.GetFullPath(Path.Combine(Project,"../../qa/repair-station-motion"));
        static double nextPoll;
        [Serializable] sealed class Result{public string state,message;}
        [InitializeOnLoadMethod] static void Watch()=>EditorApplication.update+=Poll;
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            try{Apply();File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="complete",message="Repair station motion merged and saved, preserving current scene edits."}));Debug.Log("REPAIR_STATION_MOTION_LIVE_APPLIED");}
            catch(Exception e){File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="failed",message=e.ToString()}));Debug.LogException(e);}
        }
        public static void ApplyAndBuild(){Apply();Apply();ReloadCheck();ProductionUIBuilder.BuildDelivery();Debug.Log("REPAIR_STATION_MOTION_DELIVERY_PASS");}
        [MenuItem("Calculus Lab/World/Add Bridge machinery motion")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);
            string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity";
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter4World>(true)).Single(w=>w.topic==1);
                var root=world.transform.Find(GroupName);if(root==null||root.childCount!=3)throw new Exception("Saved repair machinery required.");
                var originals=world.GetComponentsInChildren<Transform>(true);var matrices=originals.Select(t=>t.localToWorldMatrix).ToArray();
                var collisions=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();string references=JsonUtility.ToJson(world);
                var motion=root.GetComponent<RepairStationMotion>();bool added=motion==null;
                if(added)
                {
                    Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Add repair machinery motion");motion=Undo.AddComponent<RepairStationMotion>(root.gameObject);motion.world=world;
                    var generator=root.GetChild(0);var converter=root.GetChild(1);var builder=root.GetChild(2);
                    motion.generatorRotor=Pivot(generator,"Generator rotor pivot",new Vector3(0,1.06f,-.4f),new[]{"Rotor face","Turbine spoke","Rotor hub"});
                    motion.winch=Pivot(builder,"Winch axle pivot",new Vector3(.56f,1.33f,0),new[]{"Winch cable drum","Cable winding","Winch handle","Crank grip"});
                    motion.generatorRest=motion.generatorRotor.localRotation;motion.winchRest=motion.winch.localRotation;
                    var state=new SerializedObject(world);var locks=state.FindProperty("converterLocks");
                    motion.channelSources=Enumerable.Range(0,locks.arraySize).Select(i=>(Renderer)locks.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    motion.flowMaterial=(Material)state.FindProperty("repairComplete").objectReferenceValue;
                    motion.channelFlows=new Transform[3];motion.flowOrigins=new Vector3[3];
                    for(int i=0;i<3;i++)
                    {
                        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Channel "+(i+1)+" flow marker";Object.DestroyImmediate(go.GetComponent<Collider>());
                        go.transform.SetParent(converter,false);go.transform.localPosition=new Vector3((i-1)*.52f,.95f,-.205f);go.transform.localScale=new Vector3(.09f,.10f,.055f);
                        go.GetComponent<Renderer>().sharedMaterial=motion.flowMaterial;motion.channelFlows[i]=go.transform;motion.flowOrigins[i]=go.transform.localPosition;
                    }
                    motion.ResetPose();EditorUtility.SetDirty(motion);
                }
                Validate(motion);
                for(int i=0;i<originals.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(originals[i].localToWorldMatrix[j]-matrices[i][j])>.0001f)throw new Exception("Changed existing world pose: "+originals[i].name);
                if(references!=JsonUtility.ToJson(world)||!collisions.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i)))throw new Exception("Motion setup changed gameplay references or collision.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                if(Application.isBatchMode)
                {
                    // Render isolated samples, then restore the exact authoring pose before saving.
                    var parts=motion.GetComponentsInChildren<Transform>(true);var rotations=parts.Select(t=>t.localRotation).ToArray();var positions=parts.Select(t=>t.localPosition).ToArray();
                    var renderers=motion.channelFlows.Select(t=>t.GetComponent<Renderer>()).ToArray();var visible=renderers.Select(r=>r.enabled).ToArray();var materials=renderers.Select(r=>r.sharedMaterial).ToArray();
                    try
                    {
                        world.GetComponent<SimulationSky>().Apply();
                        motion.Sample(0,0,false);Capture(world,"01-power-off.png",new Vector3(-5,1.55f,-1.9f),new Vector3(-5,1,1));
                        motion.Sample(4,.18f,true);Capture(world,"02-power-running.png",new Vector3(-5,1.55f,-1.9f),new Vector3(-5,1,1));
                        motion.Sample(3,.35f,false);Capture(world,"03-two-channels.png",new Vector3(4,1.6f,-.9f),new Vector3(4,1.1f,2));
                        motion.Sample(4,.35f,true);Capture(world,"04-winch-lifting.png",new Vector3(3.5f,1.8f,2.2f),new Vector3(3,1.1f,5));
                    }
                    finally{for(int i=0;i<parts.Length;i++){parts[i].localRotation=rotations[i];parts[i].localPosition=positions[i];}for(int i=0;i<renderers.Length;i++){renderers[i].enabled=visible[i];renderers[i].sharedMaterial=materials[i];}EditorSceneManager.SaveScene(scene);}
                }
                File.WriteAllText(Path.Combine(Evidence,added?"changes.txt":"reapply.txt"),"PASS: saved rotor/axle pivots and three flow markers; old world poses, collision and gameplay references preserved; reapply creates no duplicate machinery; preview changes no mission state.\n");
                Debug.Log("REPAIR_STATION_MOTION_EDITOR_PASS");
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static Transform Pivot(Transform parent,string name,Vector3 centre,string[] names)
        {
            var parts=parent.Cast<Transform>().Where(t=>names.Contains(t.name)).ToArray();if(parts.Length==0)throw new Exception("Missing moving parts "+name);
            var pivot=new GameObject(name).transform;pivot.SetParent(parent,false);pivot.localPosition=centre;foreach(var part in parts)part.SetParent(pivot,true);return pivot;
        }
        static void Validate(RepairStationMotion motion)
        {
            if(motion.world==null||motion.generatorRotor==null||motion.winch==null||motion.generatorRotor.childCount!=10||motion.winch.childCount!=8||motion.channelFlows.Length!=3||motion.flowOrigins.Length!=3||motion.channelSources.Length!=3||motion.channelSources.Any(r=>r==null)||motion.flowMaterial==null)throw new Exception("Incomplete persisted machinery bindings.");
            if(motion.GetComponentsInChildren<RepairStationMotion>(true).Length!=1)throw new Exception("Duplicate machinery motion.");
        }
        static void ReloadCheck()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var motion=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RepairStationMotion>(true)).Single();Validate(motion);
            string state=JsonUtility.ToJson(motion.world);int colliders=motion.world.GetComponentsInChildren<Collider>(true).Length;
            motion.Sample(3,.2f,true);if(motion.channelFlows.Count(t=>t.GetComponent<Renderer>().enabled)!=2||Quaternion.Angle(motion.winch.localRotation,motion.winchRest)>.01f)throw new Exception("Locked winch / partial converter preview incorrect.");
            motion.Sample(4,.2f,true);if(Quaternion.Angle(motion.generatorRotor.localRotation,motion.generatorRest)<10||Quaternion.Angle(motion.winch.localRotation,motion.winchRest)<10||motion.channelFlows.Any(t=>!t.GetComponent<Renderer>().enabled))throw new Exception("Active machinery sample missing.");
            motion.ResetPose();if(motion.channelFlows.Any(t=>t.GetComponent<Renderer>().enabled)||Quaternion.Angle(motion.generatorRotor.localRotation,motion.generatorRest)>.01f)throw new Exception("Preview reset failed.");
            if(state!=JsonUtility.ToJson(motion.world)||colliders!=motion.world.GetComponentsInChildren<Collider>(true).Length||Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Preview changed gameplay state.");
            EditorSceneManager.SaveScene(scene);Debug.Log("REPAIR_STATION_MOTION_RELOAD_PASS");
        }
        [MenuItem("Calculus Lab/World/Preview Bridge machinery motion")]
        static void SelectPreview()
        {
            var scene=SceneManager.GetSceneByPath(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            if(!scene.IsValid()||!scene.isLoaded){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");}
            var motion=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RepairStationMotion>(true)).FirstOrDefault();if(motion==null){Debug.LogWarning("Apply Bridge machinery motion first.");return;}Selection.activeGameObject=motion.gameObject;SceneView.lastActiveSceneView?.FrameSelected();
        }
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 look)
        {
            var go=new GameObject("Machinery motion evidence camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=position;camera.transform.LookAt(look);
            var target=RenderTexture.GetTemporary(1280,720,24);var prior=RenderTexture.active;Texture2D pixels=null;
            var signs=new SerializedObject(world).FindProperty("repairSigns");var labels=Enumerable.Range(0,signs.arraySize).SelectMany(i=>((TextMesh)signs.GetArrayElementAtIndex(i).objectReferenceValue).GetComponentsInChildren<Renderer>(true)).ToArray();var visibility=labels.Select(r=>r.enabled).ToArray();
            try{foreach(var label in labels)label.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Evidence,name),pixels.EncodeToPNG());}
            finally{for(int i=0;i<labels.Length;i++)labels[i].enabled=visibility[i];camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);}
        }
    }
    [CustomEditor(typeof(RepairStationMotion))]
    public sealed class RepairStationMotionInspector:UnityEditor.Editor
    {
        int stage;float seconds;bool installing;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();if(Application.isPlaying)return;var motion=(RepairStationMotion)target;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Machinery preview (no Play)",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("0: idle; 1: generator; 2-4: converter channels; 4+: builder. Installation turns the winch. Preview changes decorative poses only; mission labels and the bridge retain their authoring state. Play resets the pose.",MessageType.Info);
            EditorGUI.BeginChangeCheck();stage=EditorGUILayout.IntSlider("Installed task stage",stage,0,11);seconds=EditorGUILayout.Slider("Sample time",seconds,0,10);installing=EditorGUILayout.Toggle("Bridge installing",installing);
            if(EditorGUI.EndChangeCheck()){Undo.RegisterFullObjectHierarchyUndo(motion.gameObject,"Preview repair machinery");motion.Sample(stage,seconds,installing);EditorSceneManager.MarkSceneDirty(motion.gameObject.scene);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
            if(GUILayout.Button("Reset machinery pose")){Undo.RegisterFullObjectHierarchyUndo(motion.gameObject,"Reset machinery preview");motion.ResetPose();stage=0;seconds=0;installing=false;EditorSceneManager.MarkSceneDirty(motion.gameObject.scene);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
        }
    }
}
#endif
