using System;
using UnityEngine;

namespace CalculusLabEscape
{
    // Authored transforms also drive the Edit Mode preview; no animation session is created.
    public sealed class BridgeRescueSequence : MonoBehaviour
    {
        [Serializable]
        public sealed class AnimalRig
        {
            public Transform root, head, tail;
            public Transform[] legs;
            public Vector3 homePosition;
            public Quaternion homeRotation, headRest, tailRest;
            public Quaternion[] legRest;
            public void Reset()
            {
                root.SetPositionAndRotation(homePosition,homeRotation);
                head.localRotation=headRest;tail.localRotation=tailRest;
                for(int i=0;i<legs.Length;i++)legs[i].localRotation=legRest[i];
            }
            public void Pose(float cycle,float strength,float nod)
            {
                for(int i=0;i<legs.Length;i++)
                    legs[i].localRotation=legRest[i]*Quaternion.Euler(Mathf.Sin(cycle+(i==0||i==3?0:Mathf.PI))*23*strength,0,0);
                head.localRotation=headRest*Quaternion.Euler(nod,0,0);
                tail.localRotation=tailRest*Quaternion.Euler(0,0,Mathf.Sin(cycle*.6f)*12*strength);
            }
        }
        [Min(3)] public float duration=6;
        public AnimalRig calf, mother;
        [Tooltip("Start, entrance, far bank, reunion. Move these in the Scene view.")]
        public Transform[] route;
        public Transform cameraPose, cameraTarget;
        [HideInInspector] public Vector3 playerCameraHomePosition;
        [HideInInspector] public Quaternion playerCameraHomeRotation;
        [Tooltip("Height of the restored bridge deck above the bank.")]
        public float deckHeight=.19f;
        public float Progress {get;private set;}
        public float Duration=>Mathf.Max(3,duration);
        static float Ease(float from,float to,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,t));
        public void ResetPose()
        {
            Progress=0;calf.Reset();mother.Reset();
            var camera=GetComponent<Chapter4World>().player.GetComponentInChildren<Camera>().transform;
            camera.SetLocalPositionAndRotation(playerCameraHomePosition,playerCameraHomeRotation);
        }
        public void Sample(float progress,bool moveCamera=true)
        {
            Progress=Mathf.Clamp01(progress);float t=Progress;
            Vector3 a,b;float blend;
            if(t<.19f){a=route[0].position;b=route[1].position;blend=t/.19f;}
            else if(t<.70f){a=route[1].position;b=route[2].position;blend=(t-.19f)/.51f;}
            else{a=route[2].position;b=route[3].position;blend=Mathf.Clamp01((t-.70f)/.14f);}
            var p=Vector3.Lerp(a,b,blend);
            float deck=Ease(route[1].position.z-.25f,route[1].position.z+.2f,p.z)
                *(1-Ease(route[2].position.z-.35f,route[2].position.z,p.z));
            float walking=1-Ease(.78f,.86f,t),cycle=t*Duration*11;
            p.y+=deck*deckHeight+Mathf.Abs(Mathf.Sin(cycle))*.018f*walking;
            calf.root.position=p;
            var toward=Quaternion.LookRotation(-(b-a).normalized,Vector3.up);
            if(t<.19f)toward=Quaternion.Slerp(toward,Quaternion.LookRotation(-(route[2].position-route[1].position).normalized),Ease(.14f,.19f,t));
            if(t<.06f)toward=Quaternion.Slerp(calf.homeRotation,toward,Ease(0,.06f,t));
            if(t>=.70f)toward=Quaternion.Slerp(Quaternion.LookRotation(-(route[2].position-route[1].position).normalized),toward,Ease(.70f,.76f,t));
            float reunion=Ease(.84f,.95f,t);
            calf.root.rotation=Quaternion.Slerp(toward,Quaternion.LookRotation(-(mother.homePosition-p).normalized),reunion);
            mother.root.SetPositionAndRotation(mother.homePosition,Quaternion.Slerp(mother.homeRotation,Quaternion.LookRotation(-(route[3].position-mother.homePosition).normalized),Ease(.70f,.91f,t)));
            calf.Pose(cycle,walking,Mathf.Sin(t*Duration*5)*3*walking+reunion*7);
            mother.Pose(0,0,reunion*5);
            if(moveCamera)
            {
                var camera=GetComponent<Chapter4World>().player.GetComponentInChildren<Camera>();
                camera.transform.position=cameraPose.position;camera.transform.LookAt(cameraTarget.position);
            }
        }
        void OnDrawGizmosSelected()
        {
            if(route==null)return;Gizmos.color=new Color(.2f,.85f,.9f);
            for(int i=0;i<route.Length;i++)if(route[i]!=null){Gizmos.DrawSphere(route[i].position,.13f);if(i>0&&route[i-1]!=null)Gizmos.DrawLine(route[i-1].position,route[i].position);}
            if(cameraPose!=null&&cameraTarget!=null)Gizmos.DrawLine(cameraPose.position,cameraTarget.position);
        }
    }
}
