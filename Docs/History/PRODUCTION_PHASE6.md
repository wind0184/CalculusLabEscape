# 第六阶段：两关场景材质、灯光与环境细节

日期：2026-10-08。本轮继续完善 Differentiation / Integration 的实际 Unity 场景，不涉及登录或 Firebase。

## 场景变化

- 显式设置环境光模式，降低反射强度并启用主光阴影，改善原先偏白、缺少层次的表现。第二关用暖色日光和绿色植被；第三关用冷色灯光与深色金属。
- 桥边：分层低多边形松树、木质扶手和桥板、石头步道、岸边岩石与草丛、河面细纹。大部分新增细节没有碰撞体，保持原有行走空间。
- 修复设备：底座、金属框架、前面板通风口、可读的标牌底板；Power Core 拨盘、Converter 导轨与 Bridge Builder 木料／装配轨道各有不同外形。顶板接近机身，不再明显悬空。
- 桥板安装后仍保留木质材质，绿色边缘标记表示已修好；错误不会撤销标记或移动已安装桥板。
- 牛增加斑纹与蹄部颜色。救援时先对准桥口再沿桥面行走，避免原来的对角路线穿过扶手；救援镜头隐藏任务标牌和导航球。
- 实验室：地面导向灯带、墙面灯条、顶部灯具与梁、箱体包角和金属边框、模块螺丝、拨盘刻度。箱盖内加入简短操作说明；场景名称改为较小的设备铭牌。

没有重建 UI Prefab，题目与评分规则保持原流程。场景使用自建几何体和程序材质，未引入外部模型、图片或纹理；这些是可玩的视觉迭代，不代表最终美术验收。

## 主要代码

- `Assets/_Project/Scripts/Chapter4/Chapter4EnvironmentArt.cs`：色彩、灯光、树木网格、环境装饰与箱体细节。
- `Chapter4World.cs`：场景生成接入、木桥与修复标记、牛的过桥路线。
- `Chapter4BridgeFeedback.cs`：高对比标牌，救援时隐藏导航对象。
- `Chapter4CaseFeedback.cs`：拨盘刻度和模块标签。
- `Assets/_Project/Scripts/Production/MissionsPlaytest.cs`：补充救援镜头及实验室探索画面，沿用两关完整交互验证。

## 验证

首轮截图暴露的标牌遮挡与过桥路径已修正。首轮锁扣断言把新增导轨也计入锁扣，已用明确的导轨命名消除混淆。

- `../../qa/environment-art-final-720/result.txt`：PASS，1280×720。
- `../../qa/environment-art-final-1080/result.txt`：PASS，1920×1080。
- 两次均完整通过第二、第三关，包括第二关真实 CharacterController 移动／碰撞、全部数学步骤、提示、保存续玩、部分接线恢复、设备动画、音效数据与暂停、最终关闭装置和本地记录。
- 查看了实际 Unity 渲染的森林探索、桥梁救援、实验室探索与装置关闭画面。未记录最终测试运行错误或 Canvas 文案溢出。
- `../../qa/environment-art-delivery-build.log`：开发版与正常启动版构建成功；正常程序集包含环境视觉代码，不包含 MissionsPlaytest。
- 源码检查点：`../backups/2026-10-08-environment-art/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 和说明，不包含 Library 或 Builds。

截图以实际 Unity 世界／Canvas 渲染至 RenderTexture；第二关由脚本设置朝向并注入移动按键，第三关仍使用设备附近定位与倒计时加速。自动测试不能代替真人的光照可读性、移动手感与整体美术验收。
