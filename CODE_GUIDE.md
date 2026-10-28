# 代码导航

初学先读 [从这里开始.md](从这里开始.md)，从场景的 `Game Manager` 和按钮 `On Click()` 跟进代码。

## 日常手动修改入口

| 要改什么 | 入口 |
|---|---|
| 场景、玩家和 UI 的连接 | `Runtime/Game/GameManager.cs`，Hierarchy 的 Game Manager |
| 玩家移动与鼠标视角 | `Runtime/Player/LabPlayerController.cs`，玩家 Inspector 的速度和相机 |
| E / Escape 和交互距离 | `Runtime/Player/PlayerInteraction.cs` |
| 主菜单与 Limits 的按钮行为 | `Runtime/UI/Frontend/ProductionCanvasButtons.cs` |
| 修桥与 Integration 的按钮行为 | `Runtime/UI/Missions/MissionCanvasButtons.cs` |
| UI 对象引用 | `Runtime/UI/Shared/UIReferenceMap.cs`；在 Inspector 搜索并拖入目标 |
| 输入框 / 滑块的回调 | `ProductionCanvasInput.cs`、`MissionKeyboard.cs` |

上述按钮事件保存于 prefab，不再在 Start 时通过闭包绑定。共享题库、存档和游戏状态继续使用下面的现有核心逻辑。

`GameManager` 继承并复用 `Chapter4Game` 的现有规则；游戏运行状态属于场景中保存的 Game Manager 组件。`Chapter4World.gameManager` 是地图指向管理器的序列化引用。地图与 UI 都使用保存的场景对象，运行时不再提供自动生成地图或 UI 的后备路径。

日常开发打开 `CalculusLabEscape`。`SimulationSkyValidation` 是验证副本，避免把它当成正式项目继续编辑。Unity 版本：6000.3.10f1，Built-in 渲染管线。

## 目录分工

```text
Assets/_Project/Scripts/
├─ Runtime/              正式游戏运行代码
│  ├─ Game/              关卡状态、答题操作、进度和完成后的跳转
│  ├─ Data/              本地存档模型、题库、引导内容的数据类型
│  ├─ Learning/          错误诊断、学习回顾、针对性练习
│  ├─ UI/
│  │  ├─ Frontend/       主菜单、Limits 界面、记录和练习面板
│  │  ├─ Missions/       Differentiation / Integration 答题界面与输入
│  │  └─ Shared/         数学排版、图示、答题背景绘制
│  └─ World/             场景表现、装置状态、机械运动、救援和关闭动画
├─ Editor/               仅 Unity Editor 使用的工具
│  ├─ Assets/            素材导入、展示、场景装饰
│  ├─ Build/             游戏构建入口
│  ├─ UI/                UI prefab 的生成和局部升级
│  ├─ World/             天空、背景、修桥设备等场景工具
│  ├─ Questions/         题库创建与选择工具
│  ├─ Workspace/         不运行游戏时的 Scene / UI 编辑预览
│  ├─ Validation/        引用检查与学习反馈、练习检查
│  ├─ Studies/           独立 UI 风格试验
│  └─ Legacy/            旧 prototype 场景生成器
├─ Development/Playtests/ 自动试玩工具，保留 Editor / Development 条件编译
└─ Legacy/
   ├─ Prototype/         早期 prototype 的 Core、Gameplay、UI
   └─ Accounts/          保留的旧 HTTP 登录实现，之后再接 Firebase
```

`Legacy` 表示历史实现，不代表可以直接删除。部分代码仍被早期场景或现有 partial 类引用。

## 要改某个功能，先找这里

以下路径均相对于 `Assets/_Project/Scripts/`。

