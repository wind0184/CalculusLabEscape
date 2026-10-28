# 第一阶段：Limits 完整流程与 Canvas UI

> 本文件记录 2026-10-06 的阶段成果。账号、后端与当前启动方法已在第二阶段更新，请以 `PRODUCTION_PHASE2.md` 为准。

## 范围

本阶段实现主菜单及 Limits 的 Canvas / TextMeshPro 界面、独立题目资源、统一答案判定、本地学习记录与继续游戏。教学顺序和数学内容遵循 Chapter 4。Differentiation、Integration 和三维场景暂时保留原型实现；这不是三关全部完成的正式发布版本。

源码备份：`../backups/2026-10-06-before-canvas/CalculusLabEscape-source.zip`。包含 Assets（及 .meta）、Packages、ProjectSettings 和原说明文件，不包含可重新生成的 Library、Temp、Builds。

## 验收清单

- 主菜单可输入本地学习者名称，进入选关、学习记录、说明和设置。
- Limits 可从任务说明进入实验室，WASD 移动、鼠标观察，靠近控制台后按 E 交互。
- 五题分别提供数值输入、图表探针与答案标记、左右极限、极限与函数值、结论与原因卡片。
- 空白回答先提示补全；错误回答记录错误类型并保留已有进度，不消耗心数；正确回答不可重复刷成绩。
- 每次确认继续恢复 20% 传送门电量；五题完成后才允许激活，激活动画结束后才记录通关。
- Basic Hint 免费；Rule Reminder / Example 每次选择使用 1 心；同一提示窗口重复点击已显示的提示不重复扣心。
- 暂停和提示窗口暂停答题计时；关闭窗口回到原来的题目。
- Save & return 保存已接受的题目、尝试、心数、用时；Continue Limits 在下一题继续。
- 本地继续游戏不保存未提交的输入、草稿、玩家精确位置；恢复时回到实验室入口。
- 结算区分正确率与完成率，记录页可翻页查看历史，并滚动查看每次答题。
- 在 1280×720、1920×1080 和非 16:9 窗口下，控件可见、可点击、没有圆环缩放错位。
- 新自动化用 Canvas 的 GraphicRaycaster 和 EventSystem 点击处理器验证交互。键鼠手感、实际输入法、音效与玩家教学体验仍需人工试玩。

## 在 Unity 编辑器中修改

- `Assets/_Project/Resources/Production/MainMenuUI.prefab`：主菜单、选关、记录、说明和设置。
- `Assets/_Project/Resources/Production/LimitsUI.prefab`：任务说明、世界 HUD、五题控件、暂停、提示、结算。
- `Assets/_Project/Resources/Production/LimitsQuestions.asset`：Inspector 中修改题干、目标答案、错误分类和三级提示。
- 两个 Prefab 已放入 MainMenu、Level01_Limits 场景；可以直接展开或进入 Prefab Mode 调整 RectTransform、颜色和字体。
- 每个页面在 `DesignFrame/Pages` 下；编辑时可切换页面的 Active 状态。运行时控制器会按当前流程显示页面。
- 控件名称供绑定使用，改布局时保留名称及组件。题目的 ID、类型和数量涉及流程契约，改这些需同步验证逻辑。
- 常规修改后使用 `Calculus Lab > Production > Validate Limits learning rules`。打包调用 `ProductionUIBuilder.Build`，不会覆盖手动调整的 Prefab。
- `Generate Canvas UI` 是重建工具，会覆盖两个生成的 Prefab 及场景中的对应实例；只在需要重建时使用。它不会覆盖已有题目资源。

## 代码职责

- `Production/ProductionCanvas.cs`：绑定可编辑的 UI 控件、导航、读写输入、显示反馈与记录。
- `Production/LimitsQuestionBank.cs`：题目数据与无界面依赖的 `LimitsRules.Evaluate`。
- `Production/Chapter4CanvasActions.cs`：新 UI 与现有 Session / 世界逻辑的接口；处理提交、提示、继续、保存返回和恢复。
- `Production/LimitsDiagram.cs`：Canvas 原生图形，绘制传送门与函数图，不使用 IMGUI 的全局旋转矩阵。
- `Chapter4/Chapter4Session.cs`：沿用实际答题记录、正确率和原子文件替换；测试写入隔离目录。
- `Editor/ProductionUIBuilder.cs`：首次生成资源、场景安装与数学规则检查。

## 后续工作

第一关三维场景仍由原有代码生成；下一阶段拆成场景对象及设备 Prefab，加入更明确的交互范围、音效与美术。真实账号、教师权限和云端数据库还未实现，本地学习者名称不代表登录验证。旧教师预览仍留在原型代码中，新的主菜单目前面向学生流程。

## 本轮验证记录（2026-10-06）

| 检查 | 结果与证据 |
| --- | --- |
| 原源码备份 | ZIP 中 108 个条目，完整性检查通过 |
| 数学与记录规则 | `PRODUCTION_RULES_PASS`，包括空白、分数、无效数值、左右极限、点值区别、原因卡、提示、重复提交及指标 |
| 首轮 1280×720 | `../../qa/production-canvas-720-first/result.txt`：PASS |
| 1920×1080 | `../../qa/production-canvas-1080/result.txt`：PASS；新增可见文字溢出和输入框可用性检查 |
| 1280×800 | `../../qa/production-canvas-800/result.txt`：PASS；检查非 16:9 适配 |
| 旧三关回归 | `../../qa/production-legacy-regression/result.txt`：PASS，26 个步骤及各关激活条件 |

Canvas 测试覆盖按钮中心射线命中、EventSystem 点击、Input System 注入 W/E/Esc、五题错误/正确提交、提示耗尽、暂停保留输入、JSON 保存后重新载入场景继续、通关落盘和记录详情。各测试目录包含界面 PNG 和独立测试存档；不改动真实学生数据。人工检查了菜单、图表、点值题、原因卡、总结与记录页截图。

最后的结算文案调整仅让 Integration 显示 Safety Mode，通过新增规则断言并重新打包验证；无需重复运行全部交互测试。键鼠实际手感、输入法、音频、教学效果和目标设备性能尚未经过真人验收，不能把上述自动化检查视作正式发布验收。
