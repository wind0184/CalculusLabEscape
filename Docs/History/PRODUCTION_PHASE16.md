# 第十六阶段：修桥救援动作与 Editor 预览

日期：2026-10-12。

修桥完成后，小牛先走到桥入口，沿桥中央穿过，再到达母牛身边。四条腿交替摆动，身体轻微起伏，头部和尾巴随步伐运动；母牛转向到达的小牛。动画默认六秒，最后停留展示团聚，再记录 Completed 并进入原有成绩页。积分关闭装置仍使用原来的四秒过程。

小牛和母牛保留原有模型和材质，增加腿部、头部、尾巴的轴心及耳朵内侧、鼻孔等细节，母牛增加角。新细节和轴心都保存在 Differentiation 场景中，材质保存在 Art/AuthoredWorlds/Level02_Differentiation。升级不是重新生成场景：第一次建立轴心后比较原有 Renderer 的世界矩阵；再次升级复用已存在的组件和路线，保留之后的手动调整。

## 不按 Play 查看

1. 打开 `Level02_Differentiation`，选择 **Calculus Lab → Scene & UI Workspace**。
2. Screen 选择 **Rescue crossing** 或 **Rescue reunion**，Game 视图直接显示对应救援画面及提示。
3. 点击 **Rescue preview settings**，Inspector 的 **Progress** 滑杆预览完整动作；**Reset animal and camera poses** 恢复动物和玩家镜头。预览支持 Undo，不创建 Chapter4Game、不提交答案、不写玩家记录。
4. Hierarchy 中 **Animals → Calf / Mother** 可编辑模型和各个 pivot；**Animals → Rescue sequence** 保存起点、桥入口、对岸、团聚地点、救援镜头和镜头目标。选中世界上的 BridgeRescueSequence 组件可显示路线 Gizmos。
5. 在 Inspector 修改 **Duration** 调整救援时长（最少三秒）。路线需要仍落在桥中央和两侧陆地上；如果改桥面高度，同时调整 **Deck height**。默认路径抬起小牛到桥面顶部，离开桥面后恢复陆地高度。

预览可以 Ctrl+S 保存，但 Play 时恢复动物的原始姿态及玩家镜头，再由真实任务进度触发救援。桥梁安装状态同样由真实任务恢复。预览不代替完成题目。

修桥完成卡缩小并移到画面下方，给小牛进入桥梁留出空间。到达母牛身边后，标题改为 **CALF REUNITED**。原来的修桥步骤、答案、尝试数、心数和成绩计算规则沿用；动画不额外产生数学尝试或 support 记录。

## 验证入口

- `ProductionUIBuilder.UpgradeBridgeRescueAndBuild`：定向升级已保存场景和修桥完成卡，重新加载场景，检查四腿引用，取 101 个路线样本检查桥面高度和中央路线，渲染过桥／团聚与全部 Editor UI，随后构建开发版。
- `--missions-qa`：实际完成 Differentiation 和 Integration，检查走路时腿部摆动、桥中段位置与高度、母牛转身、到达时腿部归位及提示、动画结束前不提交 Completed，以及答案和心数不被动画改动；同时回归原有存档、练习与记录流程。

运行证据位于工作区 `qa/editor-rescue`、`qa/editor-rescue-all`、`qa/rescue-verified-720` 和 `qa/rescue-verified-1080`。构建日志和验收结果是判断通过与否的依据。

## 本次验收结果

- 已保存场景重载、101 个路线位置样本、全部 51 张 Editor 预览通过；新材质及模型引用持久化。
- 1280×720、1920×1080 的两关完整自动游玩通过；实际团聚截图为各自目录的 `07d-calf-reunited.png`，断言确认截取时仍在 Finishing，尚未进入 Summary。截图等待按动画的绝对 Progress 定位，避免前一张截图的处理耗时挤掉团聚画面。
- 开发版构建日志 `qa/rescue-qa-build.log`、正式版日志 `qa/rescue-release-build.log` 均报告成功。正式版包含 BridgeRescueSequence，不包含 MissionsPlaytest / CanvasPlaytest。
- 源码备份：`unity/backups/2026-10-12-bridge-rescue/CalculusLabEscape-source.zip`，包含场景、脚本、材质、项目配置和阶段说明，不包含 Library 或 Builds。
