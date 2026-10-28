# 第十七阶段：Integration 完成与总关闭表现

日期：2026-10-12。

四个模块的完成记录与装置供电状态分开显示。完成模块不关闭装置；最后仍须打开保护盖并按 Master Deactivation。模块完成后保留绿色灯和已接受的接线／步骤卡。装置前侧新增文字显示：MODULES n/4、MASTER LOCKED、MASTER READY、MASTER ARMED，关闭时显示供电和安全状态。

总关闭默认四秒，依次显示 Master switch → Power off → Device safe。总开关先拨下，四条装置供电条依次熄灭，然后检查并显示 DEVICE DEACTIVATED。关闭进度不会清除绿色模块灯或数学完成记录；最后动画完成才沿用原有 Completed 保存与成绩页流程。屏幕用 [WAIT] / [NOW] / [OK] 表示阶段，不只依赖颜色。

模块总览在完成四个模块后明确提示供电仍为 ON；保护盖打开后改为要求点击 Master Deactivation，避免重复提示抬盖。未完成四个模块时显示当前数量和锁定原因。

## 不按 Play 查看

- Calculus Lab → Scene & UI Workspace → 打开 Level03_Integration。
- Screen 的 Three modules complete 显示剩余一个模块和锁定状态；Modules complete / cover closed 显示四模块完成但盖未开；Master deactivation 显示盖已开；Device powering down、Device powered off 展示总关闭及安全状态。新增的部分进度预览使用示例完成顺序，不改变实际自由模块顺序。
- Shutdown preview settings 选择 CaseShutdownSequence；Inspector 的 Progress 滑杆预览总开关和供电条，Reset device and camera poses 恢复初始装置和镜头。
- Hierarchy → Containment device → Shutdown sequence 保存前侧文字显示、镜头位置和目标；镜头节点可以在 Scene view 调整，Duration 可以调整动画时长（最低三秒）。

预览使用已保存的真实装置，不创建 Chapter4Game 会话、播放操作音效或写成绩。切换预览按所选状态重新显示装置；预览支持 Undo。Play 恢复初始供电、开关、接线／步骤卡及镜头，之后才由真实任务和存档恢复完成状态。

升级入口为 ProductionUIBuilder.UpgradeCaseShutdownAndBuild。该入口定向添加序列组件、文字和 UI 阶段标签，保持既有 Renderer 的世界矩阵、碰撞体数量／引用和 Chapter4World 序列化引用。重复执行复用已保存的序列和镜头设置，不重新生成关卡。

## 验证

编辑器验收重载已保存场景，以 101 个时间样本检查供电关闭数量不倒退、四模块完成灯不被改动以及供电／开关复位，并渲染全部 UI。实际游玩验收完成两关，检查抬盖限制、总关闭每个阶段、局部存档恢复、成绩保存、练习和学习记录。

证据目录为 `qa/editor-shutdown`、`qa/shutdown-final-720`、`qa/shutdown-final-1080`。

## 本次结果

- 编辑器的 55 张场景／UI 预览与 101 个关闭进度样本通过，已保存的序列、文字材质和装置引用均能重载。
- 1280×720、1920×1080 的两关完整自动游玩通过；总开关先动作，UI 与实体供电关闭数量一致，安全提示在仍为 Finishing 时显示。模块完成灯、数学完成数、尝试数和心数保持不变。关闭截图是 `17a-shutdown-action.png`、`17b-shutdown-verification.png`、`17c-device-powered-off.png`。
- `qa/shutdown-delivery-build.log` 中开发版与正式版构建均为 Success。正式版包含 CaseShutdownSequence，未包含 MissionsPlaytest／CanvasPlaytest。
- 源码备份为 `unity/backups/2026-10-12-integration-shutdown/CalculusLabEscape-source.zip`，包含项目资源、配置和阶段说明，不含 Library 或 Builds。
