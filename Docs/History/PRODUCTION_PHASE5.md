# 第五阶段：Integration 装置动作与提示音

日期：2026-10-08。继续完善本地游戏；登录与云端记录仍延后到 Firebase。

## 本轮表现

- Exponent Dial：两根拨针跟随指数、除数草稿转动；完成后保留已接受的位置。续玩时 UI 也恢复 `x^5 / 5 + C`。
- Wire Matching：三根实际接线分别变绿，最后单独接通共同常数插槽。已接受接线可从存档恢复。
- Symbol Sequence：四个步骤逐个变绿并抬入卡槽，只有通过的步骤改变装置。
- Pressure Gauge：释放杆跟随数值操作；正确释放后压力表转到安全区，并播放短泄压声。
- 左侧预览平滑切换到当前模块，返回概览后展示完整箱体。排除大型悬浮场景标题，保留设备上的小标签。
- 模块完成灯为绿色，但装置电源条保持青色。四个模块完成后才能掀开实际保护盖，再拨动总开关；电源条依次熄灭，四秒动画结束才记录通关。替代原先总开关向上漂移的表现。
- 重试只提示当前未完成模块，已完成灯不再一起放大闪动。

## 声音与暂停

`MissionAudio` 生成短拨盘声、箱扣声、接线声、步骤锁定声、完成声、重试声、泄压声和降调关机声。为原创合成基础音效，没有外部音频依赖或循环警报。

沿用主菜单 Settings 的 Master volume；静音不会影响玩法。暂停、提示与手册同步暂停机械动画和当前提示音；读取存档静默恢复状态，不重播成功声。声音仍需真人试听确认音量与手感。

## 代码

- `Assets/_Project/Scripts/Chapter4/Chapter4CaseFeedback.cs`：模块模型、近景镜头、阶段恢复、保护盖与关闭装置动画。
- `Assets/_Project/Scripts/Production/MissionAudio.cs`：合成音效、主音量兼容、暂停与清理。
- `MissionCanvas.cs` / `MissionCanvasActions.cs`：操作、状态与场景反馈的连接，已完成拨盘续玩数值。
- `Chapter4World.cs` / `Chapter4Game.cs` / `Chapter4CanvasActions.cs`：创建装置、最终操作前置条件、暂停。
- `MissionsPlaytest.cs`：现有两关全流程，以及本次实际场景对象、音效数据和恢复检查。

已有 UI Prefab 布局没有重新生成。箱体及模块继续使用基础几何模型，不代表正式美术已完成。

## 验证

- `../../qa/integration-feedback-final-720/result.txt`：PASS，1280×720。
- `../../qa/integration-feedback-final-1080/result.txt`：PASS，1920×1080；包含最后调整的“正在关闭装置”文案。
- 两关完整流程均通过，新增断言覆盖拨针跟随草稿、压力表安全位置、步骤槽抬升、部分接线的实际 JSON 保存／重新进入、已完成拨盘数值、读档不重播声音、未完成模块不能开盖、未开盖不能关闭、最终开关与电源条状态。
- 检查各结果提示音的非零且未削波 PCM、音量兼容配置、静音下可作答、手册暂停声音／机械动画／倒计时。此检查不等于人工监听扬声器输出或完成听感验收。
- 已检查实际 Unity 渲染的模块近景、完整箱体与关闭动画。首轮发现的大标题遮挡已修复；两分辨率未记录文字溢出或运行错误。
- `../../qa/integration-feedback-final-build.log`：开发版与正常版均构建成功。正常版包含装置反馈与 MissionAudio，不包含 MissionsPlaytest / CanvasPlaytest。
- 正常入口仍为 `../Start-LearningLab.cmd`。
- 源码检查点：`../backups/2026-10-08-integration-feedback/CalculusLabEscape-source.zip`，不含 Library 或 Builds。

测试截图来自实际 Unity 世界／Canvas 渲染到 RenderTexture；第二关脚本控制朝向并注入 W 键，第三关仍使用设备附近定位与倒计时加速。真人移动、拖放手感、声音与教学体验需要后续试玩。
