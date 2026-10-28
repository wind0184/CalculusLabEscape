#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed partial class MainMenuPlaytest:MonoBehaviour
    {
        string folder; int failures;
        readonly List<string> checks=new List<string>();
        ProductionCanvas Menu=>ProductionCanvas.Active;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--main-menu-qa")&&!Environment.GetCommandLineArgs().Contains("--door-qa"))return;
            string folder=LearningApi.Argument("--qa-output");Directory.CreateDirectory(folder);
            Chapter4Store.IsolatedFolder=Path.Combine(folder,"sessions");
            var test=new GameObject("Main menu behaviour QA").AddComponent<MainMenuPlaytest>();test.folder=folder;
            Application.runInBackground=true;
        }
        void Awake()=>Application.logMessageReceived+=Log;
        void OnDestroy()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){failures++;if(folder!=null)File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool result,string message){checks.Add((result?"PASS: ":"FAIL: ")+message);if(!result)failures++;File.WriteAllLines(Path.Combine(folder,"checks.txt"),checks);}
        IEnumerator Click(string id)
        {
            var button=Menu.Control<Button>(id);Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(button.isActiveAndEnabled&&button.interactable&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Pointer reaches "+id);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.22f);
            float deadline=Time.realtimeSinceStartup+10;
            while(GameManager.Instance.menuEntrance!=null&&GameManager.Instance.menuEntrance.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        IEnumerator Start()
        {
            yield return null;float deadline=Time.realtimeSinceStartup+15;
            while(ScreenTransition.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            if(Environment.GetCommandLineArgs().Contains("--door-qa")){yield return DoorSequence();yield break;}
            var view=Menu.GetComponent<MainMenuPresentation>();var room=GameObject.Find("Main Menu Lobby");
            Check(room!=null&&room.GetComponentsInChildren<Renderer>().Length>100,"Saved editable corridor is present before any menu action");
            Check(!view.continueButton.gameObject.activeSelf,"No unfinished session: Continue is hidden");
            // Use a friendly display name in the reference screenshot; data stays in the isolated test folder.
            view.profileInput.text="Local player";Menu.OnProfileEdited(view.profileInput.text);yield return null;
            Capture("01-main-menu");
            foreach(var text in Menu.GetComponentsInChildren<TMP_Text>())if(text.GetComponentInParent<TMP_InputField>()==null)Check(!text.isTextOverflowing,"Home text fits: "+text.name);
            yield return Click("MenuProfile");Check(view.profileModal.activeSelf,"Profile button opens its saved modal");Capture("02-profile");
            view.profileInput.text="";yield return Click("MenuProfileDone");Check(!view.profileModal.activeSelf&&Chapter4Store.Profile=="Local player","Closing an empty name restores the previous valid profile");
            yield return Click("HomeStart");Check(Menu.Control<Transform>("Topics").gameObject.activeInHierarchy,"Start opens the separate three-topic page");Capture("03-topics");yield return Click("TopicsBack");
            foreach(var route in new[]{new[]{"HomeRecords","Records","RecordsBack"},new[]{"HomeInstructions","Instructions","InstructionsBack"},new[]{"HomeSettings","Settings","SettingsBack"},new[]{"HomeExit","Exit","ExitCancel"}}){
                yield return Click(route[0]);Check(Menu.Control<Transform>(route[1]).gameObject.activeInHierarchy,"Existing route opens "+route[1]);yield return Click(route[2]);
            }
            var fixture=new Chapter4Session{profile=Chapter4Store.Profile,topic=1,required=18};Chapter4Store.Save(fixture);Menu.OnProfileEdited(view.profileInput.text);yield return null;
            Check(view.continueButton.gameObject.activeSelf&&view.continueButton.interactable,"An unfinished session reveals Continue");Capture("04-resume");
            fixture.status="Completed";Chapter4Store.Save(fixture);Menu.OnProfileEdited(view.profileInput.text);yield return null;
            Check(!view.continueButton.gameObject.activeSelf,"Completed sessions hide Continue again");
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: saved corridor; actual button raycasts and routes; profile modal and blank-name recovery; conditional resume; home text fits.":"FAILURES: "+failures);Application.Quit(failures==0?0:1);
        }
        void Capture(string name)
        {
            var roots=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.isActiveAndEnabled).ToArray();
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
                if(name=="01-main-menu"){
                    var samples=new List<Color32>();for(int y=240;y<480;y+=31)for(int x=100;x<700;x+=43)samples.Add(pixels.GetPixel(x,y));
                    Check(samples.Select(c=>(int)c.r*65536+c.g*256+c.b).Distinct().Count()>12&&samples.Average(c=>(c.r+c.g+c.b)/3f)<100,"Actual composite shows the corridor through its transparent edge fade");
                }

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
