# Phase 35 - Richer answer atmosphere

The question workspaces now sit within an illustrated simulation laboratory. Distant platforms, structural supports, ceiling beams, holographic display columns and light strips add depth behind the translucent work surfaces. The existing gradients, room grid and per-mission colors remain as a restrained overlay. The answer surface uses 78% opacity and the scene/portal panel 88%, keeping the educational content readable while showing the environment beneath it.

The scene monitors have corner brackets and a fine lower scale. Main work surfaces have a subtle edge scale, bottom light segments and, in Differentiation/Integration, a divider above the action row. A dashed connection runs through the gap between monitor and work surface. The header adds a small native vector emblem: portal, suspension bridge or device core.

## Assets and authoring

- `Assets/_Project/Art/UIAtmosphere/SimulationLabRoom-v1.png`: selected original image generated with the built-in imagegen tool, 1672 x 941, opaque. It is copied into the project and referenced by the saved UI prefabs.
- `Assets/_Project/Art/UIAtmosphere/SimulationLabRoom-prompt.txt`: complete final generation prompt and tool mode. The prompt requests a calm stylized simulation lab with quiet space behind overlaid questions, without text, UI widgets, people or logos.
- The PNG is imported explicitly as a 2D texture, with sRGB color, no mipmaps, clamp wrapping and a 2048-pixel import limit.
- Each Puzzle/SimulationAnswerArt group has a saved `SimulationRoomImage` RawImage beneath the native atmospheric overlay and work surfaces. Edit its texture, color and placement in the Inspector. Edit surface opacity and details on the existing `SimulationAnswerBackdrop` components. Header/MissionEmblem contains the native vector emblem.

Use **Calculus Lab > UI > Enrich answer atmosphere** to apply the artwork selectively and idempotently. Existing question controls, labels and positions are preserved. The new decoration has Raycast Target disabled and does not affect grading or session data. The existing Scene & UI Workspace previews render these additions without Play.

Implementation: `Scripts/Editor/AnswerAtmosphereBuilder.cs` and the additional serialized options in `Scripts/Production/SimulationAnswerBackdrop.cs`.

## Verification

Evidence is in `qa/answer-atmosphere`. Repeated prefab migration checks unchanged button/text counts, valid image references and layer order, and disabled decorative raycasts. Development and release builds succeed. Existing real-player mission and Limits checks exercise the actual textured UI, input, retries, support, pause, save/resume and final activation. The authoring checks render the mission previews in Edit Mode and reopen scenes without saving temporary poses.

The original authored scenes and question banks remain unchanged. The delivered game and source backup include the background texture, its import settings and the complete generation prompt.
