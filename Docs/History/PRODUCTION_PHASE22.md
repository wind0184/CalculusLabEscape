# 第二十二阶段：实验室与动物素材候选

日期：2026-10-14。

在已导入的森林素材之外，补充实验室室内与牛模型，制作独立 Edit Mode 预览供挑选。原有森林素材预览和玩法关卡保留。

## 实验室

[Kenney Space Station Kit](https://kenney.nl/assets/space-station-kit) 为 CC0 科幻室内包。官方页面列 90 个文件，本次下载压缩包实际包含 97 个 FBX 模型及调色纹理。

项目位置：`Assets/ThirdParty/Kenney/SpaceStationKit`。导入 FBX 与所需 `Textures/colormap.png`，配置当前 Built-in Render Pipeline 的 Standard 材质。样例 prefab 包括墙、墙面开关、关闭的门、两种电脑、显示屏、桌、储物柜和椅子。

预览：`Assets/_Project/Scenes/LabAssetsPreview.unity`，菜单 `Calculus Lab → Assets → Open laboratory asset preview`，或 `unity/Open-Lab-Assets.cmd`。该包与现有 Kenney 森林都采用简单低多边形造型，作为积分房间候选。Preview 中的电脑、门和开关尚未连接数学玩法。

## 动物

较新的 Ultimate Animated Animal Pack 的公开 Drive 牛文件下载返回 Quota exceeded，本次没有取得该新版模型。

改用 Quaternius 本人在 [OpenGameArt 发布的 Farm Animal Pack](https://opengameart.org/content/lowpoly-animated-farm-animal-pack)。下载包内有 7 个 FBX 与 CC0 授权文件，全部导入 `Assets/ThirdParty/Quaternius/FarmAnimals`；保留 Generic 骨骼和模型中的实际动画片段。牛的导入动画名称和时长记录在 `qa/additional-assets/animation-clips.txt`，以实际文件为准。

预览：`Assets/_Project/Scenes/AnimalAssetsPreview.unity`，菜单 `Calculus Lab → Assets → Open animal asset preview`，或 `unity/Open-Animal-Assets.cmd`。Cow prefab 使用可见站立姿态、示例比例和落地位置；两只牛是同一模型的尺寸对比。包内没有单独的小牛模型，缩小副本仅作参考；正式小牛仍需调整外形和救援动画。

两个预览场景不在游戏 Build Settings 中，不创建 Chapter4Game 或学习记录。尚未把新模型替换到修桥／积分正式关卡，也未更改关卡脚本、碰撞或救援路线。

## 来源与文件校验

原始包位于 `unity/asset-library/2026-10-14-additional`，目录内保存下载记录；项目中的每包保留 License.txt 和 SOURCE.txt。

- `kenney_space-station-kit.zip` SHA-256：`215E79BD5415CFF93665183390F0343ED9ACF87780306331013B78520170C6D8`。
- `quaternius_farm-animals.zip` SHA-256：`9FF42B7E2445EAF259E4734C08172637DB92E4B59A6145909AC56D946152C39B`。

Editor 配置与检查：`Scripts/Editor/AdditionalAssetLibrary.cs`，入口 `PrepareAndValidate`；再次检查用 `ValidateAndCapture`。日志 `qa/additional-assets-import.log`，证据目录 `qa/additional-assets`。此轮验证资源、材质、骨骼、动画导入和预览，不代表已完成救援动作接入。

## 验证与交付

- 97 个实验室模型与 7 个动物模型通过有效包围盒和持久化 Standard 材质检查。实验室材质使用导入的调色纹理。
- 牛实际包含 Death、Idle、Jump、Run、Walk、WalkSlow 六个动画片段。另一次 Edit Mode 检查在不同时间采样 Walk，确认骨骼姿势发生变化，并保存 `cow-walk-pose.png`；正常牛预览仍保留站立姿态。
- 实验室、牛站立与牛行走姿态截图位于 `qa/additional-assets`；最后验证日志为 `qa/additional-assets-verified.log`。
- 导入前已有的 5 个场景（含森林素材预览）与 4 个 UI prefab 校验值保持一致。没有重新运行整关玩法或更新可执行文件。
- 源码备份：`unity/backups/2026-10-14-additional-assets/CalculusLabEscape-source.zip`，包含资源、授权、Editor 代码与打开预览脚本，排除 Library 和 Builds。
