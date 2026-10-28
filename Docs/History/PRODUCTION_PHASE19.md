# 第十九阶段：保留存档的重新开始

日期：2026-10-13。

Differentiation 和 Integration 的暂停页新增 Restart this mission。点击后仍保持暂停，打开确认页，说明当前 run 会先保存，新一轮从 Brief 开始，重置未提交输入、已完成答案、Support Hearts、计时和首次引导。原有成绩和练习记录不删除。

确认页默认选择 Cancel / stay paused，按 Enter 会取消；Esc 或取消按钮只关闭确认页，仍留在 Pause。Resume 返回原来 Explore / Puzzle；数字草稿、芯片／接线等当前选择保留。确认页打开时，底层暂停页的 CanvasGroup 不可交互，防止鼠标或键盘从确认页误触 Resume；计时、压力与积分音效保持暂停。

Save & start new run 只有在实际开始过的 InProgress 任务处于 Paused 时有效。先保存原 session，成功后清空 ResumeId、加载同一关并创建新的 session ID；保存失败则保留确认页和原任务，显示简短重试提示。不会覆盖旧 session，也不把重新开始算作一次数学尝试。

新的 Brief 尚不写入任务记录或启动计时。若此时返回主菜单，Home Continue 仍可恢复刚才保存的旧任务。开始新一轮后，它成为新的未完成记录；主菜单 Continue 按原规则优先选择最近的未完成记录。历史记录中的旧答案和成绩依然保留。

桥梁启动时显式恢复未安装桥面，并重置小牛与镜头；积分沿用原有初始装置复位。动画控制的完成预览不会直接成为新一轮的完成状态；静态模型和场景编辑仍沿用已保存资源。

## Editor 与源码

- Calculus Lab → Scene & UI Workspace → Pause，查看新增入口；Restart confirmation 查看确认页，两关都无需 Play。
- 共享 UI：Resources/Production/DifferentiationUI.prefab、IntegrationUI.prefab。Pause → PauseRestart；DesignFrame → RestartModal 保存标题、说明、错误提示和两个操作按钮。
- Production/MissionRestart.cs：暂停确认、取消、默认键盘焦点、保存失败、保存后加载新一轮。
- Editor/MissionRestartBuilder.cs：定向升级两个 prefab；旧 RectTransform 的位置和尺寸不变；重复执行不生成重复窗口。正常升级入口 ProductionUIBuilder.UpgradeMissionRestartAndBuild。
- Play 会关闭保存的确认预览，并恢复 Pause 的交互状态。

## 验证入口

`--mission-restart-qa` 在隔离存档中先完成一个修桥步骤和一个积分模块，并各使用一颗心，实际走到设备后从 Pause 触发确认。检查取消／Esc／Enter、默认焦点、底层鼠标与按钮拦截、计时和音效暂停、数字草稿／待接导线保留，故意制造保存失败后重试，再检查新 Brief 的零进度、三颗心、初始桥／装置、旧 ResumeId 清除和完整旧存档保留。最后从 Home Continue 恢复旧任务，检查已安装组件及已完成拨盘的实体状态。

另运行原有两关完整 `--missions-qa`，回归修桥、救援、积分、总关闭、关卡衔接、针对性练习、存档与学习记录。

证据目录：`qa/editor-mission-restart`、`qa/mission-restart-final-720`、`qa/mission-restart-final-1080`、`qa/mission-restart-regression-720`、`qa/mission-restart-regression-1080`。所有保存失败样本仅在 QA 隔离目录创建。

## 本次结果

- 57 张 Editor 场景／UI 预览通过；重复升级无重复窗口，已有控件位置和尺寸保持不变。
- 1280×720、1920×1080 的重新开始专项验收通过：各两关、各一次真实保存失败与重试，取消／Esc／默认 Enter 保持暂停及草稿，底层按钮不可交互，新 Brief 重置进度和实体，原存档完整保留并能从 Home Continue 恢复。
- 同样两个分辨率的两关完整回归通过，各输出 78 张截图，覆盖原有救援、总关闭、下一关、成绩、练习与记录。
- `qa/mission-restart-delivery-build.log` 中开发版、正式版构建均为 Success。正式版包含 CanvasRestartMission，不包含 MissionsPlaytest / MissionRestartRunChecks。
- 源码备份：`unity/backups/2026-10-13-mission-restart/CalculusLabEscape-source.zip`，包含项目资源、配置和阶段说明，不含 Library 或 Builds。
