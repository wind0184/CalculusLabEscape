# 第二十一阶段：导入 Kenney 美术资源

日期：2026-10-14。

## 项目位置

导入 `Assets/ThirdParty/Kenney`，三个资源包各自保留 License.txt 与 SOURCE.txt。文件从已核对 SHA-256 的官方原始压缩包选择提取。

- `NatureKit/Models`：329 个 FBX 模型，包含树、岩石、草与桥部件。
- `NatureKit/Materials`：配置当前 Built-in Render Pipeline 使用的 Standard 材质；保留导入模型的颜色。
- `NatureKit/Prefabs`：10 个树、岩石、草与桥部件样例，调整成便于预览的尺寸并对齐地面。可拖入场景，按实际布局再调整大小；模型没有新增玩法碰撞。
- `SciFiUI/Sprites`：60 张蓝色默认款 PNG，配置为 Sprite，保留透明通道。
- `InterfaceSounds/Audio`：100 段 OGG 音效，配置为 AudioClip；在 Project 选中后可使用 Inspector 的音频预览。

选取一套 UI 颜色和模型 FBX 格式，避免重复导入其他模型格式、等角截图和 UI 分辨率／配色变体。原始完整压缩包仍在 `unity/asset-library/2026-10-14`。

## 不按 Play 看素材

双击 `unity/Open-Asset-Preview.cmd`，或在 Unity 打开 `Assets/_Project/Scenes/AssetLibraryPreview.unity`。菜单 `Calculus Lab → Assets → Open imported asset preview` 也可打开。Game 视图展示模型和实际 UI 图形；Hierarchy 的 Imported model samples 可直接选中查看。预览场景不在游戏 Build Settings 关卡列表中，不创建 Chapter4Game 或学习记录。

当前关卡没有替换模型或 UI，也没有把新音效接到按钮。下一步先将树、岩石、草应用到修桥河岸，再处理积分房间。素材导入与具体场景美术接入是两个不同步骤。

## 导入与检查入口

文件提取脚本：`tools/import_kenney_assets.ps1`。运行前核对压缩包校验值，保护现有同名文件，检查目标路径和文件数。

Unity 配置与检查：`Scripts/Editor/KenneyAssetLibrary.cs`，批处理入口 `CalculusLabEscape.Editor.KenneyAssetLibrary.PrepareAndValidate`。材质使用持久化资源，样例 prefab 和预览场景存在时不会重新生成。再次验证可调用 `ValidateAndCapture`。

证据位于 `qa/asset-import`，Unity 日志为 `qa/asset-import-unity-final.log`。检查所有模型的有效包围盒、持久化 Standard 材质、Sprite 加载、音频长度、预览文字，以及原有关卡／UI 文件的校验值。此阶段没有更新游戏可执行文件，因为原关卡尚未引用新素材。

## 来源

- [Kenney Nature Kit](https://kenney.nl/assets/nature-kit)，包内版本 2.1，CC0。
- [Kenney UI Pack - Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi)，包内版本 2.0，CC0。
- [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds)，包内版本 1.0，CC0。

科幻室内与动物资源仍为待下载候选，不属于本次导入。

## 本次结果

- Unity 导入检查通过：329 个 FBX、60 个 Sprite、100 个有效 AudioClip；使用 23 个共享导入材质，另有 1 个预览地面材质。
- 10 个样例 prefab 和独立预览场景已保存。`qa/asset-import/asset-library-preview.png` 为 Unity 在 Edit Mode 渲染的实际画面；原包 `Preview.png` 用于核对颜色，树叶原色就是青绿。
- 原有 4 个关卡场景与 4 个 UI prefab 的 SHA-256 均保持一致。
- 普通关卡的运行代码与可执行文件没有变更。本次验证为资源导入和预览检查，没有重新运行整关玩法测试。
- 源码备份：`unity/backups/2026-10-14-asset-import/CalculusLabEscape-source.zip`，包含导入的资源、授权文件、Editor 配置代码及打开预览脚本，排除 Library 和 Builds。
