# 第二十六阶段：虚拟模拟空间的场景背景

日期：2026-10-14。按照“继续丰富背景”的要求，在三关已有网格天空下增加保存的背景装饰。

## 三关的画面

- Limits：门户后方的外框和弧形灯带、侧边灯柱、墙面曲线显示屏、顶部开放框架与悬挂圆环。曲线屏是背景图案，没有题目答案或交互状态。
- Differentiation：外围边界灯带与灯柱、七座带松树的远景浮岛、远处模拟门框，以及数据灯塔和下方网格平台。
- Integration：带累积条纹的曲线监测屏、墙面灯轨、顶部框架与圆环。装饰位于墙面、顶部及外围，操作区沿用原有物件。

新增内容集中在世界根节点的 `Simulation background` 分组。三关分别有 229、124、334 个装饰 Renderer，多数是线条与图表；没有新增灯光或阴影投射。远景松树使用已经导入的 Kenney Nature Kit 资源及其现有 CC0 许可。

## Editor 中查看与编辑

沿用 `unity/Open-Simulation-Levels.cmd`，或菜单 `Calculus Lab → Sky → Open Limits / Bridge / Integration simulation`。不按 Play 即可看到和编辑背景。在 Hierarchy 展开 `Simulation background`，选择墙屏、灯带、圆环或浮岛，使用 Unity 的移动／缩放工具调整。

共用材质及五个持久化圆环网格位于 `Assets/_Project/Art/SimulationBackground`。金属结构使用 Standard，图表与灯线使用 Unlit/Color。圆环前后面的顶点和法线分开保存，保证从下方观看可见且表面明暗连续。

生成代码：`Assets/_Project/Scripts/Editor/SimulationBackgroundBuilder.cs`。菜单 `Calculus Lab → Background → Add saved simulation backgrounds` 只添加缺少的整个背景分组，保留已有装饰及手动编辑。它不会在 Play 时生成物件。构建入口 `SimulationBackgroundBuilder.ApplyAndBuild` 应用于没有打开 Editor 的批处理交付；没有重新生成世界或 UI。

## 保存与验证

修改前的三个场景在 `qa/simulation-background/before`。本次直接在原项目构建，构建开始时没有运行中的 Unity Editor。

- 新增背景没有 Collider、Light 或玩法组件。逐项验证已有世界矩阵、Renderer 启用状态与材质、碰撞组件及 Chapter4World 序列化引用保持一致。
- 连续执行两次设置，没有重复分组或装饰；从磁盘重新打开三关后，每关恰好一个背景分组，材质与 Mesh 均为持久化资源，没有创建游戏会话。
- 九张 Edit Mode 截图在 `qa/simulation-background`：各关的 world、player、canopy。已查看三关整体、玩家位置和顶部结构；修正了第一版圆环的法线问题。
- 最终两个 Windows 构建成功：`Builds/Chapter4/CalculusLabEscape_Chapter4.exe` 和 `Builds/LearningLab/CalculusLabEscape.exe`。最终日志 `qa/simulation-background-final-build.log`。
- 修桥／积分 720p 与 1080p 完整自动流程都通过，各 78 张截图，位于 `qa/background-missions-720`、`qa/background-missions-1080`。覆盖真实控制器行走、设备操作、救援、四模块完成、总关闭、提示、暂停和保存／恢复。
- Limits 720p 与 1080p 自动流程都通过，各 17 张截图，位于 `qa/background-limits-720`、`qa/background-limits-1080`。覆盖五题、门户激活、暂停、提示、保存／恢复和学习记录。
- 四份运行日志未出现本次检查的脚本异常或 Shader 错误标记；普通发布程序集包含 SimulationSky，不包含两个自动游玩脚本。自动输入仍不能替代真人体验及性能测量。
- 源码备份：`unity/backups/2026-10-14-simulation-background/CalculusLabEscape-source.zip`，包含场景、装饰资源、代码和说明，排除 Library、Builds 与验证副本。
