# Light laboratory - alternative UI study

This is a separate visual comparison requested after the dark simulation-lab UI. It uses warm-white work surfaces, dark formula text, jade primary actions and pale green selected answers. The existing laboratory illustration is softened beneath a light veil. Instrument scales and strong background grid lines are hidden for a calmer teaching-laboratory appearance.

The study reuses real Unity Canvas controls and the original question content. It includes unanswered and accepted examples for Differentiation and Integration. Sample progress is displayed in the screenshots; no learner session is started or graded. The production dark UI, level scenes, question banks and built players are preserved.

## View without Play

Use **Calculus Lab > UI studies > Open light laboratory - Bridge** or **Open light laboratory - Integration** to open the question preview prefab. The four editable assets are under `Assets/_Project/EditorPreviews/LightLab`. Their MissionCanvas presenters are disabled because these are static design previews. They are outside Resources and are not wired into the production scenes.

The saved scene-monitor PNGs are actual Unity camera captures. They let the preview prefabs show the matching world/device image without requiring a live scene or a running game. Background color, panel opacity, text colors and button appearance remain editable native UI properties. The preview graphics have persistent CanvasRenderer components so their native panel meshes can render in Edit Mode.

**Generate light laboratory previews** recreates the comparison assets from the current production UI. Interactive use first offers to save any unsaved scene edits. The generator opens the original scenes for rendering, saves separate standalone preview prefabs, then reloads the scenes without saving temporary preview changes.

Implementation: `Assets/_Project/Scripts/Editor/LightLabStyleStudy.cs`.

## Verification

Evidence: `qa/light-lab-style`. Four 1280 x 720 previews are rendered in Edit Mode. Button and text counts are preserved, active text fits, native panel meshes are present, monitor snapshots are persistent assets and no Chapter4Game/session is created. Production scene/prefab hashes and the existing player assemblies are checked before delivery. No gameplay build is needed for this preview-only change.
