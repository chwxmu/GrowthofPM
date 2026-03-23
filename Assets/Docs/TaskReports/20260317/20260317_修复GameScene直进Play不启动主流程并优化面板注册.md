# 修复 GameScene 直进 Play 不启动主流程并优化面板注册

## 任务背景
在 Phase 2 运行态联调中，已确认面板对象与脚本挂载正常，但直接在 `GameScene` 进入 Play 时，主流程未自动启动；同时 Console 出现大量 `The referenced script (Unknown) on this Behaviour is missing!` 噪音错误，影响验证效率。

## 修改文件
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Managers/UIManager.cs`

## 核心逻辑
1. `GameManager` 增加 direct-play 自举逻辑：
- 新增 `Start()`，调用 `TryBootstrapGameSceneWhenDirectPlay()`。
- 当当前场景为 `GameScene` 且状态不是 `Playing` 时，自动切换到 `Playing` 并执行 `InitializeGameSceneRuntime()`。
- 将原 `OnSceneLoaded(GameScene)` 的初始化逻辑抽取为 `InitializeGameSceneRuntime()`，统一入口，避免分叉行为。

2. `UIManager` 面板注册改为场景内扫描：
- `RebuildPanelRegistry()` 从 `Resources.FindObjectsOfTypeAll<UIPanelMarker>()` 调整为 `FindObjectsOfType<UIPanelMarker>(true)`。
- 仅注册当前已加载场景中的面板，避免扫描到非场景资源导致大量 Missing Script 噪音报错。

3. 编码规范处理：
- 将本次改动的 `GameManager.cs`、`UIManager.cs` 明确保存为 UTF-8 with BOM。

## 测试结果
### 用例 1：GameScene 直进 Play 自动起流程
- 步骤：在 `GameScene` 直接进入 Play。
- 结果：`DialoguePanel` 自动激活，TopStatusBar 显示项目/周次/阶段，符合剧情起始阶段预期。
- 结论：通过。

### 用例 2：答题入口阶段限制
- 步骤：剧情阶段读取 `QuizButton` 组件状态。
- 结果：`Button.interactable = false`，符合 `CanOpenQuiz()` 的阶段约束。
- 结论：通过。

### 用例 3：Console 噪音回归
- 步骤：清空 Console 后重新编译并进入 Play。
- 结果：不再出现批量 `Unknown script missing`，仅保留可忽略的 MCP 插件 warning/调试日志。
- 结论：通过。

## 注意事项
- 当前通过 MCP 可稳定验证“自动进入剧情起点”和“答题入口在非日程阶段不可用”。
- 决策 → 日程 → 答题弹窗 → 结算 → 结局/过渡 的完整点击链路仍建议在 Unity 编辑器内人工点击回归一次（MCP 对 UGUI 按钮点击联调能力有限）。