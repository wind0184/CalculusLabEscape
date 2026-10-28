using UnityEngine;

namespace CalculusLabEscape
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class RepairStationIndicator:MonoBehaviour
    {
        public Renderer source;
        public Renderer[] lights=new Renderer[0];
        void OnEnable()=>Apply();
        void OnValidate()=>Apply();
        void LateUpdate()
        {
            if(source==null)return;
            foreach(var light in lights)if(light!=null&&light.sharedMaterial!=source.sharedMaterial){Apply();break;}
        }
        public void Apply()
        {
            if(source==null||source.sharedMaterial==null)return;
            foreach(var light in lights)if(light!=null)light.sharedMaterial=source.sharedMaterial;
        }
    }
}
