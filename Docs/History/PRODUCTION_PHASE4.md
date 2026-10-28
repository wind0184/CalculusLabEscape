# 第四阶段：第二关场景引导与修复反馈

日期：2026-10-08。优先让 Differentiation 的场景探索与已有答题流程衔接。登录继续延后，后续接 Firebase。

## 行为

- 探索 HUD 显示当前目标、水平距离及前进／左转／右转／回头提示；进入既有交互范围后显示具名的 E 操作提示。
- 安装后，目标说明持续显示刚完成的组件与下一站；保存续玩后根据已接受步骤恢复说明。
- 三台设备有编号、名称、ACTIVE／LOCKED／COMPLETE 状态文字，配合青色活动灯与绿色完成灯。仅显示当前目标和附近设备的标牌，减少远处文字重叠。第三台在安装 Anchor 后保持 STABILISE CROSSING，完成最终稳定步骤才标为 COMPLETE。
- Converter Box 的三次安装逐个解锁并抬起锁扣；装好的桥板以绿色显示并在约一秒内升起。继续保留三组件与最终稳定步骤的区别。
- 错误的琥珀色反馈仅作用于未安装桥板，不再把已修复部分一并变暗；已接受工作保持不变。
- 场景仍采用基础模型。这次没有重新生成或覆盖已有 UI Prefab 布局。

## 代码

- `Assets/_Project/Scripts/Chapter4/Chapter4BridgeFeedback.cs`：方向／距离、设备状态与标牌、转换器锁扣、桥板动画。
- `Chapter4World.cs`：创建与更新这些场景反馈，错误反馈保留安装状态。
- `Chapter4Game.cs`：安装后更新世界阶段。
- `Assets/_Project/Scripts/Production/MissionCanvas.cs`：目标与下一站提示、组件完成颜色。
- `MissionCanvasActions.cs`：续玩恢复设备与锁扣。
- `MissionsPlaytest.cs`：第二关改为注入 W 键，经实际 CharacterController 与场景碰撞体走向当前设备；仅朝向由脚本设置，不传送玩家。第三关仍沿用设备附近定位。

## 验证

- `../../qa/bridge-guidance-final-720/result.txt`：PASS，1280×720。
- `../../qa/bridge-guidance-final-1080/result.txt`：PASS，1920×1080。
- 两次均跑完第二、第三关，包括第二关实际 W 键移动／碰撞、距离限制、目标切换、安装动画、答错保留已修部分、两锁扣续玩恢复，以及原有的两关数学步骤、拖放、提示、存档、结算和 Integration 模块自由顺序／最后总开关。
- 实际 Unity 渲染截图已检查；首轮发现标牌偏大／重叠，随后缩小、降低标牌，仅保留目标与近处设备名称，并调暗完成颜色。最终两分辨率无测试记录的文字溢出或运行错误。
- `../../qa/bridge-guidance-refined-build.log` 和 `../../qa/bridge-guidance-release-build.log`：开发版与正常启动版构建成功；正常版程序集包含本次导航逻辑，不包含 MissionsPlaytest / CanvasPlaytest。
- 正常入口仍为 `../Start-LearningLab.cmd`，无需账号或后台。已有 UI Prefab 没有重新生成。
- 源码检查点：`../backups/2026-10-08-bridge-guidance/CalculusLabEscape-source.zip`。

截图由真实世界与 Canvas 渲染至 RenderTexture。朝向设置、第三关定位与倒计时加速属于测试辅助；测试通过不等于真人移动手感、教学体验或正式美术已验收。
