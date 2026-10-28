#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    // Explicit development-only test mode. Runs real OnGUI submit handlers, keeps test records out of user storage.
    public sealed class Chapter4Playtest : MonoBehaviour
    {
        public static bool Active;
        public static string Click;
        string folder;
        Chapter4Game game;
        int failures;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="--chapter4-qa"))return;
            Active=true;Chapter4Store.TestMode=true;Application.runInBackground=true;
            var go=new GameObject("Chapter4 automated playtest");DontDestroyOnLoad(go);go.AddComponent<Chapter4Playtest>();
        }
        void Awake()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");
            folder=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"Chapter4QA");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;
        }
        void Log(string message,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),message+"\n"+stack+"\n");}
        }
        void Set(string name,object value)=>typeof(Chapter4Game).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
        void Check(bool value,string message)
        {
            if(!value){failures++;File.AppendAllText(Path.Combine(folder,"errors.txt"),"ASSERT: "+message+"\n");}
        }
        IEnumerator Press(string text)
        {
            Click=text;float until=Time.realtimeSinceStartup+4;
            while(Click!=null&&Time.realtimeSinceStartup<until)yield return null;
            Check(Click==null,"Button reached: "+text);Click=null;yield return null;
            if(game!=null && (bool)typeof(Chapter4Game).GetField("showRetry",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game))
            {
                int retained=game.Session.completed.Count;
                yield return Shot("retry-"+game.Topic+"-"+game.Session.attempts.Count);
                yield return Press("RETRY");Check(game.Session.completed.Count==retained,"Retry preserves earlier progress");
            }
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
        }
        IEnumerator Load(int topic)
        {
            SceneManager.LoadScene(Chapter4Game.Scenes[topic]);yield return null;yield return null;
            game=FindFirstObjectByType<Chapter4Game>();Check(game!=null,"Scene has chapter 4 controller");
            yield return Shot(topic+"-brief");game.StartSession();yield return new WaitForSeconds(.4f);yield return Shot(topic+"-world");game.OpenPuzzle();
            if(topic==2){yield return Shot("integration-case-overview");yield return Press("EXPONENT DIAL");}
        }
        IEnumerator Start()
        {
            yield return new WaitForSeconds(1);yield return Shot("menu");
            yield return Press("LOG IN");yield return Shot("main-menu");
            yield return Press("STUDENT DASHBOARD");yield return Shot("student-dashboard");yield return Press("MAIN MENU");
            yield return Press("LOG OUT");yield return Press("TEACHER PREVIEW");yield return Press("LOG IN");yield return Shot("teacher-dashboard");yield return Press("LOG OUT");yield return Press("STUDENT ACCESS");yield return Press("LOG IN");
            yield return Press("START");yield return Shot("topic-selection");
            yield return Load(0);
            game.SetState("Paused");float pausedAt=game.Session.elapsed;yield return new WaitForSeconds(.3f);Check(game.Session.elapsed==pausedAt,"Paused time excluded");game.OpenPuzzle();
            yield return Press("HINT / SUPPORT");yield return Shot("support-selection");
            yield return Press("USE SUPPORT");yield return Shot("support-example");yield return Press("RETURN TO PUZZLE");
            yield return Press("SUBMIT ANSWER");Check(game.Session.attempts.Count==1&&game.Session.completed.Count==0,"Limits wrong answer recorded");
            yield return Shot("limits-wrong");Set("a","5");yield return Press("SUBMIT ANSWER");yield return Press("CONTINUE");
            Check(game.Stage==1,"Limits advances only on Continue");Set("marker",4f);Set("probe",1.9f);yield return Shot("limits-graph");yield return Press("SUBMIT ANSWER");yield return Press("CONTINUE");
            Set("a","3");Set("b","3");Set("c","3");yield return Press("SUBMIT ANSWER");yield return Press("CONTINUE");
            Set("a","4");Set("b","7");yield return Shot("limits-function-value");yield return Press("SUBMIT ANSWER");yield return Press("CONTINUE");
            Set("choice",2);Set("order",new[]{0,1,2,-1});yield return Press("SUBMIT ANSWER");yield return Press("CONTINUE");
            Check(game.Ready&&game.Session.completed.Count==5,"Five questions unlock portal");yield return Shot("limits-activation");
            yield return Press("PULL ACTIVATION LEVER");yield return new WaitForSeconds(4.3f);Check(game.State=="Summary","Limits summary after activation");yield return Shot("limits-summary");
            Check(Mathf.Abs(game.Session.Accuracy-83.3333f)<.01f,"All Limits retries included");
            yield return Load(1);yield return Shot("bridge-power-core");
            Set("exponentChip",true);Set("a","3");yield return Press("TEST REPAIR");yield return Press("INSTALL REGULATOR");game.OpenPuzzle();
            Set("a","1/2");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("a","1/2");Set("b","-1/2");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("a","-18");Set("b","-7");yield return Shot("bridge-converter");yield return Press("TEST REPAIR");yield return Press("INSTALL BRIDGE COMPONENT");game.OpenPuzzle();
            Set("a","5");Set("b","4");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("a","-6");Set("b","-4");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("a","0");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("choice",0);yield return Press("TEST REPAIR");yield return Press("INSTALL BRIDGE ANCHOR");game.OpenPuzzle();
            yield return Press("TEST REPAIR");yield return Press("TEST REPAIR");yield return Press("TEST REPAIR");
            game.RequestHint(true);Check(game.Session.heartsUsed==1,"Only voluntary stronger hint costs a heart");
            Set("a","-2");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");Set("a","-2");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Set("choice",0);yield return Shot("bridge-final");yield return Press("TEST REPAIR");yield return Press("PLACE / CONTINUE");
            Check(game.Ready&&game.Session.completed.Count==11,"Bridge all steps and components complete");
            yield return Press("OPEN CROSSING / RESCUE CALF");yield return new WaitForSeconds(2);yield return Shot("bridge-rescue");yield return new WaitForSeconds(2.3f);yield return Shot("bridge-summary");
            Check(game.State=="Summary","Rescue completes");
            yield return Load(2);
            Set("a","5");Set("b","5");Set("c","C");yield return Press("TEST DIALS");Check(game.ModulesComplete==1,"Guided module unlocks others");
            yield return Press("PRESSURE GAUGE");game.Session.elapsed=600;Set("caseElapsed",600f);yield return null;yield return Shot("integration-safety-mode");Check(game.Session.safetyMode,"Zero countdown triggers Safety Mode");
            Set("a","2");yield return Press("PULL RELEASE LEVER");Set("a","4");yield return Press("PULL RELEASE LEVER");
            yield return Press("WIRE MATCHING");Set("wire",0);yield return Press("3x^3");Check(!game.Session.completed.Contains("I_W0"),"Wrong wire stays unlocked");
            Set("wire",0);yield return Press("x^3");Set("wire",1);yield return Press("2x^2");Set("wire",2);yield return Press("-5x");yield return Shot("integration-wires");yield return Press("ADD ONE +C TO COMMON OUTPUT");
            yield return Press("SYMBOL SEQUENCE");Set("order",new[]{1,0,2,3});Set("a","1/2");yield return Press("RUN NEXT STEP");Check(!game.Session.completed.Contains("I_SEQ_0"),"Incorrect sequence pauses at first step");
            Set("order",new[]{0,1,2,3});
            foreach(string value in new[]{"1/2","3/2","2/3","C"}){Set("a",value);yield return Press("RUN NEXT STEP");}
            Check(game.Ready&&game.Session.completed.Count==10,"Free module order completes all four modules");yield return Shot("integration-master-switch");
            yield return Press("LIFT COVER / MASTER DEACTIVATION");yield return new WaitForSeconds(4.3f);Check(game.State=="Summary","Containment summary");yield return Shot("integration-summary");
            File.WriteAllText(Path.Combine(folder,"result.txt"),failures==0?"PASS: three scene flows, 26 required steps, retries, activation gates, Support Hearts, free module order, Safety Mode, summaries.\nTest mode bypasses native input but runs the same OnGUI submission handlers.\n":"FAILURES: "+failures);
            Application.Quit(failures==0?0:1);
        }
    }
}
#endif
