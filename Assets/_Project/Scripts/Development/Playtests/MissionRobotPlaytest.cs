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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    // Opt-in integration test. All screenshots and saves stay in the QA folder.
    public sealed class MissionRobotPlaytest:MonoBehaviour
    {
        string folder;int failures,errors;bool finished;
        GameManager Game=>GameManager.Instance;
        GameObject UI=>Game.Topic==0?Game.menuOrLimitsUI.gameObject:Game.missionUI.gameObject;
        T Node<T>(string name)where T:Component=>UI.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).GetComponent<T>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--mission-robot-qa"))return;
            var folder=LearningApi.Argument("--qa-output");Directory.CreateDirectory(folder);Chapter4Store.IsolatedFolder=Path.Combine(folder,"sessions");Chapter4Store.Profile="Canvas QA";
            var go=new GameObject("Saved mission robot QA");DontDestroyOnLoad(go);go.AddComponent<MissionRobotPlaytest>().folder=folder;
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();if(Mouse.current==null)InputSystem.AddDevice<Mouse>();
        }
        void Awake()=>Application.logMessageReceived+=Log;
        void OnDestroy()=>Application.logMessageReceived-=Log;
        void Check(bool value,string text){File.AppendAllText(Path.Combine(folder,"checks.txt"),(value?"PASS ":"FAIL ")+" / "+(Game==null?-1:Game.Topic)+" / "+text+"\n");if(!value)failures++;}
        void Log(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}
        IEnumerator Wait(Func<bool> done,string text,float seconds=25){float end=Time.realtimeSinceStartup+seconds;while(!done()&&Time.realtimeSinceStartup<end)yield return null;if(!done())throw new Exception(text);}
        IEnumerator Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;}
        IEnumerator Click(string name)
        {
            yield return null;yield return null;Canvas.ForceUpdateCanvases();var button=Node<Button>(name);Check(button.gameObject.activeInHierarchy&&button.IsInteractable(),"Native button available: "+name);
            var rect=button.GetComponent<RectTransform>();var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Native pointer reaches "+name);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
        }
        static float Distance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        IEnumerator Walk(Vector3 target,float distance)
        {
            float end=Time.realtimeSinceStartup+20;
            while(Distance(Game.World.player.transform.position,target)>distance&&Time.realtimeSinceStartup<end)
            {
                var delta=target-Game.World.player.transform.position;delta.y=0;Game.World.player.transform.rotation=Quaternion.LookRotation(delta);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));yield return null;
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
            Check(Distance(Game.World.player.transform.position,target)<=distance+.15f,"Real controller can walk to the guide/device without a blocking guide collider");
        }
        IEnumerator OpenConsole()
        {
            yield return Walk(Game.world.station.position,2.8f);var delta=Game.world.station.position-Game.world.player.transform.position;delta.y=0;Game.world.player.transform.rotation=Quaternion.LookRotation(delta);yield return null;
            Check(!Game.robotDialogue.CanTalk,"Facing the active console keeps E available for its original interaction");yield return Press(Key.E);Check(Game.State=="Puzzle"&&!Game.robotDialogue.IsOpen,"E still opens the original device");
            var introduction=UI.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="IntroductionModal");if(introduction!=null&&introduction.gameObject.activeInHierarchy)yield return Click("IntroductionContinue");
        }
        IEnumerator Start()
        {
            StartCoroutine(Timeout());
            foreach(int topic in new[]{0,1,2})
            {
                SceneManager.LoadScene(Chapter4Game.Scenes[topic]);yield return null;yield return null;
                yield return Wait(()=>Game!=null&&Game.world!=null&&Game.Topic==topic&&Game.briefingVideo!=null&&Game.briefingVideo.IsReady,"Mission robot test scene did not load");
                yield return Click("BriefStart");var chat=Game.robotDialogue;
                Check(chat!=null&&chat.guide!=null&&!chat.IsOpen&&!chat.overlay.activeSelf&&!chat.CanTalk,"Saved guide and closed conversation exist before interaction");
                int uiCount=UI.GetComponentsInChildren<RectTransform>(true).Length,modelCount=Game.world.GetComponentsInChildren<Renderer>(true).Length;
                var animator=chat.guide.GetComponentInChildren<Animator>();float pose=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;yield return new WaitForSeconds(.25f);
                Check(animator.runtimeAnimatorController!=null&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>pose,"Existing saved idle animation plays");
                yield return Capture("level-"+(topic+1)+"-entry");yield return Walk(chat.guide.position,1.9f);
                var direction=chat.guide.position-Game.world.player.transform.position;direction.y=0;Game.world.player.transform.rotation=Quaternion.LookRotation(direction);yield return null;
                Check(chat.CanTalk,"Near and facing the robot enables talking");var camera=Game.world.player.GetComponentInChildren<Camera>();var look=camera.transform.localRotation;
                if(topic>0){yield return null;Check(Node<TMP_Text>("WorldGuideTitle").text=="TALK / LABORATORY AI"&&Node<TMP_Text>("WorldGuideBody").text.Contains("talk")&&Node<TMP_Text>("Interaction state").text=="IN RANGE","World guide and range status agree with the nearby robot interaction");}
                int greetingCount=SceneSoundscape.Active.Count("robot");
                yield return Press(Key.E);Check(chat.IsOpen&&Game.PresentationBusy&&Game.State=="Explore"&&!Game.world.player.ControlsEnabled,"E opens the saved dialogue and blocks movement");
                Check(SceneSoundscape.Active.Count("robot")==greetingCount+1&&SceneSoundscape.Active.robot.clip==SceneSoundscape.Active.bank.Find("robot").clip,"Opening the real conversation emits one imported robot greeting");
                float elapsed=Game.Session.elapsed,remaining=Game.CaseRemaining;var position=Game.world.player.transform.position;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));yield return new WaitForSeconds(.3f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
                Check(Game.Session.elapsed==elapsed&&Game.CaseRemaining==remaining&&Vector3.Distance(position,Game.world.player.transform.position)<.001f,"Conversation freezes movement and assessment time");
                var pause=Node<Button>("PuzzlePause");var pr=pause.GetComponent<RectTransform>();var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,pr.TransformPoint(pr.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
                Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()!=pause,"Saved overlay blocks clicks through to Pause");
                if(topic>0){Game.missionUI.OnWorldPause();Check(Game.State=="Explore","Pause cannot interrupt an active conversation");Check(!Node<Transform>("WorldGuide").gameObject.activeInHierarchy,"First-use guide stays hidden behind the conversation");}
                yield return Press(Key.Enter);yield return Capture("level-"+(topic+1)+"-greeting");
                foreach(int selection in new[]{0,1,2})
                {
                    yield return Click("DialogueTopic"+selection);yield return Press(Key.Enter);chat.message.ForceMeshUpdate();Check(!chat.message.isTextOverflowing&&!chat.topic.isTextOverflowing,"Saved dialogue labels fit for topic "+selection);
                    if(topic>0)Check(!chat.message.text.Contains("portal")&&!chat.message.text.Contains("Calibration 1 of 5")&&!Node<TMP_Text>("DialogueTopic1Label").text.Contains("limits"),"Conversation uses this mission's guidance");
                    yield return Capture("level-"+(topic+1)+"-topic-"+selection);
                }
                Check(Game.Session.attempts.Count==0&&Game.Session.support.Count==0&&Game.Session.heartsUsed==0&&Game.Session.completed.Count==0,"Navigation guidance never submits answers, buys hints or grants progress");
                Check(SceneSoundscape.Active.Count("robot")==greetingCount+1,"Changing dialogue topics does not repeat the greeting");
                yield return Press(Key.Escape);Check(!chat.IsOpen&&Game.State=="Explore"&&Game.world.player.ControlsEnabled&&!Game.PresentationBusy,"Esc closes the conversation and restores exploration");
                Check(Quaternion.Angle(camera.transform.localRotation,look)<.01f,"Closing restores the player's camera");
                yield return Press(Key.E);Check(chat.IsOpen,"Conversation can be reopened");yield return Click("DialogueClose");Check(!chat.IsOpen&&Game.world.player.ControlsEnabled,"Saved Close button restores movement");
                Check(UI.GetComponentsInChildren<RectTransform>(true).Length==uiCount&&Game.world.GetComponentsInChildren<Renderer>(true).Length==modelCount,"Talking reuses authored UI and models without generating scene objects");
                yield return OpenConsole();
                if(topic==2)
                {
                    yield return Click("PuzzleWorld");yield return Walk(chat.guide.position,1.9f);direction=chat.guide.position-Game.world.player.transform.position;direction.y=0;Game.world.player.transform.rotation=Quaternion.LookRotation(direction);yield return null;yield return Press(Key.E);
                    remaining=Game.CaseRemaining;elapsed=Game.Session.elapsed;yield return new WaitForSeconds(.3f);
                    Check(Game.World.CaseOpened&&chat.IsOpen&&Game.CaseRemaining==remaining&&Game.Session.elapsed==elapsed,"Opened case countdown and active time pause during robot conversation");
                    Check(Game.World.CaseAudio.IsPaused,"Opened case audio stays paused during robot conversation");
                    yield return Click("DialogueClose");Check(!Game.World.CaseAudio.IsPaused,"Closing resumes case audio");yield return OpenConsole();
                }
                if(topic==0){Node<TMP_InputField>("Answer0").text="5";yield return Click("Submit");}
                else if(topic==1){yield return Click("BridgeChip");yield return Click("CoefficientSlot");yield return Click("ExponentMinus");yield return Click("RepairSubmit");}
                else{yield return Click("Module0");var modal=Node<Transform>("IntroductionModal");if(modal.gameObject.activeInHierarchy)yield return Click("IntroductionContinue");yield return Click("DialExponentPlus");for(int n=0;n<4;n++)yield return Click("DivisorPlus");yield return Click("InsertC");yield return Click("DialSubmit");}
                Check(Game.Session.attempts.Count==1&&Game.Session.attempts[0].correct&&Game.Session.GuidedCompletionCount==1&&Game.Session.AssistedCompletionCount==0&&Game.Session.support.Count==0,"Original grading works after talking and navigation does not count as mathematical assistance");
                yield return Capture("level-"+(topic+1)+"-original-device");
            }
            finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),(failures==0&&errors==0?"PASS":"FAIL")+" / all three guides; saved scene objects; idle animations; real walking and E; topic buttons; shield; paused timers, movement and case audio; Esc/Close; camera restored; original device interactions and grading; no generated UI or scoring changes / "+failures+" failed assertions / "+errors+" runtime errors");Application.Quit(failures==0&&errors==0?0:1);
        }
        IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();var canvas=UI.GetComponent<Canvas>();var cameraObject=new GameObject("Robot QA capture camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;
            var mode=canvas.renderMode;var old=canvas.worldCamera;var distance=canvas.planeDistance;var transforms=UI.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();Texture2D pixels=null;
            try
            {
                foreach(var preview in Game.World.GetComponentsInChildren<Camera>().Where(c=>c.targetTexture!=null))preview.Render();
                var main=Camera.main;var prior=main.targetTexture;try{main.targetTexture=target;main.Render();}finally{main.targetTexture=prior;}
                camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=360;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.Depth;camera.cullingMask=1<<31;
                foreach(var t in transforms)t.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally{canvas.renderMode=mode;canvas.worldCamera=old;canvas.planeDistance=distance;for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];RenderTexture.active=active;Destroy(cameraObject);if(pixels!=null)Destroy(pixels);RenderTexture.ReleaseTemporary(target);}
            yield return null;
        }
        IEnumerator Timeout(){yield return new WaitForSecondsRealtime(180);if(!finished){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL / robot test timed out");Application.Quit(1);}}
    }
}
#endif