| 要改的功能 | 主要代码入口 |
| --- | --- |
| 三关的状态、计分、提交和保存流程 | [Chapter4Game.cs](Assets/_Project/Scripts/Runtime/Game/Chapter4Game.cs)、[MissionCanvasActions.cs](Assets/_Project/Scripts/Runtime/Game/MissionCanvasActions.cs)、[Chapter4CanvasActions.cs](Assets/_Project/Scripts/Runtime/Game/Chapter4CanvasActions.cs) |
| Differentiation / Integration 的 UI、刷新和按钮 | [MissionCanvas.cs](Assets/_Project/Scripts/Runtime/UI/Missions/MissionCanvas.cs) |
| 探索 HUD、移动后缩小的教学卡、交互范围与分段进度 | [ExplorationHUD.cs](Assets/_Project/Scripts/Runtime/UI/Missions/ExplorationHUD.cs)、[ExplorationPolishBuilder.cs](Assets/_Project/Scripts/Editor/UI/ExplorationPolishBuilder.cs) |
| Integration 追加题目的切换和确认界面 | [CaseCalibrationCanvas.cs](Assets/_Project/Scripts/Runtime/UI/Missions/CaseCalibrationCanvas.cs) |
| 拖放、键盘、操作帮助和首次引导 | `Runtime/UI/Missions/` 下的 `MissionDragSource`、`MissionDropTarget`、`MissionKeyboard`、`MissionControlsGuide`、`MissionOnboardingCanvas` |
| Limits UI、主菜单和记录 | [ProductionCanvas.cs](Assets/_Project/Scripts/Runtime/UI/Frontend/ProductionCanvas.cs)、[PracticeRecords.cs](Assets/_Project/Scripts/Runtime/UI/Frontend/PracticeRecords.cs)、[RecordMissionResume.cs](Assets/_Project/Scripts/Runtime/UI/Frontend/RecordMissionResume.cs) |
| 题目数据结构和校验 | `Runtime/Data/` 下的三个 `*QuestionBank.cs` |
| 本地进度、答题记录、读取保存 | [Chapter4Session.cs](Assets/_Project/Scripts/Runtime/Data/Chapter4Session.cs) |
| 错误提示、结果回顾、练习 | `Runtime/Learning/` 下的 `MissionFeedback`、`MissionReview`、`MissionPractice` |
| 三维关卡、装置状态和环境 | [Chapter4World.cs](Assets/_Project/Scripts/Runtime/World/Chapter4World.cs) 及同目录的 `Chapter4*Feedback`、`Chapter4EnvironmentArt` |
| 修桥机械、救援、Integration 关闭动画 | `Runtime/World/` 下的 `RepairStationMotion`、`BridgeRescueSequence`、`CaseShutdownSequence` |
| Laboratory AI 机器人、九个动作、眼睛和动态全息桥梁 | `Runtime/World/LaboratoryAIPreview.cs`、`LaboratoryAIBloom.cs`；`Editor/Assets/LaboratoryAIImport.cs`、`LaboratoryAIPreviewWindow.cs`；素材在 `Assets/_Project/Art/LaboratoryAI/`。菜单 **Calculus Lab > Laboratory AI > Open preview**，不用按 Play |
| 不按 Play 的 UI / 场景预览 | [SceneAuthoring.cs](Assets/_Project/Scripts/Editor/Workspace/SceneAuthoring.cs)、[SceneAuthoringWindow.cs](Assets/_Project/Scripts/Editor/Workspace/SceneAuthoringWindow.cs) |
| 构建正式版 / 开发版 | [ProductionUIBuilder.cs](Assets/_Project/Scripts/Editor/Build/ProductionUIBuilder.cs) 的 `BuildRelease` / `Build` |
| 自动试玩和回归场景 | `Development/Playtests/`；主要入口是 `MissionsPlaytest.cs` |

很多功能使用 `partial class`：例如 `MissionCanvas`、`Chapter4Game`、`Chapter4World` 和 `ProductionUIBuilder` 分布在多个文件中。查完整逻辑时按类名搜索；不要把这些文件误认为重复代码。整理没有修改类名、命名空间、菜单名或构建方法名。

## UI、题目和三维内容的实际编辑位置

