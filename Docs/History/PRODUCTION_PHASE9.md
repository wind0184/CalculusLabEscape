# 第九阶段：从学习记录恢复练习

日期：2026-10-09。

## 流程

Learning records 的第二／第三关已完成记录新增练习按钮。没有题组时显示 Start focused practice；最近题组未完成时显示 Continue practice 及已提交数量；已完成时显示 View practice review。打开的是保存的练习面板，不重新生成关卡或重放通关动画。

从主菜单、场景切换或新游戏进程读取原 JSON 后，可以恢复当前题、已经提交的答案和解析。已提交题仍锁定，下一题要求重新选择。全部完成后返回同一记录，立即刷新成绩文字和按钮。Esc 与返回按钮行为一致。

结算与记录入口共用 PracticePanel，保留原有首次提交计分和独立 practice 列表。查看已完成组不会写入存档；只有新建组、提交或前进时保存。写入失败会显示错误，关闭面板时重试；仍失败则保留面板，避免用磁盘旧数据覆盖当前回答。

入口只开放给当前本地档案的已完成 Differentiation / Integration 记录。第一关、未通关记录、其他档案及旧认证记录不使用该入口。原关卡成绩、心数、时间、状态和主菜单 Continue 的关卡续玩规则保持原样。

## 代码

- `Production/PracticePanel.cs`：共享练习交互、保存及返回处理。
- `Production/PracticeRecords.cs`：读取当前磁盘记录、入口状态、返回后刷新。
- `Production/MissionPracticeCanvas.cs`：结算入口，复用共享面板。
- `Production/MissionPractice.cs`：记录资格与按钮文案。
- `Editor/MissionPracticeBuilder.cs`：复用练习面板构造，仅升级 MainMenuUI；`UpgradePracticeRecordsAndBuild` 升级后构建开发版。
- `Editor/MissionPracticeChecks.cs`、`Production/MissionsPlaytest.cs`：旧存档、访问范围、提交锁定、记录续做及独立进程验证。

以上路径位于 `Assets/_Project/Scripts`。第八阶段中“不能从历史记录继续练习”的限制由本阶段解除。

## 验证

使用隔离 QA 存档，不修改实际玩家记录。新增 `--practice-restart-qa --qa-output <此前完整 missions-qa 的目录>`，用于在全新玩家进程中接续前一次退出前保存的题组；要求先运行完整 missions-qa。

- `../../qa/practice-resume-build.log`：MainMenuUI 选择性升级成功。
- `../../qa/practice-resume-delivery-build.log`：开发版及正常启动版构建成功，旧反馈与练习检查通过。正常程序集包含共享练习和记录入口，不包含两套 Playtest。
- `../../qa/practice-resume-final-720/result.txt`：两关完整交互及记录续做 PASS。验证只读复盘不改写数据、未完成组恢复、前一组保留、返回后立即刷新，以及控制写入失败后保留答案并重试成功。
- 同目录 `restart-result.txt`：退出上一进程后，独立新进程通过菜单读取保存题组、恢复已提交选择／锁定、完成余下两题，返回刷新后的记录。未新建关卡会话，关卡指标保持一致。
- `../../qa/practice-resume-final-1080/result.txt`：1920×1080 下的两关完整流程、记录续做及保存恢复检查 PASS。已查看 720p / 1080p 的记录入口、练习复盘、重启恢复及保存提示截图，未发现文字溢出。
- `../../qa/practice-resume-limits/result.txt`：第一关全部五题、提示、暂停、续玩、结算及记录页回归 PASS。

保存故障由 QA 在自己的隔离目录创建一个文件并暂作存档目录来触发；测试恢复路径后重试成功。对应日志中的预期保存警告属于该检查。截图使用 Unity 实际 Canvas 渲染；键鼠手感、教学效果仍需真人试玩。

源码检查点：`../backups/2026-10-09-practice-resume/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 与阶段说明，不含 Library / Builds。
