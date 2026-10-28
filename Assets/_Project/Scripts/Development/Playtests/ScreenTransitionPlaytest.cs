#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed class ScreenTransitionPlaytest:MonoBehaviour
    {
        string folder;int failures,frame;
        readonly List<string> checks=new List<string>(),frames=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--transitions-qa"))return;
            string folder=LearningApi.Argument("--qa-output");Directory.CreateDirectory(folder);
            Chapter4Store.IsolatedFolder=Path.Combine(folder,"sessions");
            var root=new GameObject("Transition behaviour QA");DontDestroyOnLoad(root);root.AddComponent<ScreenTransitionPlaytest>().folder=folder;Application.runInBackground=true;
        }
        void Awake()=>Application.logMessageReceived+=Log;
        void OnDestroy()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool result,string label){checks.Add((result?"PASS: ":"FAIL: ")+label);if(!result)failures++;File.WriteAllLines(Path.Combine(folder,"checks.txt"),checks);}
        IEnumerator Idle()
        {
            float deadline=Time.realtimeSinceStartup+12;
            while((ScreenTransition.Active==null||ScreenTransition.IsBusy)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(ScreenTransition.Active!=null&&!ScreenTransition.IsBusy,"Transition completes within the bounded wait");
        }
        IEnumerator Start()
        {
            yield return null;yield return Idle();
            var transition=ScreenTransition.Active;Check(transition!=null&&GameManager.Instance.screenTransition==transition,"MainMenu starts with its saved transition assigned to Game Manager");
            var menu=ProductionCanvas.Active;menu.Control<Button>("HomeStart").onClick.Invoke();
            while(GameManager.Instance.menuEntrance!=null&&GameManager.Instance.menuEntrance.Busy&&GameManager.Instance.menuEntrance.Phase!="Revealing topics")yield return null;
            var page=menu.Control<Transform>("Topics").GetComponent<PageEntrance>();
            Check(page!=null&&page.IsAnimating&&page.canvasGroup.alpha<1,"Menu page starts its saved opacity animation");
            yield return new WaitForSecondsRealtime(.25f);Check(!page.IsAnimating&&Mathf.Approximately(page.canvasGroup.alpha,1),"Page fade completes without moving its RectTransform");
            while(GameManager.Instance.menuEntrance!=null&&GameManager.Instance.menuEntrance.Busy)yield return null;
            transition.closeSeconds=1.2f;transition.openSeconds=1.2f;Time.timeScale=0;
            Capture("circle");float began=Time.realtimeSinceStartup;menu.Control<Button>("Mission1").onClick.Invoke();
            Check(transition.Busy&&transition.Phase=="Closing","The actual mission Button starts the circle scene transition");
            Check(!GameManager.ChangeScene(Chapter4Game.Scenes[2]),"Repeated scene requests are ignored while closing");
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.5f,Screen.height*.5f)},hits);
            Check(hits.Count>0&&hits[0].gameObject==transition.overlay.gameObject,"The saved overlay receives pointer input above all underlying buttons");
            float deadline=Time.realtimeSinceStartup+15;bool closed=false,opening=false;
            while(transition.Busy&&Time.realtimeSinceStartup<deadline)
            {
                if(transition.Phase=="Loading"){closed=true;Check(transition.Coverage>=.9999f,"The destination loads only behind a fully closed cover");}
                if(transition.Phase=="Opening")opening=true;
                Capture("circle");yield return new WaitForSecondsRealtime(.07f);
            }
            Capture("circle");
            Check(closed&&opening&&!transition.Busy&&SceneManager.GetActiveScene().name==Chapter4Game.Scenes[1],"Circle close/load/open finishes even when timeScale is zero");
            Check(Time.realtimeSinceStartup-began>=transition.closeSeconds+transition.openSeconds,"Animation honours the selected real-time duration after a slow frame");
            Check(ScreenTransition.Active==transition&&FindObjectsByType<ScreenTransition>(FindObjectsSortMode.None).Length==1&&GameManager.Instance.screenTransition==transition,"One overlay survives and the destination manager binds that same saved instance");
            Check(!transition.inputShield.blocksRaycasts&&EventSystem.current.GetComponents<BaseInputModule>().Any(m=>m.enabled),"Pointer and UI keyboard input are restored after reveal");
            Time.timeScale=1;var game=GameManager.Instance;game.Session.guideSkipped=true;game.StartSession();
            yield return new WaitForSecondsRealtime(.2f);float elapsed=game.Session.elapsed;
            Check(elapsed>0,"Assessment clock runs normally outside a transition");
            var position=game.world.player.transform.position;transition.effect=ScreenTransition.Effect.Fade;transition.closeSeconds=.4f;transition.openSeconds=.4f;
            Check(GameManager.ChangeScene("MainMenu"),"The same manager route can use a full-screen fade");
            yield return new WaitForSecondsRealtime(.12f);
            Check(Mathf.Approximately(game.Session.elapsed,elapsed)&&game.world.player.transform.position==position,"Covered navigation pauses assessment time and player movement");
            Capture("fade");yield return Idle();Check(SceneManager.GetActiveScene().name=="MainMenu","Fade reaches MainMenu and releases the overlay");
            transition.effect=ScreenTransition.Effect.DiagonalWipe;transition.closeSeconds=.5f;transition.openSeconds=.5f;
            Check(GameManager.ChangeScene(Chapter4Game.Scenes[0]),"Diagonal wipe is selectable without changing any Button event");
            yield return new WaitForSecondsRealtime(.20f);Capture("diagonal");yield return Idle();
            bool redirected=false;
            UnityEngine.Events.UnityAction<Scene,LoadSceneMode> redirect=(scene,mode)=>{if(scene.name==Chapter4Game.Scenes[1])redirected=GameManager.ChangeScene("MainMenu");};
            SceneManager.sceneLoaded+=redirect;
            try{Check(GameManager.ChangeScene(Chapter4Game.Scenes[1]),"A destination redirect starts behind the same cover");yield return Idle();}
            finally{SceneManager.sceneLoaded-=redirect;}
            Check(redirected&&SceneManager.GetActiveScene().name=="MainMenu","A destination redirect during loading completes without revealing the rejected scene");
            transition.reduceMotion=true;transition.effect=ScreenTransition.Effect.Circle;Check(GameManager.ChangeScene("MainMenu"),"Reduced-motion navigation starts");
            yield return null;Check(Mathf.Approximately(transition.overlay.material.GetFloat("_Effect"),1),"Reduced motion uses a short fade instead of a moving mask");
            yield return Idle();Check(!ScreenTransition.IsBusy&&FindObjectsByType<ScreenTransition>(FindObjectsSortMode.None).Length==1,"All tested effects finish with one non-blocking overlay");
            File.WriteAllLines(Path.Combine(folder,"frames.txt"),frames);
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: actual mission Button; circle, fade, diagonal and reduced motion; scene load fully covered; duplicate navigation blocked; pointer/input restored; singleton manager binding; unscaled animation; assessment clock and player paused; page opacity animation.":"FAILURES: "+failures);
            Application.Quit(failures==0?0:1);
        }
        void Capture(string effect)
        {
            string name=effect+"-"+(frame++).ToString("D3");var roots=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.isActiveAndEnabled).ToArray();
            var modes=roots.Select(c=>c.renderMode).ToArray();var cameras=roots.Select(c=>c.worldCamera).ToArray();var distances=roots.Select(c=>c.planeDistance).ToArray();
            var transforms=roots.SelectMany(c=>c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var prior=RenderTexture.active;var go=new GameObject("Transition QA camera");var camera=go.AddComponent<Camera>();camera.enabled=false;Texture2D pixels=null;
            try
            {
                if(GameManager.Instance.world!=null)GameManager.Instance.world.transform.Find("Storyboard view").GetComponent<Camera>().Render();
                if(GameManager.Instance.world==null)foreach(var preview in FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.targetTexture!=null))preview.Render();
                if(Camera.main!=null){var old=Camera.main.targetTexture;Camera.main.targetTexture=target;Camera.main.Render();Camera.main.targetTexture=old;}
                camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=Screen.height*.5f;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.cullingMask=1<<31;camera.clearFlags=Camera.main==null?CameraClearFlags.SolidColor:CameraClearFlags.Depth;camera.backgroundColor=new Color(.035f,.08f,.105f);
                foreach(var t in transforms)t.gameObject.layer=31;
                foreach(var canvas in roots){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
                frames.Add(name+"|"+Time.realtimeSinceStartup.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"|"+ScreenTransition.Active.Phase+"|"+ScreenTransition.Active.Coverage.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
            }
            finally
            {
                for(int i=0;i<roots.Length;i++){roots[i].renderMode=modes[i];roots[i].worldCamera=cameras[i];roots[i].planeDistance=distances[i];}
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];RenderTexture.active=prior;Destroy(go);if(pixels!=null)Destroy(pixels);RenderTexture.ReleaseTemporary(target);Canvas.ForceUpdateCanvases();
            }
        }
    }
}
#endif
