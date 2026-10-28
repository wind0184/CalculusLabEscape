# 第八阶段：通关后的针对性练习

日期：2026-10-09。

## 玩家流程

两关结算页新增 Practise this focus / 3 questions。玩家根据已有错误记录进入三个新例题，选择答案、提交、看解析，最后查看本组结果。原来整关重玩的按钮明确标为 Replay mission。

题库包含七组共 21 题：求导幂法则、根式与倒数改写、常数和线性项、积分幂法则、+C、积分操作顺序、定积分上下限。题组按最常见错误选择，未覆盖的错误类型或全对通关使用所属关的通用幂法则题组。题目固定且人工编写，不使用远程服务或随机生成。

每题只计首次提交。错误也会进入下一题，先显示正确结果及解析，不通过再次点击正确选项提高本次成绩。重做同一组题使用 Repeat this set 并新增一份记录。

PracticeRun 存在原 Session 的独立 practice 列表中。每次提交和前进都写入现有本地 JSON，关卡 attempts、完成状态、正确率、时间及心数不变。Learning records 在关卡作答证据后展示练习题目、所选答案、正误及解析；未完成组明确标记 unfinished。旧存档不需要迁移。

关闭练习回到结算页，再打开保留当前题及已提交答案。离开场景后可查看保存证据，但当前尚未提供从历史记录恢复旧练习的入口。练习分数仅描述这些选择题的首次提交，不作为学习效果验证或掌握程度认证。

## 代码

- `Assets/_Project/Scripts/Production/MissionPractice.cs`：题库、选组、独立练习数据与历史文字。
- `MissionPracticeCanvas.cs`：结算入口、选择与提交、解析、复盘、重复及返回。
- `Chapter4/Chapter4Session.cs`：可选练习列表。
- `Production/ProductionCanvas.cs`：本地记录显示。
- `Editor/MissionPracticeBuilder.cs`：只添加两关练习 UI，不重新生成其余布局。运行 `ProductionUIBuilder.UpgradePracticeAndBuild` 升级并构建开发版。
- `Editor/MissionPracticeChecks.cs`：选组、提交锁定、顺序、成绩隔离、JSON 及旧数据兼容检查。
- `Production/MissionsPlaytest.cs`：通过 Canvas 点击完成两种练习，检查 Esc、重新打开、评分、磁盘保存及历史记录。

## 验证

构建与自动交互证据位于项目上两级 `qa/focused-practice-*`。测试存档使用隔离目录。

- `focused-practice-build.log`：两关 Prefab 选择性升级；题库及数据检查通过。
- `focused-practice-final-build.log`：最终开发版构建成功，`MISSION_PRACTICE_CHECKS_PASS` 与上一阶段反馈检查均通过。
- `focused-practice-final-720/result.txt`、`focused-practice-final-1080/result.txt`：两关完整流程及新增练习交互 PASS。覆盖从结算进入、未选择时禁止提交、答案锁定、正确／错误解析、Esc 返回及重新打开、三题成绩、重复组独立保存、未完成组磁盘证据、记录页读取。
- 人工检查了实际 Unity 渲染的练习题、错误解析、两关练习复盘、结算入口及历史记录，未发现文字溢出。
- `focused-practice-release-build.log`：正常启动版构建成功；程序集包含练习逻辑，不包含 MissionsPlaytest / CanvasPlaytest。
- `focused-practice-limits/result.txt`：第一关五题、错误统计、提示、暂停、续玩及学习记录回归 PASS。

首轮 `focused-practice-720` 在隐藏窗口键盘输入阶段失败，未到达新增练习。两套 QA 入口现限定使用 Input System 的 IgnoreFocus 后台输入策略，避免窗口失去焦点后禁用模拟键盘；正常玩家入口未改变。调整后重跑结果如上。

源码检查点：`../backups/2026-10-09-focused-practice/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 和阶段说明，不包含 Library / Builds。

自动流程仍使用脚本朝向、输入注入、第三关定位与倒计时加速。选择题教学效果和玩家理解程度仍需真人试玩评价。
