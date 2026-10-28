#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        const string TimelineFolder=MenuArt+"Timeline/";
        const string EntranceAsset=TimelineFolder+"LaboratoryEntrance.playable";
        const float SequenceLength=6.5f;
        static Animator Animated(Transform target)
        {
            var animator=target.GetComponent<Animator>();if(animator==null)animator=target.gameObject.AddComponent<Animator>();
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;return animator;
        }
        static AnimationClip StoreClip(AnimationClip clip,string name)
        {
            clip.name=name;clip.legacy=false;clip.frameRate=60;string path=TimelineFolder+name+".anim";
            var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(saved==null){AssetDatabase.CreateAsset(clip,path);return clip;}
            EditorUtility.CopySerialized(clip,saved);Object.DestroyImmediate(clip);EditorUtility.SetDirty(saved);return saved;
        }
        static void Curve(AnimationClip clip,string path,Type type,string property,params float[] pairs)
        {
            var keys=new Keyframe[pairs.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(pairs[i*2],pairs[i*2+1]);
            var curve=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,type,property),curve);
        }
        static void Positions(AnimationClip clip,params (float time,Vector3 value)[] keys)
        {
            for(int axis=0;axis<3;axis++)Curve(clip,"",typeof(Transform),"m_LocalPosition."+"xyz"[axis],keys.SelectMany(k=>new[]{k.time,k.value[axis]}).ToArray());
        }
        static void Rotations(AnimationClip clip,params (float time,Quaternion value)[] keys)
        {
            for(int axis=0;axis<4;axis++)Curve(clip,"",typeof(Transform),"m_LocalRotation."+"xyzw"[axis],keys.SelectMany(k=>new[]{k.time,k.value[axis]}).ToArray());
            clip.EnsureQuaternionContinuity();
        }
        static AnimationTrack Track(TimelineAsset timeline,PlayableDirector director,GroupTrack group,string name,Animator binding)
        {
            var track=timeline.CreateTrack<AnimationTrack>(group,name);track.trackOffset=TrackOffset.ApplyTransformOffsets;track.position=Vector3.zero;track.rotation=Quaternion.identity;
            director.SetGenericBinding(track,binding);return track;
        }
        static TimelineClip Clip(AnimationTrack track,AnimationClip animation,string title,double start=0,double duration=SequenceLength,double speed=1,bool loop=false)
        {
            var clip=track.CreateClip<AnimationPlayableAsset>();clip.displayName=title;clip.start=start;clip.duration=duration;clip.timeScale=speed;
            var asset=(AnimationPlayableAsset)clip.asset;asset.clip=animation;asset.removeStartOffset=false;asset.applyFootIK=false;asset.loop=loop?AnimationPlayableAsset.LoopMode.On:AnimationPlayableAsset.LoopMode.Off;
            return clip;
        }
        static AnimationClip RobotClip(string source,bool loop)
        {
            var clone=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>(LaboratoryAIImport.Root+"/Animations/"+source+".anim"));clone.legacy=false;
            var settings=AnimationUtility.GetAnimationClipSettings(clone);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clone,settings);return StoreClip(clone,"Guide-"+source);
        }
        static GameObject GuidePrefab()
        {
            const string path=Prefabs+"MainMenuGuide.prefab";
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LaboratoryAIImport.PrefabPath));
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var preview=root.GetComponent<LaboratoryAIPreview>();preview.playing=false;preview.Sample(0,0);var model=preview.model;
            Object.DestroyImmediate(preview);
            foreach(var animation in root.GetComponentsInChildren<Animation>(true))Object.DestroyImmediate(animation);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;
            var animator=Animated(model.transform);animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var idle=RobotClip("Idle",true);
            string controllerPath=TimelineFolder+"GuideIdle.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var state=controller.layers[0].stateMachine.AddState("Idle hover");state.motion=idle;controller.layers[0].stateMachine.defaultState=state;}
            animator.runtimeAnimatorController=controller;
            foreach(var light in root.GetComponentsInChildren<Light>(true))light.cullingMask=1<<30;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
        }
        [MenuItem("Calculus Lab/Main menu/Author Timeline and robot guide")]
        public static void AuthorEntranceTimeline()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the current scene before authoring the Timeline.");
            Directory.CreateDirectory(TimelineFolder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            var manager=Object.FindFirstObjectByType<GameManager>();var entrance=manager.menuEntrance;Index(manager.menuOrLimitsUI.gameObject);
            foreach(var old in scene.GetRootGameObjects().Where(g=>g.name=="Entrance Timeline"))Object.DestroyImmediate(old);
            foreach(var old in entrance.transform.Cast<Transform>().Where(t=>t.name=="Laboratory guide").ToArray())Object.DestroyImmediate(old.gameObject);
            var guide=(GameObject)PrefabUtility.InstantiatePrefab(GuidePrefab(),scene);guide.name="Laboratory guide";guide.transform.SetParent(entrance.transform,false);guide.transform.localPosition=new Vector3(-1.05f,.35f,.6f);guide.transform.localScale=Vector3.one*.9f;
            var model=guide.GetComponentInChildren<Animator>();
            var head=model.transform.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");var visor=model.GetComponentsInChildren<Renderer>().First(r=>r.name=="Recessed octagonal faceted visor");
            Vector3 front=visor.bounds.center-head.position;front.y=0;float heading=Vector3.SignedAngle(front,Vector3.forward,Vector3.up);guide.transform.localRotation=Quaternion.Euler(0,heading,0);
            entrance.guide=guide.transform;
            foreach(string name in new[]{"Guide soft key","Guide rim"})if(entrance.transform.Find(name)==null){
                var light=new GameObject(name,typeof(Light)).GetComponent<Light>();light.transform.SetParent(entrance.transform,false);light.type=LightType.Directional;light.cullingMask=1<<30;light.shadows=LightShadows.None;light.intensity=name=="Guide soft key"?1.6f:.65f;light.color=new Color(.75f,.89f,1);light.transform.localRotation=Quaternion.Euler(35,name=="Guide soft key"?180:0,0);
            }
            // The saved 3D guide replaces the portrait within the same authored layout.
            Find("ReportWelcomePortrait").gameObject.SetActive(false);SaveReference(Find("ReportWelcomePortrait").gameObject.GetComponent<RectTransform>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(Find("ReportWelcomePortrait").gameObject);
            Find("MenuRobotHint").GetComponent<TMP_Text>().text="Ready?\nFollow me.";SaveReference(Find("MenuRobotHint").GetComponent<TMP_Text>());
            var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(EntranceAsset);
            if(timeline==null){timeline=ScriptableObject.CreateInstance<TimelineAsset>();AssetDatabase.CreateAsset(timeline,EntranceAsset);}
            else foreach(var track in timeline.GetRootTracks().ToArray())timeline.DeleteTrack(track);
            timeline.editorSettings.frameRate=60;timeline.durationMode=TimelineAsset.DurationMode.FixedLength;timeline.fixedDuration=SequenceLength;
            var timelineObject=new GameObject("Entrance Timeline",typeof(PlayableDirector),typeof(SignalReceiver));var director=timelineObject.GetComponent<PlayableDirector>();director.playableAsset=timeline;director.playOnAwake=false;director.timeUpdateMode=DirectorUpdateMode.UnscaledGameTime;director.extrapolationMode=DirectorWrapMode.None;entrance.director=director;
            var robotGroup=timeline.CreateTrack<GroupTrack>(null,"01  ROBOT GUIDE");var labGroup=timeline.CreateTrack<GroupTrack>(null,"02  LABORATORY DOORS");var cameraGroup=timeline.CreateTrack<GroupTrack>(null,"03  CAMERA AND INTERFACE");
            var body=Track(timeline,director,robotGroup,"Robot • wave, then floating walk",model);
            var welcome=RobotClip("Welcome",false);var cruise=RobotClip("Cruise",true);var greetingClip=Clip(body,welcome,"Come with me",0,1.65,4/1.65);var cruiseClip=Clip(body,cruise,"Lead the learner",1.4,5.1,1,true);greetingClip.blendOutDuration=.25;cruiseClip.blendInDuration=.25;
            var route=new AnimationClip();
            Positions(route,(0,guide.transform.localPosition),(1.35f,guide.transform.localPosition),(2.25f,new Vector3(-.75f,.4f,-.05f)),(3.6f,new Vector3(-.4f,.42f,-1.35f)),(4.8f,new Vector3(0,.44f,-2.8f)),(5.65f,new Vector3(0,.44f,-4.4f)),(6.5f,new Vector3(0,.44f,-4.4f)));
            Rotations(route,(0,Quaternion.Euler(0,heading,0)),(1.3f,Quaternion.Euler(0,heading,0)),(2.25f,Quaternion.Euler(0,heading+180,0)),(3.6f,Quaternion.Euler(0,heading+180,0)),(4.15f,Quaternion.Euler(0,heading+145,0)),(4.65f,Quaternion.Euler(0,heading+180,0)),(6.5f,Quaternion.Euler(0,heading+180,0)));
            Clip(Track(timeline,director,robotGroup,"Robot • turn and lead through the door",Animated(guide.transform)),StoreClip(route,"Guide-Route"),"Turn, lead, look back");
            foreach(int side in new[]{-1,1}){
                var door=new AnimationClip();var hinge=side<0?entrance.leftHinge:entrance.rightHinge;
                Positions(door,(0,hinge.localPosition),(6.5f,hinge.localPosition));
                Rotations(door,(0,Quaternion.identity),(2.0f,Quaternion.identity),(3.35f,Quaternion.Euler(0,side<0?98:-98,0)),(6.5f,Quaternion.Euler(0,side<0?98:-98,0)));
                Clip(Track(timeline,director,labGroup,side<0?"Left leaf • open inward":"Right leaf • open inward",Animated(hinge)),StoreClip(door,side<0?"Door-Left":"Door-Right"),"Open for the guide");
            }
            var cameraClip=new AnimationClip();var camera=entrance.lobbyCamera;var initial=camera.transform.localPosition;
            Positions(cameraClip,(0,initial),(1.5f,initial),(2.6f,new Vector3(.05f,1.6f,2.8f)),(4.4f,new Vector3(.05f,1.55f,.4f)),(5.8f,new Vector3(0,1.5f,-2.6f)),(6.5f,new Vector3(0,1.5f,-2.6f)));
            Rotations(cameraClip,(0,camera.transform.localRotation),(1.5f,camera.transform.localRotation),(2.6f,Quaternion.LookRotation(new Vector3(-.7f,-.4f,-3))), (4.4f,Quaternion.LookRotation(new Vector3(-.1f,-.4f,-2.6f))),(5.8f,Quaternion.LookRotation(Vector3.back)),(6.5f,Quaternion.LookRotation(Vector3.back)));
            Curve(cameraClip,"",typeof(Camera),"m_LensShift.y",0,camera.lensShift.y,1.1f,0,6.5f,0);
            Clip(Track(timeline,director,cameraGroup,"Camera • follow behind the guide",Animated(camera.transform)),StoreClip(cameraClip,"Camera-FollowGuide"),"Wait, follow, enter");
            var interfaceClip=new AnimationClip();var root=manager.menuOrLimitsUI.transform;
            foreach(var group in entrance.foreground){string path=AnimationUtility.CalculateTransformPath(group.transform,root);if(group.name=="Header")Curve(interfaceClip,path,typeof(CanvasGroup),"m_Alpha",0,1,.2f,1,.75f,0,5.9f,0,6.25f,1,6.5f,1);else Curve(interfaceClip,path,typeof(CanvasGroup),"m_Alpha",0,1,.15f,1,.65f,0,6.5f,0);}
            Curve(interfaceClip,AnimationUtility.CalculateTransformPath(entrance.edgeFade.transform,root),typeof(CanvasGroup),"m_Alpha",0,1,1.1f,0,6.5f,0);
            string previewPath=AnimationUtility.CalculateTransformPath(entrance.roomPreview,root);
            Curve(interfaceClip,previewPath,typeof(RectTransform),"m_AnchoredPosition.x",0,entrance.roomPreview.anchoredPosition.x,1.1f,0,6.5f,0);
            Curve(interfaceClip,previewPath,typeof(RectTransform),"m_AnchoredPosition.y",0,entrance.roomPreview.anchoredPosition.y,1.1f,0,6.5f,0);
            Curve(interfaceClip,previewPath,typeof(RectTransform),"m_SizeDelta.x",0,entrance.roomPreview.sizeDelta.x,1.1f,1280,6.5f,1280);
            Curve(interfaceClip,previewPath,typeof(RectTransform),"m_SizeDelta.y",0,entrance.roomPreview.sizeDelta.y,1.1f,720,6.5f,720);
            Curve(interfaceClip,AnimationUtility.CalculateTransformPath(entrance.lightCover.transform,root),typeof(CanvasGroup),"m_Alpha",0,0,5.25f,0,5.85f,1,6.0f,1,6.45f,0,6.5f,0);
            Clip(Track(timeline,director,cameraGroup,"UI • clear the view, then reveal Topics",Animated(root)),StoreClip(interfaceClip,"UI-Entrance"),"Menu out • full-screen room • light in/out");
            var seamTrack=timeline.CreateTrack<ActivationTrack>(labGroup,"Centre seam • closed-door light");var seamClip=seamTrack.CreateDefaultClip();seamClip.start=0;seamClip.duration=2.05;seamTrack.postPlaybackState=ActivationTrack.PostPlaybackState.Revert;director.SetGenericBinding(seamTrack,entrance.centreSeam);
            var signal=AssetDatabase.LoadAssetAtPath<SignalAsset>(TimelineFolder+"EnterTopics.signal");if(signal==null){signal=ScriptableObject.CreateInstance<SignalAsset>();AssetDatabase.CreateAsset(signal,TimelineFolder+"EnterTopics.signal");}
            var signals=timeline.CreateTrack<SignalTrack>(null,"04  SIGNAL • open Topics under full light");var marker=signals.CreateMarker<SignalEmitter>(5.9);marker.asset=signal;marker.emitOnce=true;marker.retroactive=false;
            var receiver=timelineObject.GetComponent<SignalReceiver>();var reaction=new UnityEvent();UnityEventTools.AddPersistentListener(reaction,entrance.OnTimelineCovered);receiver.AddReaction(signal,reaction);director.SetGenericBinding(signals,receiver);
            SaveReference(entrance);SaveReference(director);SaveReference(receiver);SaveReference(manager);SaveReference(model);SaveReference(guide.transform);EditorUtility.SetDirty(guide);PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
            foreach(var animator in entrance.GetComponentsInChildren<Animator>(true))SaveReference(animator);SaveReference(root.GetComponent<Animator>());
            EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            ValidateEntranceTimeline();ManualEditingMigration.Validate();EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            Debug.Log("TIMELINE_GUIDE_AUTHORED_PASS");
        }
        public static void ValidateEntranceTimeline()
        {
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");var entrance=Object.FindFirstObjectByType<GameManager>().menuEntrance;var director=entrance.director;
            if(director==null||!(director.playableAsset is TimelineAsset timeline)||entrance.guide==null||director.playOnAwake||director.timeUpdateMode!=DirectorUpdateMode.UnscaledGameTime)throw new Exception("Save the Timeline, guide and unscaled Director references.");
            foreach(var output in timeline.outputs)if(director.GetGenericBinding(output.sourceObject)==null)throw new Exception("Missing Timeline binding: "+output.streamName);
            string folder=LearningApi.Argument("--timeline-evidence",Path.GetFullPath("../../timeline-guide-2026-10-24"));Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder,"timeline-tracks.txt"),timeline.GetOutputTracks().Select(t=>t.name+" -> "+director.GetGenericBinding(t).name));
            Debug.Log("TIMELINE_GUIDE_REFERENCES_PASS");
        }
        [MenuItem("Calculus Lab/Main menu/Open entrance Timeline")]
        public static void OpenEntranceTimeline()
        {
            OpenMainMenu();var director=Object.FindFirstObjectByType<GameManager>().menuEntrance.director;Selection.activeGameObject=director.gameObject;TimelineEditor.GetOrCreateWindow().SetTimeline(director);EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        public static void AuthorTimelineAndBuild(){AuthorEntranceTimeline();ManualEditingMigration.BuildQA();}
    }
}
#endif
