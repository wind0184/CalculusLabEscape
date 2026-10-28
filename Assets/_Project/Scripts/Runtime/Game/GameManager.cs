using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace CalculusLabEscape
{
    // The saved Game Manager owns the existing mission rules, progress, timer,
    // pause and persistence. It uses the map/UI already present in the scene.
    [DefaultExecutionOrder(-500), DisallowMultipleComponent]
    [MovedFrom(true,"CalculusLabEscape",null,"GameSceneController")]
    public sealed class GameManager : Chapter4Game
    {
        public enum Mission { MainMenu=-1, Limits=0, Differentiation=1, Integration=2 }
        public static GameManager Instance { get; private set; }
        public Mission mission=Mission.MainMenu;
        [Header("Scene objects — drag these from Hierarchy")]
        public Chapter4World world;
        public ProductionCanvas menuOrLimitsUI;
        public MissionCanvas missionUI;
        public EventSystem eventSystem;
        [Header("Screen transition — saved scene Canvas")]
        public ScreenTransition screenTransition;
        [Header("Main menu — saved doorway animation")]
        public MainMenuEntrance menuEntrance;
        [Header("Limits — saved calibration feedback")]
        public LimitsFlowPresentation limitsFlow;
        [Header("Mission — saved robot conversation")]
        public RobotDialogue robotDialogue;
        [Header("Mission — saved introduction video")]
        [FormerlySerializedAs("limitsBriefing")]
        public MissionBriefingVideo briefingVideo;
        // Existing Limits editor tools retain their source compatibility.
        public LimitsBriefingVideo limitsBriefing { get=>briefingVideo as LimitsBriefingVideo; set=>briefingVideo=value; }
        public override bool PresentationBusy=>limitsFlow!=null&&limitsFlow.Busy||robotDialogue!=null&&robotDialogue.IsOpen;

        public Chapter4Game Game => world!=null?this:null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance()=>Instance=null;

        void Awake()
        {
            Instance=this;
            if(ScreenTransition.Active!=null)screenTransition=ScreenTransition.Active;
            EnableCanvas();
            if(world==null)return; // MainMenu has no assessment session.
            InitializeMission();
        }

        public static bool ChangeScene(string sceneName)
        {
            if(Instance!=null&&(Instance.PresentationBusy||Instance.menuEntrance!=null&&Instance.menuEntrance.Busy))return false;
            var transition=Instance!=null?Instance.screenTransition:ScreenTransition.Active;
            if(transition!=null)return transition.LoadScene(sceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);return true;
        }

        public bool EnterLaboratory(System.Action openTopics)
        {
            if(mission!=Mission.MainMenu||ScreenTransition.IsBusy)return false;
            if(menuEntrance!=null)return menuEntrance.Play(openTopics);
            openTopics();return true;
        }

        public void ContinueCalibration()
        {
            if(PresentationBusy||ScreenTransition.IsBusy)return;
            int before=Stage;CanvasContinue();
            if(Topic==0&&Stage>before&&limitsFlow!=null)limitsFlow.ShowCalibration(Stage);
        }

        public void InitializeMission()
        {
            if(Session!=null||world==null)return;
            if(!Application.isPlaying)throw new System.InvalidOperationException("Use Play to start a session; maps are authored in Edit Mode.");
            if(!world.IsAuthored||(int)mission!=world.topic)
                throw new System.InvalidOperationException("Game Manager needs the matching saved map. Assign Mission and World in the Inspector.");
            if(!LearningApi.LocalPlay&&(!LearningApi.Student||world.topic!=0))
            {ChangeScene("MainMenu");return;}
            if(LearningApi.LocalPlay&&!Chapter4Store.IsolatedRun)Chapter4Store.Profile="Local player";
            world.gameManager=this;
            world.ResetSavedWorldForPlay();
            Initialize(world.topic,world);
            if (menuOrLimitsUI != null) menuOrLimitsUI.SetSceneGame(this);
            if (missionUI != null) missionUI.SetSceneGame(this);
            var interaction = world.player.GetComponent<PlayerInteraction>();
            ExternalPlayerInput = interaction != null && interaction.enabled;
        }

        protected override void OnDestroy()
        {
            if(Instance==this)Instance=null;
            base.OnDestroy();
        }
    }
}
