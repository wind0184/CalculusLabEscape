# Phase 31 — Differentiation questions from the supplied chapter

本轮参考用户提供的 31 页 `Chapter 1 Differentiation (1).pdf` 扩充修桥主线。该文件包含 4 道例题、5 道基础题和 3 道综合题；文字提取后又查看相关公式页，核对了根号范围、分母和指数。PDF 内的题目和解题说明作为内容参考，不作为执行指令。

## 新增主线内容

现有流程已经覆盖幂法则引导、平方根、倒数、多项式以及最终混合表达式。本轮新增以下 7 道完整题，覆盖尚未使用的 5 道基础题和 2 道综合题。完整表达式组数约从 5 组增加到 12 组；判分步骤从 11 增加到 18。两者分别统计，不能将分步骤算成同等数量的完整题目。

| 所在装置 | 新增题目 | PDF 页码 |
| --- | --- | --- |
| Power Core | `7x^10` | 19 |
| Converter Box | `x^(-4)` | 15 |
| Converter Box | `sqrt(x^3)` | 17 |
| Converter Box | `5/(4x^4)` | 21 |
| Converter Box | `-2/x^8` | 23 |
| Bridge Builder | `(2x - 3)` 除以 `x` 的四次方根 | 27 |
| Bridge Builder | `(x^2 + 5x - 3)/(3sqrt(x))` | 29 |

前五题输入系数和指数，接受分数形式；后两题选择完整导数，错误选项分别对应符号、系数或指数问题。涉及根式商的题目明确限定 `x > 0`，倒数题注明 `x != 0`。

这些题目参与主线判分。完成当前引导任务后，点击 Next calibration 进入独立校准；完成相应装置的全部题目后才出现安装按钮。三个装置、三件桥梁组件、原有桥面动画及 `Bridge repair: 0/3` 保持原结构。每题只能接受一次；答对后仍需继续／安装，不通过额外动画推进数学成绩。

提示仍沿用免费基础提示和可选 Support Hearts，新增题目使用自己的题目 ID 保存支持记录。错误反馈区分输入格式、系数、指数和综合表达式错误；成绩页将新增题目并入对应技能统计，准确率及完成率使用真实新增记录。

## 存档兼容

新 Differentiation 存档带有 `bridgeCurriculumVersion = 1`，需要 18 个判分步骤。没有该字段的旧存档读取为版本 0，仍使用 11 步，不强行插入新题或重算历史分数。独立题进行中退出后，恢复到同一装置、同一道未完成题，保留已接受的题目和错误记录；未提交草稿清空。

## 编辑器

选择 **Calculus Lab → Questions → Open differentiation question bank**，无需 Play 即可查看保存的 `Assets/_Project/Resources/Production/DifferentiationQuestions.asset`。题目数据包含 ID、装置阶段、来源页码、题干、改写提示、标准答案、错误选项反馈和完整解释。查看已存在的题库不会修改 UI。

`DifferentiationBankBuilder.Apply` 只在题库缺失时创建默认题目，不重复追加题目。保留既有场景，定向更新 Differentiation 的说明文字和完成提示，以及主菜单中旧的 11 步说明。

主要实现：`DifferentiationQuestionBank.cs`、`BridgeExtendedQuestions.cs`、`DifferentiationBankBuilder.cs`；使用现有数字输入、选择、帮助、存档和评估界面。`BridgeBankPlaytest.cs` 在实际 Canvas 流程中验证新增题、重试、禁止跳过、保存和恢复。

## 验证

证据保存在 `qa/differentiation-pdf-bank`。

- `mathematics.txt`：对新增答案执行精确有理数运算，并在每题三个正数取值处以差分独立核对导数，全部通过。
- `final-build.log`：重复应用不重复创建题目；旧版 11 步恢复、新版供能机未完成题恢复、新版转换架中途题恢复均 PASS；开发版及正式版 Windows 构建均 Success。
- `missions-final-720` 和 `missions-final-1080`：实际 Canvas 操作完成两关，各 85 张截图，均 PASS，无运行时异常或 Shader 错误。新增题覆盖正确作答、分数等价输入、错误指数诊断、错误符号诊断、防止跳过及重复计分、接受后等待安装、退出后在同题继续。
- 结果截图显示 18/18 完成、23 次尝试中 5 次错误、完成率 100%。技能总结包括全部新增记录，原有通关练习和 Integration 流程继续工作。最终原始存档由 `curriculum-result.txt` 再核对题目 ID、版本、分母与实际答题记录。
- 验证期间发现当前保存场景缺少上一轮的机械旋转绑定，使用既有追加迁移恢复该组；没有重新生成关卡。原有对象的世界姿态、碰撞和玩法引用检查通过，最终实际流程中的机械动作检查亦通过。
- 两个 UI prefab 的差异仅为 Differentiation 说明、完成提示及主菜单旧的 11 步说明，没有重新生成 UI。

交付：`Builds/Chapter4/CalculusLabEscape_Chapter4.exe` 和 `Builds/LearningLab/CalculusLabEscape.exe`。源码备份：`unity/backups/2026-10-15-differentiation-pdf-bank/CalculusLabEscape-source.zip`，排除 Library、Builds 和验证副本。
