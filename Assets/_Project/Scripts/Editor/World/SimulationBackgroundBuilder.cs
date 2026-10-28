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
    // These are ordinary saved scene objects. No scenery is spawned by Play.
    public static class SimulationBackgroundBuilder
    {
        const string GroupName="Simulation background";
        const string Art="Assets/_Project/Art/SimulationBackground";
        static string Evidence=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../qa/simulation-background"));
        static Material metal,panel,cyan,dim,amber;
        public static void ApplyAndBuild(){ApplyAll();ApplyAll();ValidateSaved();ProductionUIBuilder.BuildDelivery();Debug.Log("SIMULATION_BACKGROUND_DELIVERY_PASS");}
        [MenuItem("Calculus Lab/Background/Add saved simulation backgrounds")]
        public static void ApplyAll()
        {
            Directory.CreateDirectory(Evidence);
            if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","SimulationBackground");
            metal=Material("Structure",new Color(.045f,.075f,.11f),false);
            panel=Material("Display backing",new Color(.012f,.026f,.045f),false);
            cyan=Material("Cyan light",new Color(.045f,.48f,.56f),true);
            dim=Material("Distant grid",new Color(.026f,.17f,.22f),true);
            amber=Material("Amber detail",new Color(.72f,.38f,.12f),true);
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Art}))
            {
                var ring=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                if(ring!=null&&ring.vertexCount==194)MakeDoubleSided(ring,ring.vertices,ring.triangles.Take(ring.triangles.Length/2).ToArray());
            }
            var reports=new List<string>();
            for(int topic=0;topic<3;topic++)Apply(topic,reports);
            File.WriteAllLines(Path.Combine(Evidence,"result.txt"),new[]{"PASS: three saved backgrounds; persistent materials/meshes; no added colliders/lights/gameplay components; existing transforms, renderers and gameplay references unchanged; nine Edit Mode views; reapplication preserves authored decoration."}.Concat(reports));
            Debug.Log("SIMULATION_BACKGROUND_EDITOR_PASS");
        }
        static Material Material(string name,Color color,bool unlit)
        {
            string path=Art+"/"+name.Replace(" ","_")+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
            var shader=Shader.Find(unlit?"Unlit/Color":"Standard");if(shader==null||!shader.isSupported)throw new Exception("Missing background shader");
            var result=new Material(shader){name=name,color=color};if(!unlit){result.SetFloat("_Glossiness",.2f);result.SetFloat("_Metallic",.25f);}AssetDatabase.CreateAsset(result,path);return result;
        }
        static Transform Group(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
        }
        static void Line(Transform parent,string name,IEnumerable<Vector3> points,Material material,float width=.045f,bool loop=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var renderer=go.AddComponent<LineRenderer>();var vertices=points.ToArray();renderer.useWorldSpace=false;renderer.positionCount=vertices.Length;renderer.SetPositions(vertices);renderer.loop=loop;renderer.widthMultiplier=width;renderer.sharedMaterial=material;renderer.numCornerVertices=2;renderer.numCapVertices=2;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        static void Ring(Transform parent,string name,Vector3 center,float radius,float width,Material material,bool horizontal=false)
        {
            string path=Art+"/Ring_"+radius.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"_"+width.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                const int segments=96;var vertices=new Vector3[(segments+1)*2];var triangles=new int[segments*6];
                for(int i=0;i<=segments;i++){float angle=i*Mathf.PI*2/segments;var v=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);vertices[i*2]=v*(radius-width/2);vertices[i*2+1]=v*(radius+width/2);if(i<segments){int at=i*6,n=i*2;triangles[at]=n;triangles[at+1]=n+2;triangles[at+2]=n+1;triangles[at+3]=n+1;triangles[at+4]=n+2;triangles[at+5]=n+3;}}
                mesh=new Mesh{name=name};MakeDoubleSided(mesh,vertices,triangles);AssetDatabase.CreateAsset(mesh,path);
            }
            // Reverse faces let the saved ring render from both sides, including beneath the canopy.
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=center;if(horizontal)go.transform.localRotation=Quaternion.Euler(90,0,0);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        static void MakeDoubleSided(Mesh mesh,Vector3[] vertices,int[] front)
        {
            // Separate vertices keep the opposite faces from cancelling their lighting normals.
            var triangles=new int[front.Length*2];Array.Copy(front,triangles,front.Length);
            for(int i=0;i<front.Length;i+=3){int at=front.Length+i;triangles[at]=front[i]+vertices.Length;triangles[at+1]=front[i+2]+vertices.Length;triangles[at+2]=front[i+1]+vertices.Length;}
            mesh.Clear();mesh.vertices=vertices.Concat(vertices).ToArray();mesh.triangles=triangles;
            mesh.normals=Enumerable.Repeat(Vector3.back,vertices.Length).Concat(Enumerable.Repeat(Vector3.forward,vertices.Length)).ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        }
        static void Arc(Transform parent,string name,Vector3 center,float radius,float from,float to,Material material,float width=.06f)
        {Line(parent,name,Enumerable.Range(0,49).Select(i=>{float a=Mathf.Lerp(from,to,i/48f)*Mathf.Deg2Rad;return center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius;}),material,width);}
        static void Graph(Transform parent,string name,Vector3 center,bool area)
        {
            var group=Group(parent,name);group.localPosition=center;
            Box(group,"Recessed display",Vector3.zero,new Vector3(3.6f,1.8f,.08f),panel);
            Line(group,"Display rim",new[]{new Vector3(-1.82f,-.92f,-.07f),new Vector3(1.82f,-.92f,-.07f),new Vector3(1.82f,.92f,-.07f),new Vector3(-1.82f,.92f,-.07f)},dim,.035f,true);
            for(int i=0;i<7;i++)Line(group,"Graph grid vertical "+i,new[]{new Vector3(-1.5f+i*.5f,-.7f,-.06f),new Vector3(-1.5f+i*.5f,.7f,-.06f)},dim,.008f);
            for(int i=0;i<4;i++)Line(group,"Graph grid horizontal "+i,new[]{new Vector3(-1.5f,-.6f+i*.4f,-.06f),new Vector3(1.5f,-.6f+i*.4f,-.06f)},dim,.008f);
            Line(group,"Curve trace",Enumerable.Range(0,40).Select(i=>{float x=-1.5f+i*3f/39;return new Vector3(x,area?.48f*Mathf.Sin(x*1.7f):.48f*(x*x-.9f),-.09f);}),cyan,.028f);
            if(area)for(int i=0;i<16;i++){float x=-1.4f+i*.18f;Line(group,"Accumulation bar "+i,new[]{new Vector3(x,-.6f,-.085f),new Vector3(x,.48f*Mathf.Sin(x*1.7f),-.085f)},dim,.025f);}
            else {Line(group,"Approach marker",new[]{new Vector3(.5f,-.7f,-.1f),new Vector3(.5f,.7f,-.1f)},amber,.018f);Ring(group,"Sample marker",new Vector3(.5f,.48f*(.25f-.9f),-.11f),.075f,.018f,amber);}
        }
        static void Room(Transform root,int topic)
        {
            var walls=Group(root,"Wall displays and light rails");
            for(int side=-1;side<=1;side+=2)
            {
                Box(walls,"Upper wall fascia "+side,new Vector3(side*10.57f,6.8f,7),new Vector3(.12f,1.05f,27),metal);
                Line(walls,"Wall horizon light "+side,new[]{new Vector3(side*10.47f,6.45f,-6.4f),new Vector3(side*10.47f,6.45f,20.3f)},cyan,.05f);
                for(int i=0;i<3;i++)
                {
                    var graph=Group(walls,"Side monitoring display "+side+"-"+i);graph.localPosition=new Vector3(side*10.43f,5.4f,2+i*6);graph.localRotation=Quaternion.Euler(0,side*90,0);Graph(graph,"Simulation curve",Vector3.zero,topic==2);
                    Box(walls,"Status rail "+side+"-"+i,new Vector3(side*10.36f,4.28f,2+i*6),new Vector3(.04f,.06f,2.3f),i==1?amber:cyan);
                }
            }
            var roof=Group(root,"Open canopy framework");
            foreach(float z in new[]{-3f,15.5f})
            {
                var points=new[]{new Vector3(-10.4f,7.25f,z),new Vector3(-7.2f,9.65f,z),new Vector3(7.2f,9.65f,z),new Vector3(10.4f,7.25f,z)};
                Line(roof,"Canopy structural arch "+z,points,metal,.28f);Line(roof,"Canopy cyan seam "+z,points.Select(p=>p+Vector3.down*.16f),dim,.035f);
            }
            Ring(roof,"Suspended simulation halo",new Vector3(0,8.55f,6.5f),7.2f,.12f,dim,true);
            Ring(roof,"Halo structural edge",new Vector3(0,8.61f,6.5f),7.42f,.2f,metal,true);
            for(int side=-1;side<=1;side+=2)Line(roof,"Canopy suspension "+side,new[]{new Vector3(side*7.2f,8.55f,6.5f),new Vector3(side*7.2f,9.65f,-3),new Vector3(side*7.2f,9.65f,15.5f)},metal,.07f);
            var rear=Group(root,"Rear backdrop");
            if(topic==0)
            {
                Box(rear,"Portal alcove backing",new Vector3(0,3.4f,20.6f),new Vector3(10.4f,5.2f,.09f),panel);
                Ring(rear,"Portal outer frame",new Vector3(0,3.3f,16.2f),3.95f,.20f,metal);
                Arc(rear,"Portal left arc",new Vector3(0,3.3f,16.12f),4.12f,18,162,cyan);
                Arc(rear,"Portal right arc",new Vector3(0,3.3f,16.12f),4.12f,198,342,cyan);
                for(int side=-1;side<=1;side+=2){Graph(rear,"Rear approach display "+side,new Vector3(side*7.4f,4.6f,20.5f),false);Box(rear,"Portal side pedestal "+side,new Vector3(side*4.7f,1.1f,16.3f),new Vector3(.7f,2.2f,.7f),metal);Box(rear,"Pedestal light "+side,new Vector3(side*4.7f,1.1f,15.93f),new Vector3(.18f,1.6f,.025f),cyan);}
            }
            else
            {
                for(int side=-1;side<=1;side+=2)Graph(rear,"Rear accumulation display "+side,new Vector3(side*6.3f,5.5f,20.35f),true);
                Line(rear,"Rear laboratory frame",new[]{new Vector3(-10.4f,.3f,20.3f),new Vector3(-10.4f,7.4f,20.3f),new Vector3(10.4f,7.4f,20.3f),new Vector3(10.4f,.3f,20.3f)},dim,.055f);
            }
        }
        static void Bridge(Transform root)
        {
            var perimeter=Group(root,"Simulation perimeter");
            Line(perimeter,"Training boundary glow",new[]{new Vector3(-15.2f,.08f,-10),new Vector3(15.2f,.08f,-10),new Vector3(15.2f,.08f,25.5f),new Vector3(-15.2f,.08f,25.5f)},cyan,.07f,true);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
            {
                var p=new Vector3(side*16.2f,0,-5+i*9);Box(perimeter,"Boundary pedestal "+side+"-"+i,p+Vector3.up*1.4f,new Vector3(.65f,2.8f,.65f),metal);Box(perimeter,"Boundary light "+side+"-"+i,p+new Vector3(-side*.34f,1.65f,0),new Vector3(.035f,1.8f,.22f),cyan);
            }
            var islands=Group(root,"Distant floating terrain");
            for(int i=0;i<7;i++)
            {
                float x=(i-3)*8.7f,z=31+(i%2)*5;var island=Group(islands,"Floating island "+i);island.localPosition=new Vector3(x,-.4f,z);
                Box(island,"Island deck",Vector3.zero,new Vector3(6.5f,.8f,5.5f),metal);Box(island,"Island lower tier",new Vector3(0,-.8f,0),new Vector3(4.4f,.8f,3.4f),panel);
                Line(island,"Island edge",new[]{new Vector3(-3.3f,.42f,-2.8f),new Vector3(3.3f,.42f,-2.8f),new Vector3(3.3f,.42f,2.8f),new Vector3(-3.3f,.42f,2.8f)},dim,.055f,true);
                string treePath="Assets/_Project/Prefabs/ThirdParty/Kenney/NatureKit/"+(i%2==0?"tree_pineRoundA":"tree_pineDefaultA")+".prefab";var tree=AssetDatabase.LoadAssetAtPath<GameObject>(treePath);if(tree==null)throw new Exception("Missing background tree");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(tree);instance.name="Distant pine";instance.transform.SetParent(island,false);var bounds=instance.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();var size=bounds[0];foreach(var b in bounds.Skip(1))size.Encapsulate(b);instance.transform.localScale*=5/size.size.y;var scaled=instance.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();var bottom=scaled.Min(b=>b.min.y);instance.transform.position+=Vector3.up*(island.position.y+.4f-bottom);PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
            var arch=Group(root,"Distant simulation gate");Ring(arch,"Gate frame",new Vector3(0,9,37),11,.35f,metal);Arc(arch,"Gate upper light",new Vector3(0,9,36.8f),11.3f,12,168,dim,.09f);
            for(int side=-1;side<=1;side+=2)Box(arch,"Gate support "+side,new Vector3(side*10.5f,3.3f,37),new Vector3(.7f,6.6f,.7f),metal);
        }
        static void Horizon(Transform root)
        {
            var grid=Group(root,"Lower data field");Box(grid,"Data field base",new Vector3(0,-2.8f,8),new Vector3(72,.08f,72),panel);
            for(int i=-8;i<=8;i++){Line(grid,"Lower grid X "+i,new[]{new Vector3(-36,-2.74f,8+i*4),new Vector3(36,-2.74f,8+i*4)},dim,.025f);Line(grid,"Lower grid Z "+i,new[]{new Vector3(i*4,-2.74f,-28),new Vector3(i*4,-2.74f,44)},dim,.025f);}
            var towers=Group(root,"Horizon data pylons");
            for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
            {
                float height=7+i%3*2.5f;var p=new Vector3(side*(24+i%2*5),height/2-2,1+i*11);
                Box(towers,"Data pylon "+side+"-"+i,p,new Vector3(1.6f,height,1.6f),metal);
                Line(towers,"Pylon outline "+side+"-"+i,new[]{p+new Vector3(-.83f,-height/2,-.83f),p+new Vector3(-.83f,height/2,-.83f),p+new Vector3(.83f,height/2,-.83f),p+new Vector3(.83f,-height/2,-.83f)},dim,.07f);
                for(int k=0;k<3;k++)Box(towers,"Data light "+side+"-"+i+"-"+k,p+new Vector3(0,height*.15f-k*.65f,-.83f),new Vector3(.9f,.12f,.025f),k==0?cyan:dim);
            }
        }
        static void Apply(int topic,List<string> reports)
        {
            string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity";var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Chapter4World>(true)).Single();if(!world.IsAuthored)throw new Exception("Expected saved authored world");
                var transforms=world.GetComponentsInChildren<Transform>(true);var matrices=transforms.Select(t=>t.localToWorldMatrix).ToArray();var collision=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();var renderers=world.GetComponentsInChildren<Renderer>(true);var renderState=renderers.Select(r=>r.enabled+":"+string.Join(",",r.sharedMaterials.Select(m=>m.GetInstanceID()))).ToArray();string gameplay=JsonUtility.ToJson(world);
                var root=world.transform.Find(GroupName);bool added=root==null;if(added){root=Group(world.transform,GroupName);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add simulation background");Horizon(root);if(topic==1)Bridge(root);else Room(root,topic);}
                for(int i=0;i<transforms.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(matrices[i][j]-transforms[i].localToWorldMatrix[j])>.0001f)throw new Exception("Background moved existing object "+transforms[i].name);
                for(int i=0;i<renderers.Length;i++)if(renderState[i]!=renderers[i].enabled+":"+string.Join(",",renderers[i].sharedMaterials.Select(m=>m.GetInstanceID())))throw new Exception("Background altered existing renderer");
                if(gameplay!=JsonUtility.ToJson(world)||!collision.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i)))throw new Exception("Background changed gameplay or collision");
                if(root.GetComponentsInChildren<Collider>(true).Length!=0||root.GetComponentsInChildren<Light>(true).Length!=0)throw new Exception("Background must be visual only");
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material==null||!AssetDatabase.Contains(material)||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Unsaved or invalid background material");
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh==null||!EditorUtility.IsPersistent(mesh.sharedMesh))throw new Exception("Unsaved background mesh");
                world.GetComponent<SimulationSky>()?.Apply();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Capture(world,topic+"-background-world.png",topic==1?new Vector3(15,11,-6):new Vector3(8,6,-5),topic==1?new Vector3(0,3,19):new Vector3(0,4,10));
                Capture(world,topic+"-background-player.png",world.player.transform.position+Vector3.up*1.6f,world.player.transform.position+new Vector3(0,4.8f,17));
                Capture(world,topic+"-background-canopy.png",new Vector3(0,2,-3),new Vector3(0,9,16));
                reports.Add(Chapter4Game.Scenes[topic]+": "+(added?"added":"retained")+" "+root.GetComponentsInChildren<Renderer>(true).Length+" decorative renderers; original transforms/renderers/colliders/references unchanged.");Debug.Log("SIMULATION_BACKGROUND_SAVED / "+Chapter4Game.Scenes[topic]);
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 targetPoint)
        {
            var go=new GameObject("Background evidence camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=position;camera.transform.LookAt(targetPoint);
            var target=RenderTexture.GetTemporary(1280,720,24);var prior=RenderTexture.active;var canvas=world.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();bool canvasEnabled=canvas!=null&&canvas.enabled;Texture2D pixels=null;
            try{if(canvas!=null)canvas.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Evidence,name),pixels.EncodeToPNG());}
            finally{if(canvas!=null)canvas.enabled=canvasEnabled;camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);}
        }
        static void ValidateSaved()
        {
            for(int topic=0;topic<3;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Chapter4World>(true)).Single();
                var groups=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name==GroupName).ToArray();if(groups.Length!=1||groups[0].GetComponentsInChildren<Renderer>(true).Length<60)throw new Exception("Background missing or duplicated after reload");
                if(groups[0].GetComponentsInChildren<Collider>(true).Length!=0||Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Decoration must not create collision or sessions");
                world.GetComponent<SimulationSky>().Apply();
            }
            Debug.Log("SIMULATION_BACKGROUND_RELOAD_PASS");
        }
    }
}
#endif
