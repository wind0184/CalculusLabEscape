using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class CaseShutdownSequence:MonoBehaviour
    {
        [Min(3)] public float duration=4;
        [Range(0,1)] public float soundStartProgress=.24f;
        public Transform lever,cameraPose,cameraTarget;
        public Renderer[] powerBars;
        public Material powerOn,powerOff;
        public TextMesh deviceStatus;
        [HideInInspector] public Vector3 playerCameraHomePosition;
        [HideInInspector] public Quaternion playerCameraHomeRotation,leverRest;
        [HideInInspector] public int playerCameraHomeMask;
        public float Progress {get;private set;}
        public int PowerOffCount {get;private set;}
        public float Duration=>Mathf.Max(3,duration);
        public int Phase=>Progress<.24f?0:Progress<.75f?1:2;
        public bool Safe=>Progress>=.91f;
        public string Title=>Safe?"DEVICE DEACTIVATED":Phase==0?"MASTER SWITCH":Phase==1?"POWERING DOWN":"VERIFYING POWER";
        public string Description=>Safe?"All four modules complete. Device power is off.":"Modules 4/4 complete. Device power: "+PowerOffCount+"/4 off.";
        public string PhaseLabel(int index)
        {
            string label=new[]{"Master switch","Power off","Device safe"}[index];
            return (index<Phase||Safe?"[OK] ":index==Phase?"[NOW] ":"[WAIT] ")+label;
        }
        public void ResetPose()
        {
            Progress=0;PowerOffCount=0;lever.localRotation=leverRest;
            foreach(var bar in powerBars)bar.sharedMaterial=powerOn;
            var camera=GetComponent<Chapter4World>().player.GetComponentInChildren<Camera>();
            camera.transform.SetLocalPositionAndRotation(playerCameraHomePosition,playerCameraHomeRotation);camera.cullingMask=playerCameraHomeMask;
            SetReadiness(0,false);
        }
        public void SetReadiness(int modules,bool cover)
        {
            deviceStatus.text="MODULES "+modules+"/4\n"+(modules<4?"MASTER LOCKED":cover?"MASTER ARMED":"MASTER READY");
            deviceStatus.color=modules==4?new Color(.45f,.94f,.61f):new Color(.6f,.85f,.9f);
        }
        public void SyncStatus(Chapter4Game game)
        {
            if(game.State=="Finishing"||game.State=="Summary")return;
            SetReadiness(game.ModulesComplete,GetComponent<Chapter4World>().MasterCoverOpen);
        }
        public void Sample(float progress)
        {
            Progress=Mathf.Clamp01(progress);PowerOffCount=0;
            lever.localRotation=leverRest*Quaternion.Euler(Mathf.Lerp(0,65,Mathf.SmoothStep(0,1,Mathf.Clamp01(Progress/.24f))),0,0);
            for(int i=0;i<powerBars.Length;i++)
            {
                bool off=Progress>=.30f+i*.14f;powerBars[i].sharedMaterial=off?powerOff:powerOn;if(off)PowerOffCount++;
            }
            deviceStatus.text=Safe?"DEVICE SAFE\nPOWER OFF":"DEVICE SHUTDOWN\nPOWER "+PowerOffCount+"/4 OFF";
            deviceStatus.color=Safe?new Color(.45f,.94f,.61f):new Color(.6f,.85f,.9f);
            var camera=GetComponent<Chapter4World>().player.GetComponentInChildren<Camera>();
            camera.cullingMask&=~(1<<2);camera.transform.position=cameraPose.position;camera.transform.LookAt(cameraTarget.position);
        }
        void OnDrawGizmosSelected()
        {if(cameraPose!=null&&cameraTarget!=null){Gizmos.color=Color.cyan;Gizmos.DrawLine(cameraPose.position,cameraTarget.position);}}
    }
}
