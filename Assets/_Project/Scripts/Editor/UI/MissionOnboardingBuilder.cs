#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        const string IntroductionAsset="Assets/_Project/Resources/Production/MissionIntroductions.asset";
        static MissionOnboarding IntroductionPlan()
        {
            var plan=AssetDatabase.LoadAssetAtPath<MissionOnboarding>(IntroductionAsset);
            if(plan==null)
            {
                plan=ScriptableObject.CreateInstance<MissionOnboarding>();plan.cards=MissionOnboarding.Defaults().ToList();
                AssetDatabase.CreateAsset(plan,IntroductionAsset);
            }
            var numbers=plan.Find("bridge-numbers");var defaultNumbers=MissionOnboarding.Defaults().First(c=>c.key=="bridge-numbers");
            if(numbers!=null&&numbers.instructions==defaultNumbers.instructions.Replace("such as 3/7","such as 1/2"))
            {numbers.instructions=defaultNumbers.instructions;EditorUtility.SetDirty(plan);}
            return plan;
        }

        static void AddMissionOnboarding(GameObject root)
        {
            Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            root.GetComponent<MissionCanvas>().onboarding=IntroductionPlan();
            if(Find("IntroductionModal")!=null)
            {
                var existing=Find("WorldGuide").GetComponent<RectTransform>();
                if(existing.anchoredPosition==new Vector2(29,-231))existing.anchoredPosition=new Vector2(29,-430);
                return;
            }
            var guide=Panel(Find("World"),"WorldGuide",29,430,612,155,Paper);
            guide.raycastTarget=false;
            Text(guide.transform,"WorldGuideTitle","GET MOVING",18,13,576,29,21,Accent,true);
            Text(guide.transform,"WorldGuideBody","",18,51,576,60,20);
            Text(guide.transform,"WorldGuideKeys","G: hide / show guides    Esc: pause",18,118,576,25,16,Muted);
            Button(Find("Pause"),"GuidePreference","First-use guides: ON / click to hide",235,591,795,44);
            var controlsEscape=Find("ControlsEscape").GetComponent<RectTransform>();
            controlsEscape.anchoredPosition=new Vector2(71,-592);controlsEscape.sizeDelta=new Vector2(1135,28);
            Button(Find("ControlsModal"),"IntroductionReplay","Show this introduction",71,625,350,49);
            var overlay=Panel(Find("DesignFrame"),"IntroductionModal",0,0,1280,720,Paper);
            Text(overlay.transform,"IntroductionEyebrow","FIRST USE / CONTROL INTRODUCTION",69,91,1135,30,18,Accent,true);
            Text(overlay.transform,"IntroductionTitle","",69,137,1135,56,37,Ink,true);
            Text(overlay.transform,"IntroductionBody","",71,227,1128,353,23);
            Text(overlay.transform,"IntroductionStatus","Free / time and pressure paused. Esc also returns to your controls.",71,590,1128,28,18,Muted);
            Button(overlay.transform,"IntroductionSkip","Hide automatic guides",71,632,350,49);
            Button(overlay.transform,"IntroductionContinue","Try these controls",855,632,348,49,true);
            overlay.gameObject.SetActive(false);
        }

        static void AddGuideZones(Chapter4World world)
        {
            var parent=world.transform.Find("Guide zones");
            if(parent==null){parent=new GameObject("Guide zones").transform;parent.SetParent(world.transform,false);}
            var targets=world.topic==1?world.devices.ToArray():new[]{world.station};
            for(int i=0;i<targets.Length;i++)
            {
                if(parent.GetComponentsInChildren<MissionGuideZone>(true).Any(z=>z.target==targets[i]))continue;
                string name=world.topic==1?Chapter4World.RepairDeviceNames[i]:"Containment case";
                var zone=new GameObject(name+" guide zone",typeof(BoxCollider),typeof(MissionGuideZone));
                zone.transform.SetParent(parent,false);zone.transform.position=targets[i].position;
                var box=zone.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(7.6f,8,7.6f);
                var data=zone.GetComponent<MissionGuideZone>();data.target=targets[i];data.deviceName=name;
            }
        }

        [MenuItem("Calculus Lab/Production/Upgrade first-use introductions")]
        public static void UpgradeOnboarding()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{AddMissionOnboarding(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            for(int topic=1;topic<=2;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var world=Object.FindFirstObjectByType<Chapter4World>();
                var renderers=world.GetComponentsInChildren<Renderer>(true);
                var positions=renderers.Select(r=>r.transform.localToWorldMatrix).ToArray();
                var colliders=world.GetComponentsInChildren<Collider>(true);string data=JsonUtility.ToJson(world);
                AddGuideZones(world);int count=world.GetComponentsInChildren<MissionGuideZone>(true).Length;AddGuideZones(world);
                if(world.GetComponentsInChildren<MissionGuideZone>(true).Length!=count||count!=(topic==1?3:1))throw new Exception("Guide zones must be unique");
                for(int i=0;i<renderers.Length;i++)if(renderers[i].transform.localToWorldMatrix!=positions[i])throw new Exception("Guide upgrade moved geometry");
                if(colliders.Any(c=>c==null)||world.GetComponentsInChildren<Collider>(true).Count(c=>!c.isTrigger)!=colliders.Count(c=>!c.isTrigger)||JsonUtility.ToJson(world)!=data)throw new Exception("Guide upgrade changed gameplay geometry/references");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("GUIDE_ZONES_SAVED / "+topic+" / "+count+" non-solid zones / renderer transforms and existing collider references preserved");
            }
            AssetDatabase.SaveAssets();
            SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-onboarding"));
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            Debug.Log("MISSION_ONBOARDING_UPGRADE_PASS");
        }
        public static void UpgradeOnboardingAndBuild(){UpgradeOnboarding();Build();}
    }
}
#endif
