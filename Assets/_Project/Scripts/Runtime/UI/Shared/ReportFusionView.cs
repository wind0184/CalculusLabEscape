using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Presentation only. All geometry, buttons and references are saved in the prefab.
    // GameManager still owns the assessment, support cost, progress and persistence.
    [DisallowMultipleComponent]
    public sealed class ReportFusionView : MonoBehaviour
    {
        [Header("Saved scene and interface references")]
        public GameManager gameManager;
        public RawImage scenePreview;
        public TMP_Text resultState;
        public Image[] progressSegments;
        [Header("Concept layout — presentation only")]
        public bool useConceptLayout;
        public MissionCanvas mission;
        public ProductionCanvas limits;
        public TMP_Text resultHeading;
        public GameObject powerControls, powerSteps, coefficientPanel, exponentPanel, feedbackPanel, overview, completionPanel, scratchpadEntry;
        public TMP_Text taskTitle, taskPrompt, nextAction, coefficientLabel, exponentLabel, powerChipLabel, originalExpression, draftExpression, feedback, sceneProgress, liveStatus, sceneTarget;
        public RectTransform dialNeedle;
        public GameObject scratchpadModal;
        public TMP_InputField scratchpad;
        public bool ScratchpadOpen=>scratchpadModal!=null&&scratchpadModal.activeSelf;

        void LateUpdate()
        {
            if(gameManager==null||gameManager.world==null)return;
            if(scenePreview!=null&&scenePreview.texture!=gameManager.world.Preview)
                scenePreview.texture=gameManager.world.Preview;
            if(gameManager.Session==null)return;
            int completed=gameManager.world.topic==0?gameManager.Stage:gameManager.world.topic==1?gameManager.BridgeParts:gameManager.ModulesComplete;
            for(int i=0;i<progressSegments.Length;i++)
                if(progressSegments[i]!=null)progressSegments[i].color=i<completed?new Color(.51f,.9f,.69f):new Color(.25f,.39f,.44f);
            if(resultState!=null)
            {
                MathText.Set(resultState,mission!=null?mission.CurrentAnswerPreview:limits!=null?limits.CurrentAnswerPreview:"");
                resultState.color=new Color(.91f,.95f,.96f);
            }
            if(resultHeading!=null)resultHeading.text=mission!=null&&mission.ShowingCaseOverview?"DEVICE STATUS":gameManager.Ready?"MISSION STATUS":"YOUR RESULT PREVIEW";
            Present();
        }
        public void Present()
        {
            if(!useConceptLayout)return;
            bool power=powerControls!=null&&powerControls.activeInHierarchy;
            bool completing=completionPanel!=null&&completionPanel.activeInHierarchy;
            bool showingOverview=overview!=null&&overview.activeInHierarchy;
            if(powerSteps!=null)powerSteps.SetActive(power);
            if(taskTitle!=null&&mission!=null)taskTitle.gameObject.SetActive(!power&&!completing&&!showingOverview);
            if(taskPrompt!=null&&power)taskPrompt.gameObject.SetActive(false);
            if(nextAction!=null)nextAction.gameObject.SetActive(!power);
            if(coefficientPanel!=null)coefficientPanel.SetActive(power);
            if(exponentPanel!=null)exponentPanel.SetActive(power);
            if(power&&originalExpression!=null)MathText.Set(originalExpression,"f(x) = x");
            if(power&&powerChipLabel!=null)powerChipLabel.text="4";
            bool placed=mission!=null&&mission.HasPowerCoefficient;
            if(power&&coefficientLabel!=null)coefficientLabel.text=placed?"4":"+";
            if(power&&draftExpression!=null&&draftExpression!=resultState)
            {
                string exponent=exponentLabel!=null?exponentLabel.text:"4";
                MathText.Set(draftExpression,(placed?"4":"□")+"x^"+exponent);
            }
            if(dialNeedle!=null&&exponentLabel!=null&&int.TryParse(exponentLabel.text,out int value))dialNeedle.localRotation=Quaternion.Euler(0,0,-(value-2)*35);
            bool hasFeedback=feedback!=null&&!string.IsNullOrEmpty(feedback.text);
            if(feedbackPanel!=null)feedbackPanel.SetActive(hasFeedback&&feedback.gameObject.activeInHierarchy);
            if(resultState!=null)resultState.gameObject.SetActive(!completing&&!showingOverview&&(gameManager==null||!gameManager.Ready||mission!=null&&mission.topic==2));
            if(scratchpadEntry!=null)scratchpadEntry.SetActive(!completing&&!showingOverview);
            if(gameManager!=null&&gameManager.Session!=null)
            {
                if(liveStatus!=null)liveStatus.text="Support Hearts  "+(3-gameManager.Session.heartsUsed)+" / 3   ·   Basic hint is free";
                if(sceneProgress!=null&&gameManager.world.topic==1)sceneProgress.text="BRIDGE REPAIR "+gameManager.BridgeParts+" / 3";
                if(sceneTarget!=null&&gameManager.world.topic==1)sceneTarget.text=gameManager.Stage==0?"POWER CORE":gameManager.Stage<4?"CONVERTER BOX":"BRIDGE BUILDER";
            }
        }
        public void OpenScratchpad()
        {
            if(gameManager==null||gameManager.State!="Puzzle"||gameManager.CanvasSupportOpen||scratchpadModal==null)return;
            gameManager.CanvasOpenSupport(true);scratchpadModal.SetActive(true);scratchpad?.ActivateInputField();
        }
        public void CloseScratchpad()
        {
            if(!ScratchpadOpen)return;scratchpadModal.SetActive(false);if(gameManager!=null)gameManager.CanvasOpenSupport(false);
        }
    }
}
