# Limits 流程优化

本次改动对应上一轮实际游戏录像里发现的三个问题：目标不清楚、校准缺少场景反馈、结算错因难理解。

## 可见变化

- 检查台的三维标签缩小为 PORTAL CONSOLE；探索 HUD 显示距离和进入范围后的 E 键提示。
- 完成一组引导题与独立题并按 Continue 后，显示约 1.5 秒传送门特写。新恢复的一段圆环亮起，电量以 20% 递增，同时说明下一步。
- 结算与学习记录将 Limits 错因代码改成可读描述；结算增加针对本轮常见错误的复习建议。

## 结构

GameManager.ContinueCalibration 先调用原判题流程推进进度，再通知 LimitsFlowPresentation 播放反馈。反馈期间锁住重复操作、暂停有效答题计时；结束后恢复输入和相机。

画面使用场景中保存的 Portal feedback view 相机和 PortalFeedback RenderTexture。反馈 prefab、五段电量条、圆环 Renderer 引用都在按 Play 前保存。运行时只改变显示状态和颜色，不生成地图或 UI。

正式资源：Assets/_Project/Prefabs/Resources/Production/LimitsCalibrationFeedback.prefab、LimitsUI.prefab，以及 Assets/_Project/Scenes/Level01_Limits.unity。

Game Manager 上的 Limits Flow 引用可以定位控制组件。Hold Seconds 调整停留时长；相机 Transform 调整特写角度；prefab 中 FlowNextTask 等 TMP 对象可以手动修改布局。

正式项目不包含这次录像使用的输入回放辅助代码。录像使用独立试玩存档、实际角色控制器与 Canvas 按钮事件；未直接设置题目完成、分数或计时。
