# 第十五阶段：首次游玩引导与设备路线验收

日期：2026-10-12。

## 引导流程

探索画面左下增加 GET MOVING → FIND / 当前设备 → READY TO INSPECT 提示。水平移动约 0.8 米后进入寻找设备的提示，进入设备引导区域且满足原来的 E 交互距离后提示按 E。左下位置保留上方设备标记和牌面的可见空间。探索中按 G 隐藏／显示引导。

第一次打开一种控件时，自动显示操作介绍。修桥介绍覆盖芯片、数字输入、表达式选择及救援；积分覆盖总览、拨盘、接线、步骤卡、压力表及总关闭。介绍只说明操作顺序和规则，不填写计算结果或选择答案；数字格式示例采用与本关答案无关的 3/7。

介绍打开时保持当前 Puzzle 状态，暂停活动时间、环境压力、积分倒计时及音效。Try these controls 或 Esc 返回原设备；Hide automatic guides 关闭自动介绍。Pause → First-use guides 可重新开启；How to play / Free → Show this introduction 可手动重看当前介绍，即使自动介绍已关闭。

`guideSeen`、`guideSkipped`、`guideMoved` 随原有任务 JSON 保存。继续存档恢复已看过的页面、跳过偏好与移动阶段；不重复弹出已看过的介绍。新建一轮任务重新提供介绍。引导不增加 attempts、support 记录，不扣心、不改变 completed 或成绩。原题目判分、失败反馈和总关闭条件保持原样。

## Unity Editor

- 首次介绍的标题和正文保存在 `Assets/_Project/Resources/Production/MissionIntroductions.asset`，可在 Inspector 修改。程序中的 key 对应控件类别，screen 对应预览名称。
- Scene & UI Workspace → Screen 新增 Movement guide、Device guide 和 Guide - … 页面。不按 Play 可以查看全部介绍及探索提示。
- 修桥共 20 个 UI 预览，积分共 26 个；介绍预览使用上述资产的实际文案，不建立任务会话或写成绩。
- Hierarchy → Guide zones 保存三个修桥设备区域和一个积分装置区域；工作区的 Guide zones 按钮可选中并定位该分组。区域是非实体 Box Collider，目标引用、中心、位置及尺寸可在编辑器查看和调整。引导同时检查现有 E 交互距离，不扩大真实交互范围。
- 场景升级不生成新设备或替换已有美术。迁移比较已有 Renderer 世界矩阵、原碰撞体身份、非触发碰撞体数量及 Chapter4World 序列化引用；重复升级不创建重复区域。已手动修改的介绍文案和引导卡位置保留，只有已知默认旧值作定向迁移。

## 实际移动检查

两关测试现在都从场景中的出生位置使用 Input System W 和原 CharacterController 走到当前设备，再按 E 打开；积分不再直接设置玩家坐标。测试仍自动调整水平朝向，并加速积分倒计时，以覆盖关卡路线、碰撞和 Safety Mode；这不代替真人鼠标操作与迷路体验验收。

最终检查：

- `qa/onboarding-delivery-build.log`：选择性升级及开发版构建成功；三／一共四个区域的重复升级和原有几何／碰撞体引用保持检查通过。
- `qa/editor-onboarding/result.txt`：磁盘重载三关、46 个任务 UI 预览渲染通过，无文字溢出，不建立玩法会话。探索预览使用玩家相机；积分探索预览保持盒盖关闭，控件预览打开盒盖。
- `qa/onboarding-final-720/result.txt` 和 `qa/onboarding-final-1080/result.txt`：1280×720、1920×1080 下完整两关均 PASS。包含首次介绍、底层点击／键盘拦截、免费暂停、继续／Esc、实际写盘、重看、跳过后续关以及重新开启，原有 11 步修桥、四个积分模块、总关闭、练习和学习记录也通过。
- `qa/onboarding-release-build.log`：正常运行版构建成功，开发版和运行版已更新。

正式程序集包含引导、区域和保存标记代码，不含任务／Canvas QA 类。测试存档确认修桥 11 个已接受步骤、积分 10 个已接受步骤，两关各 14 次评估尝试；介绍分别记住 4／6 页，没有额外评估次数。

已查看编辑模式的探索页和卡片介绍，以及实际运行的区域提示和首次介绍。探索提示移至左下，顶部设备牌面和当前设备灯保持可见。最终编辑器检查日志为 `qa/onboarding-editor-final-check.log`。

## 代码

- `Scripts/Production/MissionOnboarding.cs`：可编辑介绍资产与默认文案。
- `Scripts/Production/MissionOnboardingCanvas.cs`：首次显示、免费暂停、跳过、重看及存档状态。
- `Scripts/Production/MissionGuideZone.cs`：可编辑设备区域。
- `Scripts/Editor/MissionOnboardingBuilder.cs`：选择性 prefab / 场景迁移、几何保持检查与开发版构建入口 `ProductionUIBuilder.UpgradeOnboardingAndBuild`。
- `Scripts/Editor/SceneAuthoring.cs`：46 个任务 UI 预览、区域定位及玩家视角的探索截图。
- `Scripts/Production/MissionsPlaytest.cs`：原有完整流程加引导遮挡、键盘拦截、免费暂停、继续／Esc、存档记忆、跳过／重看／重新开启及真实路线检查。

上述 Scripts 路径均位于 `Assets/_Project` 下。

运行入口仍为上一级目录的 `Start-LearningLab.cmd`，使用 `Builds/LearningLab/CalculusLabEscape.exe`。源码检查点：`../backups/2026-10-12-onboarding/CalculusLabEscape-source.zip`。
