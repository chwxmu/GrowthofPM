# Growth of PM (项目经理成长记)

Unity 2D simulation/nurturing game where the player takes on the role of a junior project manager (朱诀) and grows through three progressively harder software projects. This is an undergraduate thesis project.

## Tech Stack

- **Engine**: Unity 2D with URP (Universal Render Pipeline)
- **Language**: C#
- **UI**: UGUI (Unity's built-in UI system)
- **Animation**: DOTween (already imported)
- **Text**: TextMesh Pro (already imported)
- **Target**: PC only (Windows), mouse + keyboard
- **Data format**: JSON for all game content (dialogues, events, tasks, quizzes)
- **Serialization**: Use Unity's `JsonUtility` for simple types, or `Newtonsoft.Json` if nested/complex structures are needed

## Architecture

Singleton-based Manager pattern:

```
GameManager        — Global game state, state machine (Menu / Playing / Paused)
├── StoryManager   — Weekly story progression, dialogue and decision event flow
├── UIManager      — Panel registration, show/hide, transitions
├── DataManager    — JSON data loading, save/load game state
└── AIAdvisor      — AI advisor NPC suggestions and trust tracking
```

All managers inherit from a generic `Singleton<T>` MonoBehaviour base class.

## Project Structure

```
Assets/
├── Scenes/
│   ├── MenuScene.unity          # Main menu
│   └── GameScene.unity          # Core gameplay (all panels live here)
├── Scripts/
│   ├── Core/                    # Singleton base, GameManager, enums, constants
│   ├── Data/                    # Data models (PlayerData, WeekEvent, DailyTask, etc.)
│   ├── Managers/                # StoryManager, UIManager, DataManager, AIAdvisor
│   ├── UI/                      # UI panel controllers (one script per panel)
│   └── MiniGames/               # CPM mini-game, Risk Dashboard mini-game
├── Resources/
│   └── Data/
│       ├── daily_tasks.json     # 12 daily tasks with energy cost and stat effects
│       ├── quiz_questions.json  # Knowledge quiz question bank
│       ├── endings.json         # Ending conditions and texts for all 3 projects
│       ├── project1/
│       │   └── story.json       # 9 weeks of dialogues, decisions, stat changes
│       ├── project2/
│       │   └── story.json       # 12 weeks + AI advice + hidden risk values
│       └── project3/
│           └── story.json       # 12 weeks + daily intro scenes + AI quality labels
├── Prefabs/                     # UI prefabs
├── Fonts/                       # SIMSUN SDF font
└── Docs/                        # PRD and Codex task documents
```

## Coding Conventions

- Use **PascalCase** for classes, methods, properties, and public fields
- Use **camelCase** for private fields and local variables
- Prefix private fields with underscore: `_playerData`
- One class per file, filename matches class name
- Use `[SerializeField]` for inspector-exposed private fields
- Use `#region` blocks to organize large classes
- All user-facing text is in **Chinese (Simplified)**, stored in JSON data files
- Never hardcode story text, decision options, or stat values in C# code — always load from JSON
- Use `Resources.Load<TextAsset>("Data/...")` to load JSON at runtime
- Use DOTween for UI animations (panel slide-in, stat bar changes, text fade)
- Use TextMesh Pro for all text rendering
- **UI中文字体规范**：所有UI组件（TextMesh Pro/UGUI）中涉及中文显示的部分，必须将字体设置为指定的宋体资源：`E:\Unity\unityProjects\GrowthofPM\Assets\Fonts\SIMSUN SDF.asset`；若为动态生成的TextMesh Pro文本，需在代码中显式指定该字体资源，禁止使用默认字体。
- **静态UI修改规范**：静态 UI 应优先在 Hierarchy / Inspector 中修改。对于固定存在的界面元素（如按钮位置、文本内容、图片引用、层级关系、默认显隐、RectTransform 参数等），优先直接在 Unity 场景层级或 Inspector 中调整；不要为了调整静态 UI 而编写额外脚本去动态修改其结构或布局。

## Core Game Loop

Each week follows this sequence:
1. **Prologue** → Show dialogue scenes (过场剧情)
2. **Decision Event** → Present choices with AI advice, apply stat changes (决策事件)
3. **Schedule** → Player allocates energy (300/week) to daily tasks (日程安排)
4. **Optional Quiz** → Answer PM knowledge questions for bonus energy (知识答题)
5. **Settlement** → Apply all stat changes, advance to next week (结算)
6. **Ending Check** → At project end, evaluate stats for ending determination

## Data Model Overview

Key data structures (see `Assets/Resources/Data/` for JSON schemas):

- **PlayerData**: techPower, commPower, managePower, stressPower, energy, currentProject, currentWeek, hiddenRisk, aiTrustRecords
- **WeekEvent**: weekNumber, phase, prologueDialogues[], decisionEvent{}, fixedStatChanges{}
- **DecisionEvent**: description, aiAdvice, aiQuality, options[]{text, effects{}, narrative, isAiRecommended}
- **DailyTask**: name, energyCost, effects{}
- **QuizQuestion**: question, options[], correctIndex, energyReward

## Important Notes

- Three projects: P1 (9 weeks), P2 (12 weeks), P3 (12 weeks). Stats carry over between projects.
- Project 2 introduces a hidden risk value that accumulates based on decisions.
- Project 2 has two mini-games: CPM critical path linking and risk dashboard correction.
- Project 3 has daily intro scenes (小故事) before each week's main event.
- AI advisor personalities differ per project: P1 (TBD), P2 "Alpha" (aggressive), P3 "Chaos" (unpredictable with good/neutral/bad quality labels).
- Record whether the player follows AI advice for each decision — this data is shown in the final stats screen.

## 核心开发执行准则 (Core Implementation Guidelines)

### A. 编程规范与文件编码 (Coding & Encoding Standards)
- **强制编码**：所有脚本和 JSON 文件必须使用 **UTF-8 with BOM** 编码，以防止 Unity 中文乱码。
- **命名与注释**：变量/方法使用英文；每个管理器类需添加类注释，说明核心功能；每个公共方法需添加注释，说明参数、返回值、作用。
- **类型安全**：严禁使用魔术字符串。所有状态（项目阶段、AI 质量、属性类型）必须引用 `GameEnums.cs`；所有路径必须引用 `GameConstants.cs`。
- **数据契约**：修改 C# 数据模型（如 `PlayerData`）时，必须同步更新对应的 JSON 模板。JSON 字段用 `camelCase`，C# 字段用 `PascalCase`。

### B. 架构解耦与 UI 驱动 (Architecture & UI Decoupling)
- **逻辑隔离 (MVVM 思想)**：UI 脚本仅负责显示，严禁在 UI 类中处理逻辑计算。所有数值变动必须通过 `StatManager` 或 `StoryManager` 处理。
- **事件驱动**：跨模块通信（如：属性变动触发 TopStatusBar 刷新）优先使用 `System.Action` 或 C# 事件，严禁模块间强耦合。
- **资源访问**：所有配置数据必须存放在 `Assets/Resources/Data/` 目录下。加载资源时需进行 `null` 检查。
- **UI 管理**：同一时间只能有一个核心面板处于 `Active` 状态。打开新面板前必须通过 `UIManager` 关闭当前面板。

### C. 周循环状态机与持久化 (State Machine & Persistence)
- **周循环顺序**：`StoryManager` 必须严格遵守流程：`周初剧情 -> 决策事件 -> 精力安排 -> 数值结算 -> 自动保存`。严禁跳步。
- **结算即保存**：在每周结算（Settlement）完成时，必须立即调用 `DataManager.SaveProgress()`。实验数据（AI 采纳情况）必须实时写入 `aiTrustRecords`。
- **单例安全性**：所有单例必须在 `Awake` 中实现自毁逻辑，确保场景切换时不产生重复实例。

### D. 学术埋点与调试 (Research Logging & Debugging)
- **实验数据追踪**：
    - **信任度**：必须记录玩家是否查看 AI 建议（`hasViewed`）以及最终决策是否与建议一致（`isFollowed`）。
    - **决策耗时**：记录从显示决策面板到玩家点击选项的 `DecisionLatency`。
- **防御性编程**：关键逻辑（如项目切换、大事件加载）必须包含 `try-catch` 或异常 Log。
- **日志标准**：使用 `Debug.Log($"[模块名] : 描述")` 格式。报错必须使用 `Debug.LogError` 并附带关键参数（如当前周数、项目编号）。

## 任务报告存储规范
### 1. 根文件夹命名
`Assets/Docs/TaskReports`（任务报告存储，与其他文档隔离）

### 2. 路径结构
`Assets/Docs/TaskReports/[完成任务日期]/[完成任务日期]_[文档内容简要概括].md`
- 日期格式：`YYYYMMDD`（如20240520，避免分隔符导致的路径问题）
- 文档内容简要概括：使用中文、简短清晰（如「实现P2隐藏风险值累加逻辑」「修复CPM小游戏点击无响应BUG」）

### 3. 报告内容要求
- 任务背景：简要说明本次实现/修复的功能背景
- 修改文件：列出本次改动的所有文件路径（如`Assets/Scripts/Managers/AIAdvisor.cs`）
- 核心逻辑：关键代码/功能的实现思路
- 测试结果：测试用例、测试步骤、是否通过测试
- 注意事项：遗留问题、后续优化点（如有）

## GitHub 代码上传规范与操作指南 (GitHub Workflow)

### 1. 远程仓库配置 (Repository Info)

* **SSH URL**: `git@github.com:chwxmu/GrowthofPM.git`
* **默认分支**: `main` (或当前工作分支)

### 2. 上传触发前提 (Pre-upload Checklist)

AI Agent 只有在满足以下全部条件后，方可启动上传流程：

1. **功能完整性**：当前分配的任务（如 P2 事件系统）功能已全部编写完成。
2. **测试通过**：代码在 Unity 编辑器中编译无错，且核心业务流程（如数值计算、事件跳转）经本地测试无 BUG。
3. **任务报告已生成**：已按照 `## 任务报告存储规范` 生成对应的本地 `.md` 报告。
4. **用户授权**：必须向用户发送明确的授权请求并获得回复（如“同意”、“上传”）。

### 3. 标准操作步骤 (Step-by-Step Commands)

在获得用户授权后，Agent 应按以下顺序执行指令序列：

* **第一步：状态检查与远程同步**
```bash
git status
git pull origin main --rebase

```
* **第二步：暂存与本地提交**
提交信息格式要求：`[模块名] 简短功能描述`
```bash
git add .
git commit -m "[提交描述]"

```
* **第三步：正式推送至远程仓库**
```bash
git push origin main

```
### 4. 强制约束 (Mandatory Constraints)
* **严禁强推**：禁止使用 `git push --force`。如遇远程冲突，必须先执行 Pull 并在无法自动合并时向用户汇报。
* **隐私保护**：严禁上传任何包含个人 API Key、密码或敏感本地配置的文件。
* **结果反馈**：上传完成后，必须在对话中确认：“代码已成功推送至 GitHub 仓库。”

## 额外执行约束
- 读取和编辑文本文件时，默认使用 UTF-8；若按 UTF-8 读取失败或出现明显乱码，必须回退尝试 GBK/GB18030 等常见编码，确认内容正确后再继续操作
- 如果发现文件中存在文字乱码，必须在本次任务中修复为正常文字；若无法可靠判断原文，先向我确认后再修改
