#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using Object=UnityEngine.Object;
namespace CalculusLabEscape.Editor
{
    public static class RiverValleyEnvironmentAuthoring
    {
        const string Art="Assets/_Project/Art/RiverValley/";
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        static string Evidence=>LearningApi.Argument("--valley-evidence",Path.GetFullPath("../../qa/river-valley-redesign-2026-10-25"));
        static Transform root;
        static Material grass,grassShade,rock,soil,bark,leaves,leavesLight,wood,steel,canvas,trail,water,foam;
        static Color Colour(string value){ColorUtility.TryParseHtmlString("#"+value,out var colour);return colour;}
        static Material Mat(string name,string colour,float gloss=.12f)
        {
            string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(m,path);}m.color=Colour(colour);m.SetFloat("_Glossiness",gloss);EditorUtility.SetDirty(m);return m;
        }
        static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static Transform Shape(Transform parent,string name,Vector3 position,Vector3 scale,Material material,PrimitiveType primitive=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
        {var t=Shape(parent,name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b)/2,width),material,PrimitiveType.Cylinder);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static Mesh SavedMesh(string name,List<Vector3> vertices,List<int>[] triangles)
        {
            string path=Art+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.indexFormat=vertices.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16;mesh.SetVertices(vertices);mesh.subMeshCount=triangles.Length;for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        static Transform Surface(Transform parent,string name,Mesh mesh,params Material[] materials)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;return go.transform;}
        static float Center(float x)=>9.5f+Mathf.Sign(x)*Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(x)-15)/30))*Mathf.Sin(Mathf.Abs(x)*.035f)*3.8f;
        static float Width(float x)=>5+Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(x)-14)/34))*3;
        static float Hill(float x,float z,float cx,float cz,float r,float height)=>height*Mathf.Exp(-((x-cx)*(x-cx)+(z-cz)*(z-cz))/(r*r));
        static float Height(float x,float z)
        {
            float outside=Mathf.Max(Mathf.Max(Mathf.Abs(x)-14.5f,0),Mathf.Max(-9.5f-z,z-25));float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/16));
            float shore=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(z-Center(x))-Width(x)/2)/8));
            float y=Hill(x,z,-34,39,19,11)+Hill(x,z,37,48,23,15)+Hill(x,z,-38,-28,23,11)+Hill(x,z,40,-30,21,13)+Hill(x,z,-63,82,25,23)+Hill(x,z,15,94,28,25)+Hill(x,z,69,100,30,32);
            y+=(Mathf.PerlinNoise(x*.055f+31,z*.055f+19)-.5f)*2.4f+.15f;return Mathf.Max(0,y*blend*shore);
        }
        static Mesh Bank(bool far)
        {
            var vertices=new List<Vector3>();var ts=new[]{new List<int>(),new List<int>(),new List<int>()};
            Vector3 P(int x,int z){float px=-96+x*3;float edge=Center(px)+(far?1:-1)*Width(px)/2;float pz=far?Mathf.Lerp(edge,135,z/48f):Mathf.Lerp(-75,edge,z/32f);return new Vector3(px,Height(px,pz),pz);}
            void Triangle(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.AddRange(new[]{a,b,c});var normal=Vector3.Cross(b-a,c-a).normalized;float h=(a.y+b.y+c.y)/3;int mat=normal.y<.76f&&h>3?2:h>4?1:0;ts[mat].AddRange(new[]{n,n+1,n+2});}
            int nz=far?48:32;for(int x=0;x<64;x++)for(int z=0;z<nz;z++){var a=P(x,z);var b=P(x,z+1);var c=P(x+1,z+1);var d=P(x+1,z);Triangle(a,b,c);Triangle(a,c,d);}
            return SavedMesh(far?"Valley far hillside":"Valley near hillside",vertices,ts);
        }
        static Mesh River()
        {
            var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<=128;i++){float x=-96+i*1.5f;v.Add(new Vector3(x,-.49f,Center(x)-Width(x)/2));v.Add(new Vector3(x,-.49f,Center(x)+Width(x)/2));if(i<128){int n=i*2;t.AddRange(new[]{n,n+1,n+3,n,n+3,n+2});}}
            return SavedMesh("Valley river surface",v,new[]{t});
        }
        static Mesh Shore()
        {
            var v=new List<Vector3>();var t=new List<int>();for(int side=-1;side<=1;side+=2)for(int i=0;i<128;i++){float x=-96+i*1.5f,nx=x+1.5f,z=Center(x)+side*Width(x)/2,nz=Center(nx)+side*Width(nx)/2;int n=v.Count;v.AddRange(new[]{new Vector3(x,Height(x,z),z),new Vector3(nx,Height(nx,nz),nz),new Vector3(nx,-1.2f,nz),new Vector3(x,-1.2f,z)});t.AddRange(side<0?new[]{n,n+2,n+1,n,n+3,n+2}:new[]{n,n+1,n+2,n,n+2,n+3});}return SavedMesh("Valley soil banks",v,new[]{t});
        }
        static void PathRibbon(string name,float width,params Vector3[] points)
        {
            var v=new List<Vector3>();var ts=new List<int>();var sampled=new List<Vector3>();
            for(int segment=0;segment<points.Length-1;segment++)for(int i=0;i<16;i++){
                float t=i/15f;var p0=points[Mathf.Max(0,segment-1)];var p1=points[segment];var p2=points[segment+1];var p3=points[Mathf.Min(points.Length-1,segment+2)];var p=.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);sampled.Add(p);
            }
            for(int i=0;i<sampled.Count;i++){var forward=(sampled[Mathf.Min(i+1,sampled.Count-1)]-sampled[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(Vector3.up,forward)*width*.5f;var p=sampled[i];p.y=.027f;v.Add(p-side);v.Add(p+side);if(i<sampled.Count-1){int n=i*2;ts.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}}
            Surface(root,name,SavedMesh(name,v,new[]{ts}),trail);
        }
        static void Retint(Renderer renderer,bool stone=false)
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>stone?rock:m!=null&&m.name.ToLowerInvariant().Contains("leaf")?(renderer.transform.position.x>0?leavesLight:leaves):m!=null&&(m.name.ToLowerInvariant().Contains("wood")||m.name.ToLowerInvariant().Contains("bark"))?bark:m).ToArray();EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        static Transform Prop(Transform parent,string model,string name,Vector3 bottom,float height,float yaw,bool tree=true)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ThirdParty/Kenney/NatureKit/"+model+".prefab");if(source==null)throw new Exception("Missing nature prefab: "+model);
            var group=Group(parent,name);group.localPosition=bottom;group.localRotation=Quaternion.Euler(0,yaw,0);var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.transform.SetParent(group,false);
            var rs=instance.GetComponentsInChildren<Renderer>(true);var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);group.localScale*=height/bounds.size.y;bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);instance.transform.position+=Vector3.up*(bottom.y-bounds.min.y);
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);foreach(var r in rs)Retint(r,!tree);return group;
        }
        static void Label(string name,string value,Vector3 pos,float size,float yaw=180)
        {
            var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,yaw,0);var t=go.GetComponent<TextMeshPro>();t.text=value;t.fontSize=size;t.font=Object.FindFirstObjectByType<GameManager>().missionUI.GetComponentsInChildren<TMP_Text>(true).First().font;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(6,1);t.color=Colour("edf1dc");
        }
        static void DressValley()
        {
            Surface(root,"Rolling meadow - near",Bank(false),grass,grassShade,rock);Surface(root,"Rolling meadow - far",Bank(true),grass,grassShade,rock);Surface(root,"Exposed riverbank soil",Shore(),soil);
            PathRibbon("Entrance to power trail",1.65f,new Vector3(0,0,-8.9f),new Vector3(0,0,-5.5f),new Vector3(-3,0,-3.7f),new Vector3(-5,0,-1.4f));
            PathRibbon("Repair station connecting trail",1.45f,new Vector3(-5,0,-1.4f),new Vector3(-1,0,-1.9f),new Vector3(3.9f,0,-.6f),new Vector3(3.1f,0,3.2f),new Vector3(0,0,5.3f));
            PathRibbon("Far bank reunion trail",1.65f,new Vector3(0,0,12.1f),new Vector3(.2f,0,15),new Vector3(4,0,18),new Vector3(8,0,23));
            foreach(var p in new[]{new Vector3(-5,.018f,1),new Vector3(4,.018f,2),new Vector3(3,.018f,5)})Shape(root,"Work area stone pad",p,new Vector3(1.55f,.008f,1.3f),trail,PrimitiveType.Cylinder);
            var forest=Group(root,"Layered hillside forest");
            for(int i=0;i<110;i++){
                float x=-78+(i*37%157),z=-47+(i*53%169);if(Mathf.Abs(x)<17&&z>-13&&z<31)continue;if(Mathf.Abs(z-Center(x))<Width(x)/2+3)continue;
                float y=Height(x,z);string model=i%4==0?"tree_oak":i%3==0?"tree_pineRoundA":"tree_pineDefaultA";Prop(forest,model,"Hillside tree "+i,new Vector3(x,y,z),4.3f+(i%5)*.8f,i*53);
            }
            for(int i=0;i<12;i++){float x=i<6?-17-i*1.7f:17+(i-6)*1.7f;float z=-8+(i%6)*5.2f;Prop(forest,i%2==0?"tree_oak":"tree_pineRoundA","Valley edge tree "+i,new Vector3(x,Height(x,z),z),5.2f+(i%3)*.65f,i*73);}
            var stones=Group(root,"River stones and meadow planting");
            for(int i=0;i<30;i++){float x=-42+i*2.9f;if(Mathf.Abs(x)<5)continue;float z=Center(x)+(i%2==0?-1:1)*(Width(x)/2+.6f);Prop(stones,i%3==0?"rock_largeC":"rock_largeA","Shore outcrop "+i,new Vector3(x,Height(x,z)-.1f,z),.6f+(i%4)*.22f,i*61,false);}
            for(int i=0;i<28;i++){float x=(i%2==0?-1:1)*(8.2f+i%4*.9f);float z=-7+(i/2)*2.3f;if(z>6&&z<13)continue;Prop(stones,"grass_large","Meadow fern "+i,new Vector3(x,Height(x,z)+.015f,z),.28f+i%3*.09f,i*47);}
            var entrance=Group(root,"Forest field station entrance");
            foreach(float x in new[]{-3.2f,3.2f}){Shape(entrance,"Entrance timber pier",new Vector3(x,1.65f,-10.15f),new Vector3(.28f,3.3f,.28f),wood);Shape(entrance,"Pier stone footing",new Vector3(x,.22f,-10.15f),new Vector3(.55f,.44f,.55f),rock);}
            Shape(entrance,"Entrance lintel",new Vector3(0,3.25f,-10.15f),new Vector3(6.9f,.32f,.5f),wood);
            Shape(entrance,"Canvas field shelter",new Vector3(0,3.65f,-12),new Vector3(7.3f,.10f,4.4f),canvas).localRotation=Quaternion.Euler(0,0,5);
            Shape(entrance,"Station name plaque",new Vector3(0,2.8f,-9.96f),new Vector3(4.7f,.57f,.1f),steel);Label("Station entrance title","RIVERBEND FIELD STATION",new Vector3(0,2.8f,-9.88f),2.0f);
            for(int side=-1;side<=1;side+=2){Shape(entrance,"Supply crate",new Vector3(side*4.7f,.5f,-11.3f),new Vector3(1.2f,1,1),wood);Shape(entrance,"Supply crate brace",new Vector3(side*4.7f,.5f,-10.79f),new Vector3(1.05f,.10f,.03f),steel).localRotation=Quaternion.Euler(0,0,side*30);}
            var waterfall=Group(root,"Upstream waterfall landmark");
            for(int i=0;i<7;i++)Prop(waterfall,"rock_largeC","Waterfall escarpment "+i,new Vector3(-47+i%3*2.4f,-.8f,4+i*1.9f),5.7f+i%3*1.3f,i*31,false);
            var fallMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"Waterfall.mat");if(fallMat==null){fallMat=new Material(water){name="Waterfall"};fallMat.SetFloat("_Fall",1);AssetDatabase.CreateAsset(fallMat,Art+"Waterfall.mat");}
            Shape(waterfall,"Flowing waterfall",new Vector3(-42.1f,2.5f,10),new Vector3(3.5f,6,.03f),fallMat,PrimitiveType.Quad).localRotation=Quaternion.Euler(0,90,0);
            Shape(waterfall,"Waterfall foam pool",new Vector3(-41.9f,-.46f,10),new Vector3(5,.02f,5),foam,PrimitiveType.Cylinder);
            foreach(float x in new[]{-7f,7f}){Shape(root,"River observation bench seat",new Vector3(x,.49f,4.7f),new Vector3(2,.13f,.6f),wood);for(int side=-1;side<=1;side+=2)Shape(root,"Bench footing",new Vector3(x+side*.7f,.24f,4.7f),new Vector3(.15f,.48f,.45f),steel);}
        }
        [MenuItem("Calculus Lab/World/Rebuild Differentiation river valley")]
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before changing the environment.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open scene edits first.");
            Directory.CreateDirectory(Evidence);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var manager=Object.FindFirstObjectByType<GameManager>();var world=manager.world;
            var before=world.GetComponentsInChildren<Collider>(true);var colliderStates=before.Select(c=>c.enabled).ToArray();var colliderMatrices=before.Select(c=>c.transform.localToWorldMatrix).ToArray();
            var critical=new[]{world.player.transform,world.calf,world.mother,world.station,world.water}.Concat(world.devices).Concat(world.bridge).Distinct().ToArray();var poses=critical.Select(t=>t.localToWorldMatrix).ToArray();
            Capture(world,"before-entry",world.player.transform.position+Vector3.up*1.6f,new Vector3(0,1.7f,9));
            var old=world.transform.Find("Riverbend Valley Environment");if(old!=null)Object.DestroyImmediate(old.gameObject);var oldWater=world.water.Find("Riverbend flowing river");if(oldWater!=null)Object.DestroyImmediate(oldWater.gameObject);
            foreach(var groupName in new[]{"Connected river landscape","Simulation background","Environment/Path and banks"}){var group=world.transform.Find(groupName);if(group!=null){group.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(group.gameObject);}}
            foreach(var r in world.GetComponentsInChildren<Renderer>(true))if(r.name=="Near riverbank"||r.name=="Far riverbank"||r.name=="Walkway stone"||r.transform.IsChildOf(world.water)){r.enabled=false;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            grass=Mat("Sunlit meadow","708748");grassShade=Mat("Distant forest slopes","52765a");rock=Mat("River limestone","78847d");soil=Mat("Riverbank earth","655d46");bark=Mat("Pine bark","71513c");leaves=Mat("Forest green","397154");leavesLight=Mat("Fresh green","548449");wood=Mat("Bridge cedar","89603c");steel=Mat("Field station metal","354e4d",.25f);canvas=Mat("Canvas ochre","c5b17a");trail=Mat("Warm gravel trail","a89b76");foam=Mat("Waterfall foam","9acfc5",.45f);
            water=AssetDatabase.LoadAssetAtPath<Material>(Art+"Valley water.mat");if(water==null){water=new Material(Shader.Find("Calculus/Valley Flowing Water")){name="Valley water"};AssetDatabase.CreateAsset(water,Art+"Valley water.mat");}
            root=Group(world.transform,"Riverbend Valley Environment");DressValley();
            var nature=world.transform.Find("Imported environment art");if(nature!=null)foreach(var r in nature.GetComponentsInChildren<Renderer>(true))Retint(r,r.name.StartsWith("rock",StringComparison.OrdinalIgnoreCase));
            var bridge=world.transform.Find("Bridge");foreach(var r in bridge.GetComponentsInChildren<Renderer>(true))if(r.name=="Bridge section 0"||r.name.StartsWith("Bridge section")||r.name.Contains("handrail"))r.sharedMaterial=wood;
            var safety=world.transform.Find("Bridge route safety");foreach(var r in safety.GetComponentsInChildren<Renderer>(true)){var text=r.GetComponent<TMP_Text>();if(text!=null){text.fontSharedMaterial=text.font.material;EditorUtility.SetDirty(text);continue;}var legacyText=r.GetComponent<TextMesh>();if(legacyText!=null){r.sharedMaterial=legacyText.font.material;legacyText.text=legacyText.text.Replace("FOLLOW THE STONE PATH","FOLLOW THE FIELD TRAIL");EditorUtility.SetDirty(legacyText);EditorUtility.SetDirty(r);continue;}r.sharedMaterial=r.name.Contains("rail")?wood:r.name.Contains("stripe")||r.name=="Post cap"?canvas:steel;EditorUtility.SetDirty(r);}
            var data=new SerializedObject(world);data.FindProperty("timber").objectReferenceValue=wood;data.FindProperty("riverMaterial").objectReferenceValue=water;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Prefabs+"RiverbendValleyEnvironment.prefab",InteractionMode.AutomatedAction);
            var river=Surface(world.transform,"Riverbend flowing river",River(),water);river.SetParent(world.water,true);PrefabUtility.SaveAsPrefabAssetAndConnect(river.gameObject,Prefabs+"RiverbendValleyWater.prefab",InteractionMode.AutomatedAction);PrefabUtility.RecordPrefabInstancePropertyModifications(river);
            var skyMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"Morning sky.mat");if(skyMat==null){skyMat=new Material(Shader.Find("Calculus/Valley Morning Sky")){name="Morning sky"};AssetDatabase.CreateAsset(skyMat,Art+"Morning sky.mat");}
            var sky=world.GetComponent<SimulationSky>();sky.environment=skyMat;sky.fogTint=Colour("9bb2af");sky.fogDensity=.0048f;sky.Apply();
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Colour("9cb5c2");RenderSettings.ambientEquatorColor=Colour("8d9c7b");RenderSettings.ambientGroundColor=Colour("526348");RenderSettings.reflectionIntensity=.25f;
            var key=world.GetComponentsInChildren<Light>(true).First(l=>l.type==LightType.Directional);key.color=Colour("fff0d2");key.intensity=1.08f;key.transform.rotation=Quaternion.Euler(48,-32,0);key.shadows=LightShadows.Soft;key.shadowStrength=.65f;
            foreach(var camera in world.GetComponentsInChildren<Camera>(true)){camera.farClipPlane=240;EditorUtility.SetDirty(camera);}
            var preview=world.transform.Find("Storyboard view");preview.position=new Vector3(8,4.8f,-3);preview.LookAt(new Vector3(0,1.8f,8));
            Physics.SyncTransforms();for(int i=0;i<before.Length;i++)if(before[i]==null||before[i].enabled!=colliderStates[i]||!Same(colliderMatrices[i],before[i].transform.localToWorldMatrix))throw new Exception("Environment changed an existing collider: "+before[i]);
            for(int i=0;i<critical.Length;i++)if(!Same(poses[i],critical[i].localToWorldMatrix))throw new Exception("Environment moved gameplay object: "+critical[i].name);
            if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Scenery must not add hidden obstacles to the existing route.");
            foreach(var component in new Component[]{world,sky,key,key.transform,preview}){EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Validate();CaptureSaved();File.WriteAllText(System.IO.Path.Combine(Evidence,"authored.txt"),"PASS: saved Riverbend valley environment/water prefabs; daytime sky, layered hills, natural forest, paths, field-station entrance and upstream waterfall. Existing player, three stations, bridge sections, animals, colliders, grading, UI and video references retained.");
        }
        static bool Same(Matrix4x4 a,Matrix4x4 b){for(int i=0;i<16;i++)if(Mathf.Abs(a[i]-b[i])>.0001f)return false;return true;}
        public static void Validate()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();
            var environment=world.transform.Find("Riverbend Valley Environment");var river=world.water.Find("Riverbend flowing river");if(environment==null||river==null||!PrefabUtility.IsPartOfPrefabInstance(environment)||!PrefabUtility.IsPartOfPrefabInstance(river))throw new Exception("Save both valley prefabs in the scene.");
            foreach(var r in environment.GetComponentsInChildren<Renderer>(true).Concat(river.GetComponentsInChildren<Renderer>(true)))foreach(var m in r.sharedMaterials)if(m==null||!AssetDatabase.Contains(m)||ShaderUtil.ShaderHasError(m.shader))throw new Exception("Invalid saved scenery material: "+r.name);
            var rescue=world.GetComponent<BridgeRescueSequence>();if(rescue==null||rescue.route.Length!=4||world.devices.Count!=3||world.bridge.Count!=9)throw new Exception("Keep the original three-device rescue layout.");
            ManualEditingMigration.Validate();File.WriteAllText(System.IO.Path.Combine(Evidence,"saved-layout.txt"),"PASS: environment and water persist as prefabs, no added scenery colliders, original three-device bridge rescue references and four-scene UI events valid.");
        }
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 target)
        {
            var go=new GameObject("Valley review camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.transform.position=position;camera.transform.LookAt(target);camera.farClipPlane=240;
            try{LaboratoryAIImport.Capture(camera,System.IO.Path.Combine(Evidence,name+".png"));}finally{Object.DestroyImmediate(go);}
        }
        public static void CaptureSaved()
        {
            Directory.CreateDirectory(Evidence);EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();
            Capture(world,"valley-entry",world.player.transform.position+Vector3.up*1.6f,new Vector3(0,1.65f,9));Capture(world,"valley-back",world.player.transform.position+Vector3.up*1.6f,new Vector3(0,2,-12));Capture(world,"valley-wide",new Vector3(24,16,-20),new Vector3(0,2,9));Capture(world,"valley-river",new Vector3(-12,3,4),new Vector3(-32,2.7f,10));
            var rescue=world.GetComponent<BridgeRescueSequence>();world.Restore(5);rescue.Sample(.9f,false);Capture(world,"valley-rescue",new Vector3(7,5,6),new Vector3(0,.8f,12));rescue.ResetPose();EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
        }
        public static void AuthorAndBuild(){Author();ManualEditingMigration.BuildQA();}
        public static void ValidateAndOpen(){Validate();Open();}
        [MenuItem("Calculus Lab/World/Open Differentiation river valley")]
        public static void Open()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();Selection.activeGameObject=world.transform.Find("Riverbend Valley Environment").gameObject;
            var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();view.LookAt(new Vector3(0,2,8),Quaternion.Euler(23,-30,0),28,false,true);view.Focus();
        }
        public static void Inspect()
        {
            Directory.CreateDirectory(Evidence);EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();
            string PathOf(Transform t)=>AnimationUtility.CalculateTransformPath(t,world.transform);
            var lines=world.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==world.transform||t.parent!=null&&t.parent.parent==world.transform).Select(t=>PathOf(t)+" | active "+t.gameObject.activeSelf+" | renderers "+t.GetComponentsInChildren<Renderer>(true).Length+" | colliders "+t.GetComponentsInChildren<Collider>(true).Length).ToList();
            lines.Add("PLAYER "+world.player.transform.position);lines.Add("WATER "+world.water.position+" scale "+world.water.localScale);
            foreach(var p in world.devices)lines.Add("DEVICE "+PathOf(p)+" "+p.position);
            foreach(var p in world.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.name.Contains("Walkway")))lines.Add("TRAIL "+PathOf(p.transform));
            File.WriteAllLines(System.IO.Path.Combine(Evidence,"before-layout.txt"),lines);
            var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ThirdParty/Kenney/NatureKit/tree_oak.prefab");
            File.WriteAllLines(System.IO.Path.Combine(Evidence,"tree-materials.txt"),tree.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+" "+string.Join(" | ",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)+" "+m.shader.name))));
        }
    }
}
#endif
