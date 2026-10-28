# Limits：悬浮观测站

这次替换的是整个 Limits 场景。五次校准仍在同一个观测站完成。

- 深色金属甲板、青色引导灯、铜色护栏和观景玻璃代替旧白墙实验室。
- 中央校准台负责原有 E 键交互；远处的弧形门框按五次校准恢复，门内有持续流动的场效果。
- 窗外是星空、带状气态行星与轨道结构。
- 原百分比圆环隐藏，顶部改成横向五张步骤卡，分别显示等待、引导题、独立检查、可应用和已完成状态。充能特写改用“已恢复几次 / 5”。

## 在 Unity 中修改

打开 `Assets/_Project/Scenes/Level01_Limits.unity`。环境 prefab 实例位于 World 下的 `Limits Observatory`，来源为 `Assets/_Project/Prefabs/Resources/Production/LimitsObservatory.prefab`。

`LimitsUI.prefab` 的 `ReportSceneStrip` 下有 `ObservatoryStep0` 至 `ObservatoryStep4`。可以直接改 RectTransform、文字和 Image。`LimitsObservatoryView` 连接五张卡片、状态文字和 Game Manager，只负责显示。

3D 机器人放在 World 下的 `Limits guide`，位于校准台左侧。来源是 Production 文件夹里的 `LimitsGuide.prefab`，使用已有的 Idle Animator 循环悬浮。可以在 Scene 中手动移动整个 prefab；机器人没有阻挡玩家的碰撞体。场景预览上的 `CALIBRATION DECK / Inspection point` 标签已删除。

材质、弧形门框网格与 shader 保存在 `Assets/_Project/Art/LimitsObservatory/`。门面的 `_Progress` 跟随已完成的校准数；纹理流动由 shader 完成。

玩家、检查台、原有边界 Collider、题库、判题、重试和存档流程继续由已保存的 Game Manager 管理。门框仍保留原来的四十个 Renderer 引用。场景和界面都在 Play 前保存；正式运行不生成地图。

`Calculus Lab > Limits > Open saved observatory` 可以打开正式场景并定位环境。`Build saved observatory scene` 是重建工具；手动调整场景后，普通 Play 不会调用它。

QA 录像使用独立试玩存档和游戏内输入回放，实际经过角色控制器、碰撞和 Canvas 按钮事件。
