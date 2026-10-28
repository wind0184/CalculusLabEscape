#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        static string EntryBayEvidence=>LearningApi.Argument("--entry-bay-evidence",Path.GetFullPath("../../qa/limits-entry-bay-2026-10-24"));
        static Material EntryMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>(ObservatoryArt+name+".mat")??throw new Exception("Missing observatory material: "+name);
        static void EntryLabel(string name,string text,Vector3 position,float size,Color colour)
        {
            ObsText(name,text,position,size,colour);observatory.Find(name).localRotation=Quaternion.Euler(0,180,0);
        }
        static void CaptureEntryBay(string suffix)
        {
            var world=Object.FindFirstObjectByType<Chapter4World>();var camera=world.player.GetComponentInChildren<Camera>();var position=camera.transform.position;var rotation=camera.transform.rotation;
            try{
                camera.transform.rotation=Quaternion.Euler(0,180,0);LaboratoryAIImport.Capture(camera,Path.Combine(EntryBayEvidence,"turn-back-"+suffix+".png"));
                if(suffix=="after"){
                    camera.transform.position=new Vector3(8,5.5f,-3);camera.transform.LookAt(new Vector3(0,2.8f,-14));LaboratoryAIImport.Capture(camera,Path.Combine(EntryBayEvidence,"entry-bay-wide.png"));
                    camera.transform.position=position;camera.transform.rotation=rotation;LaboratoryAIImport.Capture(camera,Path.Combine(EntryBayEvidence,"forward-after.png"));
                }
            }finally{camera.transform.SetPositionAndRotation(position,rotation);}
        }
        [MenuItem("Calculus Lab/Limits/Extend saved entrance bay")]
        public static void AuthorLimitsEntryBay()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before editing the entry bay.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            Directory.CreateDirectory(EntryBayEvidence);string path=SceneAuthoring.SceneFolder+"Level01_Limits.unity";var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var world=manager.world;
            if(!File.Exists(Path.Combine(EntryBayEvidence,"turn-back-before.png")))CaptureEntryBay("before");font=manager.menuOrLimitsUI.GetComponentsInChildren<TMPro.TMP_Text>(true).First().font;
            var old=world.transform.Find("Limits Entry Bay");if(old!=null)Object.DestroyImmediate(old.gameObject);
            observatory=new GameObject("Limits Entry Bay").transform;observatory.SetParent(world.transform,false);
            var hull=EntryMaterial("Midnight hull");var deck=EntryMaterial("Slate deck");var edge=EntryMaterial("Titanium edges");var bronze=EntryMaterial("Warm bronze");var cyan=EntryMaterial("Cyan navigation");var amber=EntryMaterial("Amber lighting");var glass=EntryMaterial("Observation glass");
            // Extend only the entrance side. The console, guide, gate and spawn stay at their saved positions.
            for(int z=-7;z>=-19;z-=2)for(int x=-9;x<=9;x+=2)ObsShape("Entrance deck plate",new Vector3(x,.035f,z),new Vector3(1.98f,.022f,1.98f),deck);
            ObsShape("Entrance canopy",new Vector3(0,10.3f,-13),new Vector3(9,.025f,14),glass);
            for(int side=-1;side<=1;side+=2){
                ObsShape("Entrance floor guidance",new Vector3(side*2.55f,.05f,-13),new Vector3(.045f,.025f,14),cyan);
                ObsShape("Entrance deck perimeter",new Vector3(side*10.2f,.16f,-13),new Vector3(.4f,.32f,14.4f),edge);
                var rail=ObsShape("Entrance safety rail",new Vector3(side*10,1.3f,-13),new Vector3(.12f,.12f,14.4f),bronze);rail.gameObject.AddComponent<BoxCollider>();
                ObsShape("Entrance lower rail",new Vector3(side*10,.55f,-13),new Vector3(.08f,.08f,14.4f),edge);
                ObsShape("Entrance overhead spine",new Vector3(side*4.5f,10.2f,-13),new Vector3(.3f,.35f,14.4f),hull);
                ObsShape("Entrance ceiling light",new Vector3(side*4.5f,10,-13),new Vector3(.035f,.025f,14.4f),amber);
                var roof=ObsShape("Entrance sloping canopy",new Vector3(side*7.4f,9,-13),new Vector3(6.3f,.025f,14.4f),glass);roof.localRotation=Quaternion.Euler(0,0,side*-22.5f);
                for(int z=-9;z>=-17;z-=4){
                    ObsBeam("Entrance window pier",new Vector3(side*10.3f,.2f,z),new Vector3(side*10.3f,7.8f,z),.14f,hull);
                    ObsBeam("Entrance vault rib",new Vector3(side*10.3f,7.8f,z),new Vector3(side*4.5f,10.2f,z),.12f,edge);
                    ObsBeam("Entrance vault light",new Vector3(side*10.12f,7.72f,z),new Vector3(side*4.5f,10.08f,z),.025f,cyan);
                    ObsShape("Entrance window",new Vector3(side*10.3f,4.6f,z),new Vector3(.025f,6.15f,3.8f),glass);
                    ObsShape("Entrance guard post",new Vector3(side*10,.65f,z),new Vector3(.12f,1.3f,.12f),edge);
                }
                ObsShape("Dock equipment cabinet",new Vector3(side*7.3f,1.1f,-16.5f),new Vector3(2.3f,2.2f,1.1f),hull).gameObject.AddComponent<BoxCollider>();
                for(int j=0;j<3;j++)ObsShape("Cabinet status strip",new Vector3(side*7.3f,1.35f+j*.25f,-15.93f),new Vector3(1.75f,.045f,.018f),j==0?amber:cyan);
                var light=new GameObject("Entry bay light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(observatory,false);light.transform.localPosition=new Vector3(side*5,4,-14);light.type=LightType.Point;light.color=Hex("72bdcc");light.range=15;light.intensity=2;
            }
            for(int z=-8;z>=-18;z-=2)ObsShape("Entrance centre dash",new Vector3(0,.055f,z),new Vector3(.08f,.02f,.65f),bronze);
            // A closed airlock provides a visible end to the room when looking back.
            var bulkhead=ObsShape("Entry bulkhead",new Vector3(0,5.1f,-20.5f),new Vector3(20.6f,10.2f,.35f),hull);bulkhead.gameObject.AddComponent<BoxCollider>();
            foreach(float x in new[]{-8f,-5f,5f,8f}){
                ObsShape("Bulkhead structural rib",new Vector3(x,4.9f,-20.22f),new Vector3(.16f,9.8f,.22f),edge);
                ObsShape("Bulkhead rib light",new Vector3(x,4.9f,-20.08f),new Vector3(.028f,7.2f,.028f),cyan);
            }
            var doorLight=new GameObject("Airlock welcome light",typeof(Light)).GetComponent<Light>();doorLight.transform.SetParent(observatory,false);doorLight.transform.localPosition=new Vector3(0,4.2f,-16.8f);doorLight.type=LightType.Point;doorLight.color=Hex("9bd1dd");doorLight.range=13;doorLight.intensity=3;
            ObsShape("Airlock surround",new Vector3(0,2.75f,-20.26f),new Vector3(5.4f,5.5f,.22f),edge);
            for(int side=-1;side<=1;side+=2){
                ObsShape("Sealed airlock leaf",new Vector3(side*1.23f,2.55f,-20.07f),new Vector3(2.4f,5.05f,.16f),hull).gameObject.AddComponent<BoxCollider>();
                ObsShape("Airlock viewing frame",new Vector3(side*1.24f,3.15f,-19.96f),new Vector3(1.45f,1.28f,.06f),edge);
                ObsShape("Airlock viewing pane",new Vector3(side*1.24f,3.15f,-19.91f),new Vector3(1.24f,1.06f,.04f),EntryMaterial("Display glass"));
                ObsShape("Airlock grip",new Vector3(side*.28f,1.65f,-19.91f),new Vector3(.075f,.9f,.08f),bronze);
                ObsShape("Airlock vertical light",new Vector3(side*2.72f,2.7f,-20.02f),new Vector3(.045f,5.4f,.035f),cyan);
            }
            ObsShape("Airlock centre seam",new Vector3(0,2.55f,-19.96f),new Vector3(.035f,5.05f,.035f),cyan);
            ObsShape("Airlock threshold",new Vector3(0,.045f,-19.65f),new Vector3(5.5f,.09f,.7f),edge);
            ObsShape("Airlock status band",new Vector3(0,5.65f,-20.05f),new Vector3(4.8f,.07f,.04f),amber);
            EntryLabel("Entrance title","OBSERVATORY ACCESS",new Vector3(0,6.65f,-20.02f),4.5f,Hex("a7d9df"));
            EntryLabel("Entrance status","ENTRY AIRLOCK  /  SEALED",new Vector3(0,6,-20.02f),2.1f,Hex("d4a66c"));
            foreach(string name in new[]{"Floor","Left wall","Right wall"}){
                var node=world.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==name);if(node==null)throw new Exception("Keep the saved floor and boundary: "+name);
                var p=node.position;p.z=0;node.position=p;var scale=node.localScale;scale.z=42;node.localScale=scale;SaveReference(node);
            }
            var entry=world.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Entry wall");var entryPosition=entry.position;entryPosition.z=-21;entry.position=entryPosition;SaveReference(entry);
            PrefabUtility.SaveAsPrefabAssetAndConnect(observatory.gameObject,Prefabs+"LimitsEntryBay.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CaptureEntryBay("after");
            ManualEditingMigration.Validate();OpenObservatory();
            File.WriteAllText(Path.Combine(EntryBayEvidence,"authored.txt"),"PASS: saved entry bay prefab; floor extended from 28 m to 42 m towards the rear, matching wall collisions, closed airlock, rear deck, canopy and rails. Existing console, guide, gate, player and UI references retained.");
        }
        public static void AuthorLimitsEntryBayAndBuild(){AuthorLimitsEntryBay();ManualEditingMigration.BuildQA();}
    }
}
#endif
