# Phase 33 - Question flow and feedback details

The expanded question banks now show where the learner is within the current repair or calibration. The new presentation uses saved native Canvas objects in `DifferentiationUI`, `IntegrationUI` and `LimitsUI`; it does not create controls at runtime.

- Differentiation's independent questions show their index within the active component, the accepted check count and the remaining installation action. The Converter displays four checks; the final anchor displays two. Completing a check does not imply that a component has been installed.
- Integration shows the guided task, independent checks and module stability as separate phases. Accepted options receive a green fill and border; selected answers are cyan before acceptance. The second independent answer marks the module stable, while Master Deactivation remains a separate final device action.
- Limits shows guided task, independent check and application as three phases. After accepting both answers, the charge remains unchanged until Apply calibration. The left panel explicitly explains the pending 20% charge.
- The Bridge sidebar now distinguishes continuing to another check from applying a repair.

`QuestionJourney.cs` binds the saved phase panels, captions and contextual detail labels. It also provides the answer-selection colors used by the existing native buttons. `QuestionDetailBuilder.cs` adds missing groups selectively and can be run repeatedly without duplicating them. It preserves authored scenes, question content, grading, support costs and historical session versions.

## View and edit without Play

Open **Calculus Lab > Scene & UI Workspace**, open Differentiation or Integration, then choose **UI preview > Screen > Independent check** or **Check accepted** and press **Show selected UI**. These previews use sample data and never start a gameplay session. Returning to another preview hides the calibration panel and resets choice colors.

Edit the `QuestionJourney` hierarchy in the respective UI prefab for shared layout changes. The phase images, labels, heading and detail text are serialized, editable Unity UI objects. The Limits group is beneath QuestionBody; the mission groups are beneath Puzzle. Runtime code updates their contents from actual accepted question IDs.

## Verification

Evidence: `qa/question-detail-polish`.

- Repeated prefab migration and development/release builds succeed. The three authored level scenes remain byte-identical to their pre-change versions.
- Actual 720p and 1080p Bridge-to-Integration runs pass, with 95 screenshots each, including a retry before acceptance. Checks cover phase counts, green accepted choices, incomplete module gates, installation, both save/resume flows, physical world feedback, rescue and master shutdown.
- Actual 720p and 1080p ten-question Limits runs pass, seven screenshots each. Both-answer acceptance still requires a separate application; the new detail text and phase state are checked against that gate.
- Following the final Bridge sidebar wording change, the delivered build repeats both 720p flows. No grading or curriculum change is introduced by that correction.
- Edit Mode captures validate independent, accepted and subsequent original views for both missions: no overflowing text, stale visible calibration panel or gameplay session. The scenes are reloaded without saving these temporary previews.

Delivery remains `Builds/LearningLab/CalculusLabEscape.exe`; the development harness is in `Builds/Chapter4/CalculusLabEscape_Chapter4.exe`.
