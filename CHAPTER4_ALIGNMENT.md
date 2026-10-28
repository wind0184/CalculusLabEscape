# Chapter 4 对照说明

依据 `fyp/04 Chapter 4/Chapter4_HengWeiYi_Consistency_Corrected.docx`（2026-09-14 更新）中的图 4.1–4.15 调整本原型。

| Chapter 4 | 原型对应内容 |
| --- | --- |
| 4.4 登录 | 中间账号输入，右侧学生/教师入口。演示口令 `demo`，不连接服务器、不保存密码。 |
| 4.5 主菜单 | 实验室双门入口示意图，右侧 Start、Student Dashboard、Instructions、Settings、Exit。 |
| 4.6 选关 | 三张任务卡，传送门、断桥与收容箱图示。 |
| 4.7 任务简介 | 左侧任务图，右侧 Objective、Learning Focus、Prerequisite、Support。 |
| 4.8 Limits | 左传送门及五个指示灯，右侧题目、探针/标记、草稿框、Submit，底部提示操作。 |
| 4.9 Differentiation | 上方实时桥面视图，下方指数拖放、系数槽、指数拨盘、结果预览及确认修理。后续步骤沿用输入与安装按钮。 |
| 4.10 Integration | 打开箱体后的四模块总览，先完成指数拨盘，再选择其他模块；总开关保持锁定到 4/4。 |
| 4.11 提示 | Basic Hint、Rule Reminder、Guided Demonstration 三选项，显示爱心成本，确认后显示内容。 |
| 4.12 总结 | 准确率、错误次数、完成率、爱心使用、时间及常见错误，三条返回路线。 |
| 4.13 教师页 | 本机记录、学生/主题筛选、逐次作答、完成情况、常见错误和本地 CSV 导出。 |
| 4.14 错误反馈 | Your Step、Expected Relationship、Environmental Consequence、Retry、提示。 |
| 4.15 学生页 | 总准确率、完成主题数、记录列表、最近尝试及常见错误。 |

三个三维场景使用基础模型：带控制台和设备墙的传送门房间；有桥栏、修理装置和小牛的河岸；桌面上具有铰链箱盖、四个模块和总开关的收容箱。场景配色采用低饱和灰色，界面采用黑白线框，接近章节草图的结构。

程序入口仍是 `Builds/Chapter4/CalculusLabEscape_Chapter4.exe`，Unity 场景仍是 `Assets/_Project/Scenes/MainMenu.unity`。

这是布局与玩法验证原型。机械细节、正式美术、骨骼动画、语音、真实账号权限和数据库未接入；老师入口是本地记录预览，不构成身份认证。部分机械操作使用输入框、选卡和按钮表达，+C 和模块状态仍会实际验证。积分倒计时在打开箱体后运行，零点进入 Safety Mode，暂停和提示选择时不推进。
