using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // This Canvas is authored in each scene. One saved instance survives a scene
    // change; the destination scene's duplicate is discarded before rendering.
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class ScreenTransition : MonoBehaviour
    {
        public enum Effect { Circle, Fade, DiagonalWipe }
        public static ScreenTransition Active { get; private set; }
        public static bool IsBusy=>Active!=null&&Active.Busy;
        public static bool InstantQA {
            get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return Environment.GetCommandLineArgs().Contains("--instant-screen-transitions");
#else
                return false;
#endif
            }
        }
        [Header("Scene change")]
        public Effect effect=Effect.Circle;
        [Range(.05f,1.5f)] public float closeSeconds=.25f;
        [Range(.05f,1.5f)] public float openSeconds=.30f;
        public bool reduceMotion;
        [Header("Saved overlay references")]
        public Image overlay;
        public CanvasGroup inputShield;
        public Material transitionMaterial;
        public bool Busy { get; private set; }
        public string Phase { get; private set; }="Idle";
        public float Coverage { get; private set; }
        public string PendingScene { get; private set; }
        Material liveMaterial;
        string redirectScene;
        readonly List<BaseInputModule> blockedModules=new List<BaseInputModule>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActive()=>Active=null;
        void Awake()
        {
            if(Active!=null&&Active!=this){GetComponent<Canvas>().enabled=false;Destroy(gameObject);return;}
            Active=this;DontDestroyOnLoad(gameObject);
            if(overlay==null||inputShield==null||transitionMaterial==null)throw new InvalidOperationException("Assign the saved transition overlay, shield and material before Play.");
            liveMaterial=new Material(transitionMaterial);overlay.material=liveMaterial;SetCoverage(0);Block(false);
        }
        IEnumerator Start()
        {
            if(Active!=this||Busy||InstantQA)yield break;
            Busy=true;Phase="Opening";Block(true);SetCoverage(1);
            yield return Animate(1,0,openSeconds);Finish();
        }
        public bool LoadScene(string sceneName)
        {
            if(!Application.CanStreamedLevelBeLoaded(sceneName))return false;
            // A destination may reject a missing save during Awake/Start. Follow
            // that redirect while covered; user requests during Closing are ignored.
            if(Busy){if(Phase=="Loading"&&sceneName!=PendingScene){redirectScene=sceneName;return true;}return false;}
            if(InstantQA){SceneManager.LoadScene(sceneName);return true;}
            PendingScene=sceneName;Busy=true;Phase="Closing";Block(true);
            StartCoroutine(ChangeScene());return true;
        }
        IEnumerator ChangeScene()
        {
            yield return Animate(0,1,closeSeconds);
            Phase="Loading";SetCoverage(1);
            // Let the fully covered frame render before beginning the load.
            yield return new WaitForEndOfFrame();
            Exception failure=null;
            do
            {
                if(redirectScene!=null)PendingScene=redirectScene;redirectScene=null;AsyncOperation operation=null;
                try{operation=SceneManager.LoadSceneAsync(PendingScene,LoadSceneMode.Single);}
                catch(Exception error){failure=error;}
                if(operation!=null)yield return operation;
                // Destination UI Start methods bind the restored session before reveal.
                yield return null;yield return new WaitForEndOfFrame();
            }while(redirectScene!=null&&failure==null);
            Phase="Opening";Block(true);
            yield return Animate(1,0,openSeconds);Finish();
            if(failure!=null)Debug.LogException(failure);
        }
        IEnumerator Animate(float from,float to,float seconds)
        {
            double started=Time.realtimeSinceStartupAsDouble;float elapsed=0;seconds=reduceMotion?Mathf.Min(seconds,.14f):seconds;
            while(elapsed<seconds)
            {
                elapsed=(float)(Time.realtimeSinceStartupAsDouble-started);float t=Mathf.Clamp01(elapsed/seconds);
                SetCoverage(Mathf.Lerp(from,to,t*t*(3-2*t)));yield return null;
            }
            SetCoverage(to);
        }
        void SetCoverage(float value)
        {
            Coverage=value;if(liveMaterial==null)return;
            liveMaterial.SetFloat("_Progress",value);liveMaterial.SetFloat("_Effect",reduceMotion?1:(int)effect);
            liveMaterial.SetFloat("_Aspect",Screen.width/(float)Mathf.Max(1,Screen.height));
        }
        void Update(){if(Busy){Block(true);SetCoverage(Coverage);}}
        void Block(bool value)
        {
            inputShield.blocksRaycasts=value;overlay.raycastTarget=value;
            if(value)
            {
                var events=EventSystem.current;if(events==null)return;
                events.SetSelectedGameObject(null);
                foreach(var module in events.GetComponents<BaseInputModule>())
                    if(module.enabled){blockedModules.Add(module);module.enabled=false;}
            }
            else
            {
                foreach(var module in blockedModules)if(module!=null)module.enabled=true;
                blockedModules.Clear();
            }
        }
        void Finish(){SetCoverage(0);Busy=false;Phase="Idle";PendingScene=null;Block(false);}
        void OnDestroy()
        {
            if(Active!=this)return;
            Block(false);Active=null;if(liveMaterial!=null)Destroy(liveMaterial);
        }
    }
}
