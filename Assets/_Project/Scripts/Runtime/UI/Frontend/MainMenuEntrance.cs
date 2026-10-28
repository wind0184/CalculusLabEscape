using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

namespace CalculusLabEscape
{
    // Timeline owns the cinematic keyframes. This component handles navigation,
    // input and restoring the learner's saved scene when the director stops.
    public sealed class MainMenuEntrance : MonoBehaviour
    {
        [Header("Playable Director — edit its Timeline to change the sequence")]
        public PlayableDirector director;
        public Transform guide;
        [Header("Saved laboratory doorway")]
        public Transform leftHinge,rightHinge;
        public GameObject centreSeam;
        public Camera lobbyCamera;
        [Header("Saved interface references")]
        public RectTransform roomPreview,designFrame;
        public CanvasGroup edgeFade;
        public CanvasGroup[] foreground;
        public CanvasGroup lightCover;
        public bool Busy { get; private set; }
        public float Progress { get; private set; }
        public string Phase { get; private set; }="Idle";
        public float entranceSeconds=>director!=null?(float)director.duration:0;
        public bool Covered { get; private set; }
        Action openTopics;
        bool captured,finishing,navigationPending,navigationFrameStarted;
        Vector2 previewPosition,previewSize,lensShift;
        float fieldOfView;
        readonly List<Pose> poses=new List<Pose>();
        readonly List<GroupState> groups=new List<GroupState>();
        readonly List<BaseInputModule> blockedModules=new List<BaseInputModule>();
        struct Pose { public Transform target;public Vector3 position,scale;public Quaternion rotation; }
        struct GroupState { public CanvasGroup target;public float alpha;public bool input,rays; }

        public bool Play(Action showTopics)
        {
            if(Busy||ScreenTransition.IsBusy)return false;
            if(ScreenTransition.InstantQA){showTopics();return true;}
            if(director==null||director.playableAsset==null)throw new InvalidOperationException("Assign the saved entrance Timeline on Main Menu Entrance.");
            CapturePose();openTopics=showTopics;Covered=false;Busy=true;finishing=false;navigationPending=false;navigationFrameStarted=false;Progress=0;Phase="Guide greeting";Block(true);
            if(ScreenTransition.Active!=null&&ScreenTransition.Active.reduceMotion){StartCoroutine(ReducedEntrance());return true;}
            director.stopped-=OnDirectorStopped;director.stopped+=OnDirectorStopped;
            director.time=0;director.Play();return true;
        }
        void Update()
        {
            if(!Busy||director==null||Covered||director.state!=PlayState.Playing)return;
            Progress=Mathf.Clamp01((float)(director.time/Math.Max(.01,director.duration)));
            Phase=director.time<1.6?"Guide greeting":director.time<2.3?"Following guide":director.time<3.4?"Door opening":"Passing through doorway";
        }
        // Saved Signal Receiver event at the opaque-light marker on the Timeline.
        public void OnTimelineCovered()
        {
            if(!Application.isPlaying||!Busy||Covered||navigationPending)return;
            // Timeline notifications can arrive before this frame's animation values.
            // Request navigation here, then render one opaque frame in LateUpdate.
            navigationPending=true;Phase="Covered";
        }
        void LateUpdate()
        {
            if(!navigationPending||navigationFrameStarted)return;
            navigationFrameStarted=true;lightCover.alpha=1;StartCoroutine(NavigateAfterCoveredFrame());
        }
        IEnumerator NavigateAfterCoveredFrame()
        {
            yield return new WaitForEndOfFrame();navigationPending=false;CompleteNavigation();
        }
        void CompleteNavigation()
        {
            if(!Busy||Covered)return;
            Covered=true;Phase="Revealing topics";
            try{openTopics?.Invoke();}catch(Exception error){Debug.LogException(error);}
        }
        void OnDirectorStopped(PlayableDirector source)
        {
            if(!Busy||finishing)return;finishing=true;StartCoroutine(FinishAfterGraphRelease());
        }
        IEnumerator FinishAfterGraphRelease()
        {
            // Timeline releases its animation bindings before restoring authored poses.
            yield return null;Finish();
        }
        void CapturePose()
        {
            poses.Clear();groups.Clear();
            foreach(var part in GetComponentsInChildren<Transform>(true))poses.Add(new Pose{target=part,position=part.localPosition,rotation=part.localRotation,scale=part.localScale});
            foreach(var group in foreground.Concat(new[]{edgeFade,lightCover}).Distinct()){
                groups.Add(new GroupState{target=group,alpha=group.alpha,input=group.interactable,rays=group.blocksRaycasts});
                if(group!=lightCover){group.interactable=false;group.blocksRaycasts=false;}
            }
            previewPosition=roomPreview.anchoredPosition;previewSize=roomPreview.sizeDelta;lensShift=lobbyCamera.lensShift;fieldOfView=lobbyCamera.fieldOfView;captured=true;
        }
        void RestorePose()
        {
            if(!captured)return;
            foreach(var pose in poses)if(pose.target!=null){pose.target.localPosition=pose.position;pose.target.localRotation=pose.rotation;pose.target.localScale=pose.scale;}
            foreach(var group in groups)if(group.target!=null){group.target.alpha=group.alpha;group.target.interactable=group.input;group.target.blocksRaycasts=group.rays;}
            if(roomPreview!=null){roomPreview.anchoredPosition=previewPosition;roomPreview.sizeDelta=previewSize;}
            if(lobbyCamera!=null){lobbyCamera.lensShift=lensShift;lobbyCamera.fieldOfView=fieldOfView;}
            captured=false;
        }
        IEnumerator ReducedEntrance()
        {
            double began=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-began<.14){lightCover.alpha=Mathf.SmoothStep(0,1,(float)((Time.realtimeSinceStartupAsDouble-began)/.14));yield return null;}
            lightCover.alpha=1;yield return new WaitForEndOfFrame();CompleteNavigation();began=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-began<.14){lightCover.alpha=1-Mathf.SmoothStep(0,1,(float)((Time.realtimeSinceStartupAsDouble-began)/.14));yield return null;}
            Finish();
        }
        void Finish()
        {
            RestorePose();if(lightCover!=null)lightCover.alpha=0;Block(false);Busy=false;Progress=0;Phase="Idle";openTopics=null;finishing=false;navigationPending=false;navigationFrameStarted=false;
        }
        void Block(bool value)
        {
            if(lightCover!=null)lightCover.blocksRaycasts=value;
            if(value){var events=EventSystem.current;if(events==null)return;events.SetSelectedGameObject(null);foreach(var module in events.GetComponents<BaseInputModule>())if(module.enabled){blockedModules.Add(module);module.enabled=false;}}
            else{foreach(var module in blockedModules)if(module!=null)module.enabled=true;blockedModules.Clear();}
        }
        void OnDisable()
        {
            if(director!=null)director.stopped-=OnDirectorStopped;
            if(!Busy)return;finishing=true;StopAllCoroutines();if(director!=null)director.Stop();Finish();
        }
    }
}
