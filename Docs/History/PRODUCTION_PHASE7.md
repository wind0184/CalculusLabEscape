# 第七阶段：具体错误提示与本次学习复盘

日期：2026-10-08。范围为 Differentiation / Integration 的反馈、结算与本地学习记录。

## 行为

- Differentiation 分开识别系数、指数、根式／倒数改写、常数／线性项求导及表达式组合；已正确的系数或指数会在提示中得到确认。
- Integration 拨盘依次检查升幂、除数和 +C；错误接线显示所选输出的导数，帮助检查与原项是否一致；排序区分选错操作和数值错误。
- 非法数字、非有限值、零分母与溢出分数归为输入格式问题。原有等价分数判分规则保持一致。
- 结算页按三个知识点分组显示 accepted / first try / retries。accepted 为已接受步骤；first try 为该步骤首次提交正确；retries 为错误提交次数。提示使用记录独立保留，首次答对不等于未使用帮助。
- 下一步练习建议取自本次最常见错误；并列时沿用最早出现的错误，无作答时不会生成掌握结论。
- 每次新提交保存当时的反馈文字。Learning records 显示复盘及可滚动的作答、错误名称、反馈。旧 JSON 不需要迁移，也不会凭空补写历史提示。
- 仅升级两关 Summary 的布局，保留其他 Prefab 的手工修改。题目、模块顺序、修复进度和 Master Deactivation 流程沿用现有规则。

## 代码入口

- `Assets/_Project/Scripts/Production/MissionFeedback.cs`：错误分类及对应提示。
- `MissionReview.cs`：按实际步骤与作答记录计算知识点统计及建议。
- `MissionCanvasActions.cs`、`MissionCanvas.cs`：玩法提交、结算接入。
- `Chapter4/Chapter4Game.cs`、`Chapter4Session.cs`：保存每次反馈及可读错误名称。
- `Production/ProductionCanvas.cs`：学习记录详情。
- `Editor/MissionFeedbackBuilder.cs`：选择性升级结算 Prefab；`UpgradeFeedbackAndBuild` 可升级后构建开发版。
- `Editor/MissionFeedbackChecks.cs`：诊断、等价分数、无效输入、统计与旧记录兼容性检查，随常规构建运行。

## 验证

验证结果与截图记录在项目上两级目录的 `qa/learning-feedback-*`。交互测试使用隔离存档，不修改玩家记录。

- `learning-feedback-build.log`：开发版构建成功，`MISSION_FEEDBACK_CHECKS_PASS`。
- `learning-feedback-720/result.txt` 与 `learning-feedback-1080/result.txt`：两关完整流程 PASS；新增断言覆盖具体诊断、两关分组统计、建议及重新读取的反馈。截图检查无文字溢出。
- `learning-feedback-release-build.log`：正常启动版构建成功；程序集含 MissionFeedback / MissionReview，不含两套 Playtest 测试入口。
- `learning-feedback-limits-final/result.txt`：第一关全部五题、提示、暂停、续玩与记录回归 PASS。最初旧脚本的固定时长 W 输入未走到控制台，造成连带失败；已改为逐帧输入、脚本朝向并步行到真实交互范围，未改动玩家控制器或碰撞体。重建记录为 `learning-feedback-regression-build.log`。
- 源码检查点：`../backups/2026-10-08-learning-feedback/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 和本阶段说明，不含 Library / Builds。

自动验证覆盖完整第二／第三关、具体错误提示、知识点统计、保存后重读反馈、滚动记录、修复与模块状态、提示和续玩。截图来自 Unity 实际 Canvas／场景渲染。第二关使用脚本朝向和移动按键；第三关仍使用设备附近定位与倒计时加速。真人学习效果及文字易懂程度还需要试玩确认。