- 全部 57 个 prefab：统一在 `Assets/_Project/Prefabs/`；机器人是根目录的 `LaboratoryAI.prefab`。菜单 **Calculus Lab > Assets > Open prefab folder** 可直接选中该目录。
- UI 布局：`Assets/_Project/Prefabs/Resources/Production/` 下的 `MainMenuUI`、`LimitsUI`、`DifferentiationUI`、`IntegrationUI` prefab。运行时仍按 `Production/{name}` 加载。
- 题目内容：`Assets/_Project/Resources/Production/` 下的 `LimitsQuestions.asset`、`LimitsIndependentQuestions.asset`、`DifferentiationQuestions.asset`、`IntegrationQuestions.asset`；可从 **Calculus Lab > Questions** 选择追加题库。
- 首次引导文字：同目录的 `MissionIntroductions.asset`。
- 正式场景：`Assets/_Project/Scenes/MainMenu.unity` 和三个 `Level0*` 场景。
- 导入的原始素材：`Assets/ThirdParty/`；素材试用 prefab：`Assets/_Project/Prefabs/AssetTrials/`；适配材质：`Assets/_Project/AssetTrials/Materials/`。

调整已有 UI 布局优先编辑保存的 prefab。`GenerateAndBuild` 会重新生成 UI；普通 `Build` / `BuildRelease` 保留手动布局。调整题目文字和答案优先编辑题库 asset，而不是素材预览脚本。

## 四套素材预览代码

统一在 `Editor/Assets/`，通过 `AssetTrials` 的 partial 类分工：

| 文件 | 职责 |
| --- | --- |
| `AssetTrials.cs` | 素材路径、样本列表、四个 Unity 菜单入口 |
| `AssetTrials.Import.cs` | FBX / PNG 导入、材质映射、样本 prefab |
| `AssetTrials.Materials.cs` | Built-in 材质、玻璃、灯光、线缆适配 |
| `AssetTrials.Scenes.cs` | 展示平台、场景、标签和键鼠提示卡片 |
| `AssetTrials.Validation.cs` | 采样模型自带动画、修整预览 |
| `AssetTrials.Capture.cs` | 读取保存的预览场景、检查文字、输出截图 |

这些是素材评估工具。正式玩法不会调用它们。预览场景仍可通过 **Calculus Lab > Assets > Trials** 打开。

外部下载和解压工具集中在仓库的 [`tools/assets/`](../../tools/assets/README.md)。一次性交付脚本和目录迁移记录分别归档在它的 `history/` 以及 `tools/maintenance/`，不作为日常构建入口。

## 整理后的检查

- 原有 110 个脚本的 `.meta` 随文件移动，保留 Unity 脚本 GUID。
- 正式运行脚本的内容保持不变；素材预览工具按职责拆分，入口保持不变。
- **Calculus Lab > Validation > Check scenes and prefab scripts** 会读取项目场景和 prefab，检查 Missing Script，并运行已有的学习反馈、练习和数学排版检查。
- 验证记录：`qa/code-organization-2026-10-19/`。整理前的脚本备份：`unity/backups/2026-10-19-code-organization/Scripts-before.zip`。

登录仍延后。当前 `Legacy/Accounts` 不是 Firebase 接入代码，日常游戏继续使用本地玩家和本地学习记录。

## 后续表现细化：Phase 39

修桥装置和探索提示的最新一轮细化见 [PRODUCTION_PHASE39.md](Docs/History/PRODUCTION_PHASE39.md)。入口是 `Editor/World/PresentationPolishBuilder.cs`；UI 和验证分别在 `Editor/UI/PresentationPolishUI.cs`、`Editor/Validation/PresentationPolishEvidence.cs`。三维文字的字体贴图绑定在 `Runtime/World/WorldLabelAtlas.cs`。对应样式、材质和 Shader 保存在 `Assets/_Project/Art/PresentationPolish/`。

## Integration 控制室细化（2026-10-25）

见 [第三关控制室细化说明](第三关控制室细化说明.md)。保存的环境是 `Assets/_Project/Prefabs/Resources/Production/IntegrationControlRoom.prefab`，制作入口为 `Editor/World/IntegrationPolishAuthoring.cs`，运行表现为 `Runtime/World/IntegrationRoomPresentation.cs`。后者读取现有四模块与关闭进度，控制服务柜状态、管线、核心和稳定环；地图已在编辑模式保存。场景中仍由原 GameManager 管理答题、计时和存档。
