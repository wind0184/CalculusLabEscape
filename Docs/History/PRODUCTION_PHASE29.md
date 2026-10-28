# 第二十九阶段：修桥路线、边界和碰撞检查

日期：2026-10-15。本轮执行路线图第一步：检查修桥关实际行走、碰撞与视线，修正发现的问题。

## 发现与修改

1. 山谷背景已经连续，但原训练区域的四面限制仍是不可见碰撞。现在使用 `Bridge route safety` 内的木质安全围栏标出西、东、后方和河边界，位置与原安全边界对齐。外围山坡仍是景观，没有扩大可行走范围。
2. 14 个隐藏的旧松树树干仍有碰撞，部分导入树木为了避开河道改变了位置。现在停用旧树干 Collider，为 14 棵实际导入松树配置树干 CapsuleCollider；12 块实际岸边岩石使用对应 MeshCollider。草丛与远景不添加碰撞。
3. 河边界增加双扇安全门，在现有救援开始后随 BridgeRescueSequence.Progress 打开，前 8% 进度内完成打开，早于小牛过门。不改变数学进度、安装步骤、过桥路径或原河边界碰撞。
4. 初版救援标识横在桥头，会挡住桥面与母牛；最终移到桥边。靠近围栏时，默认近裁剪 0.3 会裁掉靠近镜头的表面；第一人称近裁剪改为 0.06，上沿抬到 1.4，使接触边界时也能看清栏杆。

原物件世界矩阵与 Chapter4World 的玩法引用保持不变；相机近裁剪、指定旧树干碰撞启用状态为本轮有意修改。Collider 总数由 77 增至 103，其中原 14 个旧树干 Collider 保留但停用，没有删除。现有三个引导触发区与四面安全边界继续使用。

## 编辑与代码

- `Scripts/Editor/BridgeRouteSafetyBuilder.cs`：新增围栏、树干／岩石碰撞，检查可达区域并保存场景。菜单 `Calculus Lab → World → Add Bridge route safety`。没有打开中的 Unity Editor 时，可使用 ApplyAndBuild 批处理交付。
- `Scripts/Production/BridgeBoundaryGate.cs`：Edit Mode / Play 中读取已有救援进度并调整门扇；不自行推进时间或任务状态。暂停时仍跟随原进度。
- `Scripts/Production/BridgeRoutePlaytest.cs`：仅 Editor / Development 中的真实控制器路线检查；由 MissionsPlaytest 的 `--bridge-route-qa` 入口启动，使用隔离记录目录。

在实际 Bridge 场景展开 `Bridge route safety` 可编辑围栏、标识和门。新树干 Collider 在导入树根节点的 `Visible trunk collision` 子物件中，岩石碰撞保存为场景实例覆盖；没有改动导入素材原始 Prefab。已有 Scene & UI 的 Rescue crossing / reunion 预览也会带动门的位置。

本轮开始时 Unity 已关闭，因此直接在原项目保存与构建。修改前场景在 `qa/bridge-route-safety/before`。

## 验证结果

- 以玩家尺寸进行 0.5 单位间隔的胶囊体重叠和扫掠检查：1484 个空闲网格均从出生点连通，三个装置均有可达的近距离接近区域。没有依赖新增装饰 Collider 扩大地图边界。
- 保存后重载再次检查可达性；重复应用 Collider 总数保持 103，无重复围栏、碰撞或安全门；没有在 Editor 创建游戏会话。
- 最终 Edit Mode 四张图在 `qa/bridge-route-safety`：route-overview、river-gate、west-boundary、rescue-gate-open。
- 720p 与 1080p 的专项路线检查均通过，各 8 张实际玩家截图，位于 `qa/bridge-safety-final-route-720`、`qa/bridge-safety-final-route-1080`。通过 Input System 和原 CharacterController 行走，只有朝向由脚本控制，没有传送玩家。
- 路线检查覆盖出生点、三个装置接近点、西／后／东／河边界停止位置、Power Core 的 E 操作、未解锁转换台拒绝打开、安全门初始关闭及玩家不跌落。作答次数、心数和数学完成进度保持为零；正常探索时间与移动引导会照常记录。
- 720p 与 1080p 的修桥及后续 Integration 完整流程均通过，各 78 张截图，位于 `qa/bridge-safety-final-missions-720`、`qa/bridge-safety-final-missions-1080`。包含实际行走、操作、保存／恢复、救援、关卡衔接及总关闭。
- 已查看实际玩家贴近围栏的画面，确认最终版本可看到栏杆；也查看实际救援过桥截图，门已打开，小牛和桥面仍可见。
- 四份最终运行日志未出现本次检查的脚本异常、Shader 错误或失败标记。两个 Windows 构建成功，最终日志 `qa/bridge-route-safety-camera-build.log`。普通发布程序集包含 BridgeBoundaryGate，不包含路线检查或 MissionsPlaytest。
- 源码备份：`unity/backups/2026-10-15-bridge-route-safety/CalculusLabEscape-source.zip`，排除 Library、Builds 和验证副本。

这是脚本驱动的实际控制器检查与画面检查，不能代替后续第一次接触游戏的真人试玩。本轮没有开始路线图第二步的供能机／卷扬机械动画。
