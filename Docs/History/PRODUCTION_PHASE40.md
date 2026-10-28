# Phase 40 — exploration HUD refinement

Both Differentiation and Integration share a more compact exploration presentation.

- The objective card is 900 × 86 at the 1280 × 720 reference resolution. Three repair segments or four module segments track installed/completed work, rather than individual assessed answers.
- The first movement guide is 474 × 136; after moving it contracts to 474 × 68 with one short reminder. The saved guide preference and G shortcut still control visibility.
- The action bar separates navigation from interaction state. APPROACH changes to IN RANGE with a brighter border only when the existing gameplay proximity check permits interaction.
- Existing keyboard/mouse controls and gameplay remain in place.

`Runtime/UI/Missions/ExplorationHUD.cs` owns presentation state and layout. The component and references are saved in the two mission prefabs and run in Edit Mode as well as Play. `MissionOnboardingCanvas` provides live movement/proximity/completion values. `SceneAuthoring` supplies matching Editor preview values; no session is started by a preview.

`Editor/UI/ExplorationPolishBuilder.Apply` upgrades just the two saved prefabs. Its `ApplyAndBuild` method captures initial, moving, nearby and completed HUDs, validates all existing UI previews, then builds both Windows players. This pass does not rewrite scene files, so existing scene edits can remain open in the primary Editor.

Evidence: repository `qa/exploration-polish-2026-10-19/`. Full gameplay QA also checks that real movement compacts the guide, proximity changes the interaction state, decorative progress bars cannot intercept input, and E opens the device. QA uses a separate session folder.

Backups: `unity/backups/2026-10-19-exploration-polish/Before.zip` (scripts, prefabs and scenes) and `After.zip` (delivered delta).
