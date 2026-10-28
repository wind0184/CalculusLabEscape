# 第二阶段：真实账号、学习记录与教师工作区

> 已被当前方向取代：用户决定登录延后并使用 Firebase。游戏现在直接本地游玩，第二、第三关已迁移 Canvas。请以 `PRODUCTION_PHASE3.md` 和当前使用说明为准；下文保留为历史实施记录。

依据 Portfolio 的 FR01–04、学生记录和教师查看要求，本阶段打通 **登录 → Limits 五题 → 保存/续玩 → 结算 → 教师查看及导出**。主菜单、Limits、登录、记录与教师页面采用深色底、青色重点；正确反馈为绿色文字，错误反馈为琥珀色文字。三维场景仍是原型美术。

## 开始使用

1. 双击 `../Start-LearningLab.cmd`，启动本机学习服务和游戏。此电脑已有所需的 .NET 10 ASP.NET Core Runtime。
2. 第一次打开，填用户名和密码，点击 **Set up first teacher**。用户名 3–32 个字符，支持字母、数字、下划线、点及连字符；密码 12–128 个字符。
3. 再输入密码，点击 **Sign in**。教师登录后进入 Learning evidence；点击 Sign out 可退出。
4. 学生填自己的用户名和密码，点击 **Create student account**，然后重新输入密码登录。没有预置的正式账号或默认密码。
5. 学生通过 **Choose a mission → Begin Limits** 开始。**Pause → Save & return** 保存；下次登录同一账号，用 **Continue Limits** 续玩。
6. 学生退出后，教师登录，可按学生、主题、题目、错误类型、状态筛选，点击 **View sessions** 查看具体回答。**Export results as CSV** 导出筛选所得场次的全部答题证据，路径显示在页面上。

Unity 编辑器：在 `unity` 文件夹运行 `./Start-LearningLab.ps1 -ServiceOnly`，再打开 `Assets/_Project/Scenes/MainMenu.unity` 并 Play。默认连接 `127.0.0.1:5187`。直接打开关卡也会要求先登录。

后台服务可一直运行；需要停止时运行 `../Stop-LearningService.ps1`。停止服务不会删除数据。Windows 重启后重新运行启动器即可。

## 已实现及边界

- ASP.NET Core API + SQLite，实际存储用户、角色、主题、题目、提示、答题尝试、提示使用、设置表与游戏场次；设置表目前只建档，音量和窗口偏好仍存在本机。
- 学生注册不能自选 Teacher 权限。首次教师设置需本机一次性密钥，成功后删除密钥并关闭该入口。后续教师邀请、密码找回、账号管理尚未实现。
- 后端根据登录身份决定记录归属。学生只能读取自己的记录；教师接口在服务端校验角色。密码保存为独立加盐的 PBKDF2 哈希；登录凭据仅保存在运行时内存，注销使服务端令牌失效。
- 服务端重新核算 Limits 的正确性、完成率、错误类别、心数；拒绝跳题、伪造完成、覆盖已有回答、他人场次写入。重复上传不会重复计数。
- 成绩先原子写入本机缓存，再上传；暂时断开服务时显示待同步并自动重试。未同步成功时阻止正常注销。程序异常退出后，下次登录同一账号会恢复上传。
- 本机缓存按数据库实例 ID 和账号 ID 隔离；旧匿名 JSON 保留，不会自动导入新账号。登录后从服务端获取历史记录；密码、令牌不写入缓存。
- **只接入了 Limits**。Differentiation、Integration 的原型代码、场景与回归测试保留，正式菜单显示 Account migration pending，待接入同样的权限及成绩流程。
- 当前服务只绑定本机回环地址，供这台电脑演示和开发。没有云端部署、跨电脑共享、TLS、自动备份恢复或正式运维功能。同一账号多客户端并发编辑同一场次会拒绝冲突，尚无合并界面。
- 计时来自客户端，经过格式与单调性校验；不是防作弊考试系统。没有声称实现所有 Portfolio 要求或达到发布验收。

## 文件位置

