# 任务报告：统一TMP字体引用并清理Fallback脏文件

## 任务背景
在检查仓库与本地工作区同步状态时，发现 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` 处于未提交状态。进一步排查确认，项目内仍有少量界面元素和 TMP 全局默认字体指向 `LiberationSans SDF`，容易在编辑器显示特定字符时触发 fallback 字体自动补字并回写资产。本次任务的目标是：将残留的默认 TMP 字体引用统一切换为 `Assets/Fonts/SIMSUN SDF.asset`，并清理掉 fallback 资产的脏改动。

## 修改文件
- `Assets/Scenes/HomeScene.unity`
- `Assets/Prefabs/Main/StatsSlot.prefab`
- `Assets/TextMesh Pro/Resources/TMP Settings.asset`
- `Assets/Docs/TaskReports/20260326/20260326_统一TMP字体引用并清理Fallback脏文件.md`

## 核心逻辑
### 1) 定位默认 TMP 字体残留引用
- 通过 guid 检索确认业务侧显式引用 `LiberationSans SDF` 的位置只剩 3 处：`HomeScene` 的关闭按钮文本、`StatsSlot.prefab` 的数值文本，以及 `TMP Settings.asset` 的全局默认字体。
- 对 fallback 资产实际内容比对后，确认脏改动来自 TMP 自动向 fallback atlas 补入字符 `→`，而不是业务逻辑或脚本改动。

### 2) 统一切换为宋体资源
- 将 `Assets/Scenes/HomeScene.unity` 中 `Close_Btn` 下的 `Text (TMP)` 组件 `m_fontAsset` / `m_sharedMaterial` 改为 `SIMSUN SDF`。
- 将 `Assets/Prefabs/Main/StatsSlot.prefab` 中 `Value` 文本组件 `m_fontAsset` / `m_sharedMaterial` 改为 `SIMSUN SDF`。
- 将 `Assets/TextMesh Pro/Resources/TMP Settings.asset` 中 `m_defaultFontAsset` 改为 `SIMSUN SDF`，降低后续新增 TMP 文本走默认西文字体链路的概率。

### 3) 清理 fallback 脏文件
- 将 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` 恢复到仓库版本。
- 执行 Unity 资源刷新，确认 fallback 资产没有在本次修复后立即再次变脏。

## 测试结果
### 用例1：默认字体引用残留检查
- 步骤：按旧字体 guid `8f586378b4e144a9851e7b34d9b748ee` 检索 `.unity`、`.prefab`、`.asset` 资源。
- 结果：业务资源中不再存在显式引用。
- 结论：通过。

### 用例2：Unity 刷新与控制台检查
- 步骤：执行 Unity 资源刷新，然后读取 Console 的 `error` / `warning`。
- 结果：0 条。
- 结论：通过。

### 用例3：工作区清理结果检查
- 步骤：恢复 fallback 资产后执行 `git status --short --branch`。
- 结果：`LiberationSans SDF - Fallback.asset` 不再出现在未提交列表；剩余未提交项仅为本次有意保留的 3 处字体引用修复。
- 结论：通过。

## 注意事项
- 当前 TMP 全局默认字体已切换为 `SIMSUN SDF`，后续新增 TMP 文本更不容易再次触发默认西文字体 fallback 污染。
- 若后续脚本中存在运行时动态创建的 TMP 文本，仍建议显式指定 `Assets/Fonts/SIMSUN SDF.asset`，以完全符合 `AGENTS.md` 的字体规范。
- 本次未进行完整 UI 逐场景人工点击回归；如后续要提交或演示，建议在 Unity 编辑器中对 `HomeScene` 与引用 `StatsSlot.prefab` 的界面做一次视觉确认。
