# Laboratory AI / Unity preview

Imported from `outputs/laboratory-ai-live-bridge-v7-2026-10-23` into the main Unity 6000.3.10f1 project.

Think was revised in v7.2 to turn the palm upward beneath the chin and lean the head onto it. Updated source: `outputs/laboratory-ai-think-support-v7-2026-10-23`. The hand and chin stay visually in contact during the hold. The other eight motions retain their existing clips.

## Open

Run `unity/Open-Laboratory-AI.cmd`, or choose **Calculus Lab > Laboratory AI > Open preview** in Unity.
The preview window animates in Edit Mode; choose any of the nine motion buttons without pressing Play. Enable **Cycle through all motions** to watch the full set. The scene's on-screen buttons work in Play Mode.

- Scene: `Assets/_Project/Scenes/AssetPreviews/LaboratoryAIPreview.unity`
- Prefab: `Assets/_Project/Prefabs/LaboratoryAI.prefab`
- Original FBX files: `Models/`
- Extracted four-second clips: `Animations/`
- External materials and Built-in renderer shaders: `Materials/`, `Shaders/`

## Motions

Idle, Welcome, Explain, Think, Encourage, Remind, Cruise, TravelForward and TurnLeft.
Explain opens the panel with moving bridge deck, cables, hangers, a leftward scan and leftward telemetry. Encourage changes the eyes, halo, core and hover light to vivid green; Remind uses amber. Eye expressions are baked transform animation.

`LaboratoryAIPreview` samples the clips against the imported hierarchy and applies state colours through material property blocks. Moving clips include root motion. The preview repeats each selected motion; gameplay can call `Sample`, `PlayIndex` or `Advance` as appropriate.

The dedicated preview scene is separate from the three mission scenes. Game event triggers have not been connected.

## Rebuild and verify

**Calculus Lab > Laboratory AI > Import and build preview** regenerates external materials, clips, prefab and the preview scene from the FBX files. This replaces generated preview content; keep custom scene work separately.

**Validate and capture** renders all nine motions and checks clip duration, visible eye pairs, material support, green feedback and bridge movement. Evidence is saved under `qa/laboratory-ai-unity-2026-10-23` at the repository root.
