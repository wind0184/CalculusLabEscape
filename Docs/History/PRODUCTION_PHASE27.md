# 第二十七阶段：由方形地块改为连续河谷

日期：2026-10-14。用户指出上一版“感觉只有一块地”。本轮集中修正修桥关的地面尺度、地形轮廓和远景衔接。

## 画面与范围

中央桥梁、设备、动物和步道保持原位置，周围延伸成连续两岸。中央训练区域保持平坦；外围逐渐形成低多边形山坡，远处铺设 92 棵松树、18 处岩石及五个小型模拟测量灯。河道在中央保持原宽度，向左右延伸并逐渐弯曲。岸边新增土层侧面，补齐地形表面与水面之间的缝隙。

原 Near riverbank / Far riverbank 的 Renderer 隐藏，原物件及 Collider 保留。上一版的方形浮岛、外围框线、下方数据平台、远景门框和数据灯塔也只隐藏 Renderer，没有删除层级。原有模拟网格天空继续使用。

外围山坡目前是景观，行走边界沿用原训练区域。本轮没有扩大可行走区域或改变救援路线。Limits 和 Integration 的背景保持上一阶段版本。

## 保存与 Editor

世界根节点的 `Connected river landscape` 包含两岸地形、岸坡侧面、背景森林、岩石和灯柱。延伸河面 `Continuous river surface` 挂在现有 `Rising river` 下，因此升水、恢复存档和重开关卡都会沿用原水位变化；未增加运行时脚本。

材质及四个 Mesh 保存在 `Assets/_Project/Art/ConnectedLandscape`。地面中间沿用原河岸材质，山坡和露岩使用新 Standard 材质。远景树木和岩石使用已导入的 Kenney Nature Kit；新岩石材质只覆盖本轮岩石实例，不改动素材的共用材质。

代码：`Scripts/Editor/ConnectedLandscapeBuilder.cs`。菜单 `Calculus Lab → Background → Add connected Bridge landscape` 只在缺少分组时添加，不重建已有地形。沿用 `Calculus Lab → Sky → Open Bridge simulation` 打开实际场景，不需要按 Play。

原 Unity 窗口仍有未保存的场景预览，未关闭或强制终止。验证时使用 `unity/SimulationSkyValidation`：从原项目更新 Assets、Packages、ProjectSettings，保留独立 Library。确认原项目磁盘场景在验证期间没有改变后，同步已验证的场景、新资源与两个 Windows 构建。

本次在原项目 Library 中排队一个仅本次使用的地形应用请求。若当前 Unity 仍显示旧地块，选择 Assets → Refresh。脚本导入后的请求会在非 Play、非编译／导入时，将地形合并到已经打开的场景并保存，保留窗口中现有物件的编辑。若已执行过，则保留现有分组。状态文件为 Library/CodexConnectedLandscape.status。

## 验证与交付

- 修改前的磁盘场景及 SHA-256 位于 `qa/connected-landscape/before`；同步前核对未变化。
- 原有世界矩阵、材质、玩法引用及碰撞组件逐项保持一致；仅允许本轮指定的旧地面与背景 Renderer 隐藏。新增景观没有 Collider 或 Light。
- 连续应用两次不增加重复地形或树木；重新打开保存场景后验证分组存在，延伸水面随原河水升高 0.25 单位。没有生成游戏会话。
- 三张实际 Edit Mode 截图在 `qa/connected-landscape-copy`：landscape-world、landscape-player、landscape-panorama。已查看中央操作区、玩家视角和远景；修正了初版岸边暗缝及过大、过亮的岩石。
- 最终两个 Windows 构建成功，日志 `qa/connected-landscape-final-build.log`。已同步至原项目 Builds/Chapter4 与 Builds/LearningLab。
- 720p、1080p 两关完整自动游玩均通过，各 78 张截图，位于 `qa/landscape-missions-720`、`qa/landscape-missions-1080`。包括实际行走、修桥操作、救援、保存／恢复、关卡衔接及 Integration 总关闭。
- 两份运行日志未发现本次检查的脚本异常或 Shader 错误标记。原项目与副本的 84 个脚本一致；交付场景、景观资源、两个程序集及关卡二数据文件的 SHA-256 核对一致。普通发布程序集不包含自动游玩脚本。
- 源码备份：`unity/backups/2026-10-14-connected-landscape/CalculusLabEscape-source.zip`，排除 Library、Builds 和验证副本。自动验证不代替真人体验及性能测试。
