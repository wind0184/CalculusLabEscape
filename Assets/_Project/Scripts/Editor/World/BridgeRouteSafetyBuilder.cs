#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class BridgeRouteSafetyBuilder
    {
        const string GroupName="Bridge route safety";
        static string Evidence=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../qa/bridge-route-safety"));
        static Material wood,metal,trim;
        public static void ApplyAndBuild(){Apply();Apply();ReloadCheck();ProductionUIBuilder.BuildDelivery();Debug.Log("BRIDGE_ROUTE_SAFETY_DELIVERY_PASS");}
        [MenuItem("Calculus Lab/World/Add Bridge route safety")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();
            wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/RepairStations/Worktable_timber.mat");metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/RepairStations/Field_steel.mat");trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/RepairStations/Safety_trim.mat");if(wood==null||metal==null||trim==null)throw new Exception("Repair station materials missing");
            // Upgrade only the first generated sign position; leave later manual edits intact.
            var previousSign=world.transform.Find(GroupName+"/RESCUE CROSSING");
            if(previousSign!=null&&Vector3.Distance(previousSign.localPosition,new Vector3(0,1.8f,6.75f))<.01f){Undo.RegisterFullObjectHierarchyUndo(previousSign.gameObject,"Clear the bridge sightline");previousSign.localPosition=new Vector3(-4.2f,1.4f,6.6f);previousSign.Find("Boundary sign backing").localScale=new Vector3(2.4f,.53f,.06f);EditorUtility.SetDirty(previousSign);}
            var previousSafety=world.transform.Find(GroupName);
            if(previousSafety!=null)foreach(var part in previousSafety.GetComponentsInChildren<Transform>(true))
            {
                var p=part.localPosition;var size=part.localScale;
                if((part.name=="Timber safety rail"||part.name=="Rescue gate rail")&&Mathf.Abs(p.y-1.05f)<.01f)p.y=1.4f;
                if(part.name=="Safety post"&&Mathf.Abs(size.y-1.3f)<.01f){p.y=.85f;size.y=1.7f;}
                if(part.name=="Post cap"&&Mathf.Abs(p.y-1.31f)<.01f)p.y=1.71f;
                if(part.name=="Rescue gate end"&&Mathf.Abs(size.y-.78f)<.01f){p.y=.95f;size.y=1.2f;}
                if(part.name=="Gate safety stripe"&&Mathf.Abs(p.y-1.06f)<.01f)p.y=1.41f;
                if(part.localPosition!=p||part.localScale!=size){Undo.RecordObject(part,"Keep boundary visible at eye height");part.localPosition=p;part.localScale=size;EditorUtility.SetDirty(part);}
            }
            var playerCamera=world.player.GetComponentInChildren<Camera>();Undo.RecordObject(playerCamera,"Avoid close boundary clipping");playerCamera.nearClipPlane=.06f;EditorUtility.SetDirty(playerCamera);
            var original=world.GetComponentsInChildren<Transform>(true);var matrices=original.Select(t=>t.localToWorldMatrix).ToArray();string references=JsonUtility.ToJson(world);int oldCount=world.GetComponentsInChildren<Collider>(true).Length;
            var report=new List<string>();var root=world.transform.Find(GroupName);bool added=root==null;
            if(added)
            {
                var oldTrunks=world.GetComponentsInChildren<Collider>(true).Where(c=>c.name=="Pine trunk"&&c.GetComponent<Renderer>()!=null&&!c.GetComponent<Renderer>().enabled).ToArray();
                report.Add("Found "+oldTrunks.Length+" colliders on hidden prototype pine trunks; imported art has shifted some visible trees around the river.");
                root=Group(world.transform,GroupName);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add visible Bridge boundaries");
                Rail(root,"West training boundary",new Vector3(-14,0,-9),new Vector3(-14,0,6.7f));Rail(root,"East training boundary",new Vector3(14,0,-9),new Vector3(14,0,6.7f));Rail(root,"Rear training boundary",new Vector3(-14,0,-9),new Vector3(14,0,-9));
                Rail(root,"Left river safety rail",new Vector3(-14,0,6.7f),new Vector3(-1.7f,0,6.7f));Rail(root,"Right river safety rail",new Vector3(1.7f,0,6.7f),new Vector3(14,0,6.7f));
                var left=Group(root,"Rescue gate left hinge");left.localPosition=new Vector3(-1.7f,0,6.7f);var right=Group(root,"Rescue gate right hinge");right.localPosition=new Vector3(1.7f,0,6.7f);
                GateLeaf(left,1);GateLeaf(right,-1);var gate=root.gameObject.AddComponent<BridgeBoundaryGate>();gate.rescue=world.GetComponent<BridgeRescueSequence>();gate.left=left;gate.right=right;gate.Apply();
                Label(root,world,"RESCUE CROSSING",new Vector3(-4.2f,1.4f,6.6f),0,2.4f,.03f);
                Label(root,world,"TRAINING AREA\nFOLLOW THE STONE PATH",new Vector3(-13.88f,1.8f,-3),-90,3.6f,.03f);Label(root,world,"TRAINING AREA\nFOLLOW THE STONE PATH",new Vector3(13.88f,1.8f,-3),90,3.6f,.03f);
                Label(root,world,"TRAINING AREA",new Vector3(0,1.7f,-8.9f),180,2.8f,.03f);
                var nature=world.transform.Find("Imported environment art");if(nature==null)throw new Exception("Imported Bridge art missing");
                foreach(var old in oldTrunks){Undo.RecordObject(old,"Disable obsolete hidden tree collision");old.enabled=false;EditorUtility.SetDirty(old);}
                int trees=0,rocks=0;
                foreach(Transform tree in nature.Find("Forest trees"))
                {
                    var bounds=Bounds(tree);float height=Mathf.Min(2.2f,bounds.size.y*.5f),radius=Mathf.Clamp(bounds.size.y*.045f,.17f,.28f);
                    var go=new GameObject("Visible trunk collision");go.transform.SetParent(tree,true);go.transform.position=new Vector3(bounds.center.x,bounds.min.y+height/2,bounds.center.z);var collider=go.AddComponent<CapsuleCollider>();collider.height=height;collider.radius=radius;Undo.RegisterCreatedObjectUndo(go,"Match imported tree collision");trees++;
                }
                foreach(Transform rock in nature.Find("Riverbank rocks and grass"))if(rock.name.StartsWith("Riverbank rock",StringComparison.Ordinal))
                {
                    foreach(var filter in rock.GetComponentsInChildren<MeshFilter>()){var collider=Undo.AddComponent<MeshCollider>(filter.gameObject);collider.sharedMesh=filter.sharedMesh;EditorUtility.SetDirty(collider);rocks++;}
                }
                report.Add("Added visible rails aligned with existing side/rear/river safety colliders; gate opens only with existing calf rescue progress.");report.Add("Matched "+trees+" visible pine trunks and "+rocks+" rock meshes; no collision added to scenery hills or grass.");
            }
            for(int i=0;i<original.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(matrices[i][j]-original[i].localToWorldMatrix[j])>.0001f)throw new Exception("Route fix moved existing object "+original[i].name);
            if(references!=JsonUtility.ToJson(world))throw new Exception("Route fix changed gameplay references");
            Audit(world,report);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            world.GetComponent<SimulationSky>().Apply();Capture(world,"route-overview.png",new Vector3(16,15,-10),new Vector3(0,1,1));Capture(world,"river-gate.png",new Vector3(0,2,3.6f),new Vector3(0,1,8));Capture(world,"west-boundary.png",new Vector3(-10.5f,2,-5),new Vector3(-14,1,-2));
            var sequence=root.GetComponent<BridgeBoundaryGate>();sequence.Preview(.3f);Capture(world,"rescue-gate-open.png",new Vector3(0,2,3.6f),new Vector3(0,1,8));sequence.Apply();
            report.Add("Reapply: "+(added?"added":"retained")+". Collider count before="+oldCount+", after="+world.GetComponentsInChildren<Collider>(true).Length+". Existing world transforms and gameplay references preserved.");
            File.WriteAllLines(Path.Combine(Evidence,added?"changes.txt":"reapply.txt"),report);Debug.Log("BRIDGE_ROUTE_SAFETY_EDITOR_PASS");
        }
        static Bounds Bounds(Transform root){var renderers=root.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;}
        static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;}
        static void Rail(Transform parent,string name,Vector3 from,Vector3 to)
        {
            var root=Group(parent,name);int count=Mathf.CeilToInt(Vector3.Distance(from,to)/2.5f);for(int i=0;i<=count;i++){var p=Vector3.Lerp(from,to,i/(float)count);Box(root,"Safety post",p+Vector3.up*.85f,new Vector3(.15f,1.7f,.15f),metal);Box(root,"Post cap",p+Vector3.up*1.71f,new Vector3(.19f,.06f,.19f),trim);}
            foreach(float y in new[]{.5f,1.4f}){var rail=Box(root,"Timber safety rail",(from+to)/2+Vector3.up*y,new Vector3(.1f,.13f,Vector3.Distance(from,to)),wood);rail.transform.localRotation=Quaternion.LookRotation(to-from);}
        }
        static void GateLeaf(Transform parent,int side)
        {foreach(float y in new[]{.5f,1.4f})Box(parent,"Rescue gate rail",new Vector3(side*.85f,y,0),new Vector3(1.7f,.14f,.12f),wood);Box(parent,"Rescue gate end",new Vector3(side*1.58f,.95f,0),new Vector3(.12f,1.2f,.12f),metal);Box(parent,"Gate safety stripe",new Vector3(side*.85f,1.41f,-.071f),new Vector3(.38f,.10f,.02f),trim);}
        static void Label(Transform parent,Chapter4World world,string text,Vector3 p,float yaw,float width,float size)
        {
            var root=Group(parent,text.Replace("\n"," / "));root.localPosition=p;root.localRotation=Quaternion.Euler(0,yaw,0);Box(root,"Boundary sign backing",new Vector3(0,0,.04f),new Vector3(width,.53f,.06f),metal);
            var source=world.GetComponentsInChildren<TextMesh>(true).First();var label=Group(root,"Boundary label").gameObject.AddComponent<TextMesh>();label.font=source.font;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.83f,.94f,.93f);label.text=text;label.GetComponent<Renderer>().sharedMaterial=source.GetComponent<Renderer>().sharedMaterial;
        }
        static bool Clear(Chapter4World world,Vector3 point)
        {return Physics.OverlapCapsule(point+Vector3.up*.35f,point+Vector3.up*1.55f,.29f,~0,QueryTriggerInteraction.Ignore).All(c=>c==world.player.GetComponent<CharacterController>());}
        static void Audit(Chapter4World world,List<string> report)
        {
            Physics.SyncTransforms();const float step=.5f;const int nx=55,nz=29;var nodes=new bool[nx,nz];int open=0;
            Vector3 Point(int x,int z)=>new Vector3(-13.5f+x*step,0,-8.5f+z*step);
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++){nodes[x,z]=Clear(world,Point(x,z));if(nodes[x,z])open++;}
            var start=new Vector2Int(27,7);var visited=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();queue.Enqueue(start);visited.Add(start);
            while(queue.Count>0){var p=queue.Dequeue();foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down}){var n=p+d;if(n.x<0||n.x>=nx||n.y<0||n.y>=nz||!nodes[n.x,n.y]||visited.Contains(n))continue;var a=Point(p.x,p.y);var b=Point(n.x,n.y);if(Physics.CapsuleCastAll(a+Vector3.up*.35f,a+Vector3.up*1.55f,.29f,b-a,step,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider!=world.player.GetComponent<CharacterController>()))continue;visited.Add(n);queue.Enqueue(n);}}
            foreach(var device in world.devices){bool reachable=visited.Any(n=>Vector3.Distance(Point(n.x,n.y),new Vector3(device.position.x,0,device.position.z))<2.6f);if(!reachable)throw new Exception("No clear walking approach to "+device.name);}
            foreach(string name in new[]{"Left boundary","Right boundary","Back boundary","River safety boundary"})if(!world.GetComponentsInChildren<Collider>(true).Any(c=>c.name==name&&c.enabled))throw new Exception("Original safety boundary missing "+name);
            if(world.GetComponentsInChildren<Collider>(true).Any(c=>c.name=="Pine trunk"&&c.enabled))throw new Exception("Obsolete invisible pine collision still active");
            report.Add("Capsule grid audit: "+open+" clear cells; "+visited.Count+" reachable from spawn; all three device approaches reachable; original four safety bounds active; obsolete prototype trunk colliders disabled.");
        }
        static void ReloadCheck()
        {EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();if(world.GetComponentsInChildren<BridgeBoundaryGate>(true).Length!=1)throw new Exception("Gate missing/duplicated");var report=new List<string>();Audit(world,report);if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Audit started a session");File.WriteAllLines(Path.Combine(Evidence,"saved-audit.txt"),report);Debug.Log("BRIDGE_ROUTE_SAFETY_RELOAD_PASS");}
        static void Capture(Chapter4World world,string name,Vector3 position,Vector3 look)
        {
            var go=new GameObject("Route audit camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(world.player.GetComponentInChildren<Camera>());camera.enabled=false;camera.transform.position=position;camera.transform.LookAt(look);camera.clearFlags=CameraClearFlags.Skybox;
            var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;var canvas=Object.FindFirstObjectByType<Canvas>();bool prior=canvas.enabled;Texture2D pixels=null;
            try{canvas.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Evidence,name),pixels.EncodeToPNG());}
            finally{canvas.enabled=prior;camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);}
        }
    }
}
#endif
