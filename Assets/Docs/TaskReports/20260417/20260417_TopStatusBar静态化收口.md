# TopStatusBar静态化收口

## 任务背景

- 当前 `TopStatusBar` 虽然运行时布局已经调到目标效果，但编辑模式下的 Hierarchy 仍缺少 `ScheduleButton`、按钮图标、属性图标和装饰线，顶栏结构依赖脚本运行时临时补建。
- 本次任务目标是在保留既有运行时参数和排版结果的前提下，将 `TopStatusBar` 收口为静态场景结构，避免后续继续出现“编辑模式一套、运行时另一套”的维护成本。

## 修改文件

- `Assets/Scenes/GameScene.unity`
- `Assets/Scripts/UI/TopStatusBar.cs`
- `Assets/Tests/Editor/QuizScheduleVisibilityTests.cs`
- `Assets/Docs/TaskReports/20260417/20260417_TopStatusBar静态化收口.md`

## 核心逻辑

### 1. 顶栏静态层级补齐

- 在 `GameScene` 的 `GameCanvas/TopStatusBar` 下静态补齐 `ScheduleButton`、`QuizButton/Icon`、`ScheduleButton/Icon`、`TechIcon`、`CommIcon`、`ManageIcon`、`StressIcon`、`EnergyIcon`、`AccentLine`。
- 保留上一次已调好的顶栏参数，尤其是四维数值文本左移后的排版，以及精力区文字与 `EnergyIcon` 的位置微调结果。

### 2. 运行时逻辑收口为“配置静态节点”

- `TopStatusBar` 不再在 `Awake` 阶段动态创建 `ScheduleButton`、图标和装饰线，而是只绑定场景中已有节点并应用统一参数。
- 继续保留现有布局参数逻辑，例如：
  - `ScheduleButton` 按 `QuizButton` 左侧 `12` 像素间距对齐
  - 按钮图标 `18x18`，文字 `margin = (28, 0, 6, 0)`
  - 属性图标 `24x24`
  - `EnergyIcon` 位置为 `(-248, 89)`

### 3. 旧滑条壳层清理

- 删除 `TopStatusBar` 下已废弃的 `TechSlider`、`CommSlider`、`ManageSlider`、`StressSlider`、`EnergySlider` 节点，避免顶栏长期处于“旧方案残留 + 新方案补建”并存状态。

### 4. 自动化测试同步

- 更新 `QuizScheduleVisibilityTests`，将验证口径从“Awake 时动态创建图标和隐藏 slider”改为“Awake 时正确配置已存在的静态装饰节点”。
- 测试基线同步改成静态顶栏结构，以匹配当前场景实现。

## 测试结果

### 在线 Unity Session 验证

- 编辑模式下检查 `GameCanvas/TopStatusBar`，已可直接看到完整顶栏结构。
- 进入 Play 模式后再次检查 Hierarchy，未再新增 `ScheduleButton`、属性图标、精力图标或装饰线，说明静态层级与运行时层级一致。

### EditMode 自动化测试

- 已运行 Unity EditMode 全量测试。
- 最新结果：`96 passed / 0 failed / 0 skipped`

## 注意事项

- 本次改动刻意保留了你在上一轮已调好的顶栏排版参数，没有重新发明新坐标或新尺寸。
- 当前 `TopStatusBar` 仍会在运行时对静态节点做参数校正，但不再承担动态造节点职责；后续若继续做 Inspector 微调，可以直接在场景中调整静态节点。
- 控制台剩余的 `MCP-FOR-UNITY [WebSocket] Unexpected receive error` 为插件自身 warning，不属于项目业务代码回归。
