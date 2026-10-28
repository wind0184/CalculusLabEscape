#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class RepairStationArtBuilder
    {
        const string Art="Assets/_Project/Art/RepairStations",GroupName="Repair station machinery";
        static string Project=>Path.GetDirectoryName(Application.dataPath);
        static string Request=>Path.Combine(Project,"Library/CodexRepairStationArt.request");
        static string Status=>Path.Combine(Project,"Library/CodexRepairStationArt.status");
        static string Evidence=>Path.GetFullPath(Path.Combine(Project,Environment.GetCommandLineArgs().Contains("--station-art-validation-copy")?"../../qa/repair-station-art-copy":"../../qa/repair-station-art"));
        static Material steel,ivory,copper,wood,screen,trim;
        static double nextPoll;
        [Serializable] sealed class Result{public string state,message;}
        [InitializeOnLoadMethod] static void Watch()=>EditorApplication.update+=Poll;
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(Request);
            try{Apply();File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="complete",message="Three repair stations merged and saved."}));Debug.Log("REPAIR_STATION_ART_LIVE_APPLIED");}
            catch(Exception error){File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="failed",message=error.ToString()}));Debug.LogException(error);}
        }
        public static void ApplyAndBuild(){Apply();Apply();ReloadCheck();ProductionUIBuilder.BuildDelivery();Debug.Log("REPAIR_STATION_ART_DELIVERY_PASS");}
        [MenuItem("Calculus Lab/Assets/Add Bridge repair machinery")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","RepairStations");
            steel=Material("Field steel",new Color(.095f,.15f,.17f));ivory=Material("Generator casing",new Color(.61f,.64f,.53f));copper=Material("Copper winding",new Color(.53f,.27f,.12f));wood=Material("Worktable timber",new Color(.35f,.23f,.12f));screen=Material("Instrument face",new Color(.016f,.032f,.045f),true);trim=Material("Safety trim",new Color(.64f,.48f,.18f));
            string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity";var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter4World>(true)).Single(w=>w.topic==1);if(!world.IsAuthored||world.devices.Count!=3)throw new Exception("Three saved repair devices are required");
                var transforms=world.GetComponentsInChildren<Transform>(true);var matrices=transforms.Select(t=>t.localToWorldMatrix).ToArray();var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();string references=JsonUtility.ToJson(world);
                var existing=world.GetComponentsInChildren<Renderer>(true);var materials=existing.Select(r=>string.Join(",",r.sharedMaterials.Select(m=>m.GetInstanceID()))).ToArray();var visibility=existing.Select(r=>r.enabled).ToArray();
                var serialized=new SerializedObject(world);var property=serialized.FindProperty("repairLights");var sources=Enumerable.Range(0,property.arraySize).Select(i=>(Renderer)property.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var root=world.transform.Find(GroupName);bool added=root==null;
                if(added)
                {
                    root=Group(world.transform,GroupName);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add recognisable repair machinery");
                    for(int i=0;i<3;i++)
                    {
                        var station=Group(root,new[]{"Power Core - field generator","Converter Box - three-channel rack","Bridge Builder - construction bench"}[i]);station.localPosition=world.devices[i].position-Vector3.up*.7f;
                        var signal=new List<Renderer>();if(i==0)Generator(station,signal,sources[i].sharedMaterial);else if(i==1)Converter(station,signal,sources[i].sharedMaterial);else Builder(station,signal,sources[i].sharedMaterial);
                        var binding=station.gameObject.AddComponent<RepairStationIndicator>();binding.source=sources[i];binding.lights=signal.ToArray();binding.Apply();EditorUtility.SetDirty(binding);
                    }
                    var signs=serialized.FindProperty("repairSigns");
                    for(int i=0;i<signs.arraySize;i++)
                    {
                        var label=(TextMesh)signs.GetArrayElementAtIndex(i).objectReferenceValue;Undo.RecordObject(label,"Resize machinery label");label.characterSize=.019f;EditorUtility.SetDirty(label);
                        var plate=Shape(label.transform,"Repair machinery nameplate",new Vector3(0,0,.035f),new Vector3(1.65f,.34f,.025f),steel);Undo.RegisterCreatedObjectUndo(plate.gameObject,"Add smaller machinery label");
                    }
                    foreach(var renderer in existing)if(Replaced(renderer,world,sources)){Undo.RecordObject(renderer,"Hide old station box visuals");renderer.enabled=false;EditorUtility.SetDirty(renderer);}
                }
                for(int i=0;i<transforms.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(matrices[i][j]-transforms[i].localToWorldMatrix[j])>.0001f)throw new Exception("Machinery moved existing object "+transforms[i].name);
                for(int i=0;i<existing.Length;i++)if(materials[i]!=string.Join(",",existing[i].sharedMaterials.Select(m=>m.GetInstanceID()))||(!Replaced(existing[i],world,sources)&&visibility[i]!=existing[i].enabled))throw new Exception("Machinery changed unrelated renderer");
                if(references!=JsonUtility.ToJson(world)||!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i)))throw new Exception("Machinery changed gameplay references or collision");
                if(root.GetComponentsInChildren<Collider>(true).Length!=0||root.GetComponentsInChildren<Light>(true).Length!=0)throw new Exception("Machinery art cannot change collision or lighting");
                ValidateSignals(root,sources);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material==null||!EditorUtility.IsPersistent(material)||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Invalid machinery material");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);world.GetComponent<SimulationSky>()?.Apply();
                Capture(world,"stations-world.png",new Vector3(10,6,-5),new Vector3(0,1,5));Capture(world,"stations-player.png",world.player.transform.position+Vector3.up*1.6f,new Vector3(0,1,5));
                for(int i=0;i<3;i++)Capture(world,"station-"+i+".png",world.devices[i].position+(i==2?new Vector3(-2.3f,1.3f,-2.4f):new Vector3(2.5f,1.3f,-3.8f)),world.devices[i].position+Vector3.up*.2f);
                File.WriteAllText(Path.Combine(Evidence,"result.txt"),"PASS: generator, converter rack and construction bench saved; original positions/materials/gameplay references/colliders unchanged; specified old visuals hidden; no duplicate machinery; three source indicators preserve locked/active/completed materials; existing converter locks remain visible; five Edit Mode captures.\n");Debug.Log("REPAIR_STATION_ART_SAVED / "+(added?"added":"retained"));
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static bool Replaced(Renderer r,Chapter4World world,Renderer[] sources)
        {
            if(world.devices.Any(d=>d==r.transform)||sources.Contains(r))return true;
            return new[]{"Power Core dial","Repair station foot","Repair station frame","Station front vent","Converter guide rail","Builder assembly rail","Builder timber stock","Device sign backing"}.Contains(r.name);
        }
        static Material Material(string name,Color color,bool unlit=false)
        {
            string path=Art+"/"+name.Replace(" ","_")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;mat=new Material(Shader.Find(unlit?"Unlit/Color":"Standard")){name=name,color=color};if(!unlit){mat.SetFloat("_Glossiness",.18f);mat.SetFloat("_Metallic",name=="Worktable timber"?0:.2f);}AssetDatabase.CreateAsset(mat,path);return mat;
        }
        static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static Renderer Shape(Transform parent,string name,Vector3 position,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube,Vector3 rotation=default)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.transform.localRotation=Quaternion.Euler(rotation);Object.DestroyImmediate(go.GetComponent<Collider>());var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;return renderer;
        }
        static void Tube(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material material)
        {var renderer=Shape(parent,name,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),material,PrimitiveType.Cylinder);renderer.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static Renderer Ring(Transform parent,string name,Vector3 position,float radius,float width,Material material,Vector3 rotation)
        {
            string path=Art+"/Ring_"+radius.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"_"+width.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                const int n=48;var vertices=new List<Vector3>();var triangles=new List<int>();for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);vertices.Add(p*(radius-width/2));vertices.Add(p*(radius+width/2));if(i<n){int at=i*2;triangles.AddRange(new[]{at,at+2,at+1,at+1,at+2,at+3});}}
                int count=vertices.Count;vertices.AddRange(vertices.ToArray());int front=triangles.Count;for(int i=0;i<front;i+=3)triangles.AddRange(new[]{triangles[i]+count,triangles[i+2]+count,triangles[i+1]+count});mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.normals=Enumerable.Repeat(Vector3.back,count).Concat(Enumerable.Repeat(Vector3.forward,count)).ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(rotation);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;return go.GetComponent<Renderer>();
        }
        static void Generator(Transform root,List<Renderer> signal,Material status)
        {
            Shape(root,"Generator skid",new Vector3(0,.12f,0),new Vector3(1.85f,.24f,1.45f),steel);
            for(int side=-1;side<=1;side+=2){Shape(root,"Generator frame upright",new Vector3(side*.74f,.95f,.28f),new Vector3(.12f,1.45f,.12f),steel);Tube(root,"Copper cooling coil",new Vector3(side*.59f,.5f,.05f),new Vector3(side*.59f,1.25f,.05f),.115f,copper);for(int i=0;i<5;i++)Shape(root,"Cooling fin",new Vector3(side*.59f,.56f+i*.12f,.05f),new Vector3(.31f,.035f,.33f),steel);}
            Shape(root,"Generator top brace",new Vector3(0,1.7f,.28f),new Vector3(1.6f,.13f,.22f),steel);
            Shape(root,"Round power housing",new Vector3(0,1.06f,0),new Vector3(1.12f,.33f,1.12f),ivory,PrimitiveType.Cylinder,new Vector3(90,0,0));
            Shape(root,"Rotor face",new Vector3(0,1.06f,-.345f),new Vector3(.83f,.04f,.83f),steel,PrimitiveType.Cylinder,new Vector3(90,0,0));
            signal.Add(Ring(root,"Power status ring",new Vector3(0,1.06f,-.397f),.46f,.045f,status,Vector3.zero));
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Tube(root,"Turbine spoke",new Vector3(Mathf.Cos(a)*.15f,1.06f+Mathf.Sin(a)*.15f,-.40f),new Vector3(Mathf.Cos(a+.3f)*.36f,1.06f+Mathf.Sin(a+.3f)*.36f,-.40f),.028f,ivory);}
            Shape(root,"Rotor hub",new Vector3(0,1.06f,-.425f),new Vector3(.24f,.055f,.24f),copper,PrimitiveType.Cylinder,new Vector3(90,0,0));
            Shape(root,"Generator instrument panel",new Vector3(0,.37f,-.58f),new Vector3(1.35f,.19f,.25f),steel);signal.Add(Shape(root,"Generator status strip",new Vector3(0,.4f,-.713f),new Vector3(.54f,.04f,.025f),status));
            for(int side=-1;side<=1;side+=2)Shape(root,"Safety corner",new Vector3(side*.76f,.26f,-.59f),new Vector3(.13f,.14f,.17f),trim);
        }
        static void Converter(Transform root,List<Renderer> signal,Material status)
        {
            Shape(root,"Conversion rack base",new Vector3(0,.12f,0),new Vector3(1.95f,.24f,1.45f),steel);
            for(int side=-1;side<=1;side+=2)Shape(root,"Rack side column",new Vector3(side*.88f,.92f,.32f),new Vector3(.12f,1.6f,.12f),ivory);
            Shape(root,"Rack top crossbar",new Vector3(0,1.76f,.32f),new Vector3(1.9f,.13f,.17f),steel);
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*.52f;Shape(root,"Conversion channel socket",new Vector3(x,.64f,.06f),new Vector3(.41f,.28f,.46f),steel);Shape(root,"Channel spine",new Vector3(x,1.12f,.26f),new Vector3(.07f,.7f,.07f),copper);
                Ring(root,"Channel lower collar",new Vector3(x,.95f,.04f),.19f,.04f,ivory,new Vector3(90,0,0));Ring(root,"Channel upper collar",new Vector3(x,1.34f,.04f),.19f,.04f,ivory,new Vector3(90,0,0));
                signal.Add(Shape(root,"Conversion signal",new Vector3(x,.64f,-.185f),new Vector3(.24f,.035f,.025f),status));
            }
            Shape(root,"Converter readout panel",new Vector3(0,.38f,-.56f),new Vector3(1.65f,.25f,.18f),screen);
            for(int i=0;i<3;i++)Shape(root,"Flow arrow",new Vector3((i-1)*.48f,.38f,-.665f),new Vector3(.13f,.018f,.024f),ivory,PrimitiveType.Cube,new Vector3(0,0,i==1?0:25));
        }
        static void Builder(Transform root,List<Renderer> signal,Material status)
        {
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)Shape(root,"Workbench leg",new Vector3(side*.79f,.43f,end*.57f),new Vector3(.13f,.86f,.13f),steel);
            Shape(root,"Workbench lower rail",new Vector3(0,.27f,0),new Vector3(1.65f,.10f,1.15f),steel);
            for(int i=0;i<5;i++)Shape(root,"Worktable plank",new Vector3(0,.94f,(i-2)*.27f),new Vector3(1.9f,.16f,.245f),wood);
            for(int i=0;i<3;i++)Shape(root,"Stacked bridge timber",new Vector3(-.34f,1.08f+i*.09f,.05f),new Vector3(.85f,.08f,.21f),wood,PrimitiveType.Cube,new Vector3(0,i*8,0));
            for(int side=-1;side<=1;side+=2)Shape(root,"Winch bearing",new Vector3(.56f,1.16f,side*.25f),new Vector3(.13f,.35f,.12f),steel);
            Tube(root,"Winch cable drum",new Vector3(.56f,1.33f,-.24f),new Vector3(.56f,1.33f,.24f),.18f,copper);
            for(int i=0;i<5;i++)Ring(root,"Cable winding",new Vector3(.56f,1.33f,-.17f+i*.085f),.185f,.025f,steel,Vector3.zero);
            Tube(root,"Winch handle",new Vector3(.56f,1.33f,-.31f),new Vector3(.56f,1.52f,-.31f),.028f,ivory);Tube(root,"Crank grip",new Vector3(.56f,1.52f,-.31f),new Vector3(.56f,1.52f,-.47f),.035f,ivory);
            Shape(root,"Construction controls",new Vector3(-.35f,1.09f,-.46f),new Vector3(.75f,.10f,.32f),screen,PrimitiveType.Cube,new Vector3(-15,0,0));signal.Add(Shape(root,"Builder status strip",new Vector3(-.35f,1.17f,-.49f),new Vector3(.5f,.025f,.06f),status));
            for(int side=-1;side<=1;side+=2)Shape(root,"Workbench safety corner",new Vector3(side*.85f,.97f,-.6f),new Vector3(.14f,.18f,.09f),trim);
        }
        static void ValidateSignals(Transform root,Renderer[] sources)
        {
            var bindings=root.GetComponentsInChildren<RepairStationIndicator>(true);if(bindings.Length!=3||bindings.Any(b=>b.source==null||b.lights.Length==0)||!bindings.Select(b=>b.source).OrderBy(s=>s.GetInstanceID()).SequenceEqual(sources.OrderBy(s=>s.GetInstanceID())))throw new Exception("Missing station signal bindings");
            var serialized=new SerializedObject(root.GetComponentInParent<Chapter4World>());var states=new[]{"repairActive","repairComplete","amber"}.Select(name=>(Material)serialized.FindProperty(name).objectReferenceValue).ToArray();if(states.Any(m=>m==null))throw new Exception("Mission status material is missing");
            foreach(var binding in bindings){var prior=binding.source.sharedMaterial;try{foreach(var state in states){binding.source.sharedMaterial=state;binding.Apply();if(binding.lights.Any(r=>r.sharedMaterial!=state))throw new Exception("Signal does not follow mission material");}}finally{binding.source.sharedMaterial=prior;binding.Apply();}}
        }
        static void ReloadCheck()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();var roots=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name==GroupName).ToArray();if(roots.Length!=1||roots[0].childCount!=3)throw new Exception("Saved repair machinery missing or duplicated");
            if(world.GetComponentsInChildren<Renderer>(true).Count(r=>r.name.StartsWith("Converter lock",StringComparison.Ordinal)&&r.enabled)!=3)throw new Exception("Original converter feedback must stay visible");if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Editor art must not start a session");Debug.Log("REPAIR_STATION_ART_RELOAD_PASS");
        }
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 look)
        {
            var go=new GameObject("Repair machinery evidence camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=position;camera.transform.LookAt(look);
            var target=RenderTexture.GetTemporary(1280,720,24);var prior=RenderTexture.active;var canvas=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();bool enabled=canvas!=null&&canvas.enabled;Texture2D pixels=null;
            try{if(canvas!=null)canvas.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Evidence,name),pixels.EncodeToPNG());}
            finally{if(canvas!=null)canvas.enabled=enabled;camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);}
        }
    }
}
#endif
