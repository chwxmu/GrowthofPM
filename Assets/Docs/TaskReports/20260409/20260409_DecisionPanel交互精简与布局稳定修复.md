# 20260409 DecisionPanel交互精简与布局稳定修复

## 任务背景
- 本轮在 Phase8 视觉升级基线之上，继续收口答辩演示中 `DecisionPanel` 的信息密度与可读性问题。
- 用户反馈集中在三类：冗余提示过多、AI 建议显示流程不够直观、反馈区与选项区存在布局挤压风险。
- 目标是在不改动周流程逻辑与数据口径的前提下，优化决策面板展示体验，保证中文字体规范与测试稳定性。

## 修改文件
- `Assets/Scripts/UI/MenuSceneController.cs`
- `Assets/Scripts/UI/DecisionPanel.cs`
- `Assets/Tests/Editor/QuizScheduleVisibilityTests.cs`

## 核心逻辑
1. 主菜单按钮图标清理
- 移除 `NewGameButton`、`ContinueGameButton` 的图标资源传参。
- 当主题应用时若检测到遗留 `Icon` 子物体，运行时/编辑器态分别走 `Destroy/DestroyImmediate` 清理，避免旧层级残留。

2. 决策反馈信息精简
- `FeedbackText` 不再拼接属性变更文案（如“抗压力 +4”）。
- `FeedbackText` 同时去掉“你采纳了/没有采纳 AI 建议”提示，仅保留选项 `narrative`。
- 移除 `OptionsHeaderRow`（“结合团队状态与AI建议...”提示行），降低信息噪声。

3. AI 建议显示流程调整
- 点击 `AIAdviceButton` 后，隐藏 `AIAdviceRow`，并将 `AIAdviceText` 顶替到同位置显示。
- 保持 `AIAdviceText` 初始隐藏，只有在点击按钮后显示，且按钮状态同步更新。

4. 布局与高度稳定性修复
- 修复仅改 `LayoutElement` 但 `RectTransform` 高度不变的问题：文本区刷新时同步写入 `RectTransform.sizeDelta.y`。
- 统一 `DescriptionText`、`AIAdviceText`、`FeedbackText` 的高度刷新逻辑，支持最小/最大高度限制与隐藏态高度归零。
- 收紧间距和内边距，新增“有反馈时降低主容器 spacing”的策略，压缩 `FeedbackText` 与 `OptionsRoot` 间距。
- 当前收口参数：
  - `DescriptionText`：最小高度 `72`，最大高度 `96`。
  - `AIAdviceText`：最小高度 `56`，最大高度 `112`，垂直居中。
  - `FeedbackText`：固定高度 `100`（min=max=100），垂直居中。

5. 文本对齐视觉优化
- `AIAdviceText` 调整为 `TextAlignmentOptions.MidlineLeft`。
- `FeedbackText` 调整为 `TextAlignmentOptions.MidlineLeft`。

## 测试结果
- 脚本校验：`DecisionPanel.cs`、`QuizScheduleVisibilityTests.cs` 无编译错误（仅保留既有无关 warning）。
- EditMode 回归：多轮执行通过，最终全量结果为 `85 passed / 0 failed / 0 skipped`。
- 新增/更新测试覆盖点：
  - AI 建议点击后 `AIAdviceRow` 隐藏与 `AIAdviceText` 显示。
  - `DescriptionText` / `AIAdviceText` 高度上限约束。
  - `FeedbackText` 固定高度与居中对齐。
  - `OptionsHeaderRow` 已移除。

## 注意事项
- `FeedbackText` 固定为 100 高度后，超长叙述会被裁剪；当前按用户要求仅保证现有配置反馈可完整展示。
- 若后续出现更长 narrative，可改为固定高 + `ScrollRect` 方案以兼顾不挤压选项区与完整可读。
- 本次未变更周流程状态机、存档流程与实验埋点口径，仅调整 UI 展示层与相关测试断言。
