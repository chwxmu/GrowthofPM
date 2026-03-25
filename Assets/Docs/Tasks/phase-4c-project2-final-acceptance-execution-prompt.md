# P2：Phase 4C 最终验收与答辩演示准备执行前提示词

你现在接手的是 Unity 项目 `GrowthofPM` 的下一阶段开发任务。开始前请先阅读并严格遵守：

- `E:\Unity\unityProjects\GrowthofPM\AGENTS.md`
- `Assets/Docs/TaskReports/20260320/20260320_项目现状架构与实现分析报告.md`
- `Assets/Docs/TaskReports/20260320/20260320_下一阶段开发优先级与重构路线图.md`
- `Assets/Docs/Tasks/phase-4-project2-content.md`
- `Assets/Docs/Tasks/phase-4-project2-execution-prompt.md`
- `Assets/Docs/Tasks/phase-4b-project2-hardening-execution-prompt.md`
- `Assets/Docs/Tasks/phase-4b-project2-manual-regression-checklist.md`
- `Assets/Docs/TaskReports/20260324/20260324_P2核心实现阶段性收口.md`
- `Assets/Docs/TaskReports/20260325/20260325_P2收口与演示加固.md`
- `Assets/Docs/TaskReports/20260325/20260325_P2最终验收与P3准入计划.md`
- `Assets/Docs/PRD.md` 中与 Project 2、AI 建议、隐藏风险、小游戏、结局相关的章节

## 当前基线说明

- `P0`、`P1`、`P2 Phase 4`、`P2 Phase 4B` 已完成并已收口到当前基线。
- 当前 `main` 分支已经具备：
  - P2 AI 建议、隐藏风险、Week 3 CPM、Week 5 条件事件、Week 9 风险仪表盘、Week 11 风险对白、Week 12 结局闭环
  - `DecisionPanel`、`CPMGamePanel`、`RiskDashboardPanel` 的静态 UI 壳结构
  - `StoryManager` 关键 checkpoint 与继续游戏恢复加固
  - EditMode 自动化基线 `60 passed / 0 failed / 0 skipped`
- 当前阶段剩余问题的重点，不是“P2 功能没接上”，而是：
  - 还缺少一轮严格的 Unity 编辑器内人工点击回归
  - 还没有把 `excellent / pass / fail` 三条演示路径固化成可复现路线
  - 还没有形成是否可以进入 `P3` 的最终准入结论

## 本次任务目标

本次只做 `P2：Phase 4C 最终验收与答辩演示准备`，目标是把 P2 从“代码和结构上已收口”推进到“人工验证通过、演示路径稳定、可以判断是否进入 P3”的状态。

**本次仍然禁止提前开发 `P3` 正式内容。**

你要优先完成以下四包任务：

1. 执行 P2 关键周人工点击回归
2. 固化并验证 P2 三条演示路径
3. 仅修复答辩展示级缺陷
4. 输出 P2 最终验收与 P3 准入结论

## 严格执行约束

- 不开发 `P3` 正式内容，不改 `phase-5-project3-content.md` 对应范围。
- 不做 Phase 6 泛化任务：音频、设置、全局 UI 大改、全项目 polish 等都不在本阶段范围内。
- 不重写 `StoryManager` 主循环，不发明新流程框架。
- 静态 UI 若仍需微调，继续优先在 Unity Hierarchy / Inspector 中处理。
- 若风险曲线或结局体验需要修正，优先调 `JSON` 数据，不优先改大段 C# 逻辑。
- 不破坏现有 `60` 个自动化测试；任何修复都必须确保旧测试继续通过。
- 不把人工回归中发现的边角小问题扩展成无限制的 polish 任务，只修会影响答辩展示的关键问题。

## 推荐实施顺序

### 第一包：人工回归执行

严格按照 `Assets/Docs/Tasks/phase-4b-project2-manual-regression-checklist.md` 执行：

1. 先检查三块 P2 关键面板的静态结构、字体与布局表现
2. 再回归 Week 3 / 5 / 9 / 11 / 12 主流程
3. 再回归保存 / 继续游戏恢复边界
4. 对每个失败项记录现象、复现步骤、影响范围

### 第二包：三条演示路径验证

设计并验证三条 P2 演示路线：

1. `excellent`：谨慎路线 + 小游戏发挥稳定，命中低风险优秀结局
2. `pass`：中风险但未爆表，命中“修补匠”
3. `fail`：错误决策 + 小游戏失误，稳定推高风险并命中“背锅侠”

每条路线至少记录：

- 关键周选择
- 小游戏目标结果
- 预期 `hiddenRisk` 档位
- 预期 Week 11 文本命中
- 预期 Week 12 结局

### 第三包：最小必要修复

只修以下问题：

- 流程卡死、面板悬挂、无法继续
- 恢复后上下文错误、重复结算、结局错误
- 小游戏重进残留状态
- 关键按钮无响应、文本严重遮挡、布局明显影响展示
- JSON 可低风险调优即可改善的演示路径问题

### 第四包：最终验收与准入判断

完成后必须明确回答：

1. P2 是否已经达到“可稳定演示”
2. 三条演示路径是否都可复现
3. 是否建议进入 `P3`
4. 若仍不建议进入 `P3`，阻塞项是什么

## 测试与验收要求

本阶段完成后，至少要给出以下验证结果：

1. 现有全部 EditMode 测试继续通过
2. Unity MCP 编译通过
3. Unity 控制台无新的项目级 `error`
4. 完成一轮 `P2` 关键周人工回归并记录结果
5. 完成 `excellent / pass / fail` 三条 P2 演示路径中的至少两条实测，建议三条全测
6. 若本次新增了修复，补最小必要测试

## 交付物要求

完成后请输出：

1. 本次人工回归结果
2. 三条演示路径记录
3. 本次发现并修复的问题
4. 自动化测试结果
5. Unity MCP 编译 / 控制台结果
6. 仍然存在的剩余风险
7. 是否建议进入 `P3`，以及阻塞项说明

并按规范生成任务报告，路径放到：

- `Assets/Docs/TaskReports/[当天日期]/[当天日期]_P2最终验收与P3准入结论.md`

## 最后提醒

- 本阶段关键词是：`P2最终验收`、`人工回归`、`演示路径固化`、`P3准入判断`。
- 不要把任务扩展成 `P3` 开发。
- 不要回头大重构 Phase 4 / 4B 已完成内容。
- 你的目标不是继续加功能，而是确认 P2 是否已经真正“可演示、可答辩、可交棒到 P3”。
