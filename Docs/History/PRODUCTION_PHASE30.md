# Phase 30 — Bridge repair machinery motion

本轮完成路线图第二步：把数学任务的安装进度对应到三个修桥装置的机械反馈。登录和 Firebase 继续延后。

## 游戏表现

- Power Core：答对但尚未点击 Install regulator 时保持静止；安装后逐渐转动涡轮叶片和中心轴，机壳、支架和状态环保持固定。
- Converter Box：三条通道分别跟随原有 converter lock 的完成状态和颜色，已完成的通道出现循环向上的绿色流动标记。未完成通道不播放流动。
- Bridge Builder：施工台解锁后，卷筒、绕线、手柄围绕同一根轴转动；动作只在原有桥面升起过程中运行，桥面到位后停止。其余数学小题不会让卷筒空转。
- 暂停、支持及操作帮助打开时，机械动作停止；恢复后继续，不根据暂停期间的时间跳动。成绩页停止更新动作。
- 读取存档使用已有任务和桥面状态恢复持续运转的供能机、已完成通道；不会重播桥面安装，也不会新增答题、扣心或记录。

## 不按 Play 的预览

打开 Bridge 场景，选择 **Calculus Lab → World → Preview Bridge machinery motion**。

Inspector 中的 **Machinery preview (no Play)** 提供：

- Installed task stage：0 静止、1 供能机、2–4 逐个完成转换通道、4 及以后可预览施工台。
- Sample time：拖动时间滑杆查看机械姿态。
- Bridge installing：预览施工台卷筒转动。
- Reset machinery pose：恢复初始机械姿态。

预览只改变装饰物的姿态和流动标记，不推进任务、移动桥面或写入学习记录。Play 根据实际任务状态重置这些装饰姿态。

## 实现

- `Scripts/Production/RepairStationMotion.cs`：读取实际任务、帮助和桥面移动状态；只更新装饰物。
- `Scripts/Chapter4/Chapter4BridgeFeedback.cs`：增加只读的 `BridgeInstallationMoving`，依据现有升桥计时和桥面实际位置判断是否正在安装。
- `Scripts/Editor/RepairStationMotionBuilder.cs`：将已有涡轮和卷筒零件加入保存的旋转支点，添加三个通道标记，提供预览 Inspector 和菜单；重复执行不重复创建。
- `Scripts/Production/RepairStationMotionPlaytest.cs` 与 `MissionsPlaytest.cs`：通过实际 Canvas 操作检查安装前静止、安装后运转、通道分路、施工升桥联动、停止、暂停、帮助和存档恢复。

原有装置位置、碰撞、转换锁、桥面、牛和救援、数学判断及学习记录结构均沿用。

## 验证和交付

验证证据位于 FYP 的 `qa/repair-station-motion`。编辑器迁移检查旧对象的世界姿态、碰撞引用和玩法引用；随后重复应用并重新加载保存的场景，检查持久化与预览复位。

- `final-build.log`：编辑器迁移、重复应用、保存后重载与预览复位均 PASS；开发版和正式版 Windows 构建均 Success。
- `missions-final-720`、`missions-final-1080`：两种分辨率分别完成实际 Canvas 操作的两关完整流程，各 78 张截图，结果均 PASS，未记录运行时异常或 Shader 错误。
- 每个运行目录的 `machinery-checks.txt` 记录真实姿态变化：安装前 rotorDelta=0；已安装供能机持续转动；stage 2/3 分别只有一／两路流动；stage 4 安装时卷筒旋转、桥面到位后 winchDelta=0；首次说明打开时 rotorDelta=0 且三路流动均停止；读档恢复两条通道，卷筒不重播安装。
- 另检查了正常暂停、支持和操作帮助冻结动作，以及运行状态覆盖装饰预览姿态。动画观察前后的阶段、答题次数、已完成数和心数一致。
- 四张独立的机械姿态截图由 Unity 相机渲染；仅在取图时隐藏既有悬浮任务牌以便观察零件，结束后立即恢复，保存场景与游戏中的任务牌保持原状。它们是机械姿态样本，任务进度说明以实际流程截图为准。
- 交付路径：`Builds/Chapter4/CalculusLabEscape_Chapter4.exe`、`Builds/LearningLab/CalculusLabEscape.exe`。
- 源码备份：`unity/backups/2026-10-15-repair-station-motion/CalculusLabEscape-source.zip`，排除 Library、Builds 和验证副本。

本轮完成机械动作联动；正式牛模型、动画替换和后续关卡整理仍按原路线图继续。
