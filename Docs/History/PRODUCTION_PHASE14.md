# 第十四阶段：作答键盘操作与选中反馈

日期：2026-10-12。

## 使用变化

修桥的数字输入题支持 Tab／Shift+Tab，在当前可用输入框之间循环；只有一个输入框时不会跳到隐藏的第二框。Enter 检查答案，仍然需要点击安装／继续才能应用已经通过的修复。积分 Symbol Sequence 支持 Enter／小键盘 Enter 检查当前步骤；通过后清空数值，准备下一步。

提交复用原按钮的动作、判分和提示。切换输入框不会提交，也不会清空草稿。输入框失焦不会触发提交。暂停、说明、提示和练习等覆盖界面阻止底层输入快捷键。一次按键只处理一次；提交后退出该输入框的选中状态，避免继续按 Enter 重复计入错误尝试。

数字芯片、接线来源和步骤卡的待选状态显示 [X] 与较亮的青色边框。拖动和点击更新同一个选中项，拖动完成后仍可通过点击目标使用所选接线／卡片。已经放入的系数、通过的接线及锁定的卡片不显示待选标记。选中高亮不代表答案正确。

## 不按 Play 的预览

Calculus Lab → Scene & UI Workspace → UI preview → Screen 新增 Chip selected、Wire selected、Card selected；Number inputs 带键盘提示。修桥共 14 页，积分共 18 页。预览不创建会话或写存档，运行时重置预览中的选中样例。

仅对已有 DifferentiationUI、IntegrationUI 两个 prefab 添加选中显示组件、数字输入提示和步骤输入框占位文字。保留已有关卡几何、脚本引用及其他 UI 排版；未调用重新生成场景／界面的命令。

## 代码

- `Assets/_Project/Scripts/Production/MissionKeyboard.cs`：输入框提交事件、Tab 导航、小键盘 Enter 与提交去重。
- `Assets/_Project/Scripts/Production/MissionSelectionVisual.cs`：保存原有颜色／边框并显示选中状态。
- `Assets/_Project/Scripts/Production/MissionCanvas.cs`：拖动／点击统一选中，完成后移除待选显示。
- `Assets/_Project/Scripts/Production/MissionControlsGuide.cs`：数字／步骤题键盘说明及卡片选中提醒。
- `Assets/_Project/Scripts/Editor/MissionInputBuilder.cs`：选择性升级两个 prefab，并验证编辑模式预览。入口为 `ProductionUIBuilder.UpgradeMissionInputAndBuild`。
- `Assets/_Project/Scripts/Editor/SceneAuthoring.cs`：新增三个选中预览。
- `Assets/_Project/Scripts/Production/MissionsPlaytest.cs`：真实输入框射线点击、Input System Tab／Shift+Tab／Enter／小键盘 Enter、单次计分、空答案、失焦保留、选中显示和完整流程回归。

## 验证

已通过以下检查：

- `qa/editor-mission-input/result.txt`：重载三关世界，全部 32 个任务 UI 预览可在编辑模式渲染，无文字溢出；没有创建玩法会话。
- `qa/mission-input-build.log` 与 `qa/mission-input-rebuild.log`：选择性 prefab 升级、规则检查及更新后开发版构建成功。
- `qa/mission-input-720-fixed/result.txt`：1280×720 下完整两关回归 PASS。
- `qa/mission-input-1080/result.txt`：1920×1080 下相同完整两关回归 PASS。
- `qa/mission-input-release-build.log`：正常运行版构建成功；正式程序集包含更新后的键盘／选中代码，不含两关和 Canvas 的 QA 类。

两次回归均检查：真实射线点击输入框，Tab 到第二框／跳过隐藏框，Shift+Tab 返回，切换时保留数值且不提交；正确和错误 Enter 作答各只计一次，重复 Enter 不自动重复错误或安装，空答案不计分；积分三个普通 Enter 与最后一个小键盘 Enter 各通过一步，并清空已经接受的数值；操作帮助阻止底层键盘快捷键；点击／拖动的选中标记，以及通过接线后移除待选显示。原有 11 步修桥、4 个积分模块、总关闭、三维反馈、暂停、提示、续关、练习和学习记录也全部完成。

已查看编辑模式的芯片／接线／卡片选中页、数字输入页，以及实际运行中的双输入框与步骤卡页。两种分辨率均未报告文字溢出。测试填入文本值后发送 Input System 按键，未覆盖真实系统输入法或所有硬件布局；真人键盘／鼠标试玩仍需验收。

首次测试发现默认 Input System UI Submit 使用 `*/{Submit}`，普通 Enter 标记有 Submit usage，小键盘 Enter 没有，因此合成小键盘 Enter 未提交第四个积分步骤。现在在作答快捷键中显式处理小键盘 Enter，并与 TMP 提交共享防重和弹窗检查。原失败日志保留在 `qa/mission-input-720`，最终验证使用新的独立测试存档目录。

运行版已更新：`Builds/LearningLab/CalculusLabEscape.exe`，上一级目录的 `Start-LearningLab.cmd` 继续使用该版本。源码检查点：`../backups/2026-10-12-mission-input/CalculusLabEscape-source.zip`。
