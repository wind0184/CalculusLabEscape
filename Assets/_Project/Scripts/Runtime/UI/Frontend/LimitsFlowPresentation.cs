using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Presentation of a calibration already accepted by Game Manager.
    // The camera, overlay and progress segments are saved before Play.
    public sealed class LimitsFlowPresentation:MonoBehaviour
    {
        public GameManager gameManager;
        public GameObject overlay;
        public CanvasGroup cover;
        public Camera portalCamera;
        public RawImage portalPreview;
        public Renderer[] ringSections;
        public Image[] chargeSegments;
        public TMP_Text charge,calibration,nextTask,worldObjective,summaryAdvice;
        [Range(.7f,3)] public float holdSeconds=1.25f;
        public bool Busy { get; private set; }
        readonly List<BaseInputModule> blockedModules=new List<BaseInputModule>();
        readonly List<(Renderer target,MaterialPropertyBlock original,Color colour)> pulsing=new List<(Renderer,MaterialPropertyBlock,Color)>();
        MaterialPropertyBlock pulse;
        static readonly string[] NextTitles={"Read the approaching value","Compare both sides","Limit versus function value","Choose a conclusion"};

        void LateUpdate()
        {
            if(gameManager==null||gameManager.Session==null)return;
            if(gameManager.State=="Explore"){
                float distance=Vector3.Distance(gameManager.world.player.transform.position,gameManager.world.station.position);
                worldObjective.text=gameManager.CanvasNearStation?(gameManager.Ready?"READY: Press E to activate the restored portal.":"IN RANGE: Press E to inspect the portal console."):"NEXT: Reach the portal console  |  "+distance.ToString("0.0")+" m away";
            }
            if(gameManager.State=="Summary")summaryAdvice.text=MissionFeedback.LimitsReviewAdvice(gameManager.Session.CommonError);
        }
        public void ShowCalibration(int completed)
        {
            if(Busy)return;
            Busy=true;charge.text=completed+" / 5";calibration.text="CALIBRATION "+completed+" / 5 COMPLETE";
            nextTask.text=completed==5?"Portal stable. Next: activate the portal.":"Next: "+NextTitles[completed-1]+".";
            for(int i=0;i<chargeSegments.Length;i++)chargeSegments[i].color=i<completed?new Color(.35f,.9f,.85f):new Color(.15f,.25f,.29f);
            pulsing.Clear();pulse=new MaterialPropertyBlock();
            for(int i=(completed-1)*8;i<completed*8&&i<ringSections.Length;i++){
                var renderer=ringSections[i];var original=new MaterialPropertyBlock();renderer.GetPropertyBlock(original);pulsing.Add((renderer,original,renderer.sharedMaterial.color));
            }
            overlay.SetActive(true);cover.alpha=0;cover.blocksRaycasts=true;portalCamera.enabled=true;BlockInput();StartCoroutine(Present());
        }
        IEnumerator Present()
        {
            // Existing regression suites can request instant presentation; normal play never does.
            if(!ScreenTransition.InstantQA){
                double began=Time.realtimeSinceStartupAsDouble;float duration=holdSeconds+.24f;
                while(Time.realtimeSinceStartupAsDouble-began<duration){
                    float time=(float)(Time.realtimeSinceStartupAsDouble-began);
                    cover.alpha=Mathf.Min(Mathf.SmoothStep(0,1,time/.12f),Mathf.SmoothStep(0,1,(duration-time)/.12f));
                    float brightness=Mathf.Sin(Mathf.Clamp01(time/duration)*Mathf.PI);
                    foreach(var section in pulsing){section.target.GetPropertyBlock(pulse);pulse.SetColor("_Color",Color.Lerp(section.colour,new Color(.03f,.9f,1),brightness));section.target.SetPropertyBlock(pulse);}
                    yield return null;
                }
            }
            Finish();
        }
        void BlockInput()
        {
            var events=EventSystem.current;if(events==null)return;events.SetSelectedGameObject(null);
            foreach(var module in events.GetComponents<BaseInputModule>())if(module.enabled){blockedModules.Add(module);module.enabled=false;}
        }
        void Finish()
        {
            foreach(var section in pulsing)if(section.target!=null)section.target.SetPropertyBlock(section.original);
            pulsing.Clear();if(portalCamera!=null)portalCamera.enabled=false;
            if(cover!=null){cover.alpha=0;cover.blocksRaycasts=false;}if(overlay!=null)overlay.SetActive(false);
            foreach(var module in blockedModules)if(module!=null)module.enabled=true;blockedModules.Clear();Busy=false;
        }
        void OnDisable(){if(!Busy)return;StopAllCoroutines();Finish();}
    }
}
