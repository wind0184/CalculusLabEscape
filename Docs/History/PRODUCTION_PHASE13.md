# 第十三阶段：编辑器场景分组与完整 UI 预览

日期：2026-10-12。

## 使用变化

**Calculus Lab → Scene & UI Workspace** 窗口改为可滚动，并用 **UI preview → Screen** 下拉菜单选择界面。点 **Show selected UI** 显示，切到 Game 标签看完整排版。

两关新增 Pause、Support、Controls help、Save & return、Practice question、Practice explanation、Practice review，积分还新增 Master deactivation。修桥共 13 个预览页，积分共 16 个。练习预览使用已有题库的真实题干、选项及解析，但没有创建 PracticeRun 或会话，也没有写入存档。模块小预览相机对准所选积分模块。

**Find a device** 可直接选中并聚焦三个修桥设备、小牛／母牛、四个积分模块及总关闭开关。选中的是设备分组，展开后可以编辑组成设备的物件。

## Hierarchy

- 修桥：Environment（树木、河流、步道和岸边）、Bridge、Repair devices（Power Core、Converter Box、Bridge Builder）、Animals、Lighting、Boundaries。
- 积分：Room、Containment device（Case shell、Exponent dial、Wire matching、Symbol sequence、Pressure gauge、Master deactivation）、Lighting。
- 极限：Room、Portal、Examination console、Status indicators、Lighting。

Player 和 Storyboard view 保留为关卡根节点的直接子物件。新增分组采用默认 Transform，原物件 reparent 时保留世界位置、旋转和缩放。已有桥、盖板、指针、卡片等子结构保留；Chapter4World 的脚本引用不改。重复整理不会增加重复分组，也不会重新生成已有物件。

空名字的修桥牌子改为设备名称加 sign，方便查找；牌面文字仍由关卡状态更新。动画继续控制动态部件姿态。

## 代码与检查

- `Assets/_Project/Scripts/Editor/SceneOrganization.cs`：分组迁移、变换矩阵／碰撞体／引用保持与重复执行检查。
- `Assets/_Project/Scripts/Editor/SceneAuthoring.cs`：滚动窗口、设备选择、29 个预览页和模块镜头。

本轮调用 `SceneOrganization.OrganizeAndBuild` 整理三关场景，并将编辑模式截图写到 `../../qa/editor-workspace`。迁移检查会比较所有 Renderer 的完整世界变换矩阵、碰撞体身份和关卡组件序列化内容；随后从磁盘重载检查持久化引用、渲染 UI 并检查文本溢出。

- `qa/editor-workspace/organization.txt`：三关全部 Renderer 的世界变换保持一致，碰撞体身份和 Chapter4World 的序列化引用保持一致；重复整理稳定。
- `qa/editor-workspace/result.txt`：从磁盘重载三关并渲染 29 个任务 UI 预览 PASS，无文字溢出；没有启动玩法会话。
- `qa/editor-workspace-build.log`：开发版构建成功。
- `qa/organized-worlds-missions/result.txt`：两关完整游玩回归 PASS，覆盖真实碰撞移动、11 步修桥、4 个积分模块、三维动画、拖放、暂停、存档续关、结算练习和学习记录。
- `qa/organized-worlds-limits/result.txt`：第一关五题、真实移动、提示、暂停、存档续关和记录页回归 PASS。
- `qa/editor-workspace-release-build.log`：正常运行版构建成功，正式程序集不含测试脚本。

已查看实际编辑模式渲染的练习复盘、提示页和积分拨盘页，文字完整，模块镜头正确。自动流程检查不代替真人编辑和试玩验收。

源码检查点：`../backups/2026-10-12-editor-workspace/CalculusLabEscape-source.zip`。
