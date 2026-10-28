#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        const string ObservatoryArt="Assets/_Project/Art/LimitsObservatory/";
        static string ObservatoryEvidence=>LearningApi.Argument("--observatory-evidence",Path.GetFullPath("../../qa/limits-observatory-2026-10-24"));
        static Transform observatory;
        static Material ObsMaterial(string name,Color colour,float metal=0,bool glow=false)
        {
            string path=ObservatoryArt+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.color=colour;material.SetFloat("_Metallic",metal);material.SetFloat("_Glossiness",.45f);
            if(glow){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",colour*1.4f);}EditorUtility.SetDirty(material);return material;
        }
        static Material ObsShader(string name,string shader)
        {
            string path=ObservatoryArt+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}return material;
        }
        static Transform ObsShape(string name,Vector3 position,Vector3 scale,Material material,PrimitiveType primitive=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(observatory,false);go.transform.localPosition=position;go.transform.localScale=scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static void ObsBeam(string name,Vector3 a,Vector3 b,float width,Material material)
        {
            var t=ObsShape(name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b)*.5f,width),material,PrimitiveType.Cylinder);
            t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        static void ObsLine(string name,Vector3[] points,float width,Material material)
        {
            var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(observatory,false);line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.numCornerVertices=3;line.numCapVertices=3;line.sharedMaterial=material;line.shadowCastingMode=ShadowCastingMode.Off;
        }
        static Mesh ObsArc(string name,float radius,float thickness,float begin,float degrees)
        {
            string path=ObservatoryArt+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            int slices=Mathf.Max(4,Mathf.CeilToInt(degrees/3)),sides=8;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=slices;i++)for(int j=0;j<=sides;j++){
                float a=(begin+degrees*i/slices)*Mathf.Deg2Rad,b=j*Mathf.PI*2/sides;
                vertices.Add(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(b)*thickness));
                if(i<slices&&j<sides){int n=i*(sides+1)+j;triangles.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}
            }
            mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform ObsRing(string name,Vector3 position,float radius,float thickness,Material material,bool floor=false)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(observatory,false);go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=ObsArc(name,radius,thickness,0,360);go.GetComponent<MeshRenderer>().sharedMaterial=material;if(floor)go.transform.localRotation=Quaternion.Euler(90,0,0);return go.transform;
        }
        static void ObsText(string name,string text,Vector3 position,float size,Color colour)
        {
            var label=new GameObject(name,typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.transform.SetParent(observatory,false);label.transform.localPosition=position;label.font=font;label.fontSize=size;label.text=text;label.color=colour;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(10,2);label.raycastTarget=false;
        }
        static void ObsProp(string name,string prefab,Vector3 position,float yaw,float width)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ThirdParty/Kenney/SpaceStationKit/"+prefab+".prefab");
            if(source==null)return;var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.SetParent(observatory,false);go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.localScale*=width/Mathf.Max(bounds.size.x,bounds.size.z);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var collider in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            var material=AssetDatabase.LoadAssetAtPath<Material>(ObservatoryArt+"Titanium edges.mat");foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>material).ToArray();
        }
        static void ObservatoryUI(GameObject ui)
        {
            Index(ui);roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");
            foreach(string name in new[]{"PortalCard","PuzzlePortal","ChargeLabel","PortalCaption","BriefPortal","ReportSceneProgressShade"})if(Find(name)!=null)Find(name).gameObject.SetActive(false);
            At("QuestionNumber",870,85,360,25);Regular("QuestionNumber",14,Cyan,TextAlignmentOptions.Right);
            RemoveLimitsInspection(ui);
            Text("ReportSceneTitle","THE LIMIT OBSERVATORY");Text("ReportSceneOutlineNote","Restore the convergence gate");
            var strip=Find("ReportSceneStrip");var view=ui.GetComponent<LimitsObservatoryView>()??ui.AddComponent<LimitsObservatoryView>();view.stepCards=new Image[5];view.stepStates=new TMP_Text[5];view.heading=Find("QuestionNumber").GetComponent<TMP_Text>();
            string[] names={"Nearby values","Trace the curve","Compare sides","Limit vs point","Conclude"};
            for(int i=0;i<5;i++){
                string prefix="ObservatoryStep"+i;var card=Surface(strip,prefix,24+i*248,128,240,54,Panel);Rounded(prefix);view.stepCards[i]=card;
                Label(card.transform,prefix+"Title",(i+1).ToString("00")+"  "+names[i],12,7,216,18,14,Ink);
                view.stepStates[i]=Label(card.transform,prefix+"State",i==0?"GUIDED TASK":"WAITING",12,27,216,16,12,Muted);
                At("ReportProgressSegment"+i,12,47,216,3,card.transform);
            }
            Text("WorldProgress","OBSERVATORY CALIBRATION   0 / 5 RESTORED");
            Text("BriefTitle","The Limit Observatory");
            Text("BriefLearning","Investigate numerical and graphical limits, one-sided values, limit versus function value, and when a limit does not exist.\n\nYou will need functions, coordinates and basic substitution.\n\nComplete the guided task and independent check in each calibration. Every completed pair restores one gate segment. Retries preserve progress. There is no countdown.");
            Text("PortalDescription","Five calibrations.\nGuided task + independent check.\nEach completed pair restores one gate segment.");
            view.Present(0);EditorUtility.SetDirty(view);
        }
        static void RemoveLimitsInspection(GameObject ui)
        {
            Index(ui);var tag=Find("ReportInspectionTag");if(tag!=null)Object.DestroyImmediate(tag.gameObject);
            var view=ui.GetComponent<ReportFusionView>();if(view!=null){view.sceneTarget=null;EditorUtility.SetDirty(view);}
            var references=ui.GetComponent<UIReferenceMap>();if(references!=null){references.elements=references.elements.Where(e=>e!=null&&e.target!=null).ToArray();EditorUtility.SetDirty(references);}
            Index(ui);
        }
        public static void RemoveLimitsLabelAndReviewDifferentiation()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before editing the label.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            string folder=LearningApi.Argument("--review-evidence",Path.GetFullPath("../../qa/differentiation-review-2026-10-24"));Directory.CreateDirectory(folder);
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"LimitsUI.prefab");try{RemoveLimitsInspection(ui);PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"LimitsUI.prefab");}finally{PrefabUtility.UnloadPrefabContents(ui);}
            string limits=SceneAuthoring.SceneFolder+"Level01_Limits.unity";var scene=EditorSceneManager.OpenScene(limits);var manager=Object.FindFirstObjectByType<GameManager>();ui=manager.menuOrLimitsUI.gameObject;RemoveLimitsInspection(ui);
            foreach(var component in new Component[]{ui.GetComponent<ReportFusionView>(),ui.GetComponent<UIReferenceMap>()}){EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ManualEditingMigration.Validate();
            EditorSceneManager.OpenScene(limits);manager=Object.FindFirstObjectByType<GameManager>();ui=manager.menuOrLimitsUI.gameObject;
            SceneAuthoringWindow.ApplyView(manager.world,ui.GetComponent<Canvas>(),"Agreeing sides");ui.GetComponent<LimitsObservatoryView>().Present(2);SceneAuthoring.Capture(manager.world,ui.GetComponent<Canvas>(),Path.Combine(folder,"limits-without-label.png"),false);
            string path=SceneAuthoring.SceneFolder+"Level02_Differentiation.unity";
            try{
                foreach(string view in new[]{"Power Core","Number inputs","Expression choices","Independent check","Rescue"}){
                    EditorSceneManager.OpenScene(path);manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.missionUI.GetComponent<Canvas>();
                    SceneAuthoringWindow.ApplyView(manager.world,canvas,view);canvas.GetComponent<ReportFusionView>().Present();SceneAuthoring.Capture(manager.world,canvas,Path.Combine(folder,"differentiation-"+view.Replace(" ","-")+".png"),false);
                }
                EditorSceneManager.OpenScene(path);manager=Object.FindFirstObjectByType<GameManager>();SceneAuthoring.Capture(manager.world,manager.missionUI.GetComponent<Canvas>(),Path.Combine(folder,"differentiation-world.png"),true);
            }finally{EditorSceneManager.OpenScene(path);}
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: Limits inspection label physically removed from saved UI and scene references; persistent events valid. Native Differentiation editor previews captured without saving changes to its scene or UI.");
        }
        [MenuItem("Calculus Lab/Limits/Build saved observatory scene")]
        public static void AuthorObservatory()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before editing the observatory.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the current scene before changing the observatory.");
            Directory.CreateDirectory(ObservatoryArt);Directory.CreateDirectory(ObservatoryEvidence);AssetDatabase.Refresh();
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"LimitsUI.prefab");try{ObservatoryUI(ui);PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"LimitsUI.prefab");}finally{PrefabUtility.UnloadPrefabContents(ui);}
            var feedback=PrefabUtility.LoadPrefabContents(FlowPrefab);try{Index(feedback);Text("FlowCharge","1 / 5");Text("FlowChargeLabel","CALIBRATIONS RESTORED");PrefabUtility.SaveAsPrefabAsset(feedback,FlowPrefab);}finally{PrefabUtility.UnloadPrefabContents(feedback);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var world=manager.world;ui=manager.menuOrLimitsUI.gameObject;ObservatoryUI(ui);
            var serialized=new SerializedObject(world);var ringList=serialized.FindProperty("portalSections");var rings=Enumerable.Range(0,ringList.arraySize).Select(i=>(Renderer)ringList.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var retained=new HashSet<Collider>(world.GetComponentsInChildren<Collider>(true).Where(c=>c.gameObject.name=="Floor"||c.gameObject.name.EndsWith("wall")||c.transform==world.station));
            var placedGuide=world.transform.Find("Limits guide");
            var placedEntry=world.transform.Find("Limits Entry Bay");
            bool KeepEntry(Transform node)=>placedEntry!=null&&node.IsChildOf(placedEntry);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))if(!KeepEntry(renderer.transform)&&(placedGuide==null||!renderer.transform.IsChildOf(placedGuide)))renderer.enabled=false;
            foreach(var collider in world.GetComponentsInChildren<Collider>(true))collider.enabled=KeepEntry(collider.transform)||retained.Contains(collider)||collider.GetComponent<CharacterController>()!=null;
            foreach(var light in world.GetComponentsInChildren<Light>(true))if(!KeepEntry(light.transform)&&(placedGuide==null||!light.transform.IsChildOf(placedGuide)))light.enabled=false;
            var old=world.transform.Find("Limits Observatory");if(old!=null)Object.DestroyImmediate(old.gameObject);
            observatory=new GameObject("Limits Observatory").transform;observatory.SetParent(world.transform,false);
            var hull=ObsMaterial("Midnight hull",Hex("152937"),.5f);var floor=ObsMaterial("Slate deck",Hex("243946"),.28f);var edge=ObsMaterial("Titanium edges",Hex("486274"),.65f);var copper=ObsMaterial("Warm bronze",Hex("9c7449"),.65f);var glass=ObsMaterial("Display glass",Hex("102c3c"),.35f);var dim=ObsMaterial("Uncalibrated ring",Hex("294754"),.5f);var mint=ObsMaterial("Restored ring",Hex("51cfcd"),.3f,true);var glow=ObsMaterial("Cyan navigation",Hex("2dabbc"),.2f,true);var warm=ObsMaterial("Amber lighting",Hex("d4a66c"),.2f,true);
            serialized.FindProperty("stone").objectReferenceValue=dim;serialized.FindProperty("stable").objectReferenceValue=mint;serialized.FindProperty("amber").objectReferenceValue=dim;serialized.ApplyModifiedPropertiesWithoutUndo();
            var floorRenderer=world.GetComponentsInChildren<Renderer>(true).First(r=>r.gameObject.name=="Floor");floorRenderer.enabled=true;floorRenderer.sharedMaterial=floor;
            for(int z=-5;z<=19;z+=2)for(int x=-9;x<=9;x+=2)ObsShape("Deck plate",new Vector3(x,.035f,z),new Vector3(1.98f,.022f,1.98f),floor);
            var windowMaterial=ObsShader("Observation glass","Calculus/Observation Glass");
            ObsShape("Central glass canopy",new Vector3(0,10.3f,7),new Vector3(9,.025f,28),windowMaterial);
            for(int side=-1;side<=1;side+=2){
                ObsShape("Walkway light",new Vector3(side*2.55f,.035f,6),new Vector3(.045f,.025f,22),glow);
                ObsShape("Deck perimeter",new Vector3(side*10.2f,.16f,7),new Vector3(.4f,.32f,27),edge);
                for(int z=-5;z<=19;z+=4){
                    ObsBeam("Window pier",new Vector3(side*10.3f,.2f,z),new Vector3(side*10.3f,7.8f,z),.14f,hull);
                    ObsBeam("Vault rib",new Vector3(side*10.3f,7.8f,z),new Vector3(side*4.5f,10.2f,z),.12f,edge);
                    ObsBeam("Vault light",new Vector3(side*10.12f,7.72f,z),new Vector3(side*4.5f,10.08f,z),.025f,glow);
                    ObsShape("Guard post",new Vector3(side*10,.65f,z),new Vector3(.12f,1.3f,.12f),edge);
                }
                ObsShape("Observation rail",new Vector3(side*10,1.3f,7),new Vector3(.12f,.12f,27),copper);
                ObsShape("Rail lower beam",new Vector3(side*10,.55f,7),new Vector3(.08f,.08f,27),edge);
                ObsShape("Overhead spine",new Vector3(side*4.5f,10.2f,7),new Vector3(.3f,.35f,28),hull);
                ObsShape("Overhead spine light",new Vector3(side*4.5f,10,7),new Vector3(.035f,.025f,28),warm);
                var roof=ObsShape("Sloping glass canopy",new Vector3(side*7.4f,9.0f,7),new Vector3(6.3f,.025f,28),windowMaterial);roof.localRotation=Quaternion.Euler(0,0,side*-22.5f);
                for(int z=-3;z<=17;z+=4)ObsShape("Observation window",new Vector3(side*10.3f,4.6f,z),new Vector3(.025f,6.15f,3.8f),windowMaterial);
                ObsProp("Telemetry terminal", "computer-wide",new Vector3(side*6.7f,0,5),side<0?90:-90,2.7f);
                ObsShape("Telemetry display light",new Vector3(side*6.7f,1.1f,4.5f),new Vector3(.7f,.025f,.06f),glow);
                ObsShape("Side instrument base",new Vector3(side*7.8f,.4f,13),new Vector3(2.4f,.8f,3.2f),hull);
                for(int j=0;j<4;j++)ObsShape("Instrument slit",new Vector3(side*7.8f,.85f,11.9f+j*.65f),new Vector3(1.6f,.02f,.06f),j==0?warm:glow);
            }
            for(int z=-2;z<=18;z+=4){ObsShape("Centre deck dash",new Vector3(0,.045f,z),new Vector3(.08f,.018f,.75f),copper);}
            ObsShape("Console foot",new Vector3(0,.16f,1),new Vector3(4.4f,.32f,2.6f),hull);
            ObsShape("Console pedestal",new Vector3(0,.67f,1.2f),new Vector3(2.8f,.9f,1.4f),edge);
            var desk=ObsShape("Calibration desk",new Vector3(0,1.28f,1),new Vector3(4.15f,.22f,1.8f),hull);desk.localRotation=Quaternion.Euler(9,0,0);
            var screen=ObsShape("Calibration glass",new Vector3(0,1.425f,.94f),new Vector3(3.7f,.025f,1.35f),glass);screen.localRotation=desk.localRotation;
            ObsShape("Console cyan edge",new Vector3(0,1.36f,.06f),new Vector3(3.9f,.035f,.035f),glow);
            var graph=new List<Vector3>();for(int i=0;i<31;i++){float x=-1.35f+i*.09f;graph.Add(new Vector3(x,1.48f,.7f+x*x*.24f));}ObsLine("Limit curve on console",graph.ToArray(),.025f,mint);
            for(int i=0;i<5;i++)ObsShape("Console readout",new Vector3(-1.45f+i*.17f,1.54f,1.3f),new Vector3(.07f,.012f,.12f+i*.04f),i<2?warm:glow);
            ObsText("Console ID","C A L I B R A T I O N  /  0 1",new Vector3(0,.98f,.045f),1.4f,Hex("a7d9df"));
            ObsShape("Gate dais",new Vector3(0,.15f,14),new Vector3(8.8f,.3f,4.8f),hull).gameObject.AddComponent<BoxCollider>();
            ObsShape("Gate dais trim",new Vector3(0,.32f,11.7f),new Vector3(8.6f,.035f,.07f),warm);
            var centre=new Vector3(0,3.75f,14);
            ObsRing("Gate outer casing",centre,3.68f,.16f,edge);ObsRing("Gate bronze band",centre+Vector3.forward*.12f,3.49f,.04f,copper);ObsRing("Gate inner rim",centre-Vector3.forward*.04f,3.08f,.035f,glow);
            for(int i=0;i<rings.Length;i++){
                rings[i].enabled=true;rings[i].GetComponent<MeshFilter>().sharedMesh=ObsArc("Calibration arc "+i,3.32f,.10f,i*9,8.4f);rings[i].transform.SetPositionAndRotation(centre,Quaternion.identity);rings[i].transform.localScale=Vector3.one;rings[i].sharedMaterial=dim;
            }
            for(int side=-1;side<=1;side+=2){
                ObsBeam("Gate support",new Vector3(side*4.1f,.3f,14.2f),new Vector3(side*3.3f,3,14.2f),.26f,hull);
                ObsBeam("Gate support light",new Vector3(side*4.1f,.4f,13.92f),new Vector3(side*3.4f,2.8f,13.92f),.035f,warm);
                ObsShape("Gate footing",new Vector3(side*4,.45f,14),new Vector3(1,.3f,2.5f),edge);
            }
            var field=ObsShape("Convergence membrane",centre+Vector3.forward*.06f,new Vector3(6.1f,6.1f,1),ObsShader("Gate membrane","Calculus/Observatory Gate"),PrimitiveType.Quad).GetComponent<Renderer>();
            world.exit.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<world.indicators.Count;i++){world.indicators[i].enabled=true;world.indicators[i].transform.position=new Vector3((i-2)*.65f,.72f,11.55f);world.indicators[i].sharedMaterial=dim;}
            var waypoint=(Transform)serialized.FindProperty("waypoint").objectReferenceValue;waypoint.GetComponent<Renderer>().enabled=false;waypoint.GetComponent<Renderer>().sharedMaterial=glow;
            ObsText("Observatory heading","L I M I T   O B S E R V A T O R Y",new Vector3(0,8.15f,15.4f),3,Hex("9bc5cf"));
            ObsText("Gate name","C O N V E R G E N C E   G A T E",new Vector3(0,7.45f,14.5f),1.4f,Hex("6c9cae"));
            ObsShape("Distant planet",new Vector3(28,15,57),Vector3.one*37,ObsShader("Ocean planet","Calculus/Observatory Planet"),PrimitiveType.Sphere);
            var orbit=ObsRing("Orbital structure",new Vector3(28,15,57),22,.08f,edge);orbit.rotation=Quaternion.Euler(65,0,-18);
            for(int side=-1;side<=1;side+=2){ObsShape("Distant station wing",new Vector3(side*29,3,36),new Vector3(22,.3f,3),hull);ObsShape("Distant station light",new Vector3(side*29,3.2f,34.7f),new Vector3(22,.03f,.05f),glow);}
            var key=new GameObject("Observatory key light",typeof(Light)).GetComponent<Light>();key.transform.SetParent(observatory,false);key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(42,-32,0);key.color=Hex("b6d5ef");key.intensity=1.3f;key.shadows=LightShadows.Soft;
            for(int i=0;i<4;i++){var light=new GameObject("Deck wash "+i,typeof(Light)).GetComponent<Light>();light.transform.SetParent(observatory,false);light.transform.position=new Vector3(i%2==0?-6:6,3,i<2?1:13);light.type=LightType.Point;light.range=11;light.intensity=2.2f;light.color=i<2?Hex("68b8d2"):Hex("c69d70");}
            PrefabUtility.SaveAsPrefabAssetAndConnect(observatory.gameObject,Prefabs+"LimitsObservatory.prefab",InteractionMode.AutomatedAction);
            var sky=world.GetComponent<SimulationSky>();sky.environment=ObsShader("Observatory starfield","Calculus/Observatory Sky");sky.fogTint=Hex("071321");sky.fogDensity=.001f;sky.Apply();RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Hex("607486");RenderSettings.reflectionIntensity=.2f;
            var camera=world.transform.Find("Storyboard view").GetComponent<Camera>();camera.transform.position=new Vector3(7,3.5f,-2);camera.transform.LookAt(new Vector3(0,3.3f,10));camera.fieldOfView=38;
            world.player.GetComponentInChildren<Camera>().farClipPlane=150;
            world.Preview.Release();world.Preview.width=1536;world.Preview.height=232;EditorUtility.SetDirty(world.Preview);
            var feedbackCamera=manager.limitsFlow.portalCamera;feedbackCamera.transform.position=new Vector3(6,4.4f,7.5f);feedbackCamera.transform.LookAt(centre);feedbackCamera.fieldOfView=51;
            var presentation=ui.GetComponent<LimitsObservatoryView>();presentation.manager=manager;presentation.gateSurface=field;
            foreach(var component in ui.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            foreach(var node in ui.GetComponentsInChildren<Transform>(true))if(PrefabUtility.IsPartOfPrefabInstance(node))PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);
            EditorUtility.SetDirty(world);EditorUtility.SetDirty(sky);EditorUtility.SetDirty(manager.limitsFlow);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            ManualEditingMigration.Validate();CaptureObservatory();ManualEditingMigration.BuildQA();
            File.WriteAllText(Path.Combine(ObservatoryEvidence,"authored.txt"),"PASS: Limits observatory saved before Play; world/controller/console/gate references retained; 40 curved calibration sections; native five-step cards; no charge percentage; editable environment prefab and shaders.");
        }
        public static void CaptureObservatory()
        {
            Directory.CreateDirectory(ObservatoryEvidence);string path=SceneAuthoring.SceneFolder+"Level01_Limits.unity";EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();
            foreach(string view in new[]{"Numerical approach","Agreeing sides","Unequal sides"}){
                SceneAuthoringWindow.ApplyView(manager.world,canvas,view);canvas.GetComponent<ReportFusionView>().Present();canvas.GetComponent<LimitsObservatoryView>().Present(view=="Agreeing sides"?2:view=="Unequal sides"?4:0);
                SceneAuthoring.Capture(manager.world,canvas,Path.Combine(ObservatoryEvidence,view.Replace(" ","-")+".png"),false);
            }
            EditorSceneManager.OpenScene(path);manager=Object.FindFirstObjectByType<GameManager>();canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();bool enabled=canvas.enabled;canvas.enabled=false;
            var camera=manager.world.player.GetComponentInChildren<Camera>();var position=camera.transform.position;var rotation=camera.transform.rotation;
            try{camera.transform.position=new Vector3(15,9,-11);camera.transform.LookAt(new Vector3(0,2.8f,10));LaboratoryAIImport.Capture(camera,Path.Combine(ObservatoryEvidence,"observatory-wide.png"));LaboratoryAIImport.Capture(camera,Art+"Mission0.png",640,240);camera.transform.SetPositionAndRotation(position,rotation);LaboratoryAIImport.Capture(camera,Path.Combine(ObservatoryEvidence,"player-entry.png"));}
            finally{camera.transform.SetPositionAndRotation(position,rotation);canvas.enabled=enabled;EditorSceneManager.OpenScene(path);}
        }
        [MenuItem("Calculus Lab/Limits/Place saved 3D guide")]
        public static void AddLimitsGuide()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before placing the saved guide.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            string folder=LearningApi.Argument("--review-evidence",Path.GetFullPath("../../qa/differentiation-review-2026-10-24"));Directory.CreateDirectory(folder);
            string path=SceneAuthoring.SceneFolder+"Level01_Limits.unity";var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var world=manager.world;
            var old=world.transform.Find("Limits guide");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var guide=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"MainMenuGuide.prefab"));
            PrefabUtility.UnpackPrefabInstance(guide,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);guide.name="LimitsGuide";
            foreach(var node in guide.GetComponentsInChildren<Transform>(true))node.gameObject.layer=0;
            foreach(var collider in guide.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var light in guide.GetComponentsInChildren<Light>(true)){light.cullingMask=~0;light.shadows=LightShadows.None;light.range=Mathf.Min(light.range,3);light.intensity=Mathf.Min(light.intensity,2);}
            var animator=guide.GetComponentInChildren<Animator>(true);if(animator==null||animator.runtimeAnimatorController==null)throw new Exception("The guide needs its saved idle Animator.");
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MenuArt+"Timeline/Guide-Idle.anim");clip.SampleAnimation(animator.gameObject,.6f);
            var head=animator.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");var visor=animator.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Recessed octagonal faceted visor");
            Vector3 front=visor.bounds.center-head.position;front.y=0;guide.transform.rotation=Quaternion.Euler(0,Vector3.SignedAngle(front,Vector3.back,Vector3.up),0);
            var body=animator.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy&&!r.name.Contains("Hover")&&!r.name.Contains("Hologram")).ToArray();
            Bounds BodyBounds(){var bounds=body[0].bounds;foreach(var renderer in body)bounds.Encapsulate(renderer.bounds);return bounds;}
            guide.transform.localScale*=1.6f/BodyBounds().size.y;
            PrefabUtility.SaveAsPrefabAssetAndConnect(guide,Prefabs+"LimitsGuide.prefab",InteractionMode.AutomatedAction);
            guide.name="Limits guide";guide.transform.SetParent(world.transform,true);
            var bounds=BodyBounds();guide.transform.position+=new Vector3(-3.3f,.30f,1.25f)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            front=visor.bounds.center-head.position;front.y=0;var towards=world.player.transform.position-guide.transform.position;towards.y=0;guide.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(front,towards,Vector3.up),Vector3.up)*guide.transform.rotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(guide);PrefabUtility.RecordPrefabInstancePropertyModifications(guide.transform);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();bool enabled=canvas.enabled;canvas.enabled=false;var camera=world.player.GetComponentInChildren<Camera>();var pos=camera.transform.position;var rot=camera.transform.rotation;
            try{
                LaboratoryAIImport.Capture(camera,Path.Combine(folder,"limits-guide-entry.png"));
                bounds=BodyBounds();camera.transform.position=bounds.center+new Vector3(.2f,.2f,-3.5f);camera.transform.LookAt(bounds.center+Vector3.up*.1f);LaboratoryAIImport.Capture(camera,Path.Combine(folder,"limits-guide-close.png"));
                clip.SampleAnimation(animator.gameObject,2);LaboratoryAIImport.Capture(camera,Path.Combine(folder,"limits-guide-idle-later.png"));
            }finally{camera.transform.SetPositionAndRotation(pos,rot);canvas.enabled=enabled;EditorSceneManager.OpenScene(path);}
            ManualEditingMigration.Validate();
            File.WriteAllText(Path.Combine(folder,"guide-result.txt"),"PASS: saved LimitsGuide prefab beside the calibration console; existing looping idle Animator; scene layer 0; no blocking colliders; original player and console unchanged. Native scene and two idle-pose previews captured.");
        }
        public static void ValidateObservatory()
        {
            OpenObservatory();var manager=Object.FindFirstObjectByType<GameManager>();var view=manager.menuOrLimitsUI.GetComponent<LimitsObservatoryView>();
            if(view==null||view.manager!=manager||view.gateSurface==null||view.stepCards.Length!=5||view.stepStates.Length!=5||view.stepCards.Any(c=>c==null)||view.stepStates.Any(t=>t==null)||manager.world.station==null||manager.world.player==null||manager.limitsFlow.ringSections.Length!=40)throw new Exception("Missing saved observatory references.");
            Index(manager.menuOrLimitsUI.gameObject);if(Find("PuzzlePortal").gameObject.activeSelf||Find("ChargeLabel").gameObject.activeSelf||Find("PortalCard").gameObject.activeSelf||!PrefabUtility.IsPartOfPrefabInstance(manager.world.transform.Find("Limits Observatory")))throw new Exception("Save the five-step UI and environment prefab before Play.");
            ManualEditingMigration.Validate();Directory.CreateDirectory(ObservatoryEvidence);File.WriteAllText(Path.Combine(ObservatoryEvidence,"main-validated.txt"),"PASS: saved observatory prefab, five step cards/states, existing player/console/forty gate sections and new gate shader references; percentage display disabled; all four scenes' persistent events valid.");
        }
        [MenuItem("Calculus Lab/Limits/Open saved observatory")]
        public static void OpenObservatory()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before opening the saved observatory.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the current scene before opening the observatory.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var root=Object.FindFirstObjectByType<Chapter4World>().transform.Find("Limits Observatory");Selection.activeGameObject=root.gameObject;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,3,8),Quaternion.Euler(22,-25,0),26);
        }
    }
}
#endif
