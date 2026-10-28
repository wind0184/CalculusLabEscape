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
    public static class ConnectedLandscapeBuilder
    {
        const string Art="Assets/_Project/Art/ConnectedLandscape",GroupName="Connected river landscape",WaterName="Continuous river surface";
        static string Project=>Path.GetDirectoryName(Application.dataPath);
        static string Request=>Path.Combine(Project,"Library/CodexConnectedLandscape.request");
        static string Status=>Path.Combine(Project,"Library/CodexConnectedLandscape.status");
        static string Evidence=>Path.GetFullPath(Path.Combine(Project,Environment.GetCommandLineArgs().Contains("--landscape-validation-copy")?"../../qa/connected-landscape-copy":"../../qa/connected-landscape"));
        [Serializable] sealed class Result{public string state,message;}
        static double nextPoll;
        [InitializeOnLoadMethod] static void ResumeRequest(){EditorApplication.update+=PollRequest;}
        static void PollRequest()
        {if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+2;if(File.Exists(Request)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode)RunRequest();}
        static void RunRequest()
        {
            if(!File.Exists(Request))return;if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            try{Apply();File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="complete",message="Connected landscape merged into the open scene and saved; existing authored edits preserved."}));Debug.Log("CONNECTED_LANDSCAPE_LIVE_APPLIED");}
            catch(Exception error){File.WriteAllText(Status,JsonUtility.ToJson(new Result{state="failed",message=error.ToString()}));Debug.LogException(error);}
        }
        public static void ApplyAndBuild(){Apply();Apply();ReloadCheck();ProductionUIBuilder.BuildDelivery();Debug.Log("CONNECTED_LANDSCAPE_DELIVERY_PASS");}
        [MenuItem("Calculus Lab/Background/Add connected Bridge landscape")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","ConnectedLandscape");
            string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity";var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter4World>(true)).Single(w=>w.topic==1);if(!world.IsAuthored)throw new Exception("Saved Bridge world is required");
                var transforms=world.GetComponentsInChildren<Transform>(true);var matrices=transforms.Select(t=>t.localToWorldMatrix).ToArray();var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();string references=JsonUtility.ToJson(world);
                var existing=world.GetComponentsInChildren<Renderer>(true);var materials=existing.Select(r=>string.Join(",",r.sharedMaterials.Select(m=>m.GetInstanceID()))).ToArray();var visible=existing.Select(r=>r.enabled).ToArray();
                var root=world.transform.Find(GroupName);bool added=root==null;
                if(added)
                {
                    root=Group(world.transform,GroupName);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add connected landscape");
                    var grass=world.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Near riverbank").sharedMaterial;
                    var shade=Material("Hillside grass",new Color(.14f,.25f,.19f));var rock=Material("Exposed rock",new Color(.23f,.27f,.27f));
                    Surface(root,"Near rolling riverbank",TerrainMesh(false),new[]{grass,shade,rock});Surface(root,"Far rolling riverbank",TerrainMesh(true),new[]{grass,shade,rock});
                    Surface(root,"Riverbank soil faces",ShoreMesh(),new[]{rock});
                    var river=Surface(root,WaterName,WaterMesh(),new[]{world.water.GetComponent<Renderer>().sharedMaterial});river.transform.SetParent(world.water,true);
                    PlantForest(Group(root,"Background forest"));PlaceRocks(Group(root,"Riverbank outcrops"));PlaceBeacons(Group(root,"Simulation survey lights"));
                    foreach(var renderer in existing)if(Replaced(renderer,world)){Undo.RecordObject(renderer,"Replace block terrain appearance");renderer.enabled=false;EditorUtility.SetDirty(renderer);}
                }
                for(int i=0;i<transforms.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(matrices[i][j]-transforms[i].localToWorldMatrix[j])>.0001f)throw new Exception("Landscape moved existing object "+transforms[i].name);
                for(int i=0;i<existing.Length;i++)if(materials[i]!=string.Join(",",existing[i].sharedMaterials.Select(m=>m.GetInstanceID()))||(!Replaced(existing[i],world)&&visible[i]!=existing[i].enabled))throw new Exception("Landscape altered an unrelated renderer");
                if(references!=JsonUtility.ToJson(world)||!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i)))throw new Exception("Landscape changed gameplay or collision");
                if(root.GetComponentsInChildren<Collider>(true).Length!=0||root.GetComponentsInChildren<Light>(true).Length!=0)throw new Exception("Background terrain must not change collision or lighting");
                var water=world.water.Find(WaterName);if(water==null)throw new Exception("Connected water must follow the existing rising river");
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true).Concat(water.GetComponentsInChildren<MeshFilter>(true)))if(filter.sharedMesh==null||!EditorUtility.IsPersistent(filter.sharedMesh))throw new Exception("Landscape mesh is not saved");
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material==null||!AssetDatabase.Contains(material)||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Landscape material is invalid");
                world.GetComponent<SimulationSky>()?.Apply();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Capture(world,"landscape-world.png",new Vector3(15,10,-8),new Vector3(0,3,20));Capture(world,"landscape-player.png",world.player.transform.position+Vector3.up*1.6f,world.player.transform.position+new Vector3(0,3.3f,25));Capture(world,"landscape-panorama.png",new Vector3(31,23,-20),new Vector3(0,3,20));
                File.WriteAllText(Path.Combine(Evidence,"result.txt"),"PASS: saved connected riverbanks, extended rising river, hills, forest and rocks; existing object transforms/materials/gameplay/colliders unchanged; only specified block scenery renderers hidden; no added collider/light; no duplicate terrain on reapply; three Edit Mode views.\n");
                Debug.Log("CONNECTED_LANDSCAPE_SAVED / "+(added?"added":"retained")+" / trees "+root.Find("Background forest").childCount);
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static bool Replaced(Renderer renderer,Chapter4World world)
        {
            if(renderer.name=="Near riverbank"||renderer.name=="Far riverbank"||renderer.transform==world.water)return true;
            string[] groups={"Distant floating terrain","Lower data field","Distant simulation gate","Simulation perimeter","Horizon data pylons"};
            var background=world.transform.Find("Simulation background");if(background==null)return false;
            return groups.Any(name=>{var group=background.Find(name);return group!=null&&renderer.transform.IsChildOf(group);});
        }
        static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static Material Material(string name,Color color)
        {
            string path=Art+"/"+name.Replace(" ","_")+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Glossiness",.05f);AssetDatabase.CreateAsset(material,path);return material;
        }
        static GameObject Surface(Transform parent,string name,Mesh mesh,Material[] materials)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;return go;}
        static float RiverCenter(float x){float outer=Mathf.Max(0,Mathf.Abs(x)-14);return 9.5f+Mathf.Sign(x)*Mathf.Sin(outer*.075f)*4;}
        static float RiverWidth(float x)=>5+Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(x)-14)/24))*2.5f;
        static float Hill(float x,float z,float cx,float cz,float size,float height)=>height*Mathf.Exp(-((x-cx)*(x-cx)+(z-cz)*(z-cz))/(size*size));
        static float Height(float x,float z)
        {
            float distance=Mathf.Max(Mathf.Max(Mathf.Abs(x)-13.8f,0),Mathf.Max(-8.8f-z,z-23.8f));float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/9));
            float riverDistance=Mathf.Max(0,Mathf.Abs(z-RiverCenter(x))-RiverWidth(x)/2);float shore=Mathf.SmoothStep(0,1,Mathf.Clamp01(riverDistance/6));
            float height=.5f+Hill(x,z,-27,29,16,10)+Hill(x,z,27,35,17,12)+Hill(x,z,-34,-17,15,7)+Hill(x,z,34,-16,15,8)+Hill(x,z,0,57,23,15)+Hill(x,z,-49,60,20,16)+Hill(x,z,49,57,22,18);
            height+=(Mathf.PerlinNoise(x*.08f+12,z*.08f+8)-.5f)*1.5f;
            float edge=Mathf.Max(Mathf.Abs(x)-54,Mathf.Max(-36-z,z-70));float fade=Mathf.Clamp01(edge/10);
            return Mathf.Lerp(height*blend*shore,-10,fade);
        }
        static Mesh TerrainMesh(bool far)
        {
            string path=Art+(far?"/Far_bank.asset":"/Near_bank.asset");var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;
            const int nx=64,nz=40;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
            Vector3 Point(int i,int j){float x=-64+i*2;float edge=RiverCenter(x)+(far?1:-1)*RiverWidth(x)/2;float z=far?Mathf.Lerp(edge,80,j/(float)nz):Mathf.Lerp(-46,edge,j/(float)nz);return new Vector3(x,Height(x,z),z);}
            void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);uv.Add(new Vector2(a.x,a.z)*.08f);uv.Add(new Vector2(b.x,b.z)*.08f);uv.Add(new Vector2(c.x,c.z)*.08f);
                var normal=Vector3.Cross(b-a,c-a).normalized;float y=(a.y+b.y+c.y)/3;int material=normal.y<.78f&&y>2?2:y>1.8f?1:0;triangles[material].AddRange(new[]{index,index+1,index+2});
            }
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){var a=Point(i,j);var b=Point(i,j+1);var c=Point(i+1,j+1);var d=Point(i+1,j);Triangle(a,b,c);Triangle(a,c,d);}
            var mesh=new Mesh{name=far?"Continuous far riverbank":"Continuous near riverbank"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh WaterMesh()
        {
            string path=Art+"/Continuous_river.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=128;i++){float x=-64+i;vertices.Add(new Vector3(x,-.5f,RiverCenter(x)-RiverWidth(x)/2));vertices.Add(new Vector3(x,-.5f,RiverCenter(x)+RiverWidth(x)/2));if(i<128){int n=i*2;triangles.AddRange(new[]{n,n+1,n+3,n,n+3,n+2});}}
            var mesh=new Mesh{name="Extended rising river"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh ShoreMesh()
        {
            string path=Art+"/Shore_faces.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int side=-1;side<=1;side+=2)for(int i=0;i<128;i++)
            {
                float x=-64+i,next=x+1,z=RiverCenter(x)+side*RiverWidth(x)/2,nz=RiverCenter(next)+side*RiverWidth(next)/2;float y=Height(x,z),ny=Height(next,nz);int n=vertices.Count;
                vertices.AddRange(new[]{new Vector3(x,y,z),new Vector3(next,ny,nz),new Vector3(next,Mathf.Min(-.68f,ny-.4f),nz),new Vector3(x,Mathf.Min(-.68f,y-.4f),z)});
                triangles.AddRange(side<0?new[]{n,n+2,n+1,n,n+3,n+2}:new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=new Mesh{name="Visible riverbank soil"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static GameObject Place(Transform parent,string model,string name,Vector3 bottom,float height,float yaw,bool byHeight=true)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ThirdParty/Kenney/NatureKit/"+model+".prefab");if(prefab==null)throw new Exception("Missing landscape asset "+model);var root=Group(parent,name);root.localPosition=bottom;root.localRotation=Quaternion.Euler(0,yaw,0);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.SetParent(root,false);
            var bounds=instance.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();var combined=bounds[0];foreach(var b in bounds.Skip(1))combined.Encapsulate(b);root.localScale=Vector3.one*height/(byHeight?combined.size.y:Mathf.Max(combined.size.x,combined.size.y,combined.size.z));
            float baseY=instance.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);root.position+=Vector3.up*(bottom.y-baseY);
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);return root.gameObject;
        }
        static void PlantForest(Transform parent)
        {
            var random=new System.Random(427);int count=0;for(int attempt=0;attempt<1200&&count<92;attempt++)
            {
                float x=-48+(float)random.NextDouble()*96,z=-30+(float)random.NextDouble()*96;
                if(Mathf.Abs(x)<18&&z>-13&&z<29||Mathf.Abs(z-RiverCenter(x))<RiverWidth(x)/2+4)continue;
                Place(parent,count%3==0?"tree_pineSmallA":count%2==0?"tree_pineRoundA":"tree_pineDefaultA","Hillside pine "+(++count),new Vector3(x,Height(x,z),z),5+(float)random.NextDouble()*5,(float)random.NextDouble()*360);
            }
            if(count!=92)throw new Exception("Forest placement did not complete");
        }
        static void PlaceRocks(Transform parent)
        {
            var rock=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Exposed_rock.mat");
            for(int side=-1;side<=1;side+=2)for(int i=0;i<9;i++)
            {
                float x=side*(18+i*3.2f),z=RiverCenter(x)+(i%2==0?-1:1)*(RiverWidth(x)/2+2.5f);var instance=Place(parent,i%2==0?"rock_largeA":"rock_largeC","Shore outcrop "+side+"-"+i,new Vector3(x,Height(x,z)-.15f,z),3+i%3*.8f,i*37,false);
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>()){renderer.sharedMaterials=Enumerable.Repeat(rock,renderer.sharedMaterials.Length).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            }
        }
        static void PlaceBeacons(Transform parent)
        {
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/SimulationBackground/Structure.mat");var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/SimulationBackground/Cyan_light.mat");if(metal==null||glow==null)throw new Exception("Simulation background materials are required");
            foreach(var p in new[]{new Vector2(-26,28),new Vector2(28,36),new Vector2(-34,-18),new Vector2(35,-18),new Vector2(0,56)})
            {
                var beacon=Group(parent,"Hill survey beacon");beacon.localPosition=new Vector3(p.x,Height(p.x,p.y),p.y);
                void Part(string name,Vector3 center,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(beacon,false);go.transform.localPosition=center;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;}
                Part("Survey mast",new Vector3(0,2,0),new Vector3(.25f,2,.25f),metal);Part("Cyan survey band",new Vector3(0,3.5f,0),new Vector3(.5f,.12f,.5f),glow);
            }
        }
        static void ReloadCheck()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();if(world.GetComponentsInChildren<Transform>(true).Count(t=>t.name==GroupName)!=1||world.water.childCount==0)throw new Exception("Landscape failed to reload");
            var surface=world.water.Find(WaterName);var start=surface.position;var river=world.water.position;world.water.position+=Vector3.up*.25f;if(Mathf.Abs(surface.position.y-start.y-.25f)>.0001f)throw new Exception("Extended river does not follow water height");world.water.position=river;
            if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Editor landscape should not create a session");Debug.Log("CONNECTED_LANDSCAPE_RELOAD_PASS");
        }
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 look)
        {
            var go=new GameObject("Landscape evidence camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=position;camera.transform.LookAt(look);
            var target=RenderTexture.GetTemporary(1280,720,24);var prior=RenderTexture.active;var canvas=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();bool enabled=canvas!=null&&canvas.enabled;Texture2D pixels=null;
            try{if(canvas!=null)canvas.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Evidence,name),pixels.EncodeToPNG());}
            finally{if(canvas!=null)canvas.enabled=enabled;camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);}
        }
    }
}
#endif
