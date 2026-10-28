using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CalculusLabEscape
{
    [DefaultExecutionOrder(100),DisallowMultipleComponent]
    public sealed class RobotDialogue:MonoBehaviour
    {
        public GameManager manager;
        public Transform guide;
        public Transform lookTarget;
        public GameObject overlay;
        public GameObject[] hideWhileTalking;
        public TMP_Text message,topic,worldPrompt,worldObjective;
        [Min(.5f)] public float talkDistance=2.6f;
        [Range(15,100)] public float charactersPerSecond=52;
        public bool IsOpen {get;private set;}
        float reveal;
        bool[] previousVisibility;
        Camera conversationCamera;
        Quaternion previousCameraRotation;
        void Awake()
        {
            // Clear an optional Editor conversation preview when entering Play.
            if(!Application.isPlaying)return;
            if(overlay!=null)overlay.SetActive(false);
            if(hideWhileTalking!=null)foreach(var node in hideWhileTalking)if(node!=null)node.SetActive(true);
        }
        public bool CanTalk
        {
            get{
                if(manager==null||manager.world==null||guide==null||manager.State!="Explore")return false;
                var player=manager.world.player.transform;var delta=guide.position-player.position;delta.y=0;
                return delta.magnitude<=talkDistance&&(delta.sqrMagnitude<.01f||Vector3.Dot(player.forward,delta.normalized)>.35f);
            }
        }
        public bool HandlePlayerInput()
        {
            if(manager==null||manager.Session==null)return false;
            var keys=Keyboard.current;
            if(IsOpen){
                if(keys!=null){
                    if(keys.escapeKey.wasPressedThisFrame)Close();
                    else if(keys.enterKey.wasPressedThisFrame||keys.spaceKey.wasPressedThisFrame){message.maxVisibleCharacters=int.MaxValue;reveal=999;EventSystem.current?.SetSelectedGameObject(null);}
                }
                return true;
            }
            if(CanTalk&&!manager.PresentationBusy&&!manager.CanvasSupportOpen&&!ScreenTransition.IsBusy&&keys!=null&&keys.eKey.wasPressedThisFrame){Open();return true;}
            return false;
        }
        public void Open()
        {
            if(IsOpen||!CanTalk||manager.PresentationBusy||ScreenTransition.IsBusy||manager.CanvasSupportOpen)return;
            IsOpen=true;overlay.SetActive(true);manager.world.player.ControlsEnabled=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            SceneSoundscape.Active?.Play("robot");
            manager.world.CaseAudio?.SetPaused(true);
            HideHUD();conversationCamera=manager.world.player.GetComponentInChildren<Camera>();previousCameraRotation=conversationCamera.transform.localRotation;
            ShowTopic(-1,manager.Stage);EventSystem.current?.SetSelectedGameObject(null);
        }
        public void ChooseTopic(int index)
        {
            if(!IsOpen)return;ShowTopic(Mathf.Clamp(index,0,2),manager.Stage);EventSystem.current?.SetSelectedGameObject(null);
        }
        void LateUpdate()
        {
            if(manager==null||manager.Session==null)return;
            if(IsOpen){
                if(manager.State!="Explore"){Close();return;}
                reveal+=Time.unscaledDeltaTime*charactersPerSecond;message.maxVisibleCharacters=Mathf.FloorToInt(reveal);
                if(conversationCamera!=null&&lookTarget!=null){var direction=lookTarget.position-conversationCamera.transform.position;if(direction.sqrMagnitude>.01f)conversationCamera.transform.rotation=Quaternion.Slerp(conversationCamera.transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-Time.unscaledDeltaTime*9));}
            }else if(CanTalk&&!ScreenTransition.IsBusy&&!manager.PresentationBusy){
                if(worldPrompt!=null)worldPrompt.text="[E]  TALK TO LABORATORY AI     /     [Esc]  Pause";
                if(worldObjective!=null)worldObjective.text="Your laboratory guide is nearby. Face the robot and press E to talk.";
            }
        }
        public void Close()
        {
            if(overlay!=null)overlay.SetActive(false);RestoreHUD();if(!IsOpen)return;IsOpen=false;
            if(conversationCamera!=null)conversationCamera.transform.localRotation=previousCameraRotation;conversationCamera=null;
            if(manager!=null&&manager.world!=null){bool move=manager.State=="Explore"&&!ScreenTransition.IsBusy;manager.world.player.ControlsEnabled=move;manager.world.CaseAudio?.SetPaused(manager.State=="Paused"||manager.CanvasSupportOpen);Cursor.lockState=move?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!move;}
            EventSystem.current?.SetSelectedGameObject(null);
        }
        void ShowTopic(int index,int stage)
        {
            if(manager!=null&&(int)manager.mission>0){ShowMissionTopic(index,stage);reveal=0;message.maxVisibleCharacters=0;message.ForceMeshUpdate();return;}
            bool ready=stage>=5;
            topic.text=index<0?"YOUR OBSERVATORY GUIDE":index==0?"OUR NEXT OBJECTIVE":index==1?"LET'S THINK ABOUT LIMITS":"A LITTLE SUPPORT";
            if(index<0)message.text=ready?"The portal is stable. Your repairs are bringing the training lab back online. Let's activate the gate.":"I'm a local support unit. I can guide you, but I can't repair the damaged systems. Let's restore this portal together.";
            else if(index==0)message.text=ready?"Return to the calibration console and open the activation controls. Your completed work is safe; activating the gate takes you to your results.":"Calibration "+(stage+1)+" of 5 is next. Use the console for the guided task, then complete its independent check. Apply the accepted pair to restore one gate segment.";
            else if(index==2)message.text="The basic hint is free. Stronger hints use a Support Heart. You can retry an answer without losing accepted work. Open Hint / support while working at the console.";
            else message.text=new[]{"A limit describes where outputs are heading as the input approaches a target. Check nearby values from both sides. The value at the target itself may be different.","Follow the curve towards the target from each side. Look for the approached height. A hole does not automatically mean that the limit fails to exist.","Find the left-hand and right-hand limits separately. A two-sided limit exists only when those approached values agree.","Read the limit from nearby values, and the function value from the filled point. The two values answer different questions and do not have to match.","Compare the two sides before choosing a conclusion. If they approach different values, there is no single two-sided limit."}[Mathf.Clamp(stage,0,4)];
            reveal=0;message.maxVisibleCharacters=0;message.ForceMeshUpdate();
        }
        void ShowMissionTopic(int index,int stage)
        {
            bool bridge=manager.mission==GameManager.Mission.Differentiation;
            bool ready=manager.Session!=null&&manager.Ready;
            topic.text=index<0?(bridge?"YOUR BRIDGE GUIDE":"YOUR CONTAINMENT GUIDE"):index==0?"OUR NEXT OBJECTIVE":index==1?(bridge?"ABOUT DIFFERENTIATION":"ABOUT INTEGRATION"):"A LITTLE SUPPORT";
            if(index==2){message.text="Basic Hint is free. Rule Reminder and Guided Demonstration each use one Support Heart. Open Hint / support at the device for mathematical help. Hints used before you answer are recorded as assistance; accepted work is kept.";return;}
            if(index==1){message.text=bridge?"Differentiation describes how a function changes. Each repair asks you to interpret a function and enter a derivative. For help with a calculation, use Hint / support at the device so the game records the assistance you choose.":"Integration can describe a family of antiderivatives or accumulated change over an interval. The four modules use different controls. For help with a calculation, open Hint / support at that module so your assistance is recorded.";return;}
            if(bridge)
            {
                if(index<0)message.text=ready?"The bridge is ready. Let's open the crossing and help the calf reach its mother.":"I'm your laboratory guide for this simulation. Repair the bridge components, then help the calf cross safely to its mother. I'll help you find your way.";
                else message.text=ready?"Return to Bridge Builder and choose Open crossing / rescue calf. The reunion finishes this mission and saves your results.":"Head to "+Chapter4World.RepairDeviceNames[stage==0?0:stage<4?1:2]+", marked in cyan. Complete its guided tasks and independent checks. Accepted work is kept when you retry, and finished repairs restore the bridge.";
                return;
            }
            bool dialDone=manager.Session!=null&&manager.CaseModuleComplete(0);
            if(index<0)message.text=ready?"All four modules are stable. We can now close down the containment device safely.":"Welcome to containment control. Stabilise the four modules at the case, then use the protected master switch to shut down the device. I'll help you navigate the controls.";
            else message.text=ready?"Return to the containment case. Lift the master switch cover, then choose Master Deactivation. Wait for the shutdown to finish before viewing your results.":dialDone?"Return to the case and choose an unfinished module. Review each accepted result, then Continue to its independent checks. All four modules must be complete before shutdown.":"Follow the cyan marker to the containment case and open the Exponent Dial module first. Review its accepted result, then Continue to the independent checks. Finishing that module unlocks the other three.";
        }
        // Editor sample only; does not start a session or grant progress.
        public void Preview(int stage=0,int selection=-1)
        {
            overlay.SetActive(true);HideHUD();ShowTopic(selection,stage);message.maxVisibleCharacters=int.MaxValue;
        }
        void HideHUD()
        {
            if(hideWhileTalking==null||previousVisibility!=null)return;previousVisibility=new bool[hideWhileTalking.Length];
            for(int i=0;i<hideWhileTalking.Length;i++)if(hideWhileTalking[i]!=null){previousVisibility[i]=hideWhileTalking[i].activeSelf;hideWhileTalking[i].SetActive(false);}
        }
        void RestoreHUD()
        {
            if(previousVisibility==null)return;for(int i=0;i<previousVisibility.Length;i++)if(hideWhileTalking[i]!=null)hideWhileTalking[i].SetActive(previousVisibility[i]);previousVisibility=null;
        }
        void OnDisable(){if(IsOpen)Close();else RestoreHUD();}
    }
}
