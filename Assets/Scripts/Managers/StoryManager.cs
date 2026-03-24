using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : Singleton<StoryManager>
{
    private const string DialoguePanelName = "DialoguePanel";
    private const string DecisionPanelName = "DecisionPanel";
    private const string CpmGamePanelName = "CPMGamePanel";
    private const string RiskDashboardPanelName = "RiskDashboardPanel";
    private const string SchedulePanelName = "SchedulePanel";
    private const string QuizPanelName = "QuizPanel";
    private const string EndingPanelName = "EndingPanel";
    private const string TransitionPanelName = "TransitionPanel";
    private const string CpmCorrectFlag = GameConstants.EVENT_FLAG_CPM_CORRECT;
    private const KeyCode SkipMainStoryKey = KeyCode.P;

    private WeekEventData _currentWeekEvent;
    private int _decisionStepIndex;
    private bool _isHandlingGameScene;
    private bool _quizOpenRequestedFromSchedule;
    private bool _hasShownRiskBasedDialogue;
    private StoryFlowStage _currentFlowStage = StoryFlowStage.None;

    public event Action<WeekEventData> WeekStarted;
    public event Action<StoryFlowStage> FlowStageChanged;
    public event Action<EndingResultData> ProjectEnded;

    public WeekEventData CurrentWeekEvent => _currentWeekEvent;
    public StoryFlowStage CurrentFlowStage => _currentFlowStage;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(SkipMainStoryKey))
        {
            return;
        }

        TrySkipWeekMainStoryToDecision();
    }

    public void HandleGameSceneLoaded()
    {
        if (_isHandlingGameScene)
        {
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        _isHandlingGameScene = true;
        TryAutoBindPanels();
        if (!TryRestoreSavedFlow())
        {
            StartWeek();
        }
        _isHandlingGameScene = false;
    }

    public void StartProject(int projectNumber)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        ResetRuntimeState();
        GameManager.Instance.StartProject(projectNumber);
        StartWeek();
    }

    public void StartWeek()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        ResetWeekState();
        if (!PrepareCurrentWeekContext())
        {
            return;
        }

        if (HasDialogues(_currentWeekEvent.prologueDialogues))
        {
            SetFlowStage(StoryFlowStage.Prologue);
            ShowDialogue(_currentWeekEvent.prologueDialogues, OnPrologueComplete);
            return;
        }

        OnPrologueComplete();
    }

    public void OnPrologueComplete()
    {
        if (_currentWeekEvent == null)
        {
            return;
        }

        if (HasDialogues(_currentWeekEvent.dailyIntroDialogues))
        {
            SetFlowStage(StoryFlowStage.DailyIntro);
            ShowDialogue(_currentWeekEvent.dailyIntroDialogues, OnDailyIntroComplete);
            return;
        }

        OnDailyIntroComplete();
    }

    public void OnDailyIntroComplete()
    {
        _decisionStepIndex = 0;
        ShowNextDecisionOrSchedule();
    }

    public void TrySkipWeekMainStoryToDecision()
    {
        if (!CanSkipWeekMainStory())
        {
            return;
        }

        Debug.Log("[StoryManager] : 玩家按下P，跳过本周主剧情并进入决策阶段。");

        DialoguePanel dialoguePanel = FindObjectOfType<DialoguePanel>(true);
        if (dialoguePanel != null)
        {
            dialoguePanel.ForceCloseWithoutCallback();
        }

        _decisionStepIndex = 0;
        ShowNextDecisionOrSchedule();
    }

    public void OnDecisionComplete()
    {
        DecisionEventData completedDecision = GetDecisionByIndex(_decisionStepIndex);
        _decisionStepIndex += 1;

        ConditionalEventData conditionalEvent = _currentWeekEvent != null ? _currentWeekEvent.conditionalEvent : null;
        if (completedDecision != null && _decisionStepIndex == 1 && ShouldRunConditionalEvent(conditionalEvent))
        {
            ApplyConditionalEvent(conditionalEvent);
            return;
        }

        if (_decisionStepIndex >= GetDecisionCount())
        {
            RunRemainingWeekContentOrSchedule();
            return;
        }

        ShowNextDecisionOrSchedule();
    }

    public void OnScheduleComplete(List<DailyTaskData> selectedTasks)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        SetFlowStage(StoryFlowStage.Settlement);

        TopStatusBar topStatusBar = FindObjectOfType<TopStatusBar>(true);
        if (topStatusBar != null)
        {
            topStatusBar.SetQuizEntryInteractable(false);
            topStatusBar.SetScheduleEntryInteractable(false);
        }

        List<DailyTaskData> taskList = selectedTasks ?? new List<DailyTaskData>();
        StatEffects totalEffects = SumTaskEffects(taskList);
        int spentEnergy = SumTaskEnergy(taskList);
        int availableEnergy = GameManager.Instance.CurrentPlayerData != null
            ? Mathf.Max(0, GameManager.Instance.CurrentPlayerData.energy)
            : GameConstants.BASE_ENERGY_PER_WEEK;
        bool isFinalWeek = IsCurrentWeekFinalWeek();

        if (isFinalWeek)
        {
            PlayerData playerData = GameManager.Instance.CurrentPlayerData;
            Debug.Log($"[StoryManager] : Final week schedule complete. project={playerData.currentProject} week={playerData.currentWeek}/{GameManager.Instance.GetCurrentProjectTotalWeeks()} selectedTasks={taskList.Count} spentEnergy={spentEnergy} remainingEnergy={Mathf.Max(0, availableEnergy - spentEnergy)}");
        }

        GameManager.Instance.SetEnergy(Mathf.Max(0, availableEnergy - spentEnergy));
        GameManager.Instance.ApplyStatChanges(totalEffects);
        ApplyWeekFixedChanges();
        ApplyWeekRiskChanges();
        GameManager.Instance.ClearFlowCheckpoint();
        GameManager.Instance.SaveProgress();

        AdvanceWeek();
    }

    public void AdvanceWeek()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return;
        }

        Debug.Log($"[StoryManager] : AdvanceWeek requested. project={GameManager.Instance.CurrentPlayerData.currentProject} week={GameManager.Instance.CurrentPlayerData.currentWeek}/{GameManager.Instance.GetCurrentProjectTotalWeeks()} flowStage={_currentFlowStage}");

        if (GameManager.Instance.CurrentPlayerData.currentWeek >= GameManager.Instance.GetCurrentProjectTotalWeeks())
        {
            Debug.Log($"[StoryManager] : Current week reached project ending threshold. project={GameManager.Instance.CurrentPlayerData.currentProject} week={GameManager.Instance.CurrentPlayerData.currentWeek}");
            EndProject();
            return;
        }

        GameManager.Instance.SetCurrentWeek(GameManager.Instance.CurrentPlayerData.currentWeek + 1);
        GameManager.Instance.SetEnergy(GameConstants.BASE_ENERGY_PER_WEEK);
        GameManager.Instance.ReloadCurrentProjectStory();
        GameManager.Instance.SaveProgress();
        StartWeek();
    }

    public void EndProject()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        PlayerData playerData = GameManager.Instance.CurrentPlayerData;
        if (playerData != null)
        {
            Debug.Log($"[StoryManager] : Ending flow triggered. project={playerData.currentProject} week={playerData.currentWeek} totalWeeks={GameManager.Instance.GetCurrentProjectTotalWeeks()} stats=({playerData.techPower},{playerData.commPower},{playerData.managePower},{playerData.stressPower}) hiddenRisk={playerData.hiddenRisk}");
        }

        UIManager.Instance.HideAllPanels();
        SetFlowStage(StoryFlowStage.Ending);

        EndingResultData result = GameManager.Instance.EvaluateCurrentProjectEnding();
        if (result != null)
        {
            Debug.Log($"[StoryManager] : Ending result ready. endingId={result.endingId} grade={result.grade} title={result.title}");
        }
        else
        {
            Debug.LogError("[StoryManager] Ending evaluation returned null result.");
        }

        GameManager.Instance.UpdateFlowCheckpoint(StoryFlowStage.Ending);
        GameManager.Instance.SaveProgress();
        ShowEndingResult(result);
    }

    public bool CanOpenQuiz()
    {
        return _currentFlowStage == StoryFlowStage.Schedule || _currentFlowStage == StoryFlowStage.Quiz;
    }


    public bool CanOpenSchedule()
    {
        return _currentFlowStage == StoryFlowStage.Schedule || _currentFlowStage == StoryFlowStage.Quiz;
    }

    public void OpenScheduleFromTopBar()
    {
        if (!CanOpenSchedule())
        {
            return;
        }

        _quizOpenRequestedFromSchedule = false;
        UIManager.Instance.HidePanel(QuizPanelName);
        ShowSchedulePanel(false);
    }
    public void OpenQuizFromSchedule()
    {
        if (!CanOpenQuiz())
        {
            return;
        }

        _quizOpenRequestedFromSchedule = true;
        SetFlowStage(StoryFlowStage.Quiz);

        QuizPanel quizPanel = FindObjectOfType<QuizPanel>(true);
        if (quizPanel != null)
        {
            quizPanel.ShowQuiz();
            return;
        }

        UIManager.Instance.ShowPanel(QuizPanelName);
    }

    public void CloseQuizAndReturn()
    {
        UIManager.Instance.HidePanel(QuizPanelName);
        if (_quizOpenRequestedFromSchedule)
        {
            _quizOpenRequestedFromSchedule = false;
            ShowSchedulePanel(false);
        }
    }

    public void ContinueToNextProjectFromEnding()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        PlayerData playerData = GameManager.Instance.CurrentPlayerData;
        if (playerData != null)
        {
            Debug.Log($"[StoryManager] : Continue to next project requested. currentProject={playerData.currentProject} hasNextProject={GameManager.Instance.HasNextProject()}");
        }

        EndingResultData currentResult = GameManager.Instance.EvaluateCurrentProjectEnding();
        if (currentResult == null)
        {
            Debug.LogWarning("[StoryManager] 当前结局为空，无法继续到下一个项目。");
            return;
        }

        if (string.Equals(currentResult.grade, "fail", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[StoryManager] 当前为失败结局，已阻止进入下一个项目。");
            return;
        }

        if (!GameManager.Instance.HasNextProject())
        {
            Debug.LogWarning("[StoryManager] 无法继续到下一个项目，没有可用的后续项目。");
            return;
        }

        int nextProjectNumber = GameManager.Instance.CurrentPlayerData.currentProject + 1;
        ProjectStoryData nextProjectStory = DataManager.Instance.LoadProjectStory(nextProjectNumber);
        if (nextProjectStory == null)
        {
            Debug.LogError($"[StoryManager] 无法加载下一项目剧情数据。project={nextProjectNumber}");
            return;
        }

        GameManager.Instance.UpdateFlowCheckpoint(StoryFlowStage.Transition, 0, nextProjectNumber);
        GameManager.Instance.SaveProgress();

        UIManager.Instance.HidePanel(EndingPanelName);
        SetFlowStage(StoryFlowStage.Transition);
        ShowTransitionResult(nextProjectStory);
    }

    public void StartCurrentProjectFromTransition()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return;
        }

        int currentProjectNumber = GameManager.Instance.CurrentPlayerData.currentProject;
        int pendingProjectNumber = GameManager.Instance.CurrentPlayerData.pendingProjectNumber;
        int targetProjectNumber = pendingProjectNumber > 0 ? pendingProjectNumber : currentProjectNumber + 1;
        if (targetProjectNumber <= currentProjectNumber)
        {
            Debug.LogWarning($"[StoryManager] 无法从转场开始新项目，pendingProject={pendingProjectNumber} currentProject={currentProjectNumber}");
            return;
        }

        Debug.Log($"[StoryManager] : Starting project from transition. project={targetProjectNumber} previousProject={currentProjectNumber}");

        UIManager.Instance.HidePanel(TransitionPanelName);
        GameManager.Instance.StartProject(targetProjectNumber, true);
        GameManager.Instance.SaveProgress();
        StartWeek();
    }

    private void ShowNextDecisionOrSchedule()
    {
        while (true)
        {
            DecisionEventData decision = GetDecisionByIndex(_decisionStepIndex);
            if (IsDecisionEmpty(decision))
            {
                decision = null;
            }

            if (decision == null)
            {
                ConditionalEventData conditionalEvent = _currentWeekEvent != null ? _currentWeekEvent.conditionalEvent : null;
                if (_decisionStepIndex == 0 && ShouldRunConditionalEvent(conditionalEvent))
                {
                    ApplyConditionalEvent(conditionalEvent);
                    return;
                }

                RunRemainingWeekContentOrSchedule();
                return;
            }

            if (decision.isMiniGame)
            {
                ShowMiniGame(decision);
                return;
            }

            if (!HasSelectableOptions(decision))
            {
                Debug.LogWarning("[StoryManager] 跳过无可用选项的决策事件。");
                _decisionStepIndex += 1;
                continue;
            }

            SetFlowStage(StoryFlowStage.Decision);
            ShowDecision(decision);
            return;
        }
    }

    private void ShowMiniGame(DecisionEventData decision)
    {
        if (decision == null || string.IsNullOrWhiteSpace(decision.miniGameType))
        {
            Debug.LogError("[StoryManager] : Mini-game decision is missing required configuration.");
            RunRemainingWeekContentOrSchedule();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateFlowCheckpoint(StoryFlowStage.MiniGame, _decisionStepIndex);
            GameManager.Instance.SaveProgress();
        }

        UIManager.Instance.HideAllPanels();
        SetFlowStage(StoryFlowStage.MiniGame);

        switch (decision.miniGameType.Trim())
        {
            case GameConstants.MINI_GAME_TYPE_CPM:
                CPMGamePanel cpmGamePanel = FindObjectOfType<CPMGamePanel>(true);
                if (cpmGamePanel != null)
                {
                    cpmGamePanel.ShowGame(decision, OnCPMGameCompleted);
                    return;
                }

                Debug.LogError("[StoryManager] : Missing CPMGamePanel for CPM mini-game.");
                break;
            case GameConstants.MINI_GAME_TYPE_RISK_DASHBOARD:
                RiskDashboardPanel riskDashboardPanel = FindObjectOfType<RiskDashboardPanel>(true);
                if (riskDashboardPanel != null)
                {
                    riskDashboardPanel.ShowGame(decision, OnRiskDashboardCompleted);
                    return;
                }

                Debug.LogError("[StoryManager] : Missing RiskDashboardPanel for risk dashboard mini-game.");
                break;
            default:
                Debug.LogError($"[StoryManager] : Unknown mini-game type '{decision.miniGameType}'.");
                break;
        }

        RunRemainingWeekContentOrSchedule();
    }

    private bool CanSkipWeekMainStory()
    {
        if (_currentWeekEvent == null)
        {
            return false;
        }

        return _currentFlowStage == StoryFlowStage.Prologue || _currentFlowStage == StoryFlowStage.DailyIntro;
    }

    private void RunPostDecisionContentOrSchedule()
    {
        if (_currentWeekEvent != null && HasDialogues(_currentWeekEvent.postDecisionDialogues))
        {
            SetFlowStage(StoryFlowStage.PostDecision);
            ShowDialogue(_currentWeekEvent.postDecisionDialogues, OnPostDecisionDialoguesComplete);
            return;
        }

        OnPostDecisionDialoguesComplete();
    }

    private void OnPostDecisionDialoguesComplete()
    {
        if (GameManager.Instance != null && _currentWeekEvent != null && _currentWeekEvent.postDecisionStatChanges != null)
        {
            GameManager.Instance.ApplyStatChanges(_currentWeekEvent.postDecisionStatChanges);
        }

        ShowSchedulePanel();
    }

    private void RunRemainingWeekContentOrSchedule()
    {
        if (TryShowRiskBasedDialogue())
        {
            return;
        }

        RunPostDecisionContentOrSchedule();
    }

    private bool TryShowRiskBasedDialogue()
    {
        if (_hasShownRiskBasedDialogue)
        {
            return false;
        }

        List<DialogueLine> selectedDialogues = GetRiskBasedDialoguesForCurrentWeek();
        if (!HasDialogues(selectedDialogues))
        {
            return false;
        }

        _hasShownRiskBasedDialogue = true;
        SetFlowStage(StoryFlowStage.PostDecision);
        ShowDialogue(selectedDialogues, RunPostDecisionContentOrSchedule);
        return true;
    }

    private void ShowSchedulePanel(bool resetData = true)
    {
        SetFlowStage(StoryFlowStage.Schedule);

        TopStatusBar topStatusBar = FindObjectOfType<TopStatusBar>(true);
        if (topStatusBar != null)
        {
            topStatusBar.SetQuizEntryInteractable(true);
            topStatusBar.SetScheduleEntryInteractable(true);
        }

        SchedulePanel schedulePanel = FindObjectOfType<SchedulePanel>(true);
        if (schedulePanel != null)
        {
            PlayerData currentPlayerData = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
            int currentAvailableEnergy = currentPlayerData != null ? currentPlayerData.energy : GameConstants.BASE_ENERGY_PER_WEEK;

            if (!resetData && schedulePanel.HasCachedSchedule)
            {
                schedulePanel.SyncAvailableEnergy(currentAvailableEnergy);
                schedulePanel.ReopenSchedule();
                return;
            }

            schedulePanel.ShowSchedule(DataManager.Instance.LoadDailyTasks(), currentAvailableEnergy, OnScheduleComplete);
            return;
        }

        UIManager.Instance.ShowPanel(SchedulePanelName);
    }

    private bool IsCurrentWeekFinalWeek()
    {
        return GameManager.Instance != null
            && GameManager.Instance.CurrentPlayerData != null
            && GameManager.Instance.CurrentPlayerData.currentWeek >= GameManager.Instance.GetCurrentProjectTotalWeeks();
    }

    private void ShowDialogue(List<DialogueLine> dialogues, Action onComplete)
    {
        UIManager.Instance.HideAllPanels();

        DialoguePanel dialoguePanel = FindObjectOfType<DialoguePanel>(true);
        if (dialoguePanel != null)
        {
            dialoguePanel.ShowDialogues(dialogues, onComplete);
            return;
        }

        UIManager.Instance.ShowPanel(DialoguePanelName);
    }

    private void ShowDecision(DecisionEventData eventData)
    {
        UIManager.Instance.HideAllPanels();

        DecisionPanel decisionPanel = FindObjectOfType<DecisionPanel>(true);
        if (decisionPanel != null)
        {
            decisionPanel.ShowDecision(eventData, OnDecisionOptionSelected);
            return;
        }

        UIManager.Instance.ShowPanel(DecisionPanelName);
    }

    private void OnDecisionOptionSelected(int selectedIndex, bool hasViewedAiAdvice, bool isFollowedAiAdvice, int decisionLatencyMs)
    {
        DecisionEventData decision = GetDecisionByIndex(_decisionStepIndex);
        if (GameManager.Instance == null || decision == null || decision.options == null || selectedIndex < 0 || selectedIndex >= decision.options.Count)
        {
            return;
        }

        OptionData option = decision.options[selectedIndex];
        StoryFlowStage checkpointStage;
        int checkpointDecisionIndex;
        int recommendedOption = AIAdvisor.Instance != null ? AIAdvisor.Instance.GetRecommendedOption(decision) : decision.aiRecommendedOption;
        GetCheckpointAfterDecision(out checkpointStage, out checkpointDecisionIndex);
        GameManager.Instance.ApplyStatChanges(option.effects);
        GameManager.Instance.ModifyHiddenRisk(option.riskChange);
        GameManager.Instance.UpdateFlowCheckpoint(checkpointStage, checkpointDecisionIndex);
        if (AIAdvisor.Instance != null)
        {
            AIAdvisor.Instance.RecordDecision(decision.eventId, selectedIndex, recommendedOption, hasViewedAiAdvice, decisionLatencyMs, decision.aiQuality);
        }
        else
        {
            GameManager.Instance.RecordAIDecision(decision.eventId, hasViewedAiAdvice, isFollowedAiAdvice, decisionLatencyMs, decision.aiQuality);
        }

        OnDecisionComplete();
    }

    private void ApplyConditionalEvent(ConditionalEventData conditionalEvent)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyStatChanges(conditionalEvent.statPenalty);
            GameManager.Instance.ApplyRiskChange(conditionalEvent.riskPenalty);
        }

        if (HasDialogues(conditionalEvent.dialogues))
        {
            SetFlowStage(StoryFlowStage.Conditional);
            ShowDialogue(conditionalEvent.dialogues, RunRemainingWeekContentOrSchedule);
            return;
        }

        RunRemainingWeekContentOrSchedule();
    }

    private void ApplyWeekFixedChanges()
    {
        if (GameManager.Instance == null || _currentWeekEvent == null)
        {
            return;
        }

        if (_currentWeekEvent.fixedStatChanges != null)
        {
            GameManager.Instance.ApplyStatChanges(_currentWeekEvent.fixedStatChanges);
        }
    }

    private void ApplyWeekRiskChanges()
    {
        if (GameManager.Instance == null || _currentWeekEvent == null)
        {
            return;
        }

        GameManager.Instance.ApplyRiskChange(_currentWeekEvent.riskAutoChange);
    }

    private bool PrepareCurrentWeekContext(bool clearScheduleCache = true)
    {
        if (GameManager.Instance == null)
        {
            return false;
        }

        if (clearScheduleCache)
        {
            SchedulePanel schedulePanel = FindObjectOfType<SchedulePanel>(true);
            if (schedulePanel != null)
            {
                schedulePanel.ClearCachedSchedule();
            }
        }

        _currentWeekEvent = GameManager.Instance.GetCurrentWeekEvent();
        if (_currentWeekEvent == null)
        {
            Debug.LogWarning("[StoryManager] 当前周剧情数据为空，无法开始周流程。");
            return false;
        }

        TopStatusBar topStatusBar = FindObjectOfType<TopStatusBar>(true);
        if (topStatusBar != null)
        {
            topStatusBar.SetQuizEntryInteractable(false);
            topStatusBar.SetScheduleEntryInteractable(false);
            topStatusBar.UpdateDisplay(GameManager.Instance.CurrentPlayerData);
        }

        WeekStarted?.Invoke(_currentWeekEvent);
        return true;
    }

    private bool TryRestoreSavedFlow()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return false;
        }

        PlayerData playerData = GameManager.Instance.CurrentPlayerData;
        switch (playerData.savedFlowStage)
        {
            case StoryFlowStage.Decision:
                _decisionStepIndex = Mathf.Max(0, playerData.savedDecisionStepIndex);
                if (!PrepareCurrentWeekContext())
                {
                    return false;
                }

                ShowNextDecisionOrSchedule();
                return true;

            case StoryFlowStage.MiniGame:
                _decisionStepIndex = Mathf.Max(0, playerData.savedDecisionStepIndex);
                if (!PrepareCurrentWeekContext())
                {
                    return false;
                }

                DecisionEventData miniGameDecision = GetDecisionByIndex(_decisionStepIndex);
                if (miniGameDecision == null || !miniGameDecision.isMiniGame)
                {
                    return false;
                }

                ShowMiniGame(miniGameDecision);
                return true;

            case StoryFlowStage.Conditional:
                _decisionStepIndex = Mathf.Max(0, playerData.savedDecisionStepIndex);
                if (!PrepareCurrentWeekContext())
                {
                    return false;
                }

                ConditionalEventData conditionalEvent = _currentWeekEvent != null ? _currentWeekEvent.conditionalEvent : null;
                if (ShouldRunConditionalEvent(conditionalEvent))
                {
                    ApplyConditionalEvent(conditionalEvent);
                    return true;
                }

                ShowNextDecisionOrSchedule();
                return true;

            case StoryFlowStage.PostDecision:
                _decisionStepIndex = Mathf.Max(0, playerData.savedDecisionStepIndex);
                if (!PrepareCurrentWeekContext())
                {
                    return false;
                }

                RunPostDecisionContentOrSchedule();
                return true;

            case StoryFlowStage.Schedule:
                _decisionStepIndex = Mathf.Max(0, playerData.savedDecisionStepIndex);
                if (!PrepareCurrentWeekContext())
                {
                    return false;
                }

                ShowSchedulePanel();
                return true;

            case StoryFlowStage.Ending:
                UIManager.Instance.HideAllPanels();
                SetFlowStage(StoryFlowStage.Ending);
                ShowEndingResult(GameManager.Instance.EvaluateCurrentProjectEnding());
                return true;

            case StoryFlowStage.Transition:
                int targetProjectNumber = playerData.pendingProjectNumber > 0 ? playerData.pendingProjectNumber : playerData.currentProject + 1;
                ProjectStoryData nextProjectStory = DataManager.Instance.LoadProjectStory(targetProjectNumber);
                if (nextProjectStory == null)
                {
                    return false;
                }

                UIManager.Instance.HideAllPanels();
                SetFlowStage(StoryFlowStage.Transition);
                ShowTransitionResult(nextProjectStory);
                return true;

            default:
                return false;
        }
    }

    private void ShowEndingResult(EndingResultData result)
    {
        ProjectEnded?.Invoke(result);

        EndingPanel endingPanel = FindObjectOfType<EndingPanel>(true);
        if (endingPanel != null)
        {
            endingPanel.ShowEnding(result);
            return;
        }

        UIManager.Instance.ShowPanel(EndingPanelName);
    }

    private void ShowTransitionResult(ProjectStoryData projectStory)
    {
        TransitionPanel transitionPanel = FindObjectOfType<TransitionPanel>(true);
        if (transitionPanel != null)
        {
            transitionPanel.ShowTransition(projectStory);
            return;
        }

        UIManager.Instance.ShowPanel(TransitionPanelName);
    }

    private void GetCheckpointAfterDecision(out StoryFlowStage checkpointStage, out int checkpointDecisionIndex)
    {
        checkpointDecisionIndex = _decisionStepIndex + 1;
        DecisionEventData completedDecision = GetDecisionByIndex(_decisionStepIndex);
        ConditionalEventData conditionalEvent = _currentWeekEvent != null ? _currentWeekEvent.conditionalEvent : null;
        if (completedDecision != null && checkpointDecisionIndex == 1 && ShouldRunConditionalEvent(conditionalEvent))
        {
            checkpointStage = StoryFlowStage.Conditional;
            return;
        }

        if (checkpointDecisionIndex < GetDecisionCount())
        {
            checkpointStage = StoryFlowStage.Decision;
            return;
        }

        checkpointStage = HasPostDecisionContent() ? StoryFlowStage.PostDecision : StoryFlowStage.Schedule;
    }

    private bool HasPostDecisionContent()
    {
        return _currentWeekEvent != null
            && (HasDialogues(_currentWeekEvent.postDecisionDialogues) || _currentWeekEvent.postDecisionStatChanges != null);
    }

    private List<DialogueLine> GetRiskBasedDialoguesForCurrentWeek()
    {
        if (_currentWeekEvent == null || _currentWeekEvent.riskBasedDialogue == null || GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return null;
        }

        int hiddenRisk = GameManager.Instance.CurrentPlayerData.hiddenRisk;
        if (hiddenRisk < GameConstants.PROJECT2_RISK_DIALOGUE_MEDIUM_THRESHOLD)
        {
            return _currentWeekEvent.riskBasedDialogue.low;
        }

        if (hiddenRisk >= GetRiskFailThreshold())
        {
            return _currentWeekEvent.riskBasedDialogue.high;
        }

        return _currentWeekEvent.riskBasedDialogue.medium;
    }

    private void OnCPMGameCompleted(bool isCorrect)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetEventFlag(CpmCorrectFlag, isCorrect);
        CompleteMiniGameStep();
    }

    private void OnRiskDashboardCompleted(RiskDashboardGame.SessionResult result)
    {
        if (GameManager.Instance == null || result == null)
        {
            return;
        }

        GameManager.Instance.ModifyHiddenRisk(result.TotalRiskChange);

        DecisionEventData completedDecision = GetDecisionByIndex(_decisionStepIndex);
        if (completedDecision != null && !string.IsNullOrWhiteSpace(completedDecision.eventId))
        {
            GameManager.Instance.SetEventFlag(completedDecision.eventId, true);
        }

        CompleteMiniGameStep();
    }

    private void CompleteMiniGameStep()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        StoryFlowStage checkpointStage;
        int checkpointDecisionIndex;
        GetCheckpointAfterDecision(out checkpointStage, out checkpointDecisionIndex);
        GameManager.Instance.UpdateFlowCheckpoint(checkpointStage, checkpointDecisionIndex);
        GameManager.Instance.SaveProgress();
        OnDecisionComplete();
    }

    private bool ShouldRunConditionalEvent(ConditionalEventData conditionalEvent)
    {
        if (conditionalEvent == null || string.IsNullOrWhiteSpace(conditionalEvent.conditionFlag))
        {
            return false;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        bool currentValue = false;
        return GameManager.Instance.TryGetEventFlag(conditionalEvent.conditionFlag, out currentValue)
            && currentValue == conditionalEvent.conditionValue;
    }

    private int GetDecisionCount()
    {
        int count = 0;
        if (_currentWeekEvent != null && HasDecisionContent(_currentWeekEvent.decisionEvent))
        {
            count += 1;
        }

        if (_currentWeekEvent != null && HasDecisionContent(_currentWeekEvent.secondDecisionEvent))
        {
            count += 1;
        }

        return count;
    }

    private DecisionEventData GetDecisionByIndex(int index)
    {
        if (_currentWeekEvent == null)
        {
            return null;
        }

        if (index == 0)
        {
            return _currentWeekEvent.decisionEvent;
        }

        if (index == 1)
        {
            return _currentWeekEvent.secondDecisionEvent;
        }

        return null;
    }

    private static bool HasSelectableOptions(DecisionEventData decision)
    {
        if (decision == null || decision.options == null || decision.options.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < decision.options.Count; i += 1)
        {
            if (decision.options[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDecisionContent(DecisionEventData decision)
    {
        return decision != null && (decision.isMiniGame || HasSelectableOptions(decision));
    }

    private static bool IsDecisionEmpty(DecisionEventData decision)
    {
        return decision != null
            && !decision.isMiniGame
            && string.IsNullOrWhiteSpace(decision.eventId)
            && string.IsNullOrWhiteSpace(decision.description)
            && string.IsNullOrWhiteSpace(decision.aiAdvice)
            && string.IsNullOrWhiteSpace(decision.aiQuality)
            && string.IsNullOrWhiteSpace(decision.miniGameType)
            && string.IsNullOrWhiteSpace(decision.conditionStat)
            && decision.conditionThreshold <= 0
            && (decision.options == null || decision.options.Count == 0);
    }

    private int GetRiskFailThreshold()
    {
        EndingsData endingsData = DataManager.Instance != null ? DataManager.Instance.LoadEndings() : null;
        if (endingsData == null || endingsData.projects == null || GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return 60;
        }

        ProjectEndingData projectEnding = endingsData.projects.Find(item => item != null && item.projectNumber == GameManager.Instance.CurrentPlayerData.currentProject);
        return projectEnding != null && projectEnding.riskFailThreshold >= 0 ? projectEnding.riskFailThreshold : 60;
    }
    private static bool HasDialogues(List<DialogueLine> dialogues)
    {
        return dialogues != null && dialogues.Count > 0;
    }

    private static StatEffects SumTaskEffects(List<DailyTaskData> tasks)
    {
        StatEffects total = new StatEffects();
        if (tasks == null)
        {
            return total;
        }

        foreach (DailyTaskData task in tasks)
        {
            if (task == null || task.effects == null)
            {
                continue;
            }

            total.techPower += task.effects.techPower;
            total.commPower += task.effects.commPower;
            total.managePower += task.effects.managePower;
            total.stressPower += task.effects.stressPower;
        }

        return total;
    }

    private static int SumTaskEnergy(List<DailyTaskData> tasks)
    {
        int total = 0;
        if (tasks == null)
        {
            return total;
        }

        foreach (DailyTaskData task in tasks)
        {
            if (task == null)
            {
                continue;
            }

            total += Mathf.Max(0, task.energyCost);
        }

        return total;
    }

    private void ResetRuntimeState()
    {
        ResetWeekState();
    }

    private void ResetWeekState()
    {
        _currentWeekEvent = null;
        _decisionStepIndex = 0;
        _quizOpenRequestedFromSchedule = false;
        _hasShownRiskBasedDialogue = false;
        SetFlowStage(StoryFlowStage.None);
    }

    private void SetFlowStage(StoryFlowStage stage)
    {
        _currentFlowStage = stage;
        FlowStageChanged?.Invoke(stage);
    }

    private void TryAutoBindPanels()
    {
        FindObjectOfType<DialoguePanel>(true);
        FindObjectOfType<DecisionPanel>(true);
        FindObjectOfType<CPMGamePanel>(true);
        FindObjectOfType<RiskDashboardPanel>(true);
        FindObjectOfType<SchedulePanel>(true);
        FindObjectOfType<QuizPanel>(true);
        FindObjectOfType<EndingPanel>(true);
        FindObjectOfType<TransitionPanel>(true);
    }
}

public enum StoryFlowStage
{
    None,
    Prologue,
    DailyIntro,
    Decision,
    MiniGame,
    Conditional,
    PostDecision,
    Schedule,
    Quiz,
    Settlement,
    Ending,
    Transition
}






