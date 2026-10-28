# 第十阶段：两关数学表达式的阅读体验

日期：2026-10-10。

## 显示变化

Differentiation / Integration 的题干、表达式预览、组合选项、接线端、拖动文字、提示、手册及练习题／解析使用真正的上标。整数、负指数、括号中的分数指数，以及 n+1 / n-1 等规则表达式均支持；sqrt(x) 显示为 √x。

保留数字和分数输入方式，输入框不解析富文本。此次没有改判题规则、题目数、正确答案或得分；存档仍保存原始的 x^4、x^(-1/2)、1/2 等字符串。学习记录打开时转换显示，不迁移或重写历史内容。

所选标签新增 MathText 组件，Source 字段保留纯文本，TextMeshPro 文本由该组件生成。反复刷新相同内容不重新运行格式转换。生成器与选择性升级方法均保留这套设置，其余 Prefab 布局不重新生成。

支持的表达式按明确语法识别；未知写法保持原样。保存文字中的 TMP 标签按字面显示，避免玩家输入改变历史记录字号或隐藏文本。记录明细一次性从原始数据生成，避免把已转换的标记再次处理。

当前采用行内分数（例如上标中的 -1/2），不是完整 LaTeX 排版器。世界内的旧 TextMesh 设备字牌未参与此轮 Canvas 字体更新。

## 代码

- `Assets/_Project/Scripts/Production/MathText.cs`：格式转换、字面文本处理、原文与渲染缓存。
- `Production/MissionCanvas.cs`、`Production/ProductionCanvas.cs`：标签赋值入口；学习记录保持原文拼接。
- `Editor/MathTextBuilder.cs`：所选标签、输入框配置、数学格式检查。`ProductionUIBuilder.UpgradeMathAndBuild` 升级 MainMenuUI、DifferentiationUI、IntegrationUI 后构建开发版。
- `Production/MissionsPlaytest.cs`：实际字符上标位置／缩放、输入原文、接线和练习显示、根号字形及保存文本隔离检查。

## 验证

证据目录为项目上两级的 `qa/math-notation-*`，测试使用隔离存档。

首轮截图显示字体默认上标缩为一半，分数指数偏小；已增大到当前字体下约 70% 的正文字号。首轮测试也暴露 TMP pointSize 不包含上标缩放因子，检查改用实际字符 scale 和基线位置，直接验证呈现出的字号与高度。

放大后发现 PowerExpression 与 PracticeResults 的高度不足，已增加对应文本空间并调整复盘布局。随后加入全部 7 组／21 题的实际 TMP 排版检查，覆盖最长的选中状态前缀及全部题目需复习的情况。

- `math-notation-build.log`：选择性升级数学文本组件及格式检查。
- `math-notation-layout-build.log`：最终布局开发版构建成功，数学格式、原有反馈与练习逻辑检查通过。
- `math-notation-layout-720/result.txt`：两关完整流程、练习及记录续做 PASS；实际上标缩放／基线、分数输入原文、全部练习排版、根号字形、字面标签及纯文本存档检查均通过。
- `math-notation-release-build.log`：正常启动版构建成功。
- `math-notation-layout-720/restart-result.txt`：独立新进程从学习记录继续原练习并完成 PASS。
- `math-notation-layout-1080/result.txt`：1920×1080 下两关完整流程、全部练习排版及记录检查 PASS。
- `math-notation-limits/result.txt`：第一关全部五题、错误统计、提示、暂停、续玩及记录页回归 PASS。
- 正常程序集包含 MathNotation / MathText，不包含 MissionsPlaytest / CanvasPlaytest。

已查看 Unity 实际渲染的求导、负分数指数、练习复盘和积分手册。测试输入与存档隔离，不影响玩家数据；自动排版检查不代表真人阅读体验验收。

源码检查点：`../backups/2026-10-10-math-notation/CalculusLabEscape-source.zip`，包含 Assets、Packages、ProjectSettings 与阶段说明，不含 Library / Builds。
