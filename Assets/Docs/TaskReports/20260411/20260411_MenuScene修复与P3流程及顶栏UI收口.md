# 20260411 MenuScene 修复与 P3 流程及顶栏 UI 收口

## 任务背景
- 本轮工作承接前一轮答辩封版后的 UI 收口与流程排查，重点处理用户在实际试玩中发现的场景报错、结局/过渡面板冗余装饰、项目 3 前置剧情循环，以及 `TopStatusBar` 的信息表达问题。
- 调整目标是在不改动周流程主状态机、研究埋点、存档口径和项目切换规则的前提下，优先修复阻塞性问题，再继续完成顶部状态栏的视觉简化与静态布局收口。
- 本轮同时补充对应 EditMode 回归测试，确保 UI 收口与剧情流程修复不会引入新的行为回归。

## 修改文件
- `Assets/Scenes/MenuScene.unity`
- `Assets/Scenes/GameScene.unity`
- `Assets/Scripts/Managers/StoryManager.cs`
- `Assets/Scripts/UI/EndingPanel.cs`
- `Assets/Scripts/UI/TopStatusBar.cs`
- `Assets/Scripts/UI/TransitionPanel.cs`
- `Assets/Tests/Editor/Phase2Flow7To12Tests.cs`
- `Assets/Tests/Editor/Project3Phase5Tests.cs`
- `Assets/Tests/Editor/QuizScheduleVisibilityTests.cs`

## 核心逻辑
1. `MenuScene` 层级异常修复
- 修复 `MenuScene` 中 `BackgroundOverlay` 被错误登记到 `EventSystem` 子节点列表的问题。
- 统一 `BackgroundOverlay` 的 `m_Father` 与父节点 `m_Children` 数据，消除打开场景时的 `Transform child has another parent` 报错。

2. `EndingPanel` 与 `TransitionPanel` 冗余装饰精简
- `EndingPanel` 中移除 `ButtonRow` 内按钮图标与无信息承载的 `Watermark`，并将按钮文本恢复为居中显示。
- `TransitionPanel` 中移除 `HeaderIcon`、`StartButton` 图标和 `Watermark`，同时保持标题与按钮层级稳定。
- 为兼容旧场景/旧运行时对象，脚本中增加了残留节点清理逻辑，确保即使对象已存在也会被正确移除。

3. 项目 3 周前剧情循环修复
- 修复 `StoryManager.OnPrologueComplete()` 的错误回调链：此前序章结束后会再次回到 `OnDailyIntroComplete()`，导致项目 3 中 `dailyIntro -> prologue -> prologue` 循环。
- 调整为序章结束后直接归零 `_decisionStepIndex` 并进入 `ShowNextDecisionOrSchedule()`，确保项目 3 每周都能正常从前置剧情进入决策。
- 结合 `project3/story.json` 结构核查后确认，原问题覆盖 Week1-12，而不只是用户先试玩到的 Week1-4。

4. `TopStatusBar` 图标化与滑条移除
- 为技术力、沟通力、管理力、抗压力、精力值接入对应图标资源，并关闭原有五个 `Slider`，顶栏改为“图标 + 标签 + 数值”表达。
- 结合项目“静态 UI 优先”约束，将四维与精力值文本布局主要收口到 `GameScene` 静态场景中，不再由脚本持续强制重排。
- 在脚本侧仅保留滑条隐藏和图标生成兜底逻辑，减少运行时布局干预。

5. 顶栏后续对齐与遮挡收口
- 针对四维属性出现的越界、图标重叠问题，使用静态场景重新调整标签与数值位置，给图标留出稳定间距。
- 针对精力值与 `日程安排` 按钮重叠问题，将精力图标、标签、数值整体上移到按钮区上方，避免遮挡并保持信息分区清晰。
- 同步微调运行时创建的 `EnergyIcon` 与四维图标纵向中心线，使图标与文字在视觉上保持同一基线。

6. 回归测试同步更新
- `Phase2Flow7To12Tests` 新增结局面板/过渡面板去图标去水印断言。
- `Project3Phase5Tests` 新增项目 3 Week1-12 “日常引入 -> 序章 -> 决策”链路测试，覆盖本轮剧情循环修复。
- `QuizScheduleVisibilityTests` 新增 `TopStatusBar` 图标生成、滑条隐藏、数值刷新相关测试。

## 测试结果
- Unity 控制台检查：本轮刷新与验证后未出现新的项目级报错；测试阶段仍可见既有 `UIManager` 面板未注册 warning，属于测试桩环境特征，不是本轮改动引入。
- EditMode 全量回归通过：`96 passed / 0 failed / 0 skipped`。
- 重点验证项：
  - `MenuScene` 可正常打开，原始层级报错已消失。
  - `EndingPanel` / `TransitionPanel` 的图标与水印移除结果已被测试覆盖。
  - 项目 3 Week1-12 前置剧情都能在序章后正常进入决策。
  - `TopStatusBar` 中五个滑条已隐藏，图标与数值显示逻辑可正常刷新。

## 注意事项
- `TopStatusBar` 本轮采用“静态场景为主、脚本兜底”的方式收口；若后续继续微调位置，应优先在 `Assets/Scenes/GameScene.unity` 中调整，而不是重新把文字布局逻辑迁回脚本。
- 运行 Unity EditMode 测试前需确保编辑器已退出 Play Mode；本轮排查中曾出现测试任务无法初始化的情况，根因是测试启动时编辑器状态不合适，退出 Play Mode 后即可恢复。
- 工作区中与本轮无关的脏文件（如 `.gitignore`、历史 `.meta`、`Assets/Screenshots/` 下自动截图）未纳入本次处理内容，不应混入后续提交。
