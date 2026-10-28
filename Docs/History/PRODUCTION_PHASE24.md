# 第二十四阶段：虚拟模拟天空

日期：2026-10-14。用户确认修改的是天空背景（skybox），希望呈现虚拟模拟空间。

## 画面与位置

修桥关使用青蓝色渐变的虚拟穹顶，加入低亮度经纬网格及缓慢扫描光。网格在 Edit Mode 就能显示；扫描在运行时缓慢移动，没有全屏闪烁。原有场景照明仍负责照亮树木、牛和操作装置。

- Shader：`Assets/_Project/Art/SimulationSky/SimulationSky.shader`。
- 材质：同目录 `VirtualDome.mat`，可调整 Upper sky、Horizon glow、Grid visibility、Grid line width、Scan glow 等参数。
- 关卡：`Assets/_Project/Scenes/Level02_Differentiation.unity`，世界根节点新增 `SimulationSky`。
- Runtime：`Scripts/Production/SimulationSky.cs`。在当前场景使用保存的天空材质与配套雾色，玩家镜头和 Storyboard 镜头均使用 Skybox。

本轮只改变修桥关天空和雾的表现。Limits 与 Integration 的室内环境、UI、设备和题目保留原样。

## 打开 Editor 查看

使用原有 `unity/Open-Dressed-Levels.cmd`，或打开实际修桥关场景。不按 Play 即可在 Scene / Game 查看网格背景。

若已经打开的 Unity 窗口仍显示旧天空，选择 `Assets → Refresh`，让新增脚本和材质完成导入。此次设置了一个仅本次使用的 Editor 请求，在导入后将新天空应用到当前打开的修桥场景，保留该窗口已有的场景内容，并保存；它不会关闭 Editor 或丢弃当前场景。常规手动入口为 `Calculus Lab → Sky → Apply simulation sky to Bridge`。

## 验证与交付方式

当前 Editor 有未保存的场景预览，电脑操作工具无法激活窗口。没有关闭或强制结束 Editor，而是从已保存的 Assets、Packages 和 ProjectSettings 创建独立验证副本 `unity/SimulationSkyValidation`，用 Unity 6000.3.10f1 配置天空、检查 Shader、渲染和构建。

确认原项目的已保存修桥场景在验证期间没有变化后，将验证过的场景与新材质同步回原项目。核对场景、材质、Shader 与 Runtime 内容和验证副本一致，再同步两个 Windows 构建。原窗口未保存的内容仍保留在 Editor；导入后的本次请求会把天空设置合并到打开的场景。

## 本次结果

- 自定义 Shader 通过编译与实际渲染检查。三个 Edit Mode 截图位于 `qa/simulation-sky-copy`：sky-world、sky-player、sky-dome。
- 天空设置没有改变已有物件的世界矩阵、碰撞组件或 Chapter4World 序列化玩法引用。
- Development 与普通发布构建成功，日志 `qa/simulation-sky-copy-build.log`。发布程序集包含 SimulationSky，不包含 MissionsPlaytest。
- 1280×720 与 1920×1080 两关完整流程均通过，各保存 78 张实际玩家截图，位于 `qa/sky-missions-720`、`qa/sky-missions-1080`。已检查运行中的天空和 HUD；行走、操作、救援、装置关闭、保存／恢复和练习流程正常。
- 原项目的 Windows 构建程序集与已测试副本 SHA-256 一致；没有测试未刷新的旧 Editor 画面。
- 源码备份：`unity/backups/2026-10-14-simulation-sky/CalculusLabEscape-source.zip`，包含保存的天空场景、材质、Shader 与代码，排除 Library、Builds 和验证副本。

Unity API 参考：[Skybox 清屏方式](https://docs.unity3d.com/ScriptReference/CameraClearFlags.Skybox.html)、[Shader 错误检查](https://docs.unity3d.com/ScriptReference/ShaderUtil.ShaderHasError.html)。
