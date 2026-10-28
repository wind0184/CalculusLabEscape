using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Inspector button events. Each method contains the existing action for one control.
    public sealed partial class MissionCanvas
    {
        // Button: SummaryNext
        public void OnSummaryNext()
        {
            if(modal==null)Game.CanvasNextMission();
            Refresh();
        }

        // Button: PauseRestart
        public void OnPauseRestart()
        {
            OnPauseRestartAction();
            Refresh();
        }
        void OnPauseRestartAction()
        {
            if(Game.State!="Paused"||modal!=null)return;
                restartSaveFailed=false;modal="Restart";Show("RestartModal",true);Control<UnityEngine.CanvasGroup>("Pause").interactable=false;
                EventSystem.current?.SetSelectedGameObject(Control<UnityEngine.UI.Button>("RestartCancel").gameObject);
        }

        // Button: RestartCancel
        public void OnRestartCancel()
        {
            CloseModal();
            Refresh();
        }

        // Button: RestartConfirm
        public void OnRestartConfirm()
        {
            OnRestartConfirmAction();
            Refresh();
        }
        void OnRestartConfirmAction()
        {
            if(modal!="Restart")return;
                if(!Game.CanvasRestartMission())restartSaveFailed=!string.IsNullOrEmpty(Chapter4Store.LastError);
        }

        // Button: CalibrationOption0, CalibrationOption1, ...
        public void OnCalibrationOption(int index)
        {
            string before = AnswerDraft();
            calibrationChoice=index;
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: CalibrationSubmit
        public void OnCalibrationSubmit()
        {
            Game.SubmitCaseCalibration(calibrationChoice);
            Refresh();
        }

        // Button: CalibrationNext
        public void OnCalibrationNext()
        {
            ContinueCalibration();
            Refresh();
        }

        // Button: BriefStart
        public void OnBriefStart()
        {
            sceneController?.briefingVideo?.LeaveBriefing();
            Game.StartSession();
            Refresh();
        }

        // Button: BriefBack
        public void OnBriefBack()
        {
            sceneController?.briefingVideo?.LeaveBriefing();
            Game.CanvasSaveAndLeave();
            Refresh();
        }

        // Button: WorldPause
        public void OnWorldPause()
        {
            Pause();
            Refresh();
        }

        // Button: PuzzlePause
        public void OnPuzzlePause()
        {
            Pause();
            Refresh();
        }

        // Button: PuzzleWorld
        public void OnPuzzleWorld()
        {
            Game.SetState("Explore");
            Refresh();
        }

        // Button: PauseResume
        public void OnPauseResume()
        {
            Game.SetState(priorState);
            Refresh();
        }

        // Button: PauseLeave
        public void OnPauseLeave()
        {
            modal="Leave";Show("LeaveModal",true);
            Refresh();
        }

        // Button: LeaveCancel
        public void OnLeaveCancel()
        {
            modal=null;Show("LeaveModal",false);
            Refresh();
        }

        // Button: LeaveConfirm
        public void OnLeaveConfirm()
        {
            Game.CanvasSaveAndLeave();
            Refresh();
        }

        // Button: SummaryHome
        public void OnSummaryHome()
        {
            Game.CanvasSaveAndLeave();
            Refresh();
        }

        // Button: SummaryReplay
        public void OnSummaryReplay()
        {
            GameManager.ChangeScene(Chapter4Game.Scenes[topic]);
            Refresh();
        }

        // Button: SummaryRecords
        public void OnSummaryRecords()
        {
            Chapter4Frontend.ReturnPage="Dashboard";GameProgress.ReturnToMenu();
            Refresh();
        }

        // Button: SupportOpen
        public void OnSupportOpen()
        {
            modal="Support";hints.Clear();Game.CanvasOpenSupport(true);Show("SupportModal",true);Control<HintPanelSelection>("SupportModal").Select(-1);Text("SupportText","Choose a hint. Stronger support is optional and costs one heart.");
            Refresh();
        }

        // Button: SupportClose
        public void OnSupportClose()
        {
            CloseModal();
            Refresh();
        }

        // Button: SupportTier0, SupportTier1, ...
        public void OnSupportTier(int tier)
        {
            if(tier<0||tier>2||tier>0&&Game.Session.heartsUsed>=3&&!hints.ContainsKey(tier))return;
            if(!hints.TryGetValue(tier,out var hint)){hint=Game.MissionHint(tier,selectedWire);hints[tier]=hint;}Text("SupportText",hint);
            Control<HintPanelSelection>("SupportModal").Select(tier);
            Refresh();
        }

        // Button: ManualOpen
        public void OnManualOpen()
        {
            modal="Manual";Game.CanvasOpenSupport(true);Show("ManualModal",true);
            Refresh();
        }

        // Button: ManualClose
        public void OnManualClose()
        {
            CloseModal();
            Refresh();
        }

        // Button: BridgeChip
        public void OnBridgeChip()
        {
            BeginSelection(0,4);
            Refresh();
        }

        // Button: CoefficientSlot
        public void OnCoefficientSlot()
        {
            string before = AnswerDraft();
            if(DragKind==0||coefficient)coefficient=true;
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: ExponentMinus
        public void OnExponentMinus()
        {
            string before = AnswerDraft();
            exponent=Mathf.Max(-6,exponent-1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: ExponentPlus
        public void OnExponentPlus()
        {
            string before = AnswerDraft();
            exponent=Mathf.Min(8,exponent+1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: RepairSubmit
        public void OnRepairSubmit()
        {
            Game.MissionRepair(Value("BridgeA"),Value("BridgeB"),coefficient,exponent,choice);
            Refresh();
        }

        // Button: RepairContinue
        public void OnRepairContinue()
        {
            Game.CanvasContinue();
            Refresh();
        }

        // Button: BridgeChoice0, BridgeChoice1, ...
        public void OnBridgeChoice(int index)
        {
            string before = AnswerDraft();
            choice=index;
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: Rescue
        public void OnRescue()
        {
            Game.Activate();
            Refresh();
        }

        // Button: Module0, Module1, ...
        public void OnModule(int module)
        {
            if(Game.MissionSelectModule(module)){caseOverview=false;selectedWire=-1;selectedCard=-1;}
            Refresh();
        }

        // Button: CaseBack
        public void OnCaseBack()
        {
            caseOverview=true;
            Refresh();
        }

        // Button: DialExponentMinus
        public void OnDialExponentMinus()
        {
            string before = AnswerDraft();
            exponent=Mathf.Max(0,exponent-1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: DialExponentPlus
        public void OnDialExponentPlus()
        {
            string before = AnswerDraft();
            exponent=Mathf.Min(8,exponent+1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: DivisorMinus
        public void OnDivisorMinus()
        {
            string before = AnswerDraft();
            divisor=Mathf.Max(1,divisor-1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: DivisorPlus
        public void OnDivisorPlus()
        {
            string before = AnswerDraft();
            divisor=Mathf.Min(8,divisor+1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: InsertC
        public void OnInsertC()
        {
            string before = AnswerDraft();
            constant=!constant;
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: DialSubmit
        public void OnDialSubmit()
        {
            Game.MissionDial(exponent,divisor,constant);
            Refresh();
        }

        // Button: WireSource0, WireSource1, ...
        public void OnWireSource(int source)
        {
            selectedWire=source;BeginSelection(1,source);
            Refresh();
        }

        // Button: WireTarget0, WireTarget1, ...
        public void OnWireTarget(int target)
        {
            ConnectWire(selectedWire,target);
            Refresh();
        }

        // Button: CommonC
        public void OnCommonC()
        {
            Game.MissionCommonConstant();
            Refresh();
        }

        // Button: SequenceCard0, SequenceCard1, ...
        public void OnSequenceCard(int index)
        {
            selectedCard=index;BeginSelection(2,index);
            Refresh();
        }

        // Button: SequenceSlot0, SequenceSlot1, ...
        public void OnSequenceSlot(int index)
        {
            PlaceCard(selectedCard,index);
            Refresh();
        }

        // Button: SequenceSubmit
        public void OnSequenceSubmit()
        {
            int next=SequenceStep();if(next<4){Game.MissionSequence(order[next],Value("SequenceValue"));if(SequenceStep()>next)Control<TMP_InputField>("SequenceValue").SetTextWithoutNotify("");}
            Refresh();
        }

        // Button: ReleaseMinus
        public void OnReleaseMinus()
        {
            string before = AnswerDraft();
            release=Mathf.Max(0,release-1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: ReleasePlus
        public void OnReleasePlus()
        {
            string before = AnswerDraft();
            release=Mathf.Min(10,release+1);
            if (AnswerDraft() != before) Game.CanvasAnswerChanged();
            Refresh();
        }

        // Button: GaugeSubmit
        public void OnGaugeSubmit()
        {
            Game.MissionGauge(release);
            Refresh();
        }

        // Button: MasterCover
        public void OnMasterCover()
        {
            if(Game.Ready){Game.World.LiftMasterCover();cover=Game.World.MasterCoverOpen;}
            Refresh();
        }

        // Button: MasterActivate
        public void OnMasterActivate()
        {
            if(cover&&Game.Ready)Game.Activate();
            Refresh();
        }

        // Button: ControlsOpen
        public void OnControlsOpen()
        {
            EndDrag();modal="Controls";Game.CanvasOpenSupport(true);
                Text("ControlsTitle",topic==1?"Bridge controls":"Case controls");
                Text("ControlsBody",ControlsInstructions());Show("ControlsModal",true);
            Refresh();
        }

        // Button: ControlsClose
        public void OnControlsClose()
        {
            CloseModal();
            Refresh();
        }

        // Button: IntroductionContinue
        public void OnIntroductionContinue()
        {
            FinishIntroduction(false);
            Refresh();
        }

        // Button: IntroductionSkip
        public void OnIntroductionSkip()
        {
            FinishIntroduction(true);
            Refresh();
        }

        // Button: GuidePreference
        public void OnGuidePreference()
        {
            Game.Session.guideSkipped=!Game.Session.guideSkipped;Game.CanvasSaveGuide();
            Refresh();
        }

        // Button: IntroductionReplay
        public void OnIntroductionReplay()
        {
            CloseModal();OpenIntroduction(CurrentIntroduction());
            Refresh();
        }

        // Button: SummaryPractice
        public void OnSummaryPractice()
        {
            if(Game.State=="Summary"){modal="Practice";practicePanel.Open(Game.Session);}
            Refresh();
        }
    }
}
