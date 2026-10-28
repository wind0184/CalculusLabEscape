# Phase 37 — Clear stale answer feedback

Answer edits now clear the previous inline verdict in Differentiation, Integration and Limits. A fresh submission supplies the next verdict. Editing does not submit an answer, reset accepted work, change accuracy, consume hearts or rewrite saved attempt feedback.

The native controls cover coefficients, exponents, answer choices, number/fraction fields, integration dials and +C, wire/card selections and placement, pressure release, Limits conclusions/reason cards and the answer marker. Clicking an unchanged dial limit or the same choice does not count as an edit. Successful locked answers keep their current success feedback.

Opening an Integration independent calibration clears the previous guided question's success message before rendering the new question. Advancing to the next calibration also starts without the earlier verdict. Programmatic field resets use SetTextWithoutNotify so clearing a sequence input after a successful step does not accidentally erase that step's new feedback.

Implementation: `Chapter4CanvasActions.cs`, `MissionCanvas.cs`, `MissionKeyboard.cs`, `CaseCalibrationCanvas.cs` and `ProductionCanvas.cs`. Scene layouts, native prefabs, question banks, curriculum versions and grading rules are unchanged. This change addresses the shared feedback finding in the AI playtest dated 2026-10-18; the separate first-use installation wording and Finish module navigation findings remain pending.

## Verification

Evidence: `../../qa/feedback-clear-2026-10-18`.

- Development and release Windows builds succeeded.
- Full Differentiation → Integration player test passed at 1280×720: both 18-step curricula, retry history, installation/module gates, rescue/shutdown, hints, save/resume and learning records; 97 actual Canvas screenshots.
- Expanded Limits player test passed at 1920×1080: 10 questions, retries, five gated calibrations, hints, save/resume and final portal activation; 8 screenshots.
- Added 67 feedback assertions across those workflows. They check edited drafts and fresh questions have no stale verdict, and editing preserves assessment evidence. Existing accepted-answer, duplicate-submit, results and saved-attempt checks also pass.
- All 16 authored scenes, UI prefabs and question/data assets included in the before/after audit are byte-identical. No learner saves were used or removed; tests use isolated folders.

Key images: `missions-720/feedback-bridge-edited.png`, `missions-720/feedback-integration-new-question.png` and `limits-1080/feedback-limits-edited.png` inside the evidence folder. The original AI playtest evidence remains intact.
