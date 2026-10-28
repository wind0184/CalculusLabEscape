# 第十八阶段：完成后的关卡衔接

日期：2026-10-13。

Differentiation 的成绩页在练习按钮旁新增 Next: Integration，先保存本轮完成结果，再直接打开 Integration 的关卡说明。新一轮 Integration 使用新的 session ID，从 Brief 开始，不带入修桥答案、心数或已看过的操作介绍，也不自动开始倒计时。原有重玩、针对性练习、学习记录与主菜单入口保留。

Integration 的成绩页新增 Choose another mission，保存结果后直接回到关卡选择。返回选择页不会创建新任务记录；选择关卡后才打开对应关卡。两种跳转都清除旧 ResumeId，避免把旧任务当成新一轮继续。

只有状态为 Summary 且 session 为 Completed 时才能执行此跳转。Practice 等覆盖层打开时，底层 Next 操作不会执行。如果保存失败，留在原成绩页，保留当前结果和练习，提示重试；成功保存后才离开。跳转不重复提交答案，不改数学完成数、准确率、心数或已保存的练习。

## Editor 与代码

- Calculus Lab → Scene & UI Workspace → Summary，可在两关直接预览新按钮和说明，不按 Play。
- 共享布局位于 Resources/Production/DifferentiationUI.prefab 与 IntegrationUI.prefab 的 Summary → SummaryNext / SummaryNextHint。按钮新增在空余位置，不移动已有控件；之后可以在 prefab 或场景覆盖中调整。
- Production/MissionCompletionNavigation.cs：完成状态检查、保存、失败重试与跳转；ProductionCanvas.SetupMenu 接收 Topics 返回位置。
- Editor/MissionCompletionNavigationBuilder.cs：定向添加按钮，检查既有 RectTransform 位置与尺寸不变，重复升级不创建重复按钮，并渲染已有全部 Editor 页面。

升级入口：ProductionUIBuilder.UpgradeCompletionNavigationAndBuild。重新生成 UI 时也会创建同样的按钮，但正常升级无需重新生成关卡。

## 验证入口

实际 Canvas 验收完成修桥并做完针对性练习，在隔离目录人为造成一次保存失败，确认留在成绩页并可重试，再从真实按钮进入新的 Integration Brief。检查旧结果和练习仍在、旧 ResumeId 已清除、未完成任务不能继续跳关。完成 Integration 后从真实按钮返回 Topics，并从主菜单打开记录，继续验证两关成绩、练习及存档。

证据目录：`qa/editor-completion-navigation`、`qa/completion-navigation-verified-720`、`qa/completion-navigation-verified-1080`。保存失败测试只操作本次 QA 的隔离路径，不触碰真实玩家记录；重试成功后比较完整 session JSON，确认已完成练习与尚未完成的重练都保持原样。

## 本次结果

- 55 张 Editor 场景／UI 预览通过，旧控件的位置和尺寸不变；重复升级不产生重复 Next 按钮。
- 1280×720、1920×1080 的两关完整游玩通过，各保存 78 张实际 Canvas 截图。`08d-next-save-retry.png` 展示保存失败时保留成绩及重试入口；`08c-next-integration-brief.png` 展示直接打开的新关说明；`18c-completed-mission-selection.png` 展示积分完成后返回选择页。
- 失败保存与重试均走真实按钮、真实文件系统；成功跳转后的完整 session JSON 与跳转前一致，包括已完成及未完成练习。新 Brief 不额外写任务记录；结束后仍能从学习记录继续原来的练习。
- `qa/completion-navigation-delivery-build.log` 中开发版、正式版均为 Success；正式版包含 CanvasNextMission / RefreshCompletionNavigation，不包含 MissionsPlaytest / CanvasPlaytest。
- 源码备份：`unity/backups/2026-10-13-completion-navigation/CalculusLabEscape-source.zip`，包含项目资源、配置和阶段说明，不含 Library 或 Builds。
