using System.Collections.Generic;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class Chapter4World
    {
        public static readonly string[] RepairDeviceNames={"Power Core","Converter Box","Bridge Builder"};
        [SerializeField] List<Renderer> repairLights=new List<Renderer>();
        [SerializeField] List<Renderer> converterLocks=new List<Renderer>();
        [SerializeField] List<TextMesh> repairSigns=new List<TextMesh>();
        [SerializeField] Material repairActive,repairComplete,repairWarning;
        float bridgeLift;
        [SerializeField,Min(0)] float bridgeInstallSoundDelay=.36f;
        float bridgeSoundPending=-1;
        int bridgeStage=-1;
        public bool BridgeInstallationMoving
        {
            get
            {
                if(topic!=1||bridgeLift<=0)return false;
                int sections=restoredCount>=4?9:restoredCount*2;
                for(int i=0;i<bridge.Count;i++)if(Mathf.Abs(bridge[i].position.y-(i<sections?.08f:-1.1f))>.001f)return true;
                return false;
            }
        }

        void InitialiseBridgeFeedback()
        {
            repairActive=Mat("Active repair - cyan",new Color(.12f,.8f,.9f),true);
            repairComplete=Mat("Installed repair - green",new Color(.15f,.48f,.27f));
            repairWarning=Mat("Unfinished repair - amber",new Color(1f,.55f,.12f),true);
            SetBridgeStage(0);
        }

        public void SetBridgeStage(int stage)
        {
            if(topic!=1||bridgeStage==stage)return;
            bridgeStage=stage;int active=stage==0?0:stage<4?1:2;
            for(int i=0;i<3;i++)
            {
                bool done=stage>=(i==0?1:i==1?4:11);
                repairLights[i].sharedMaterial=done?repairComplete:i==active?repairActive:amber;
                string state=done?"COMPLETE":i!=active?"LOCKED":i==2&&stage>=8?"STABILISE CROSSING":"ACTIVE";
                repairSigns[i].text=(i+1).ToString("00")+" / "+RepairDeviceNames[i].ToUpperInvariant()+"\n"+state;
                repairSigns[i].color=done?new Color(.45f,.94f,.61f):new Color(.82f,.95f,.92f);
            }
            for(int i=0;i<converterLocks.Count;i++)
            {
                bool open=stage>=i+2;
                converterLocks[i].sharedMaterial=open?repairComplete:dark;
                converterLocks[i].transform.position=devices[1].position+new Vector3((i-1)*.52f,open?1.12f:.84f,0);
            }
            if(waypoint!=null)waypoint.GetComponent<Renderer>().sharedMaterial=stage>=11?repairComplete:repairActive;
        }

        void UpdateBridgeFeedback()
        {
            var camera=player.GetComponentInChildren<Camera>();
            var game=Game;bool cinematic=game!=null&&(game.State=="Finishing"||game.State=="Summary");
            bool paused=game==null||game.State=="Paused"||game.CanvasSupportOpen||game.PresentationBusy;
            if(CaseAudio!=null)CaseAudio.SetPaused(paused);
            int active=bridgeStage==0?0:bridgeStage<4?1:2;
            for(int i=0;i<repairSigns.Count;i++)
            {
                repairSigns[i].gameObject.SetActive(!cinematic&&(i==active||Vector3.Distance(player.transform.position,devices[i].position)<4));
                if(camera!=null)repairSigns[i].transform.rotation=camera.transform.rotation;
            }
            if(waypoint!=null){waypoint.gameObject.SetActive(!cinematic);waypoint.GetComponent<Renderer>().sharedMaterial=bridgeStage>=11?repairComplete:repairActive;}
            if(paused)return;
            if(bridgeSoundPending>=0)
            {
                bridgeSoundPending-=Time.deltaTime;
                if(bridgeSoundPending<=0){bridgeSoundPending=-1;CaseAudio?.Play("bridge");}
            }
            if(bridgeLift<=0)return;
            bridgeLift=Mathf.Max(0,bridgeLift-Time.deltaTime);
            int sections=restoredCount>=4?9:restoredCount*2;
            for(int i=0;i<bridge.Count;i++)
            {
                var p=bridge[i].position;p.y=Mathf.MoveTowards(p.y,i<sections?.08f:-1.1f,Time.deltaTime*1.6f);bridge[i].position=p;
            }
        }

        public string RepairNavigation(bool nearby)
        {
            int index=bridgeStage==0?0:bridgeStage<4?1:2;
            string name=RepairDeviceNames[index];
            if(nearby)return "[E]  "+(bridgeStage>=11?"OPEN RESCUE CONTROLS": "OPEN "+name.ToUpperInvariant());
            var offset=station.position-player.transform.position;offset.y=0;
            float angle=Vector3.SignedAngle(player.transform.forward,offset,Vector3.up);
            string direction=Mathf.Abs(angle)>145?"TURN AROUND":angle>25?"TURN RIGHT >":angle< -25?"< TURN LEFT":"AHEAD";
            return direction+"  /  "+name+"  /  "+offset.magnitude.ToString("0.0")+" m";
        }
    }
}
