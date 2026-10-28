# Phase 39 — Machinery and exploration presentation

This pass applies imported asset data to the production levels and refines the existing dark simulation presentation. It keeps the current questions, assessment rules, device interactions and original movement collision.

## Saved scene and UI changes

- Bridge: eight Factory Kit model instances across the three existing repair devices. Power Core gains a pipe valve and monitor; Converter Box gains three pipe connections and a monitor; Bridge Builder gains a posed articulated arm and lever. Materials are adapted to the existing steel/copper palette in `Assets/_Project/Art/PresentationPolish/`.
- Service platforms add machined rims, caution marks and a stage rail bound to each station's original indicator. Existing rotor, channel and winch animation bindings are preserved. The added robot arm uses a saved joint pose; it does not introduce a new runtime animation controller.
- Nameplate proportions and text depth are corrected. `WorldLabelAtlas` binds the live font atlas to a persistent depth-tested text material, so foreground equipment can occlude background labels.
- Bridge and Integration exploration pages have rounded glass panels and a native keyboard/mouse hint strip. Navigation text concentrates on direction, distance and the active device; the separate legend shows Move, Look, Inspect, Pause and Guide.
- Integration's front identity and module/master status use separate physical plates. The disabled Master Cover action is dark with an explicit locked label. Four completed modules enable the cover; the subsequent Master Deactivation remains a separate action.

All geometry and UI are saved and visible in Edit Mode. **Calculus Lab > Scene & UI Workspace** still previews the answer screens and first-use guides without Play. **Calculus Lab > Presentation > Apply machinery and controls polish** applies missing presentation elements; ordinary builds preserve saved edits.

## Code

- `Scripts/Editor/World/PresentationPolishBuilder.cs`: materials, Factory instances and scene dressing.
- `Scripts/Editor/UI/PresentationPolishUI.cs`: native hint strip, rounded surfaces and locked-button styling.
- `Scripts/Editor/Validation/PresentationPolishEvidence.cs`: saved-scene validation and real Unity camera captures.
- `Scripts/Runtime/World/WorldLabelAtlas.cs` and `Art/PresentationPolish/WorldLabel.shader`: depth-tested 3D text using the live font atlas.
- Small presentation updates in `MissionCanvas`, `Chapter4BridgeFeedback` and `SceneAuthoring`: navigation copy, lock state label/contrast and matching editor previews.

`RebuildOwnedDetails` and `RefineAndBuild` are maintenance entry points for rebuilding this pass's own detail groups. They are not ordinary play or build steps and can replace manual edits inside those groups.

## Validation and delivery

- Existing world/session references, movement colliders and machinery animation bindings are checked while applying the Bridge details.
- 71 Edit Mode world/UI captures validate persistence, custom graphics and text fit across all three topics.
- Full Bridge and Integration Canvas playthroughs pass: movement, chip/card/wire input, retries, expanded questions, installation, pause/support, saved progress, rescue, four modules, cover gating, final device shutdown and learning records.
- Both development and normal Windows builds succeed and are copied to the primary project. `Start-LearningLab.cmd` launches the updated normal build.
- Only seven existing source/scene/prefab files change, alongside scoped new presentation assets/source. The other 3,024 existing asset/package/settings files match the baseline.

Evidence: repository `qa/presentation-polish-2026-10-19/`. Backups: `unity/backups/2026-10-19-presentation-polish/`. QA uses an isolated session folder and does not modify the user's local learning records.
