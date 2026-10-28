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
    public static class MissionAssetDressing
    {
        const string Nature="Assets/_Project/Prefabs/ThirdParty/Kenney/NatureKit/";
        const string Station="Assets/ThirdParty/Kenney/SpaceStationKit/Models/";
        const string GroupName="Imported environment art";
        [MenuItem("Calculus Lab/Assets/Open dressed Bridge level")]
        public static void OpenBridge()=>Open(1);
        [MenuItem("Calculus Lab/Assets/Open dressed Integration level")]
        public static void OpenCase()=>Open(2);
        static void Open(int topic)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();var canvas=Object.FindFirstObjectByType<Canvas>();Undo.RegisterFullObjectHierarchyUndo(world.gameObject,"Preview dressed environment");Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Preview dressed environment UI");SceneAuthoringWindow.ApplyView(world,canvas,"World");
            Selection.activeGameObject=world.transform.Find(GroupName).gameObject;
            var view=SceneView.lastActiveSceneView;if(view!=null)view.LookAt(topic==1?new Vector3(0,1,7):new Vector3(0,2,6),Quaternion.Euler(30,-25,0),topic==1?23:16);
            SceneAuthoringWindow.Open();EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();Debug.Log("DRESSED_LEVEL_OPEN / "+Chapter4Game.Scenes[topic]+" / Edit Mode");
        }

        static Transform Group(Transform parent,string name)
        {
            var group=new GameObject(name).transform;group.SetParent(parent,false);return group;
        }
        static Bounds BoundsOf(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new Exception("Asset has no renderers: "+root.name);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
        }
        static GameObject Place(Transform group,string path,string name,Vector3 bottom,float size,float yaw=0)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new Exception("Missing imported asset: "+path);
            var root=new GameObject(name);root.transform.SetParent(group,false);var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.transform.SetParent(root.transform,false);visual.transform.localRotation=Quaternion.Euler(0,yaw,0)*visual.transform.localRotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
            var bounds=BoundsOf(root);root.transform.localScale=Vector3.one*(size/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));bounds=BoundsOf(root);root.transform.position+=bottom-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative asset unexpectedly changes collision: "+name);
            return root;
        }
        static GameObject Fit(Transform group,string model,string name,Vector3 bottom,Vector3 size,float yaw=0)
        {
            var root=Place(group,Station+model+".fbx",name,Vector3.zero,1,yaw);var bounds=BoundsOf(root);
            root.transform.localScale=Vector3.Scale(root.transform.localScale,new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z));bounds=BoundsOf(root);root.transform.position+=bottom-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);return root;
        }
        static void Forest(Chapter4World world,Transform group)
        {
            var trees=Group(group,"Forest trees");var banks=Group(group,"Riverbank rocks and grass");
            string[] variants={"tree_pineDefaultA","tree_pineRoundA","tree_pineSmallA"};
            for(int i=0;i<14;i++)
            {
                float x=(i%2==0?-1:1)*(8+i%3*2),z=-4+i*1.8f;if(z>6.4f&&z<13)z=i%2==0?6.2f:13.3f;
                Place(trees,Nature+variants[i%3]+".prefab","Bank pine "+(i+1),new Vector3(x,0,z),4.4f+(i%3)*.5f,i*37);
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)
            {
                float z=new[]{-5f,-1f,3f,5.6f,14.6f,18f}[i];var p=new Vector3(side*(6.3f+i%2*.6f),0,z);
                Place(banks,Nature+(i%2==0?"rock_largeA":"rock_largeC")+".prefab","Riverbank rock "+side+"-"+i,p,.7f+i%2*.2f,i*41);
                for(int j=0;j<2;j++)Place(banks,Nature+"grass_large.prefab","Riverbank grass "+side+"-"+i+"-"+j,p+new Vector3(side*(.65f+j*.3f),0,.45f+j*.4f),.7f+j*.2f,i*29+j*60);
            }
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
                if(!renderer.transform.IsChildOf(group)&&(renderer.name.StartsWith("Pine",StringComparison.Ordinal)||renderer.name=="Bank rock"||renderer.name=="Grass tuft"))renderer.enabled=false;
        }
        static void Laboratory(Chapter4World world,Transform group)
        {
            var furniture=Group(group,"Workstation furniture");var panels=Group(group,"Wall panels and displays");
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<3;i++)
                {
                    Fit(panels,"wall-detail","Wall panel "+side+"-"+i,new Vector3(side*10.6f,0,3+i*5),new Vector3(.16f,4.3f,3.8f),90);
                    Fit(panels,"display-wall-wide","Monitoring display "+side+"-"+i,new Vector3(side*10.15f,2.2f,3+i*5),new Vector3(.18f,1.3f,2),90);
                }
                for(int i=0;i<2;i++)Place(furniture,Station+(i==0?"computer":"computer-wide")+".fbx","Workbench terminal "+side+"-"+i,new Vector3(side*8,2.01f,4.6f+i*4.8f),1.35f,side*90);
                Place(furniture,Station+"chair-armrest-headrest.fbx","Workstation chair "+side,new Vector3(side*6,0,7),1.4f,-side*90);
                Place(furniture,Station+"container-tall.fbx","Equipment cabinet "+side,new Vector3(side*8.3f,0,16.7f),2.5f,side*90);
                Place(furniture,Station+"container-wide.fbx","Equipment crate "+side,new Vector3(side*6,0,17.5f),1.6f,side*90);
            }
            Fit(panels,"door-double-closed","Rear service door",new Vector3(0,0,20.55f),new Vector3(3.4f,4.5f,.22f));
            for(int i=0;i<4;i++)Fit(panels,"wall-detail","Rear wall panel "+i,new Vector3(new[]{-8f,-4.6f,4.6f,8f}[i],0,20.63f),new Vector3(3.3f,4.4f,.16f));
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
                if(!renderer.transform.IsChildOf(group)&&(renderer.name=="Wall equipment"||renderer.name=="Equipment face"))renderer.enabled=false;
        }

        public static void ApplyAndBuild()
        {
            string evidence=Path.GetFullPath("../../qa/mission-asset-dressing");Directory.CreateDirectory(evidence);var report=new List<string>();
            for(int topic=1;topic<=2;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();if(world==null||!world.IsAuthored)throw new Exception("Authored world missing");
                var existing=world.GetComponentsInChildren<Transform>(true);var matrices=existing.Select(t=>t.localToWorldMatrix).ToArray();var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id).ToArray();string references=JsonUtility.ToJson(world);
                var group=world.transform.Find(GroupName);bool added=group==null;
                if(added){group=Group(world.transform,GroupName);if(topic==1)Forest(world,group);else Laboratory(world,group);}
                for(int i=0;i<existing.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(existing[i].localToWorldMatrix[j]-matrices[i][j])>.0001f)throw new Exception("Dressing moved authored object: "+existing[i].name);
                if(references!=JsonUtility.ToJson(world)||!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id)))throw new Exception("Dressing changed gameplay references or collision");
                int instances=group.childCount;foreach(Transform subgroup in group)instances+=subgroup.childCount;
                report.Add(Chapter4Game.Scenes[topic]+": "+(added?"added":"retained")+" imported art; "+instances+" grouped objects; existing transforms/colliders/gameplay references unchanged.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            File.WriteAllLines(Path.Combine(evidence,"migration.txt"),report);SceneAuthoring.ValidateAndCaptureTo(evidence);EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");Debug.Log("MISSION_ASSET_DRESSING_PASS");ProductionUIBuilder.BuildDelivery();
        }
    }
}
#endif
