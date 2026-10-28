using UnityEngine;

namespace CalculusLabEscape
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class BridgeBoundaryGate:MonoBehaviour
    {
        public BridgeRescueSequence rescue;
        public Transform left,right;
        [Range(80,120)] public float openAngle=100;
        void OnEnable()=>Apply();
        void OnValidate()=>Apply();
        void LateUpdate()=>Apply();
        public void Apply()=>Preview(rescue!=null?rescue.Progress:0);
        public void Preview(float progress)
        {
            float angle=openAngle*Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/.08f));
            if(left!=null&&Quaternion.Angle(left.localRotation,Quaternion.Euler(0,-angle,0))>.001f)left.localRotation=Quaternion.Euler(0,-angle,0);
            if(right!=null&&Quaternion.Angle(right.localRotation,Quaternion.Euler(0,angle,0))>.001f)right.localRotation=Quaternion.Euler(0,angle,0);
        }
    }
}
