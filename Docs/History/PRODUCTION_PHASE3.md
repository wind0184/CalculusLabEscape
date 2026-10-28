# 第三阶段：暂缓登录，完善 Differentiation / Integration

用户当前方向：先完善第二、第三关的 UI／玩法；登录和云端记录之后接 Firebase。此前本机 ASP.NET/SQLite 方案不继续扩展，也不作为正常入口。服务已停止，原代码及数据保留以便查阅。当前没有连接或声称实现 Firebase。

## 本轮内容

- 默认直接进入本地主菜单，三关均可进入。启动器只启动游戏，Unity Play 也不需要后台服务。
- 第二、第三关迁移到独立、可编辑的 Canvas / TextMeshPro Prefab，不再运行 IMGUI 题目面板或全局旋转绘图。
- Power Core 的数字芯片、Integration 的接线和顺序卡使用原生拖拽／放置事件；也支持先点击来源再点击目标。
- 断桥关保留 11 个数学步骤，三个组件依次安装；每次确认安装才切换设备或推进修复。最终组合正确后，单独开启过桥救援。
- Integration 首先完成 Exponent Dial，再自由选择 Wires、Sequence、Pressure。各模块分别锁定已完成内容，世界中的指示灯按实际模块亮起。Wires 三根线全部完成后仍需一个共同的 +C。
- 四个模块完成只解锁最终操作：先掀开保护盖，再 Master Deactivation，动画完成才结束场次。
- 错误反馈就地显示，不清除正确进度；空白数值／未选择必要控件不算已提交尝试。正确率包含错误重试，完成率独立计算。
- 三级支持：Basic Hint 免费，Rule Reminder 和 Guided Demonstration 各消耗 1 心；同一提示窗口重复查看不重复扣心。免费的 Integration Manual 不消耗心数。
- 暂停、提示、手册暂停活动计时与环境推进；Integration 倒计时归零进入 Safety Mode，继续完成学习。
- 两关均可保存返回及继续，恢复已接受步骤、心数、用时、模块状态、倒计时及环境压力状态。未提交草稿和玩家精确位置不恢复。
- 记录保存在原 `Application.persistentDataPath/Chapter4Sessions` 下的 `Local player` 档案中，账号测试记录不导入。主菜单 Continue 选择最近的未完成场次。

## 修改位置

| 用途 | 路径（相对于 Unity 项目） |
| --- | --- |
| 新关卡 UI 与事件绑定 | `Assets/_Project/Scripts/Production/MissionCanvas.cs` |
| 答题、模块前置条件、恢复与提示 | `Assets/_Project/Scripts/Production/MissionCanvasActions.cs` |
| 断桥题干、答案和顺序规则 | `Assets/_Project/Scripts/Production/MissionRules.cs` |
| 原生拖拽、放置及锁定接线绘制 | `MissionDragSource.cs`、`MissionDropTarget.cs`、`MissionConnections.cs`，同上目录 |
| 编辑器生成 UI | `Assets/_Project/Scripts/Editor/MissionUIBuilder.cs` |
| 第二关布局 | `Assets/_Project/Resources/Production/DifferentiationUI.prefab` |
| 第三关布局 | `Assets/_Project/Resources/Production/IntegrationUI.prefab` |
| 本机存档与环境恢复字段 | `Assets/_Project/Scripts/Chapter4/Chapter4Session.cs`、`Chapter4Game.cs` |

可以在 Prefab Mode 编辑文字、颜色、布局和按钮大小。保留控件名称及绑定组件。正常打包调用 `ProductionUIBuilder.BuildRelease`；不要在手动修改布局后随意运行 `GenerateAndBuild`，后者会重新生成四个 UI Prefab 和场景实例。

## 验证方式

开发版本使用 `--missions-qa --qa-output <隔离目录>`。它在正常本地模式运行，**不使用** `--canvas-qa` 或旧登录绕过标记。测试执行 Canvas 射线、按钮点击、芯片／卡片／接线拖放，并检查完整关卡流程与实际 JSON 保存续玩。

测试在设备附近脚本定位玩家后，使用 Input System 的 E 键实际打开设备；倒计时边界通过测试专用反射加速到零。截图由实际 Unity 世界／Canvas 渲染到 RenderTexture，避免隐藏窗口返回黑色交换缓冲区。这些检查不代表真人行走、拖拽手感、输入法或教学体验已验收。

## 仍需完善

三维场景继续使用现有基础模型，尚未替换成正式美术；音效、角色表现、关卡叙事、手感和无障碍需要后续迭代。题目数据目前在源码规则中，Limits 保留独立 ScriptableObject 题库。Firebase 配置、登录、角色权限、云端成绩、教师端重新接入均延后。

## 两关测试结果

- `../../qa/missions-canvas-verified-720/result.txt`：PASS，1280×720。
- `../../qa/missions-canvas-verified-1080/result.txt`：PASS，1920×1080。
- 覆盖正常本地入口、三关可选、第二关 11 步及三组件、第三关任意模块顺序与 10 步、真实 Canvas 拖放／点击、空白输入不计次、错误重试、提示扣心、暂停、两关实际 JSON 保存与重新载入、模块指示灯、倒计时 Safety Mode、保护盖与总开关分别操作、完整成绩记录。
- 测试开始及结束均验证没有创建 `LearningApi` 连接对象；后台服务保持停止。
- 已人工查看游戏实际渲染的断桥修复、组件组合、模块概览、指数拨盘、锁定接线与总开关截图。初次发现的两处选关文字溢出及总开关与提示按钮重叠已修正。
- `../../qa/missions-limits-regression/result.txt`：PASS，原 Limits 的五题、移动／E／Esc、提示、保存续玩、结算及记录均通过；通用操作说明页面也检查了文字溢出与画面。
- `../../qa/missions-final-delivery-build.log`：开发版与正常启动版均构建成功；正常启动版程序集不包含三个自动化 QA runner。
- 源码检查点：`../backups/2026-10-07-local-missions/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 与本阶段说明，不包含可重新生成的 Library 或 Builds。
