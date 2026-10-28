# 第二十阶段：从学习记录恢复指定任务

日期：2026-10-14。

Learning records → Session details 新增 Continue this saved run。玩家可从多份未完成记录中选择具体一轮，恢复其原 session ID、已接受答案、已用心数、计时和环境状态。未提交的草稿与精确玩家位置仍按原有恢复规则重置。主菜单 Continue 保持“最近一份未完成任务”的原有行为。

列表的开始时间显示到秒，按电脑本地时间格式化，帮助区分同一天的多次游玩。详情标题标明 Limits / Differentiation / Integration。新增按钮位于 Back to records 与 focused practice 之间，不移动原有控件。

仅当前本地 learner 的 InProgress 记录可继续；已完成记录继续提供成绩查看和原有针对性练习入口。点击会重新读取记录，不使用详情页持有的旧副本。记录已移走、已完成或不再属于当前本地 learner 时，留在详情页、禁用继续按钮并提示返回记录；Back 会重新读取列表。

关卡加载时再次检查选定记录和预期关卡。如果加载前记录发生变化，返回 Learning records 显示恢复失败提示，不进入空白新任务。有效记录使用原有 MissionResume / CanvasResume，重建接受的步骤卡顺序，不新建任务记录、不改历史成绩。查看详情只读取 session，不写回存档。此阶段仍使用本地保存，登录／Firebase 接入保持后续处理。

## Editor 预览

Calculus Lab → Learning record preview，选择 Unfinished Bridge、Unfinished Case、Unfinished Limits、Completed record 或 Unavailable record，然后 Show selected record preview。该窗口打开已保存的 MainMenu 场景；只填示例 UI，不创建 session、写存档或启动 Play。修改后可 Undo；Play 仍打开正常主菜单。

布局位于 MainMenuUI.prefab → Detail → DetailResume / RecordResumeStatus。恢复逻辑位于 Production/RecordMissionResume.cs；UI 定向升级与预览位于 Editor/RecordMissionResumeBuilder.cs。升级入口 ProductionUIBuilder.UpgradeRecordMissionResumeAndBuild，重复调用不会新增重复按钮，旧 RectTransform 位置和尺寸保持不变。

## 验证入口

`--record-resume-qa` 通过真实 Canvas 操作产生并保留修桥和积分的部分进度，再启动一轮更晚的空积分任务，验证选择较早的记录恢复其自身答案、心数和实体状态。查看详情前后比较完整 JSON。专项测试还在隔离目录模拟记录被移走、变成 Completed、变更 learner，以及 Scene 加载时被移走；这些场景均不得打开新任务。Limits 路由使用明确构造的隔离 L1 checkpoint 样本，检查相同 ID 和 Stage 1 恢复，不代表本次重新验证全部 Limits 题目判分。

原有 `--missions-qa` 继续验证两关完整游玩、关卡衔接、保存失败重试、成绩和针对性练习记录。

证据目录：`qa/editor-record-resume`、`qa/record-resume-final-720`、`qa/record-resume-final-1080`、`qa/record-resume-regression-720`、`qa/record-resume-regression-1080`。所有记录变化模拟只操作 QA 隔离路径；测试结束恢复原样本。

## 本次结果

- Editor 五种记录状态预览通过，文字无溢出，不创建游玩记录。
- 1280×720 与 1920×1080 指定记录恢复专项测试均通过，各保存 22 张实际 Canvas 截图。验证较早的修桥／积分记录、接受答案和心数、实体进度、积分拨盘 5/5 与 +C，以及点击或加载期间记录失效的处理。查看详情前后 JSON 保持一致。
- 两种分辨率的两关完整流程回归均通过，各保存 78 张实际 Canvas 截图；关卡衔接、原有保存／继续、救援、装置关闭、提示和练习仍正常。
- `qa/record-resume-final-build.log` 确认 development 与普通发布版本均构建成功。普通发布程序集含恢复逻辑，不含 `MissionsPlaytest` 或专项测试入口。
- 源码备份：`unity/backups/2026-10-14-record-resume/CalculusLabEscape-source.zip`，含 Assets、Packages、ProjectSettings、阶段说明及 Unity 打开脚本，排除 Library 和 Builds。