| 用途 | 文件 |
| --- | --- |
| 登录、权限路由、教师筛选与导出 | `Assets/_Project/Scripts/Production/ProductionAccountScreens.cs` |
| HTTP 请求、登录状态、同步队列 | `Assets/_Project/Scripts/Production/LearningApi.cs` |
| 页面、题目及记录交互 | `Assets/_Project/Scripts/Production/ProductionCanvas.cs` |
| 账号页面生成器 | `Assets/_Project/Scripts/Editor/AccountUIBuilder.cs` |
| 可视化调整页面 | `Assets/_Project/Resources/Production/MainMenuUI.prefab`、`LimitsUI.prefab` |
| API、身份验证及权限 | `../LearningService/Program.cs` |
| SQLite 数据读写与证据保护 | `../LearningService/Database.cs` |
| 服务端数学规则与密码哈希 | `../LearningService/Models.cs` |
| 数据库结构、题库 | `../LearningService/schema.sql`、`limits.json` |
| 正式运行数据 | `../LearningService/data/learning.db`；首次启动后生成 |

题库调整：认证模式从服务的 `limits.json` 读取题干与提示。Unity 的 `LimitsQuestions.asset` 是离线/开发测试的副本。两者须同步；改变答案、题型、顺序或 ID 时，也要改服务端 `LimitsValidation` 并运行测试。只改 Unity 资源不会更改认证模式的后端题库。

启动器使用不含 QA 绕过入口的 `Builds/LearningLab/CalculusLabEscape.exe`。正常打包调用 Unity `ProductionUIBuilder.BuildRelease`（菜单 `Calculus Lab > Production > Build Windows app`），保留已编辑的 Prefab。`Build` 输出开发测试版到 `Builds/Chapter4`；`GenerateAndBuild` 会覆盖生成 UI，仅在需要重建时使用。

发布本机后端（在 FYP 根目录）：

```powershell
dotnet publish unity/LearningService/LearningService.csproj -c Release -o unity/LearningService/bin/publish
```

交付另一台电脑需要 .NET 10 ASP.NET Core Runtime，且要保留整个 Unity build 与 LearningService 发布目录。当前启动器没有自动安装依赖。

## 测试与备份

改动前源码备份：`../backups/2026-10-07-before-auth/CalculusLabEscape-source.zip`。

`LearningService/test_api.py` 只可用于**全新隔离测试数据库**：检查首次教师设置、角色限制、个人记录隔离、服务端答案复算、证据不可重写、幂等上传、筛选、CSV、注销与数据库完整性。

Unity 开发版本的 `--auth-qa` 会使用真实 HTTP 登录，不开启匿名测试绕过。它需要 `--qa-output`、`--qa-credentials`、`--auth-cache` 和 `--api-url`，后两项应指向隔离测试路径与服务。测试密码文件只用于隔离测试，不是正式账号配置。

`--canvas-qa` 与 `--chapter4-qa` 仅用于开发回归，会明确绕过登录；启动器所用的非 Development 构建已移除这些入口。后端访问始终需要真实登录，不受客户端测试开关影响。

自动化以 Canvas 射线与 EventSystem 点击、Input System 注入移动/E/Esc 验证流程；隐藏窗口通过 Unity RenderTexture 渲染实际 Canvas/世界以核查布局。实际键鼠手感、输入法、可访问性、音频、教学效果还需要真人试玩。

## 本轮验证结果（2026-10-07）

| 验证 | 结果及证据（相对于 FYP 根目录） |
| --- | --- |
| .NET 后端编译及发布 | 成功，0 warnings / 0 errors |
| API / SQLite | `qa/auth-api-final/api-result.txt`：17 项断言通过，并验证 401/403/409/400 错误分支 |
| 1280×720 登录到教师审阅 | `qa/production-auth-verified-720/result.txt`：PASS |
| 1920×1080 完整流程、下拉选项点击、断线恢复 | `qa/production-auth-verified-1080/result.txt`：PASS；测试确实停止后端，再重启并验证补传后的服务端记录 |
| 最终开发版及正常启动版 | `qa/production-auth-final-build.log`：两个构建均成功，数学规则检查通过 |
| 正常启动版检查 | `qa/production-auth-release-smoke.log`：启动并成功请求 setup-status，无异常；程序集不包含两种 QA runner，传入测试参数也未执行绕过流程 |

已检查登录、题目图表、教师筛选、展开的下拉选项及答题证据的 Unity 渲染截图。正常启动版使用与开发测试相同的 UI Prefab。

本轮旧 IMGUI 三关回归在隐藏窗口中无法触发 OnGUI 按钮，留下 `qa/production-auth-legacy-regression/result.txt` 的失败记录；该运行不能用来判断三关玩法是否通过。另两关未获得本轮人工或可见窗口验收。早期截图黑屏和集中测试触发登录限流的失败记录同样保留；上述 verified 目录是可用的最终验证证据。

本机正式服务已启动在 5187，首次教师账号仍待用户自行设置。所有测试账号、密码和场次都在独立 QA 数据库中。
