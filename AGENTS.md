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
