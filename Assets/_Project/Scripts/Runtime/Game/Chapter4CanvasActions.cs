using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        public bool UsesCanvas { get; private set; }
        public bool CanvasAccepted => accepted;
        public bool CanvasNearStation => NearStation();
        public bool CanvasSupportOpen => supportModal;
        public string CanvasFeedback => feedback;
        bool canvasSessionStarted;

        public void EnableCanvas() { UsesCanvas = true; }
        public void CanvasClearFeedback() { feedback = ""; showRetry = false; }
        public void CanvasAnswerChanged()
        {
            if (State == "Puzzle" && !accepted && !supportModal) CanvasClearFeedback();
        }
        public void CanvasSaveGuide(){if(canvasSessionStarted)Save();}
        public void CanvasSubmit(LimitsQuestion question, LimitsAnswer answer)
        {
            if (Topic != 0 || State != "Puzzle" || Ready || accepted || supportModal || PresentationBusy) return;
            if(LimitsExtension!=null)question=LimitsExtension;
            var result = LimitsRules.Evaluate(question, answer);
            if (!result.complete) { feedback = "Complete all answer controls before submitting."; return; }
            var note = MissionFeedback.Limits(question, answer);
            accepted = ValidateStep(question.id, result.response, result.correct, note.code, note.message);
            // Canvas gives inline, persistent feedback instead of the legacy IMGUI modal.
            showRetry = false;
        }
        public void CanvasContinue() { if (State == "Puzzle" && accepted && !Ready&&!supportModal&&!PresentationBusy){if(Topic==1&&ContinueBridgeExtension()||Topic==0&&ContinueLimitsExtension())return;Advance();} }
        public void CanvasOpenSupport(bool open) { supportModal = open;if(World.CaseAudio!=null)World.CaseAudio.SetPaused(open||State=="Paused"); }
        public string CanvasHint(LimitsQuestion question, int tier)
        {
            if (State != "Puzzle" || Ready || !supportModal) return "Return to an active question to request support.";
            if (!Session.Hint(question.id, tier > 0)) return "No Support Hearts left. Basic hints remain free.";
            Session.support.Last().type = tier == 2 ? "GuidedDemonstration" : tier == 1 ? "RuleReminder" : "BasicHint";
            Save();
            return tier == 2 ? question.demonstration : tier == 1 ? question.ruleReminder : question.basicHint;
        }
        public void CanvasSaveAndLeave()
        {
            if (canvasSessionStarted) { Save(); if (!string.IsNullOrEmpty(Chapter4Store.LastError)) return; }
            Chapter4Frontend.ReturnPage = "Home";
            GameProgress.ReturnToMenu();
        }
        public void CanvasResume(Chapter4Session session)
        {
            if (session == null || session.topic != 0 || session.profile != Chapter4Store.Profile || session.status != "InProgress") return;
            Session = session; Stage = 0;
            while (Stage < 5 && session.completed.Contains("L" + (Stage + 1))&&PendingLimitsExtension(Stage)==null) Stage++;
            ResetInputs(); World.Restore(Stage); canvasSessionStarted = true;
            saveAt = Session.elapsed + 10; SetState("Explore");
            LimitsExtension=Stage<5&&Done("L"+(Stage+1))?PendingLimitsExtension(Stage):null;
        }
    }
}
