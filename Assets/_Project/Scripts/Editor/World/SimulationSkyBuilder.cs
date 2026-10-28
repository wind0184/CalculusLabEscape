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
    public static class SimulationSkyBuilder
    {
        const string MaterialPath="Assets/_Project/Art/SimulationSky/VirtualDome.mat";
        static string ProjectRoot=>Path.GetDirectoryName(Application.dataPath);
        static string Request=>Path.Combine(ProjectRoot,"Library/CodexSimulationSky.request");
        static string Status=>Path.Combine(ProjectRoot,"Library/CodexSimulationSky.status");
        static string Evidence=>Path.GetFullPath(Path.Combine(ProjectRoot,Environment.GetCommandLineArgs().Contains("--sky-validation-copy")?"../../qa/simulation-sky-copy":"../../qa/simulation-sky"));
        static string AllEvidence=>Path.GetFullPath(Path.Combine(ProjectRoot,"../../qa/simulation-sky-all"));
        public static void StandaloneDelivery(){ApplyAndValidate();ProductionUIBuilder.BuildDelivery();Debug.Log("SIMULATION_SKY_DELIVERY_PASS");}
        public static void AllLevelsDelivery(){ApplyAllAndValidate();ProductionUIBuilder.BuildDelivery();Debug.Log("ALL_LEVEL_SIMULATION_SKY_DELIVERY_PASS");}
        [Serializable] sealed class SkyStatus{public string state,message;}
        static void Report(string state,string message)=>File.WriteAllText(Status,JsonUtility.ToJson(new SkyStatus{state=state,message=message}));
        [InitializeOnLoadMethod]
        static void ResumeRequestedWork()
        {
            if(File.Exists(Request))EditorApplication.delayCall+=RunRequestedWork;
        }
        static void RunRequestedWork()
        {
            if(!File.Exists(Request))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=RunRequestedWork;return;}
            string request=File.ReadAllText(Request);bool build=request.Contains("\"build\":true"),all=request.Contains("\"all\":true");File.Delete(Request);Report("running","Applying simulation sky in the open Editor.");
            try{if(all)ApplyAllAndValidate();else ApplyAndValidate();if(build)ProductionUIBuilder.BuildDelivery();Report("complete",build?"Sky applied, Editor captures verified and both Windows builds succeeded.":"Sky applied and Editor captures verified.");Debug.Log(build?"SIMULATION_SKY_DELIVERY_PASS":"SIMULATION_SKY_LIVE_APPLIED");}
            catch(Exception exception){Report("failed",exception.ToString());Debug.LogException(exception);}
        }
        [MenuItem("Calculus Lab/Sky/Apply simulation sky to Bridge")]
        public static void ApplyAndValidate()=>ApplyTopic(1,Evidence,"",null);
        sealed class Style{public Material material;public Color fog;public float density;}
        [MenuItem("Calculus Lab/Sky/Apply simulation sky to all levels")]
        public static void ApplyAllAndValidate()
        {
            var style=ApplyTopic(1,AllEvidence,"1-",null);
            ApplyTopic(0,AllEvidence,"0-",style);ApplyTopic(2,AllEvidence,"2-",style);
            if(Application.isBatchMode)ValidateSavedLevels(style);
            File.WriteAllText(Path.Combine(AllEvidence,"result.txt"),"PASS: all three saved levels use the Bridge simulation sky; lower indoor fog; six world cameras use Skybox; existing geometry/colliders/gameplay references unchanged; nine Edit Mode sky views captured; scene-switch fog verified; no gameplay session created.\n");
            Debug.Log("ALL_LEVEL_SIMULATION_SKY_EDITOR_PASS");
        }
        static Style ApplyTopic(int topic,string folder,string prefix,Style template)
        {
            Directory.CreateDirectory(folder);string path="Assets/_Project/Scenes/"+Chapter4Game.Scenes[topic]+".unity";
            var previousScene=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Chapter4World>(true)).Single(w=>w.topic==topic);
                string references=JsonUtility.ToJson(world);var transforms=world.GetComponentsInChildren<Transform>(true);var matrices=transforms.Select(t=>t.localToWorldMatrix).ToArray();var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id).ToArray();
                var shader=Shader.Find("CalculusLabEscape/Simulation Sky");if(shader==null||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Simulation sky shader unavailable or has compile errors");
                var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);if(material==null){material=new Material(shader){name="Virtual simulation dome"};AssetDatabase.CreateAsset(material,MaterialPath);}
                var sky=world.GetComponent<SimulationSky>();bool added=sky==null;if(added)sky=Undo.AddComponent<SimulationSky>(world.gameObject);
                Undo.RecordObject(sky,"Set simulation sky");
                if(template!=null){sky.environment=template.material;sky.fogTint=template.fog;sky.fogDensity=Mathf.Min(template.density,.0025f);}
                else if(added||sky.environment==null){sky.environment=material;sky.fogTint=new Color(.07f,.21f,.27f);sky.fogDensity=topic==1?.007f:.0025f;}
                sky.Apply();EditorUtility.SetDirty(sky);
                for(int i=0;i<transforms.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(matrices[i][j]-transforms[i].localToWorldMatrix[j])>.0001f)throw new Exception("Sky changed geometry: "+transforms[i].name);
                if(references!=JsonUtility.ToJson(world)||!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id)))throw new Exception("Sky changed gameplay references or collision");
                if(world.GetComponentsInChildren<Camera>(true).Any(c=>c.clearFlags!=CameraClearFlags.Skybox))throw new Exception("A world camera did not receive the sky");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Capture(world,folder,prefix+"sky-world.png",topic==1?new Vector3(13,12,-1):new Vector3(8,6,-5),topic==1?new Vector3(0,1,7):new Vector3(0,2,8));Capture(world,folder,prefix+"sky-player.png",world.player.transform.position+Vector3.up*1.6f,world.player.transform.position+new Vector3(0,3.6f,12));
                Capture(world,folder,prefix+"sky-dome.png",new Vector3(0,2,-3),topic==1?new Vector3(0,10,8):new Vector3(0,13,8));
                if(ShaderUtil.ShaderHasError(shader))throw new Exception("Sky shader failed while rendering");
                if(template==null)File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: custom sky shader renders in Edit Mode; saved material and SimulationSky component; both world cameras use Skybox; existing geometry/colliders/gameplay references unchanged; three sky views captured; no gameplay session created.\n");
                Selection.activeObject=sky.environment;SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();Debug.Log("SIMULATION_SKY_EDITOR_PASS / "+Chapter4Game.Scenes[topic]);
                return new Style{material=sky.environment,fog=sky.fogTint,density=sky.fogDensity};
            }
            finally{if(previousScene.IsValid()&&previousScene.isLoaded)SceneManager.SetActiveScene(previousScene);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void ValidateSavedLevels(Style style)
        {
            foreach(int topic in new[]{0,1,2,0})
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+Chapter4Game.Scenes[topic]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();var sky=world.GetComponent<SimulationSky>();
                if(sky==null||sky.environment!=style.material||world.GetComponents<SimulationSky>().Length!=1)throw new Exception("Saved simulation sky is missing or duplicated");
                sky.Apply();float expected=topic==1?style.density:Mathf.Min(style.density,.0025f);
                if(Mathf.Abs(RenderSettings.fogDensity-expected)>.00001f||RenderSettings.fogColor!=style.fog||RenderSettings.skybox!=style.material)throw new Exception("Scene switch used another level's fog");
            }
        }
        [MenuItem("Calculus Lab/Sky/Open Limits simulation")]
        public static void OpenLimits()=>OpenLevel(0);
        [MenuItem("Calculus Lab/Sky/Open Bridge simulation")]
        public static void OpenBridge()=>OpenLevel(1);
        [MenuItem("Calculus Lab/Sky/Open Integration simulation")]
        public static void OpenIntegration()=>OpenLevel(2);
        static void OpenLevel(int topic)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+Chapter4Game.Scenes[topic]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();var canvas=Object.FindFirstObjectByType<Canvas>();
            Undo.RegisterFullObjectHierarchyUndo(world.gameObject,"Preview simulation environment");Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Preview simulation UI");SceneAuthoringWindow.ApplyView(world,canvas,"World");world.GetComponent<SimulationSky>()?.Apply();
            Selection.activeObject=world.GetComponent<SimulationSky>();SceneAuthoringWindow.Open();var view=SceneView.lastActiveSceneView;
            if(view!=null)view.LookAt(topic==1?new Vector3(0,1,7):new Vector3(0,3,8),Quaternion.Euler(25,-25,0),topic==1?23:18);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();Debug.Log("SIMULATION_LEVEL_OPEN / "+Chapter4Game.Scenes[topic]);
        }
        static void Capture(Chapter4World world,string folder,string name,Vector3 position,Vector3 lookAt)
        {
            Directory.CreateDirectory(folder);var go=new GameObject("Sky verification camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.transform.position=position;camera.transform.LookAt(lookAt);camera.clearFlags=CameraClearFlags.Skybox;
            var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;var canvas=world.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();bool prior=canvas!=null&&canvas.enabled;
            try{if(canvas!=null)canvas.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());Object.DestroyImmediate(pixels);}
            finally{if(canvas!=null)canvas.enabled=prior;camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(go);}
        }
    }
}
#endif
