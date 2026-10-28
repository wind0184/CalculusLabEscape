#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed partial class MainMenuPlaytest
    {
        IEnumerator DoorSequence()
        {
            var actor=GameManager.Instance.menuEntrance;
            Check(actor!=null&&actor.leftHinge!=null&&actor.rightHinge!=null&&actor.lightCover!=null,"Game Manager owns the saved two-leaf doorway and cover");
            if(actor==null){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: missing saved doorway");Application.Quit(1);yield break;}
            Check(actor.director!=null&&actor.director.playableAsset is UnityEngine.Timeline.TimelineAsset&&actor.guide!=null,"A saved Timeline asset and 3D robot are assigned");
            var guideStart=actor.guide.localPosition;var guideFacing=actor.guide.localRotation;
            var hand=actor.guide.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Ceramic hand paddle 1");var handStart=actor.guide.InverseTransformPoint(hand.position);
            var cameraPosition=actor.lobbyCamera.transform.localPosition;var previewPosition=actor.roomPreview.anchoredPosition;var previewSize=actor.roomPreview.sizeDelta;
            int objects=actor.GetComponentsInChildren<Transform>(true).Length;
            Menu.Control<TMPro.TMP_InputField>("ProfileInput").text="Local player";Menu.OnProfileEdited("Local player");yield return null;
            Capture("door-000");
            var button=Menu.Control<Button>("HomeStart");var point=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=point};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Actual Start button receives the entrance click");
            Time.timeScale=0;ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(actor.Busy&&actor.lightCover.blocksRaycasts&&EventSystem.current.GetComponents<BaseInputModule>().All(m=>!m.enabled),"Door animation blocks pointer and UI keyboard immediately");
            int callbacks=0;Check(!GameManager.Instance.EnterLaboratory(()=>callbacks++)&&!GameManager.ChangeScene(Chapter4Game.Scenes[0]),"Repeated Start and scene navigation are rejected while entering");
            double began=Time.realtimeSinceStartupAsDouble,nextFrame=began;float deadline=Time.realtimeSinceStartup+12;int frame=1;
            bool moved=false,opened=false,expanded=false,coveredBeforeTopics=false,earlyTopics=false,guideMoved=false,guideTurned=false,guideWaved=false;double furthestTimelineTime=0;
            var frames=new List<string>{"door-000|0"};
            while(actor.Busy&&Time.realtimeSinceStartup<deadline){
                moved|=Vector3.Distance(actor.lobbyCamera.transform.localPosition,cameraPosition)>2;
                furthestTimelineTime=Math.Max(furthestTimelineTime,actor.director.time);
                opened|=Quaternion.Angle(actor.leftHinge.localRotation,Quaternion.identity)>80&&Quaternion.Angle(actor.rightHinge.localRotation,Quaternion.identity)>80;
                expanded|=actor.roomPreview.sizeDelta.x>1279;
                guideMoved|=Vector3.Distance(actor.guide.localPosition,guideStart)>3;
                guideTurned|=Quaternion.Angle(actor.guide.localRotation,guideFacing)>140;
                if(actor.director.time<1.5)guideWaved|=Vector3.Distance(actor.guide.InverseTransformPoint(hand.position),handStart)>.08f;
                bool topics=Menu.Control<Transform>("Topics").gameObject.activeInHierarchy;
                if(actor.lightCover.alpha>.999f&&!topics)coveredBeforeTopics=true;
                if(actor.Phase!="Revealing topics"&&topics)earlyTopics=true;
                if(Time.realtimeSinceStartupAsDouble>=nextFrame){
                    string name="door-"+(frame++).ToString("D3");Capture(name);frames.Add(name+"|"+(Time.realtimeSinceStartupAsDouble-began).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));nextFrame=Time.realtimeSinceStartupAsDouble+.07;
                }
                yield return null;
            }
            Capture("door-"+frame.ToString("D3"));frames.Add("door-"+frame.ToString("D3")+"|"+(Time.realtimeSinceStartupAsDouble-began).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
            File.WriteAllLines(Path.Combine(folder,"frames.txt"),frames);
            Check(opened&&moved&&expanded,"Both doors swing on their hinges as the room fills the screen and camera moves through");
            Check(guideMoved&&guideTurned&&guideWaved,"Timeline plays the robot's greeting, turn and leading route");
            Check(coveredBeforeTopics&&!earlyTopics,"Topics appear only after the doorway light fully covers the screen");
            Check(!actor.Busy&&actor.Covered&&Menu.Control<Transform>("Topics").gameObject.activeInHierarchy&&furthestTimelineTime>=5.9&&Time.timeScale==0,"Timeline reaches its covered signal and opens Topics while timeScale is zero");
            Check(actor.lobbyCamera.transform.localPosition==cameraPosition&&actor.roomPreview.anchoredPosition==previewPosition&&actor.roomPreview.sizeDelta==previewSize&&Quaternion.Angle(actor.leftHinge.localRotation,Quaternion.identity)<.01f,"Camera, hinges and edited UI layout restore behind the light cover");
            Check(actor.guide.localPosition==guideStart&&Quaternion.Angle(actor.guide.localRotation,guideFacing)<.01f&&actor.centreSeam.activeSelf,"Guide placement and closed-door seam restore after Timeline stop");
            Check(!actor.lightCover.blocksRaycasts&&actor.lightCover.alpha==0&&EventSystem.current.GetComponents<BaseInputModule>().Any(m=>m.enabled)&&actor.GetComponentsInChildren<Transform>(true).Length==objects,"Input returns without generating room objects");
            Time.timeScale=1;yield return Click("TopicsBack");Capture("return-home");
            Check(Menu.Control<Transform>("Home").gameObject.activeInHierarchy,"Returning home shows the original closed doorway");
            Check(GameManager.Instance.EnterLaboratory(()=>callbacks++),"The same Timeline can be played again after returning Home");
            deadline=Time.realtimeSinceStartup+12;while(actor.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(!actor.Busy&&callbacks==1&&actor.guide.localPosition==guideStart,"A repeated Director play emits exactly one covered signal and restores the guide");callbacks=0;
            actor.roomPreview.anchoredPosition+=new Vector2(7,-4);var editedPosition=actor.roomPreview.anchoredPosition;
            ScreenTransition.Active.reduceMotion=true;Check(GameManager.Instance.EnterLaboratory(()=>callbacks++),"Reduced-motion entrance can run again");
            yield return new WaitForSecondsRealtime(.05f);
            Check(actor.lobbyCamera.transform.localPosition==cameraPosition&&Quaternion.Angle(actor.leftHinge.localRotation,Quaternion.identity)<.01f,"Reduced motion keeps the camera and leaves stationary");
            deadline=Time.realtimeSinceStartup+5;while(actor.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(callbacks==1&&!actor.Busy&&actor.roomPreview.anchoredPosition==editedPosition&&!actor.lightCover.blocksRaycasts,"Exactly one callback, preserved manual layout and released input on a repeat entrance");
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: real Start raycast; saved hinged doors; camera passage; opaque light before page change; input/duplicate protection; unscaled timing; exact pose/layout restoration; repeat and reduced-motion entrance; no runtime construction.":"FAILURES: "+failures);
            Application.Quit(failures==0?0:1);
        }
    }
}
#endif
