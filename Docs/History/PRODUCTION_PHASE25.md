# 第二十五阶段：三关统一虚拟模拟天空

日期：2026-10-14。按照“其他关卡也这样”的要求，将修桥关的虚拟模拟天空扩展到 Limits 和 Integration。

## 保存的关卡与编辑入口

三个实际游戏场景都使用 `Assets/_Project/Art/SimulationSky/VirtualDome.mat`：深蓝渐变、青色穹顶网格和缓慢扫描光。保持修桥关已设置的天空参数；两个室内关卡使用较淡的雾（密度 0.0025，修桥关为 0.007），让近处设备与文字保持清楚。房间墙壁仍遮挡侧面天空，上方开放区域可以看到网格穹顶。

- 双击 `unity/Open-Simulation-Levels.cmd` 打开 Limits 的 Editor 预览。
- 菜单 `Calculus Lab → Sky → Open Limits simulation / Open Bridge simulation / Open Integration simulation` 切换关卡。
- 不需要按 Play。Scene & UI Workspace 仍能查看世界和已有 UI 页面；预览不会创建游戏存档。
- 修改世界根节点 `SimulationSky` 的 Fog Tint / Fog Density 可调整本关雾；修改 `VirtualDome.mat` 会同时影响三关天空。
- 批量设置入口：`Calculus Lab → Sky → Apply simulation sky to all levels`。打开新关卡的菜单会先使用 Unity 的保存提示保护未保存编辑。

## 代码与保存范围

`Scripts/Production/SimulationSky.cs` 在 Edit Mode 和运行时应用当前活动场景的天空与雾。三关共用同一个材质，场景切换时仍检查雾色与密度，避免沿用上一关的雾。

`Scripts/Editor/SimulationSkyBuilder.cs` 新增三关应用、保存验证、截图及打开场景入口。先读取修桥关已有样式，再设置室内两关。没有重新生成世界；已有物件世界矩阵、碰撞组件及 Chapter4World 序列化玩法引用均通过一致性检查。六个世界镜头使用 Skybox。

本次直接使用原项目的已保存场景构建；构建前没有运行中的 Unity Editor。修改前场景副本在 `qa/simulation-sky-all/before`。

## 验证与交付

- Unity 6000.3.10f1 中 Shader 编译及实际渲染检查通过。
- 九张 Edit Mode 视图在 `qa/simulation-sky-all`：各关的 world、player、dome；已人工查看 Limits / Integration 的房间与穹顶画面。
- 已保存场景按 Limits → Bridge → Integration → Limits 重新加载，验证共用材质与各关雾设置。
- 两个 Windows 构建成功：`Builds/Chapter4/CalculusLabEscape_Chapter4.exe` 和 `Builds/LearningLab/CalculusLabEscape.exe`；日志 `qa/simulation-sky-all-build.log`。
- 720p 与 1080p 的修桥／积分完整自动流程都通过，各 78 张截图，位于 `qa/all-sky-missions-720`、`qa/all-sky-missions-1080`。包括真实控制器行走、设备操作、救援、四模块完成、最终装置关闭、提示、暂停、保存／恢复、关卡衔接。
- 720p 与 1080p 的 Limits 自动流程都通过，各 17 张截图，位于 `qa/all-sky-limits-720`、`qa/all-sky-limits-1080`。包括五道题、重试统计、提示、暂停、保存／恢复、门户激活和记录页面。
- 普通发布程序集包含 SimulationSky，不包含 MissionsPlaytest / CanvasPlaytest。测试使用隔离记录目录；自动输入与截图不能代替真人操作体验测试。
- 源码备份：`unity/backups/2026-10-14-all-level-sky/CalculusLabEscape-source.zip`，包含三关、天空材质、代码、说明和启动入口，排除 Library、Builds 和验证副本。
