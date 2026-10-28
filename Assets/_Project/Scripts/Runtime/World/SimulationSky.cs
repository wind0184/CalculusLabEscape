using UnityEngine;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class SimulationSky:MonoBehaviour
    {
        public Material environment;
        public Color fogTint=new Color(.07f,.21f,.27f);
        [Range(0,.03f)] public float fogDensity=.007f;
        void OnEnable()=>Apply();
        void Start()=>Apply();
        void OnValidate()=>Apply();
        void Update()
        {
            if(environment!=null&&gameObject.scene==SceneManager.GetActiveScene()&&
                (RenderSettings.skybox!=environment||Mathf.Abs(RenderSettings.fogDensity-fogDensity)>.00001f||RenderSettings.fogColor!=fogTint))Apply();
        }
        [ContextMenu("Apply simulation sky")]
        public void Apply()
        {
            if(environment==null||gameObject.scene!=SceneManager.GetActiveScene())return;
            RenderSettings.skybox=environment;RenderSettings.fogColor=fogTint;RenderSettings.fogDensity=fogDensity;
            foreach(var camera in GetComponentsInChildren<Camera>(true))camera.clearFlags=CameraClearFlags.Skybox;
        }
    }
}
