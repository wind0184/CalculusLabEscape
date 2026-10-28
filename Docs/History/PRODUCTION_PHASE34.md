# Phase 34 - Simulation answer backgrounds

The three question workspaces replace the flat black backdrop with a blue-gray simulation room and two rounded work surfaces. The backdrop has soft color gradients, a faint perspective floor grid and edge lighting. The main answer surface and the scene/portal panel have restrained borders, a subtle vertical gradient and a small shadow. Mission accents are blue for Limits, jade for Differentiation and blue-violet for Integration.

The artwork is decorative and static. It adds no labels or buttons, and all new Graphics have Raycast Target disabled. The existing question controls, mathematical content, input positions, grading and save logic are unchanged. The extra footer frame from the initial visual draft was removed, and Limits' answer surface starts slightly higher to provide space above its question kicker.

## Saved UI and editing

`SimulationAnswerArt` is the first child of the Puzzle page in each of `LimitsUI`, `DifferentiationUI` and `IntegrationUI`. Its saved `SimulationAnswerSky`, `AnswerGlassSurface` and `DeviceGlassSurface` objects use `SimulationAnswerBackdrop`, a native Unity UI Graphic that renders in Edit Mode. Panel colors, corner radius, light tint, borders and RectTransform dimensions are editable in the Inspector. No raster assets, custom shaders or runtime-created controls are needed.

Use **Calculus Lab > Scene & UI Workspace** to open Differentiation or Integration and show the existing question previews without Play. **Calculus Lab > UI > Apply simulation answer backgrounds** selectively applies missing artwork to the three UI prefabs; it does not regenerate scenes. The original scene-preview textures and world geometry remain in place.

Implementation: `Scripts/Production/SimulationAnswerBackdrop.cs` and `Scripts/Editor/AnswerBackgroundBuilder.cs`.

## Verification

Evidence is in `qa/answer-background`:

- Repeated migration validates unchanged button/text counts, all three saved background surfaces and no decorative raycast targets. Development and release builds succeed.
- Existing real-player mission checks cover answer selection, number input, drag/drop, retries, hints, pause, save/resume, installation, module completion, rescue and Master Deactivation.
- Existing Limits checks cover all ten questions, paired calibration, charge application, hints, retries, save/resume and portal activation.
- Edit Mode captures reuse the authoring checks across the saved worlds and mission UI previews; they reload scenes without saving preview poses and do not create a gameplay session.
- Actual screenshots are inspected for the new backgrounds, panel spacing and text readability. The three authored scene files remain unchanged; delivery contains only the UI artwork and scripts for this change.

The normal player remains `Builds/LearningLab/CalculusLabEscape.exe`.
