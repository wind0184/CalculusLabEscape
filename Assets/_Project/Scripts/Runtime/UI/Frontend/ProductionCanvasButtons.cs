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
    public sealed partial class ProductionCanvas
    {
        // Button: LoginSubmit
        public void OnLoginSubmit()
        {
            StartCoroutine(SignIn());
        }

        // Button: LoginExit
        public void OnLoginExit()
        {
            if(SceneSoundscape.Active!=null)SceneSoundscape.Active.QuitAfterButtonSound();else Application.Quit();
        }

        // Button: RegisterStudent
        public void OnRegisterStudent()
        {
            StartCoroutine(Register(false));
        }

        // Button: SetupTeacher
        public void OnSetupTeacher()
        {
            StartCoroutine(Register(true));
        }

        // Button: HomeLogout
        public void OnHomeLogout()
        {
            StartCoroutine(SignOut());
        }

        // Button: TeacherLogout
        public void OnTeacherLogout()
        {
            StartCoroutine(SignOut());
        }

        // Button: TeacherSearch
        public void OnTeacherSearch()
        {
            StartCoroutine(TeacherSearch());
        }

        // Button: TeacherView
        public void OnTeacherView()
        {
            recordPage=0;RefreshRecords();OpenPage("Records");
        }

        // Button: TeacherExport
        public void OnTeacherExport()
        {
            StartCoroutine(ExportTeacher());
        }

        // Button: TeacherStudents
        public void OnTeacherStudents()
        {
            StartCoroutine(ShowStudents());
        }

        // Button: StudentsBack
        public void OnStudentsBack()
        {
            OpenPage("Teacher");
        }

        // Button: DetailPractice
        public void OnDetailPractice()
        {
            // Read again rather than writing an older copy held by the records page.
                var saved=Chapter4Store.Read().FirstOrDefault(s=>s.id==selectedRecordId);
                if(!LearningApi.LocalPlay||teacherRecords||!MissionPractice.CanOpenRecord(saved,Chapter4Store.Profile))return;
                recordPracticeOpen=true;recordPractice.Open(saved);
        }

        // Button: HomeStart
        public void OnHomeStart()
        {
            if(!SaveProfile())return;
            GetComponentInChildren<MainMenuWelcome>(true)?.HideForEntrance();
            if(sceneController!=null)sceneController.EnterLaboratory(()=>OpenPage("Topics"));
            else OpenPage("Topics");
        }

        // Button: HomeContinue
        public void OnHomeContinue()
        {
            if (SaveProfile()) ResumeLatest();
        }

        // Button: HomeRecords
        public void OnHomeRecords()
        {
            if (SaveProfile()) { recordPage = 0; RefreshRecords(); OpenPage("Records"); }
        }

        // Button: HomeInstructions
        public void OnHomeInstructions()
        {
            OpenPage("Instructions");
        }

        // Button: HomeSettings
        public void OnHomeSettings()
        {
            OpenPage("Settings");
        }

        // Button: HomeExit
        public void OnHomeExit()
        {
            OpenPage("Exit");
        }

        // Button: ExitCancel
        public void OnExitCancel()
        {
            OpenPage("Home");
        }

        // Button: ExitConfirm
        public void OnExitConfirm()
        {
            if(SceneSoundscape.Active!=null)SceneSoundscape.Active.QuitAfterButtonSound();else Application.Quit();
        }

        // Button: TopicsBack
        public void OnTopicsBack()
        {
            OpenPage("Home");
        }

        // Button: Mission0
        public void OnMission0()
        {
            ResumeId = null; GameManager.ChangeScene(Chapter4Game.Scenes[0]);
        }

        // Button: Mission1
        public void OnMission1()
        {
            GameManager.ChangeScene(Chapter4Game.Scenes[1]);
        }

        // Button: Mission2
        public void OnMission2()
        {
            GameManager.ChangeScene(Chapter4Game.Scenes[2]);
        }

        // Button: InstructionsBack
        public void OnInstructionsBack()
        {
            OpenPage("Home");
        }

        // Button: RecordsBack
        public void OnRecordsBack()
        {
            OpenPage("Home");
        }

        // Button: RecordsPrevious
        public void OnRecordsPrevious()
        {
            recordPage--; RefreshRecords();
        }

        // Button: RecordsNext
        public void OnRecordsNext()
        {
            recordPage++; RefreshRecords();
        }

        // Button: DetailBack
        public void OnDetailBack()
        {
            RefreshRecords();OpenPage("Records");
        }

        // Button: Record0, Record1, ...
        public void OnRecord(int row)
        {
            OpenRecord(recordPage * 5 + row);
        }

        // Button: SettingsDisplay
        public void OnSettingsDisplay()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        // Button: SettingsBack
        public void OnSettingsBack()
        {
            if(!Chapter4Store.IsolatedRun){PlayerPrefs.SetFloat("C4Volume", AudioListener.volume); PlayerPrefs.Save();} OpenPage("Home");
        }

        // Button: BriefStart
        public void OnBriefStart()
        {
            sceneController?.briefingVideo?.LeaveBriefing();
            game.StartSession();
        }

        // Button: BriefBack
        public void OnBriefBack()
        {
            sceneController?.briefingVideo?.LeaveBriefing();
            game.CanvasSaveAndLeave();
        }

        // Button: WorldPause
        public void OnWorldPause()
        {
            Pause();
        }

        // Button: PuzzlePause
        public void OnPuzzlePause()
        {
            Pause();
        }

        // Button: PuzzleWorld
        public void OnPuzzleWorld()
        {
            game.SetState("Explore");
        }

        // Button: Submit
        public void OnSubmit()
        {
            Submit();
        }

        // Button: Continue
        public void OnContinue()
        {
            if(sceneController!=null)sceneController.ContinueCalibration();else game.CanvasContinue();
        }

        // Button: ActivatePortal
        public void OnActivatePortal()
        {
            game.Activate();
        }

        // Button: PuzzleSupport
        public void OnPuzzleSupport()
        {
            OpenSupport();
        }

        // Button: SupportClose
        public void OnSupportClose()
        {
            CloseSupport();
        }

        // Button: SupportTier0, SupportTier1, ...
        public void OnSupportTier(int n)
        {
            UseSupport(n);
        }

        // Button: Conclusion0, Conclusion1, ...
        public void OnConclusion(int n)
        {
            if(answer.conclusion!=n){answer.conclusion=n;LimitsAnswerChanged();} RefreshReason();
        }

        // Button: ReasonCard0, ReasonCard1, ...
        public void OnReasonCard(int n)
        {
            if(selectedCard!=n){selectedCard=n;LimitsAnswerChanged();} RefreshReason();
        }

        // Button: ReasonSlot0, ReasonSlot1, ...
        public void OnReasonSlot(int n)
        {
            if (selectedCard < 0||answer.reason[n]==selectedCard) return; for (int j = 0; j < 3; j++) if (answer.reason[j] == selectedCard) answer.reason[j] = -1; answer.reason[n] = selectedCard; LimitsAnswerChanged(); RefreshReason();
        }

        // Button: PauseResume
        public void OnPauseResume()
        {
            ResumePause();
        }

        // Button: PauseLeave
        public void OnPauseLeave()
        {
            modal = "Leave"; Show("LeaveModal", true);
        }

        // Button: LeaveCancel
        public void OnLeaveCancel()
        {
            modal = null; Show("LeaveModal", false);
        }

        // Button: LeaveConfirm
        public void OnLeaveConfirm()
        {
            game.CanvasSaveAndLeave();
        }

        // Button: SummaryReplay
        public void OnSummaryPractice()
        {
            if(isMenu||game==null||game.State!="Summary"||!LearningApi.LocalPlay)return;
            modal="Practice";recordPracticeOpen=true;recordPractice.Open(game.Session);
        }

        // Button: SummaryReplay
        public void OnSummaryReplay()
        {
            GameManager.ChangeScene(Chapter4Game.Scenes[0]);
        }

        // Button: SummaryHome
        public void OnSummaryHome()
        {
            game.CanvasSaveAndLeave();
        }

        // Button: SummaryRecords
        public void OnSummaryRecords()
        {
            Chapter4Frontend.ReturnPage = "Dashboard"; GameProgress.ReturnToMenu();
        }

        // Button: DetailResume
        public void OnDetailResume()
        {
            ResumeSelectedMission();
        }
    }
}
