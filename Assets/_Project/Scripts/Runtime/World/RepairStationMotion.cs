using UnityEngine;

namespace CalculusLabEscape
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class RepairStationMotion:MonoBehaviour
    {
        public Chapter4World world;
        public Transform generatorRotor,winch;
        public Transform[] channelFlows=new Transform[0];
        public Renderer[] channelSources=new Renderer[0];
        public Material flowMaterial;
        [Min(0)] public float generatorSpeed=120,winchSpeed=220,flowSpeed=.9f;
        [HideInInspector] public Quaternion generatorRest=Quaternion.identity,winchRest=Quaternion.identity;
        [HideInInspector] public Vector3[] flowOrigins=new Vector3[0];
        float rotorAngle,winchAngle,flowTime,rotorSpeed;
        bool started;

        void OnEnable(){started=false;}
        void LateUpdate()
        {
            if(!Application.isPlaying||world==null)return;
            var game=world.Game;if(game==null)return;
            if(!started){ResetPose();started=true;Apply(game.Stage,0,0,0);}
            if((game.State!="Explore"&&game.State!="Puzzle"&&game.State!="Finishing")||game.CanvasSupportOpen)return;
            rotorSpeed=Mathf.MoveTowards(rotorSpeed,game.Stage>=1?generatorSpeed:0,Time.deltaTime*180);
            rotorAngle=Mathf.Repeat(rotorAngle+rotorSpeed*Time.deltaTime,360);
            if(game.Stage>=4&&world.BridgeInstallationMoving)winchAngle=Mathf.Repeat(winchAngle+winchSpeed*Time.deltaTime,360);
            if(game.Stage>=2)flowTime+=Time.deltaTime;
            Apply(game.Stage,rotorAngle,winchAngle,flowTime);
        }

        public void ResetPose()
        {
            rotorAngle=winchAngle=flowTime=rotorSpeed=0;
            Apply(0,0,0,0);
        }

        // Samples decorative transforms only. No task, bridge, collider or record is changed.
        public void Sample(int stage,float seconds,bool installing)
        {
            seconds=Mathf.Max(0,seconds);
            Apply(stage,stage>=1?seconds*generatorSpeed:0,installing&&stage>=4?seconds*winchSpeed:0,seconds);
        }

        void Apply(int stage,float rotor,float crank,float seconds)
        {
            if(generatorRotor!=null)generatorRotor.localRotation=generatorRest*Quaternion.AngleAxis(rotor,Vector3.forward);
            if(winch!=null)winch.localRotation=winchRest*Quaternion.AngleAxis(crank,Vector3.forward);
            for(int i=0;i<channelFlows.Length;i++)
            {
                var flow=channelFlows[i];if(flow==null||i>=flowOrigins.Length)continue;
                bool active=stage>=i+2;
                flow.localPosition=flowOrigins[i]+Vector3.up*(active?Mathf.Repeat(seconds*flowSpeed+i/3f,1)*.38f:0);
                var renderer=flow.GetComponent<Renderer>();if(renderer==null)continue;
                renderer.enabled=active;
                var material=Application.isPlaying&&i<channelSources.Length&&channelSources[i]!=null?channelSources[i].sharedMaterial:flowMaterial;
                if(material!=null&&renderer.sharedMaterial!=material)renderer.sharedMaterial=material;
            }
        }
    }
}
