# Phase 36 - Persistent Edit Mode UI rendering

The custom question-background, portal/graph and wire graphics now require a CanvasRenderer explicitly. Previously the player could acquire a renderer on demand while an Editor-loaded graphic could cache a missing component, leaving panels, icons or diagrams invisible until Play. The saved production UI prefabs are migrated to include the renderer before the graphic is enabled.

The repair adds components to the existing GameObjects. It preserves graphic settings and identities, UI positions, native controls, artwork, question banks and authored level geometry. Fresh prefab reloads verify that the renderers are actually saved. A second migration adds nothing further.

## Preview without Play

Open **Calculus Lab > Scene & UI Workspace**, open a level, select **UI preview > Screen**, then click **Show selected UI**. Differentiation and Integration retain their existing question, accepted-answer and device previews. Their sample progress labels use the current 18-step curricula; the independent Bridge example also shows the matching installed bridge sections.

Limits now offers Numerical approach, Graph marker, Agreeing sides, Limit and point value, Unequal sides, Independent numerical, Independent check, Check accepted and Portal ready, as well as Brief. The preview uses the actual question banks and native controls. It does not start a session, grade an answer or write a learning record. Values are explicitly marked as Editor sample data.

**Calculus Lab > Scene & UI > Repair saved UI graphics** is available for future repairs. Interactive use preserves unsaved scene edits through Unity's normal save prompt and reloads the open scenes after updating the prefabs, so old renderer caches are discarded. New graphics created by the existing builders receive their required renderer automatically.

Implementation: RequireComponent annotations in `SimulationAnswerBackdrop.cs`, `LimitsDiagram.cs` and `MissionConnections.cs`; `EditorPreviewRepair.cs`; `LimitsEditorPreview.cs`; targeted updates to the existing authoring workspace.

## Verification

Evidence: `qa/editor-preview-repair`.

The authoring capture now validates real, populated meshes on every active custom graphic, including the background, rounded panels, header emblems and portal/graph diagrams. The wire check explicitly draws an accepted connection. Validation uses normal canvas rendering, without forcing Graphic.Rebuild, overriding culling or replacing graphic components. The same checks are repeated after restarting the Editor process and loading the saved assets.

Development and release builds are checked alongside the existing real-player Bridge/Integration and ten-question Limits flows. The authored scene files and question-bank assets remain unchanged. The main-menu portal is included in the saved-component repair.

Final evidence includes 72 Edit Mode captures after a fresh Editor-process start, a passing 720p Bridge/Integration run (95 captures) and a passing 1080p Limits run (7 captures). A direct YAML comparison confirms that all 19 custom graphics previously lacked a saved renderer and now have one; all pre-existing component records are byte-identical apart from their GameObjects' added component references. Limits completes 10/10 answers, and Bridge/Integration each complete 18/18, including the existing deliberate retry and save/resume scenarios.
