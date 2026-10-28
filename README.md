# Calculus Lab Escape

**学习和手动修改先读：[从这里开始.md](从这里开始.md)。**

四个正式场景的 `Game Manager` 是入口。玩家、场景、UI 通过 Inspector 引用连接；按钮和输入框的事件保存到 UI prefab。选择按钮即可在 `On Click()` 看到具名方法。

`GameManager.cs` 自身承载现有关卡规则和会话状态，已保存到场景，按 Play 时不会再往地图添加管理组件。三张地图及 UI 都在运行前保存好；生成工具只在 Edit Mode 使用。Calculus Lab 菜单继续保留为辅助工具。

- 移动：`Runtime/Player/LabPlayerController.cs`；E / Escape：`PlayerInteraction.cs`。
- 主菜单 / Limits 按钮：`Runtime/UI/Frontend/ProductionCanvasButtons.cs`。
- 修桥 / Integration 按钮：`Runtime/UI/Missions/MissionCanvasButtons.cs`。
- UI 引用：界面根对象的 `UIReferenceMap`，Inspector 支持搜索和替换目标。
- 旧阶段记录已归入 `Docs/History/`。普通编辑直接保存场景 / prefab，普通构建保留手动修改。

Unity **6000.3.10f1**, Built-in render pipeline. The primary project is `unity/CalculusLabEscape`; `unity/SimulationSkyValidation` is a separate verification checkout.

## Run and edit

To get this repository on another computer, install Git LFS and clone the project:

```sh
git lfs install
git clone https://github.com/wind0184/CalculusLabEscape.git
cd CalculusLabEscape
git lfs pull
```

Robot animation clips and the Main Menu entrance animation clips use Git LFS. Clone with Git LFS so Unity receives the complete animation assets. In Unity Hub, add the cloned `CalculusLabEscape` folder and open it with **6000.3.10f1**. The repository includes `Assets`, `Packages` and `ProjectSettings`; Unity regenerates `Library` on first import. Standalone `Builds` are generated locally.

- Run `../Start-LearningLab.cmd` for the saved release build.
- In Unity, open `Assets/_Project/Scenes/MainMenu.unity` and press Play.
- No backend or login is required. Firebase authentication remains deferred; current sessions use the local player profile.
- Use **Calculus Lab > Scene & UI Workspace** to inspect saved UI and world content without Play.
- Use **Calculus Lab > Assets > Trials** for the four imported asset preview scenes, or launch `../Open-Asset-Trials.cmd`.

## Code navigation

Start with **[CODE_GUIDE.md](CODE_GUIDE.md)** for the directory map and the exact files to edit for UI, questions, gameplay, saves, worlds, builds and assets.

| Folder under `Assets/_Project/Scripts/` | Responsibility |
| --- | --- |
| `Runtime/Game` | Shared mission state and answer actions |
| `Runtime/Data` | Local session models and question banks |
| `Runtime/Learning` | Feedback, review and focused practice |
| `Runtime/UI` | Frontend, mission controls and shared UI rendering |
| `Runtime/World` | World presentation, repair equipment and sequences |
| `Editor` | Authoring, asset previews, builds and validation |
| `Development/Playtests` | Editor/development-only automated playtests |
| `Legacy` | Preserved early prototypes and the previous HTTP account implementation |

Keep `partial class` files together logically when reviewing a feature: `Chapter4Game`, `Chapter4World`, `MissionCanvas` and `ProductionUIBuilder` span multiple files. Script filenames, namespaces, Unity GUIDs and public build/menu methods are preserved by the directory cleanup.

## Saved content and builds

The latest pass refines the exploration HUD: compact movement guides, three/four segment progress, and proximity-aware interaction presentation. See [PRODUCTION_PHASE40.md](Docs/History/PRODUCTION_PHASE40.md). Imported equipment and the previous status/controls polish are documented in [PRODUCTION_PHASE39.md](Docs/History/PRODUCTION_PHASE39.md).

- All 57 prefab files are grouped under `Assets/_Project/Prefabs/`. **Calculus Lab > Assets > Open prefab folder** selects this folder.
- Robot: `Assets/_Project/Prefabs/LaboratoryAI.prefab`, with all nine motions and the current palm-up, chin-support Think.
- Native UI prefabs: `Assets/_Project/Prefabs/Resources/Production/{MainMenuUI,LimitsUI,DifferentiationUI,IntegrationUI}.prefab`. Their runtime resource keys remain `Production/{name}`.
- Question assets: `LimitsQuestions`, `LimitsIndependentQuestions`, `DifferentiationQuestions`, `IntegrationQuestions` under `Assets/_Project/Resources/Production/`. First-use copy: `MissionIntroductions.asset` in that data folder.
- Gameplay scenes: `MainMenu.unity` and the three `Level0*` scenes in `Assets/_Project/Scenes/`.
- `ProductionUIBuilder.BuildRelease` creates `Builds/LearningLab`; `Build` creates the development player in `Builds/Chapter4`. Ordinary builds preserve saved prefab layouts. `GenerateAndBuild` regenerates UI and can overwrite layout edits.
- Original models, textures and vendor materials remain under `Assets/ThirdParty/`. Imported sample prefabs are grouped under `Assets/_Project/Prefabs/ThirdParty/`; trial prefabs are under `Assets/_Project/Prefabs/AssetTrials/`, with trial materials in `Assets/_Project/AssetTrials/Materials/`. Preview scenes are outside player build settings.

New sessions assess 10 Limits answers, 18 Differentiation steps and 18 Integration steps. Bridge completion requires three installed components and final stabilisation. Integration requires four completed modules, followed by a separate cover and Master Deactivation. Versioned old saves retain their earlier question counts. Local data is stored in `Application.persistentDataPath/Chapter4Sessions`; code organization does not migrate or reset those records.

## Checks and implementation history

- **Calculus Lab > Validation > Check scenes and prefab scripts** loads the saved project scenes and prefabs, checks for missing script components and runs existing feedback, practice and math notation checks.
- Code cleanup evidence: repository `qa/code-organization-2026-10-19/`; pre-cleanup source backup: `unity/backups/2026-10-19-code-organization/Scripts-before.zip`.
- Latest asset trials: [../ASSET_TRIALS_2026-10-18.md](../ASSET_TRIALS_2026-10-18.md); external tooling: [../../tools/assets/README.md](../../tools/assets/README.md).
- The detailed feature history and phase links are preserved in [Docs/PROJECT_HISTORY_2026-10-18.md](Docs/PROJECT_HISTORY_2026-10-18.md). Phase documents record their original implementation dates; use the current code guide for source locations.
