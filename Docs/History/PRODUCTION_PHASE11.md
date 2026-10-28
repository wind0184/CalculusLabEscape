# 第十一阶段：两关操作引导

日期：2026-10-11。

## 玩家能看到的变化

Differentiation 与 Integration 的设备侧栏新增 NEXT ACTION。提示跟随实际交互状态：芯片选择、放入槽位、修改指数、填写数值、选择表达式、答对后安装、顺序卡当前槽位、接线来源选择、连接共用 +C、返回模块总览，以及掀盖后的 Master Deactivation。

左侧 How to play / Free 打开当前设备的操作说明，覆盖修桥的拖放／输入／选择／救援和积分的总览／拨盘／接线／顺序／压力。说明介绍控制方式和提交时机，不提供当前题目的数值答案。可用返回按钮或 Esc 关闭，无需首次进入时强制完成教程。

说明打开期间复用现有支持窗口的暂停机制：暂停有效时间、倒计时、环境压力与装置音效。遮挡底层操作，不消耗心数，不新增答题或支持记录。关闭后保留当前输入和已接受进度。关闭游戏后的存档恢复规则维持原有行为：恢复已接受的进度，不恢复未提交的输入。

为放置提示，设备侧栏重新分配了步骤统计、桥梁部件列表、操作说明与手册按钮的位置。题干、答案区、数学上标、判题和关卡流程保持原有逻辑。

## 代码位置

- `Assets/_Project/Scripts/Production/MissionControlsGuide.cs`：根据当前 UI／关卡状态产生下一步提示及操作说明。
- `Assets/_Project/Scripts/Production/MissionCanvas.cs`：绑定操作说明入口，每次刷新设备时更新提示。
- `Assets/_Project/Scripts/Editor/MissionControlsBuilder.cs`：侧栏和说明窗口的选择性 Prefab 升级。
- `Assets/_Project/Scripts/Editor/MissionUIBuilder.cs`：未来重建两关 UI 时也生成这些控件。
- `Assets/_Project/Scripts/Production/MissionsPlaytest.cs`：实际 Canvas 点击、遮挡、暂停／恢复、状态提示与文字布局验证。

运行 `ProductionUIBuilder.UpgradeControlsAndBuild` 仅为 DifferentiationUI / IntegrationUI 添加本轮布局和窗口并构建开发版。此升级可重复调用，已有 ControlsModal 时不重复添加。普通 Build / BuildRelease 保留 Prefab。不要为更新运行 GenerateAndBuild，它会重新生成全部布局。

## 验证

证据在项目上两级的 `qa/`，所有自动游玩使用隔离存档。

- `controls-guide-build.log`：选择性升级两关 Prefab，开发版构建成功；原有判题、反馈、练习及数学格式检查通过。
- `controls-guide-720/result.txt`：1280×720 两关完整流程 PASS。
- `controls-guide-1080/result.txt`：1920×1080 两关完整流程 PASS。
- `controls-guide-release-build.log`：正常运行版构建成功，输出 `Builds/LearningLab/CalculusLabEscape.exe`，由 `../Start-LearningLab.cmd` 启动。正式程序集包含操作引导，不包含 MissionsPlaytest / CanvasPlaytest。
- 新增检查覆盖九种操作说明文字布局、真实 Canvas 按钮命中与底层遮挡、免费打开／关闭、计时和装置音效暂停、返回与 Esc 关闭后时间恢复，以及关键下一步提示。原有 11 步修桥、4 模块／10 步积分、失败重试、拖放、存档续关、结算练习和记录续做一起通过。

已查看实际 Unity 渲染的安装提醒、共用 +C 提醒及两个分辨率的说明窗口；无文字截断或控件重叠。这是自动流程与截图检查，尚未代替真人首次游玩测试。没有修改第一关或存档格式，本轮未另跑第一关和独立进程练习恢复测试。

源码检查点：`../backups/2026-10-11-controls-guide/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings、README、使用说明及阶段文档，不含 Library / Builds。
