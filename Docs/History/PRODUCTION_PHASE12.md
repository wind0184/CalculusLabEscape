# 第十二阶段：Unity 编辑模式中的场景与 UI

日期：2026-10-11。

## 使用入口

关闭的项目可以通过 `../Open-Unity-Editor.cmd` 打开；它使用本机 Unity 6000.3.10f1，进入 Differentiation 场景并打开 Scene & UI 窗口。如果项目已经开着，直接点顶部菜单 **Calculus Lab → Scene & UI Workspace**，不要再启动第二个编辑器。

窗口提供三关的打开按钮。Scene 视图用于三维物件，Game 视图用于完整 UI 预览，两者都无需按 Play。

- **3D world / hide UI**：隐藏 Canvas，查看场景。
- **Select world in Hierarchy**：选中保存好的关卡根物件，展开后直接编辑树木、步道、桥、牛、仪器、墙面等。
- **Select UI in Hierarchy**：编辑 Canvas / RectTransform / TMP 字体颜色与布局。
- **UI / …**：切换修桥的幂法则、数字输入、表达式选择、救援，积分的模块总览、拨盘、接线、顺序、压力，以及 Brief、Summary、Manual。
- 数学标签可直接改 MathText.Source，Inspector 会立即更新上标与根号。

Ctrl+S 保存场景。场景中的 UI 调整是 Prefab override；共享布局请编辑 `Assets/_Project/Resources/Production` 中的 UI Prefab。切换预览属于可撤销的编辑操作。预览中的成绩是示例值，底部标有 EDITOR PREVIEW，不创建游戏会话或写存档。

## 从运行时生成改为场景保存

三个 `.unity` 文件现在持有实际关卡物件：Limits 102 个 Renderer，Differentiation 325 个，Integration 188 个。动态生成的网格、材质和预览 RenderTexture 已保存到 `Assets/_Project/Art/AuthoredWorlds` 的各关目录。关卡逻辑依赖的引用也被序列化，重新打开项目后保留。

Play 时复用场景中的 Chapter4World，不再重新生成这套几何体。仅相机的实时 RenderTexture 在运行时创建，退出后释放；磁盘中的材质、网格与 RenderTexture 资产不被销毁。旧的未迁移场景仍可使用原生成路径。

场景的静态物件和材质修改会用于实际游戏。桥板升降、小牛救援路线、装置盖板和拨盘等仍由关卡动画控制，不应把运行时姿态当作静态摆放。不要删除 Chapter4World 依赖的设备、玩家、指针和桥板引用。

编辑器预览可以隐藏 Canvas 或打开积分箱盖。按 Play 后 Canvas 恢复、预览弹层关闭、箱盖恢复起始位置、界面由真实游戏状态接管。

## 代码

- `Assets/_Project/Scripts/Editor/SceneAuthoring.cs`：一次性迁移、工作窗口、UI 示例、编辑模式检查与截图、数学文本 Inspector。
- `Chapter4/Chapter4World.cs`：保存的场景引用、复用入口、编辑模式安全生成、临时资源清理。
- `Chapter4/Chapter4BridgeFeedback.cs`、`Chapter4CaseFeedback.cs`、`Chapter4EnvironmentArt.cs`：序列化动画与反馈依赖的引用。
- `Production/MissionCanvas.cs`、`ProductionCanvas.cs`：启动时从编辑器预览恢复正常 Canvas。

`SceneAuthoring.BakeMissingWorlds` 跳过已有 Chapter4World 的场景，不重建已编辑物件。`SceneAuthoring.ValidateAndCapture` 在 Edit Mode 重新加载三关并检查、输出截图；用于批处理，会切换场景，手动调用前应保存工作。平时使用工作窗口，无需再次迁移。

## 验证

证据位于项目上两级 `qa/`。编辑模式检查已通过：三关从磁盘重载、材质与网格和 RT 均持久保存、没有创建 Chapter4Game，十四个任务 UI 预览无文本溢出。截图来自未进入 Play 的相机与 Canvas 渲染。

- `qa/editor-authoring/result.txt`：编辑模式重载、资产持久化、十四个 UI 预览与文字布局 PASS。
- `qa/editor-authoring-build.log`：开发版构建成功。
- `qa/authored-worlds-missions/result.txt`：两关完整回归 PASS；每次进入设备均检查只存在一个保存的 Chapter4World，没有重新生成几何体。11 步修桥、四模块积分、真实碰撞移动、拖放、暂停、存档续关、练习和记录均通过。
- `qa/authored-worlds-limits/result.txt`：第一关五题、提示、暂停、存档续关及结算记录 PASS。
- `qa/editor-authoring-release-build.log`：正常运行版构建成功。

编辑模式截图已查看森林整体、积分接线界面与场景物件；它们是未按 Play 的渲染结果。自动检查不替代实际编辑与玩家体验验收。

源码检查点：`../backups/2026-10-11-editor-authoring/CalculusLabEscape-source.zip`，包含三关场景、持久化美术资产、代码、配置、文档与编辑器启动入口。
