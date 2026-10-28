# 第二十三阶段：把导入美术布置到实际关卡

日期：2026-10-14。

已在保存的 Differentiation / Integration 关卡中布置 Kenney 模型，使用原有世界而非重新生成关卡。导入素材现在参与实际游玩场景；独立素材库预览继续保留。

## 场景内容

- 修桥：14 棵松树、12 块岩石、24 组草。旧树、岩石和草的占位外观隐藏；旧物件、碰撞和路线仍保留。
- 积分：墙板、墙面显示屏、侧工作台电脑、椅子、储物柜、设备箱及后墙装饰门。家具与墙面道具布置在房间两侧和后部；装置及四模块继续使用原有对象和引用。

Hierarchy 的世界根节点下新增 `Imported environment art`，分组为 Forest trees、Riverbank rocks and grass 或 Workstation furniture、Wall panels and displays。新增外观保留模型／prefab 引用，可在 Editor 中选中、移动和调整比例。

本轮新增对象作为装饰外观，不新增碰撞或互动按钮。牛模型仍在独立 AnimalAssetsPreview 中，尚未替换救援角色；后续需适配小牛外形、骨骼动画与救援路线。

## 不按 Play 查看

双击 `unity/Open-Dressed-Levels.cmd`，打开实际修桥关及 Scene & UI 工作区，选择新增素材分组；菜单 `Calculus Lab → Assets → Open dressed Bridge level / Open dressed Integration level` 可切换两关。打开时显示 World 预览；Scene 可看全景，Game 可看玩家镜头。

该打开入口只设置 Editor 预览，不自动保存预览变化。原有的 UI 预览和设备选择工具仍可使用。

## 修改保护与验证

修改前的两关场景副本在 `qa/mission-asset-dressing/before`。Editor 迁移检查已有物件的世界矩阵、所有碰撞组件及 Chapter4World 序列化引用；除了指定旧装饰的可见性，新素材不修改这些值。重复调用会保留已存在的 Imported environment art 分组，不覆盖后续手动布置。

迁移与构建入口：`Scripts/Editor/MissionAssetDressing.cs` 的 `ApplyAndBuild`。全套 Edit Mode 场景与 UI 检查写入 `qa/mission-asset-dressing`；Windows development / 普通发布构建日志为 `qa/mission-asset-dressing-build.log`。使用既有两关完整流程测试确认玩法、移动、保存与完成表现。

## 本次结果

- 两关的原有世界矩阵、碰撞组件与序列化玩法引用检查通过；四个源 UI prefab 的 SHA-256 保持一致。
- Edit Mode 检查通过，保存 57 张真实 Unity 场景／UI 截图，包含新森林与积分房间全景；没有创建游玩 session。
- Development 与普通发布版本均构建成功。1280×720 和 1920×1080 两关完整流程测试均通过，各保存 78 张截图，证据在 `qa/dressed-missions-720`、`qa/dressed-missions-1080`。
- 完整流程包括实际玩家控制器行走、修桥三个部件和救援、积分四模块和关闭装置、提示、保存／恢复、关卡衔接及练习记录。测试使用隔离存档，不改个人历史记录。
- 源码备份：`unity/backups/2026-10-14-dressed-levels/CalculusLabEscape-source.zip`，含场景、导入素材、授权、Editor 代码与打开脚本，排除 Library 和 Builds。
