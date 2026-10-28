#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Opt-in development QA. Uses real Canvas raycasts, pointer handlers and queued Input System keys.
    // Persistence uses an isolated folder; no learner records or PlayerPrefs are changed.
    public sealed class CanvasPlaytest : MonoBehaviour
    {
        string folder;
        int failures;
        bool finished;
        bool auth;
        [Serializable] sealed class TestCredentials { public string username,teacher,password; }
        TestCredentials credentials;
        ProductionCanvas View => ProductionCanvas.Active;
        Chapter4Game Game => View.Game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("--canvas-qa")&&!args.Contains("--auth-qa"))return;
            int at=Array.IndexOf(args,"--qa-output");if(at<0||at+1>=args.Length)throw new Exception("Canvas QA requires --qa-output.");
            string folder=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(folder);
            bool auth=args.Contains("--auth-qa");
            if(!auth)Chapter4Store.IsolatedFolder=Path.Combine(folder,"sessions-"+Guid.NewGuid().ToString("N"));
            else if(string.IsNullOrEmpty(LearningApi.Argument("--auth-cache")))throw new Exception("Auth QA requires an isolated --auth-cache.");
            var go=new GameObject("Canvas interaction QA");DontDestroyOnLoad(go);var qa=go.AddComponent<CanvasPlaytest>();qa.folder=folder;
            qa.auth=auth;
            if(auth)qa.credentials=JsonUtility.FromJson<TestCredentials>(File.ReadAllText(LearningApi.Argument("--qa-credentials")));
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground=true;
            if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
            if(Mouse.current==null)InputSystem.AddDevice<Mouse>();
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failures++;if(folder!=null)File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}}
        void Check(bool condition,string message){if(!condition){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),"ASSERT: "+message+"\n");}}
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            foreach(var label in View.GetComponentsInChildren<TMP_Text>())if(label.isTextOverflowing&&label.name!="DetailAttempts"&&label.GetComponentInParent<TMP_InputField>()==null)Check(false,"Visible text overflows in "+name+": "+label.name);
            // Hidden Windows players can return a black swapchain. Render the same Canvas and
            // world into a real Unity render target for visual QA, then restore all UI state.
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);
            var cameraObject=new GameObject("QA capture camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var root=View.GetComponent<Canvas>();var mode=root.renderMode;var priorCamera=root.worldCamera;float priorDistance=root.planeDistance;
            var transforms=View.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
            var active=RenderTexture.active;Texture2D pixels=null;
            try
            {
                if(Camera.main!=null){var original=Camera.main.targetTexture;Camera.main.targetTexture=target;Camera.main.Render();Camera.main.targetTexture=original;}
                camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=Screen.height*.5f;
                camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.clearFlags=Camera.main!=null?CameraClearFlags.Depth:CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;
                foreach(var t in transforms)t.gameObject.layer=31;
                root.renderMode=RenderMode.ScreenSpaceCamera;root.worldCamera=camera;root.planeDistance=1;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
                Check(pixels.GetPixels32().Where((_,i)=>i%97==0).Select(c=>(int)c.r*65536+c.g*256+c.b).Distinct().Take(32).Count()>=32,"Rendered image contains visible content: "+name);
                File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                root.renderMode=mode;root.worldCamera=priorCamera;root.planeDistance=priorDistance;
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
                RenderTexture.active=active;Destroy(cameraObject);if(pixels!=null)Destroy(pixels);RenderTexture.ReleaseTemporary(target);Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }
        IEnumerator Click(string name)
        {
            yield return null;Canvas.ForceUpdateCanvases();
            var button=View.Control<Button>(name);Check(button.gameObject.activeInHierarchy&&button.IsInteractable(),"Button available: "+name);
            var rect=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Button receives raycast without overlap: "+name);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
        }
        IEnumerator Key(Key key,float seconds=.05f)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(seconds);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        void Answer(int index,string value){var input=View.Control<TMP_InputField>("Answer"+index);Check(input.gameObject.activeInHierarchy&&input.IsInteractable(),"Answer input enabled: "+index);input.text=value;}
        IEnumerator WalkToConsole()
        {
            // Hidden players may lose a single held-key event during a focus reset.
            // Keep injecting W and steer yaw; exercise the actual controller and colliders.
            float deadline=Time.realtimeSinceStartup+8;
            while(!Game.CanvasNearStation&&Time.realtimeSinceStartup<deadline)
            {
                var direction=Game.World.station.position-Game.World.player.transform.position;direction.y=0;
                Game.World.player.transform.rotation=Quaternion.LookRotation(direction);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            Check(Game.CanvasNearStation,"Controller walking reaches console interaction radius");
        }
        IEnumerator Start()
        {
            StartCoroutine(Watchdog());yield return new WaitForSeconds(1);
            if(auth)
            {
                Check(!LearningApi.Authenticated&&!LearningApi.TestBypass,"Auth QA uses real authentication");
                yield return Shot("00-login");yield return LoginAs(credentials.username);
                Check(LearningApi.Student,"Student role obtained from backend");
            }
            Check(View!=null&&View.isMenu,"Canvas main menu loaded");yield return Shot("01-main-menu");
            yield return Click("HomeInstructions");yield return Shot("01-instructions");yield return Click("InstructionsBack");
            yield return Click("HomeRecords");Check(View.Control<TMP_Text>("RecordsPage").text.Contains("No records"),"Fresh profile has no fabricated records");yield return Click("RecordsBack");
            yield return Click("HomeStart");yield return Shot("02-missions");yield return Click("Mission0");yield return new WaitForSeconds(.7f);
            yield return Shot("03-brief");yield return Click("BriefStart");
            bool expanded=Game.ExtendedLimits;int assessedQuestions=expanded?10:5;
            Check(Game.State=="Explore"&&Game.World.player.ControlsEnabled,"Player controls active in world");
            Vector3 start=Game.World.player.transform.position;yield return WalkToConsole();
            Check(Vector3.Distance(start,Game.World.player.transform.position)>1,"W input moves the player");Check(Game.CanvasNearStation,"Movement reaches console interaction radius");
            yield return Shot("04-world");yield return Key(UnityEngine.InputSystem.Key.E);
            Check(Game.State=="Puzzle"&&!Game.World.player.ControlsEnabled,"E opens console and disables movement");
            yield return Shot("05-numerical");yield return Click("Submit");Check(Game.Session.attempts.Count==0,"Empty answer does not count as an attempt");
            Answer(0,"4");yield return Click("Submit");Check(Game.Session.attempts.Count==1&&Game.Stage==0&&Game.Session.heartsUsed==0,"Incorrect attempt stays retryable and free");yield return Shot("06-retry");
            if(Environment.GetCommandLineArgs().Contains("--sync-recovery"))
            {
                yield return LearningApi.Instance.Flush();File.WriteAllText(Path.Combine(folder,"offline-request"),"Stop only the isolated QA service.");
                while(!File.Exists(Path.Combine(folder,"offline-ready")))yield return null;
            }
            yield return Click("PuzzleSupport");float time=Game.Session.elapsed;yield return new WaitForSeconds(.2f);Check(Game.Session.elapsed==time,"Support pauses elapsed time");
            yield return Click("SupportTier0");yield return Click("SupportTier1");yield return Click("SupportTier1");Check(Game.Session.heartsUsed==1,"Double-clicked visible hint is not charged twice");
            yield return Click("SupportTier2");yield return Shot("07-support");yield return Key(UnityEngine.InputSystem.Key.Escape);Check(!Game.CanvasSupportOpen,"Escape closes support");
            yield return Click("PuzzleSupport");yield return Click("SupportTier1");Check(Game.Session.heartsUsed==3&&!View.Control<Button>("SupportTier2").IsInteractable(),"Paid support disabled at zero hearts");yield return Click("SupportTier0");yield return Click("SupportClose");
            Answer(0,"10/2");yield return Click("Submit");Check(Game.CanvasAccepted,"Correct fraction accepted");int count=Game.Session.attempts.Count;
            Game.CanvasSubmit(View.questionBank.questions[0],new LimitsAnswer{values=new[]{"5","",""}});Check(Game.Session.attempts.Count==count,"Duplicate correct submission ignored");
            yield return Click("Continue");yield return CompleteIndependentCalibration();Check(Game.Stage==1,"Continue restores one calibration");
            View.Control<Slider>("Probe").value=1.9f;View.Control<Slider>("Marker").value=4;yield return Shot("08-graph");
            yield return Key(UnityEngine.InputSystem.Key.Escape);time=Game.Session.elapsed;yield return new WaitForSeconds(.2f);Check(Game.State=="Paused"&&Game.Session.elapsed==time,"Escape pause stops active timer");
            yield return Click("PauseLeave");yield return Click("LeaveCancel");yield return Click("PauseResume");Check(Game.State=="Puzzle"&&View.Control<Slider>("Marker").value==4,"Cancel and resume retain current input");
            yield return Click("PuzzlePause");yield return Click("PauseLeave");string id=Game.Session.id;yield return Click("LeaveConfirm");yield return new WaitForSeconds(.5f);
            var saved=Chapter4Store.Read().Single(s=>s.id==id);Check(saved.completed.Count==(expanded?2:1)&&saved.heartsUsed==3&&saved.status=="InProgress","Actual JSON preserves resumable learning record");
            if(Environment.GetCommandLineArgs().Contains("--sync-recovery"))
            {
                Check(LearningApi.Instance.HasPending&&Directory.GetFiles(LearningApi.CacheFolder,"*.pending").Length>0,"Service outage preserves durable pending upload");
                File.WriteAllText(Path.Combine(folder,"online-request"),"Restart the isolated QA service.");
                while(!File.Exists(Path.Combine(folder,"online-ready")))yield return null;
                yield return LearningApi.Instance.Flush();Check(!LearningApi.Instance.HasPending,"Pending results upload after service recovery");
                yield return LearningApi.Instance.Request("GET","/api/sessions",null,json=>Check(JsonUtility.FromJson<SessionList>(json).sessions.Single(s=>s.id==id).completed.Count==1,"Recovered server contains accepted work"));
            }
            yield return Shot("09-continue-menu");yield return Click("HomeContinue");yield return new WaitForSeconds(.5f);Check(Game.Stage==1&&Game.Session.id==id&&Game.Session.heartsUsed==3,"Reload resumes same session at next question");
            yield return WalkToConsole();yield return Key(UnityEngine.InputSystem.Key.E);View.Control<Slider>("Marker").value=4;yield return Click("Submit");yield return Click("Continue");yield return CompleteIndependentCalibration();
            Answer(0,"3");Answer(1,"4");Answer(2,"3");yield return Click("Submit");Check(Game.Stage==2&&!Game.CanvasAccepted,"Wrong one-sided value rejected");
            Answer(1,"3");yield return Shot("10-one-sided");yield return Click("Submit");yield return Click("Continue");yield return CompleteIndependentCalibration();
            Answer(0,"4");Answer(1,"7");yield return Shot("11-point-value");yield return Click("Submit");yield return Click("Continue");yield return CompleteIndependentCalibration();
            yield return Click("Conclusion2");for(int i=0;i<3;i++){yield return Click("ReasonCard"+i);yield return Click("ReasonSlot"+i);}yield return Shot("12-reason");yield return Click("Submit");yield return Click("Continue");yield return CompleteIndependentCalibration();
            Check(Game.Ready&&Game.Session.completed.Count==assessedQuestions&&Game.Session.status=="InProgress","All required answers unlock lever but do not complete session");yield return Shot("13-activation");yield return Click("ActivatePortal");yield return new WaitForSeconds(4.3f);
            Check(Game.State=="Summary"&&Game.Session.status=="Completed","Animation completes session");Check(Mathf.Abs(Game.Session.Accuracy-100f*assessedQuestions/(assessedQuestions+2))<.01f,"Two retries included in accuracy");yield return Shot("14-summary");
            yield return Click("SummaryRecords");yield return new WaitForSeconds(.4f);Check(Chapter4Store.Read().Single(s=>s.id==id).status=="Completed","Completion saved on disk");yield return Shot("15-records");yield return Click("Record0");yield return Shot("16-attempts");
            // Warn about visible TMP overflow; these screenshots also receive manual visual review.
            foreach(var label in View.GetComponentsInChildren<TMP_Text>())if(label.isTextOverflowing)Check(false,"Visible text overflows: "+label.name);
            if(auth)
            {
                yield return Click("DetailBack");yield return Click("RecordsBack");
                yield return LearningApi.Instance.Flush();Check(!LearningApi.Instance.HasPending,"All revisions accepted by server");
                yield return Click("HomeLogout");yield return new WaitForSeconds(.5f);Check(!LearningApi.Authenticated,"Student signs out");
                yield return LoginAs(credentials.teacher);Check(LearningApi.Account!=null&&LearningApi.Account.role=="Teacher","Teacher role obtained from backend");
                View.Control<TMP_InputField>("FilterStudent").text=credentials.username;
                View.Control<TMP_Dropdown>("FilterStatus").value=2;
                yield return Click("TeacherSearch");yield return new WaitForSeconds(.7f);
                Check(View.Control<TMP_Text>("TeacherSummary").text.StartsWith("1 matching sessions"),"Teacher sees exactly the authenticated student's completed session");
                yield return Shot("17-teacher");
                // Open the actual dropdown template through a Canvas raycast.
                var drop=View.Control<TMP_Dropdown>("FilterStep");var rect=(RectTransform)drop.transform;
                var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<TMP_Dropdown>()==drop,"Filter dropdown receives pointer");
                if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return new WaitForSeconds(.2f);yield return Shot("18-filter-dropdown");
                var option=drop.GetComponentsInChildren<TMP_Text>().First(t=>t.text=="L3 / One-sided"&&t.GetComponentInParent<Toggle>()!=null);
                var toggle=option.GetComponentInParent<Toggle>();var itemRect=(RectTransform)toggle.transform;
                pointer.position=RectTransformUtility.WorldToScreenPoint(null,itemRect.TransformPoint(itemRect.rect.center));hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
                Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Toggle>()==toggle,"Question option receives pointer");
                if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return new WaitForSeconds(.25f);Check(drop.value==3,"Dropdown selects the clicked question");
                yield return Click("TeacherSearch");yield return new WaitForSeconds(.3f);
                yield return Click("TeacherView");yield return Click("Record0");
                Check(View.Control<TMP_Text>("DetailAttempts").text.Contains("RETRY"),"Teacher can inspect actual incorrect attempt evidence");yield return Shot("19-teacher-evidence");
                yield return Click("DetailBack");yield return Click("RecordsBack");yield return Click("TeacherExport");yield return new WaitForSeconds(.4f);
                Check(Directory.GetFiles(Path.Combine(LearningApi.CacheRoot,"Exports"),"*.csv").Length>0,"Teacher export creates CSV");
                yield return Click("TeacherStudents");yield return new WaitForSeconds(.4f);yield return Shot("20-students");
                Check(View.Control<TMP_Text>("StudentsList").text.Contains(credentials.username),"Teacher lists registered students");
                yield return Click("StudentsBack");yield return Click("TeacherLogout");yield return new WaitForSeconds(.3f);
                Check(!LearningApi.Authenticated,"Teacher signs out");
            }
            finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: Canvas raycasts, pointer handlers, Input System movement/E/Escape, all required guided and independent questions, retry metrics, hint charging, pause, disk save/resume, activation, dashboard."+(auth?" Real student/teacher login, server sync, teacher filters, evidence, CSV, account list, logout.":"")+"\nImages rendered from the actual Unity Canvas/world into a render target. Automated input; physical keyboard/mouse usability still needs manual playtesting.\n":"FAILURES: "+failures);
            Application.Quit(failures==0?0:1);
        }
        IEnumerator CompleteIndependentCalibration()
        {
            if(Game.LimitsExtension==null)yield break;
            int stage=Game.Stage;
            if(stage==0)Answer(0,"11");
            else if(stage==2)Answer(0,"3");
            else yield return Click("Conclusion"+(stage==1?0:stage==3?1:2));
            yield return Click("Submit");
            Check(Game.CanvasAccepted,"Independent calibration accepted at stage "+stage);
            yield return Click("Continue");
        }
        IEnumerator LoginAs(string username)
        {
            View.Control<TMP_InputField>("LoginUsername").text=username;
            View.Control<TMP_InputField>("LoginPassword").text=credentials.password;
            yield return Click("LoginSubmit");
            float until=Time.realtimeSinceStartup+12;
            while(!LearningApi.Authenticated&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSeconds(.8f);
            Check(LearningApi.Authenticated,"Account login completed");
        }
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(90);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: timed out; see errors.txt and player log.");Application.Quit(1);}}
    }
}
#endif
