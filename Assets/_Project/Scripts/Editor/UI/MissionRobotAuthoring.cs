#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace CalculusLabEscape.Editor
{
    // Adds the existing guide and conversation to saved assets in Edit Mode.
    // Play only uses these authored scene objects and Inspector references.
    public static class MissionRobotAuthoring
    {
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        const string Scenes="Assets/_Project/Scenes/";
        static string Evidence=>LearningApi.Argument("--robot-evidence",Path.GetFullPath("../../qa/mission-robots-2026-10-26"));
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static void Remember(Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        static string Level(int topic)=>topic==1?"Differentiation":"Integration";
        static string GuideName(int topic)=>Level(topic)+" guide";
        static void RequireClean()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before placing saved guides.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current scene edits before authoring guides.");
        }
        static void AddConversation(GameObject root,int topic)
        {
            var chat=root.GetComponentInChildren<RobotDialogue>(true);
            if(chat==null)
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"RobotDialogue.prefab"),root.scene);
                instance.transform.SetParent(Find(root,"DesignFrame"),false);instance.transform.SetAsLastSibling();chat=instance.GetComponent<RobotDialogue>();
            }
            Find(chat.gameObject,"DialogueTopic1Label").GetComponent<TMP_Text>().text=topic==1?"About differentiation":"About integration";
            chat.topic.text=topic==1?"YOUR BRIDGE GUIDE":"YOUR CONTAINMENT GUIDE";
            chat.message.text="Hello! I'm your laboratory guide.";chat.message.maxVisibleCharacters=int.MaxValue;
            chat.overlay.SetActive(false);
            Remember(Find(chat.gameObject,"DialogueTopic1Label").GetComponent<TMP_Text>());Remember(chat.topic);Remember(chat.message);Remember(chat.overlay);Remember(chat);
            ManualEditingMigration.ConfigureCanvas(root);
        }
        static Renderer[] Body(Transform root)=>root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.sqrMagnitude>.0001f&&!r.name.Contains("Hover")&&!r.name.Contains("Hologram")).ToArray();
        static Bounds Bounds(Transform root){var renderers=Body(root);if(renderers.Length==0)throw new Exception("The guide has no visible body.");var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
        static Transform PlaceGuide(GameManager manager,int topic)
        {
            var world=manager.world;var guide=world.transform.Find(GuideName(topic));
            if(guide==null)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"LimitsGuide.prefab"),world.gameObject.scene);
                go.name=GuideName(topic);guide=go.transform;guide.SetParent(world.transform,true);
            }
            foreach(var node in guide.GetComponentsInChildren<Transform>(true))node.gameObject.layer=0;
            foreach(var collider in guide.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            var animator=guide.GetComponentInChildren<Animator>(true);if(animator==null||animator.runtimeAnimatorController==null)throw new Exception("Reuse the guide's saved idle Animator.");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/UI/MainMenu/Timeline/Guide-Idle.anim");idle.SampleAnimation(animator.gameObject,.6f);
            var head=guide.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");
            var visor=guide.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Recessed octagonal faceted visor");
            Vector3 destination=world.player.transform.position+world.player.transform.right*(topic==1?2.4f:-2.4f)+world.player.transform.forward*3.2f;
            Physics.SyncTransforms();
            var ground=Physics.RaycastAll(destination+Vector3.up*5,Vector3.down,12,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.8f&&!h.transform.IsChildOf(world.player.transform)&&!h.transform.IsChildOf(guide)&&(world.water==null||!h.transform.IsChildOf(world.water))).OrderBy(h=>h.distance).ToArray();
            float floor=ground.Length>0?ground[0].point.y:world.player.transform.position.y;
            var b=Bounds(guide);guide.position+=destination-new Vector3(b.center.x,floor,b.center.z);
            Vector3 front=visor.bounds.center-head.position;front.y=0;Vector3 towards=world.player.transform.position-guide.position;towards.y=0;
            guide.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(front,towards,Vector3.up),Vector3.up)*guide.rotation;
            b=Bounds(guide);guide.position+=new Vector3(destination.x,floor+.3f,destination.z)-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var light in guide.GetComponentsInChildren<Light>(true)){light.cullingMask=~0;light.shadows=LightShadows.None;Remember(light);}
            Remember(animator);Remember(guide);Remember(guide.gameObject);
            return guide;
        }
        [MenuItem("Calculus Lab/Missions/Add saved robot guides to levels 2 and 3")]
        public static void AuthorAndBuild()
        {
            RequireClean();Directory.CreateDirectory(Evidence);
            for(int topic=1;topic<=2;topic++)
            {
                string prefab=Prefabs+Level(topic)+"UI.prefab";var root=PrefabUtility.LoadPrefabContents(prefab);
                try{AddConversation(root,topic);PrefabUtility.SaveAsPrefabAsset(root,prefab);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
                var scene=EditorSceneManager.OpenScene(Scenes+"Level0"+(topic+1)+"_"+Level(topic)+".unity");
                var manager=Object.FindFirstObjectByType<GameManager>();var guide=PlaceGuide(manager,topic);
                AddConversation(manager.missionUI.gameObject,topic);
                var chat=manager.missionUI.GetComponentInChildren<RobotDialogue>(true);manager.robotDialogue=chat;
                chat.manager=manager;chat.guide=guide;chat.lookTarget=guide.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");
                chat.worldPrompt=Find(manager.missionUI.gameObject,"WorldPrompt").GetComponent<TMP_Text>();chat.worldObjective=Find(manager.missionUI.gameObject,"WorldObjective").GetComponent<TMP_Text>();
                chat.hideWhileTalking=new[]{"WorldHUD","PromptPanel","Crosshair","WorldGuide","WorldPause"}.Select(name=>Find(manager.missionUI.gameObject,name).gameObject).ToArray();
                chat.overlay.SetActive(false);Remember(chat);Remember(manager);Remember(manager.missionUI.uiReferences);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            Validate();Capture();ManualEditingMigration.BuildQA();
        }
        public static void Validate()
        {
            RequireClean();
            foreach(int topic in new[]{1,2})
            {
                var scene=EditorSceneManager.OpenScene(Scenes+"Level0"+(topic+1)+"_"+Level(topic)+".unity");var manager=Object.FindFirstObjectByType<GameManager>();var chat=manager.robotDialogue;
                if(chat==null||chat.manager!=manager||chat.guide==null||chat.guide!=manager.world.transform.Find(GuideName(topic))||chat.lookTarget==null||chat.worldPrompt==null||chat.worldObjective==null||chat.overlay.activeSelf)throw new Exception("Robot references must be saved before Play in "+scene.name);
                if(manager.world.GetComponentsInChildren<RobotDialogue>(true).Length!=0||manager.missionUI.GetComponentsInChildren<RobotDialogue>(true).Length!=1)throw new Exception("Each scene needs exactly one saved conversation UI.");
                if(!PrefabUtility.IsPartOfPrefabInstance(chat.guide)||chat.guide.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new Exception("Guide must reuse its saved prefab without blocking movement.");
                foreach(var button in chat.GetComponentsInChildren<Button>(true))if(button.onClick.GetPersistentEventCount()!=1||button.onClick.GetPersistentTarget(0)!=chat)throw new Exception("Conversation button must retain its Inspector callback: "+button.name);
                if(Find(chat.gameObject,"DialogueTopic1Label").GetComponent<TMP_Text>().text!=(topic==1?"About differentiation":"About integration"))throw new Exception("Save the matching mission button label.");
            }
            ManualEditingMigration.Validate();
            File.WriteAllText(Path.Combine(Evidence,"saved-robots.txt"),"PASS: levels 2 and 3 contain the existing animated guide prefab and authored conversation UI; scene-specific manager, guide, camera target, HUD and persistent button references; inactive dialogue at start; no blocking guide colliders; all original scene references validate.");
        }
        public static void Capture()
        {
            Directory.CreateDirectory(Evidence);
            foreach(int topic in new[]{1,2})
            {
                string path=Scenes+"Level0"+(topic+1)+"_"+Level(topic)+".unity";
                try
                {
                    EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.missionUI.GetComponent<Canvas>();
                    SceneAuthoringWindow.ApplyView(manager.world,canvas,"World");
                    var camera=manager.world.player.GetComponentInChildren<Camera>();LaboratoryAIImport.Capture(camera,Path.Combine(Evidence,Level(topic)+"-scene-entry.png"));
                    SceneAuthoringWindow.ApplyView(manager.world,canvas,"Movement guide");
                    camera.transform.position=manager.robotDialogue.lookTarget.position+new Vector3(.3f,.15f,-3.4f);camera.transform.LookAt(manager.robotDialogue.lookTarget.position);
                    foreach(int selection in new[]{-1,0,1,2})
                    {
                        manager.robotDialogue.Preview(0,selection);Canvas.ForceUpdateCanvases();
                        if(manager.robotDialogue.message.isTextOverflowing)throw new Exception("Robot dialogue text must fit: "+Level(topic)+" topic "+selection);
                        SceneAuthoring.Capture(manager.world,canvas,Path.Combine(Evidence,Level(topic)+"-dialogue-"+selection+".png"),false);
                    }
                }
                finally{EditorSceneManager.OpenScene(path);}
            }
        }
    }
}
#endif
