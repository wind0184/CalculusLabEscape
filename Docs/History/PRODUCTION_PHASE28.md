# 第二十八阶段：三台修桥装置的功能外观

日期：2026-10-14。用户确认希望替换修桥关的三个方箱装置。本轮用“供能、转换、施工”分别表现其用途，沿用 Chapter 4 的名称与玩法。

## 三种外观

- Power Core：圆形供能机，带转轮轮廓、铜色冷却结构、支架和圆形状态灯。
- Converter Box：开放式三通道转换架，带三个通道座、环形卡槽、支架及前部读数面板。原来的三个转换卡锁保留，接受改写后仍按现有规则升起、变色。
- Bridge Builder：木工作台，带桥梁木料、卷扬筒、手柄和施工控制面板。

三台形状与材质各有区别，现场名称牌缩小，减少遮挡。题目、既有名称、提示、操作区域、桥梁安装及救援流程保持原逻辑。供能机转轮与施工卷扬筒目前是静态机械外观，没有新增旋转或牵引动画。

## 保存与代码

实际场景的 `Repair station machinery` 分组包含三台装饰模型。六个材质、三个双面圆环 Mesh 位于 `Assets/_Project/Art/RepairStations`；其他部件使用 Unity 内置基础 Mesh。没有新增灯光、碰撞体或外部素材依赖。

原方箱、旧底座／支架／通风条、旧拨盘及大号名称牌背板只隐藏 Renderer，保留物件及碰撞。原三个 repairLights 作为状态源保留，其旧外观隐藏。新增 `Scripts/Production/RepairStationIndicator.cs` 将新灯带和圆环的 sharedMaterial 绑定到对应状态源，在 LateUpdate 同步锁定、当前可操作和完成状态；不创建新材质或另行判断答题进度。它也在 Edit Mode 工作。

名称文本仍由现有修桥反馈更新；characterSize 调整为 0.019。新名称牌挂在原 TextMesh 下，继续跟随朝向与显隐。没有移动原世界物件或更改其材质；只修改指定外观的可见性及名称文字尺寸。

Editor 入口 `Calculus Lab → Assets → Add Bridge repair machinery`，代码 `Scripts/Editor/RepairStationArtBuilder.cs`。已有分组会保留，重复应用不重建模型。仍可用 `Calculus Lab → Sky → Open Bridge simulation` 不按 Play 查看和编辑。

## 验证与交付

- 修改前场景及 SHA-256 位于 `qa/repair-station-art/before`。
- 没有关闭当前 Unity。使用现有独立验证副本，从原项目更新 Assets、Packages、ProjectSettings；同步回原项目之前，确认原项目磁盘场景未变化。
- 原世界矩阵、原材质、玩法引用与碰撞组件逐项一致；指定旧外观隐藏。三台新物件没有 Collider 或 Light。
- 连续应用两次无重复模型；保存后重新加载，每台绑定到正确原状态源，原三个卡锁仍可见。编辑验证分别切换锁定、当前、完成材质，并恢复原状态。
- 五张最终 Edit Mode 截图位于 `qa/repair-station-art-copy`：stations-world、stations-player 和 station-0/1/2。已查看三台整体与近景，缩小初版过大的标签并调整施工台验证视角。
- 最终两个 Windows 构建成功，日志 `qa/repair-station-art-final-build.log`，同步到原项目 Builds/Chapter4 与 Builds/LearningLab。
- 最终 720p／1080p 两关完整自动流程通过，各 78 张实际截图，位于 `qa/station-art-final-missions-720`、`qa/station-art-final-missions-1080`。
- 在 MissionsPlaytest 每张修桥截图时新增检查：三个新状态绑定必须存在，全部新状态灯必须与当前原状态源使用相同材质；覆盖实际操作、保存／恢复和阶段切换。测试仍使用原控制器行走与原题目 UI。
- 两份最终运行日志未出现本次检查的脚本异常、Shader 错误或失败标记。原项目与副本 86 个脚本一致；交付场景、装置资源、程序集及关卡二数据文件 SHA-256 核对一致。
- 普通发布程序集包含 RepairStationIndicator，不包含 MissionsPlaytest / CanvasPlaytest。
- 源码备份：`unity/backups/2026-10-14-repair-station-art/CalculusLabEscape-source.zip`，包含实际场景、模型资源、代码和说明，排除 Library、Builds 和验证副本。

本次在原项目 Library 排队一次外观合并请求。若当前 Unity 仍显示旧方箱，选择 Assets → Refresh。完成导入后，非 Play 状态下会合并到当前打开场景并保存，保留窗口中的已有物件编辑；状态文件 Library/CodexRepairStationArt.status。自动流程验证仍不能代替真人操作体验。
