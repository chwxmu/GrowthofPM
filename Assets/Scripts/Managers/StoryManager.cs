using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : Singleton<StoryManager>
{
    private const string DialoguePanelName = "DialoguePanel";
    private const string DecisionPanelName = "DecisionPanel";
    private const string SchedulePanelName = "SchedulePanel";
    private const string QuizPanelName = "QuizPanel";
    private const string EndingPanelName = "EndingPanel";
    private const string TransitionPanelName = "TransitionPanel";
    private const string CpmCorrectFlag = "cpmCorrect";
    private const KeyCode SkipMainStoryKey = KeyCode.P;

    private readonly Dictionary<string, bool> _runtimeFlags = new Dictionary<string, bool>();

    private WeekEventData _currentWeekEvent;
    private int _decisionStepIndex;
    private bool _isHandlingGameScene;
    private bool _quizOpenRequestedFromSchedule;
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
        StartWeek();
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

        SchedulePanel schedulePanel = FindObjectOfType<SchedulePanel>(true);
        if (schedulePanel != null)
        {
            schedulePanel.ClearCachedSchedule();
        }

        _currentWeekEvent = GameManager.Instance.GetCurrentWeekEvent();
        if (_currentWeekEvent == null)
        {
            Debug.LogWarning("[StoryManager] 当前周剧情数据为空，无法开始周流程。");
            return;
        }

        TopStatusBar topStatusBar = FindObjectOfType<TopStatusBar>(true);
        if (topStatusBar != null)
        {
            topStatusBar.SetQuizEntryInteractable(false);
            topStatusBar.SetScheduleEntryInteractable(false);
            topStatusBar.UpdateDisplay(GameManager.Instance.CurrentPlayerData);
        }

        WeekStarted?.Invoke(_currentWeekEvent);

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

        if (completedDecision != null && completedDecision.isMiniGame)
        {
            ResolveMiniGamePlaceholder(completedDecision);
        }

        ConditionalEventData conditionalEvent = _currentWeekEvent != null ? _currentWeekEvent.conditionalEvent : null;
        if (completedDecision != null && _decisionStepIndex == 1 && ShouldRunConditionalEvent(conditionalEvent))
        {
            ApplyConditionalEvent(conditionalEvent);
            return;
        }

        if (_decisionStepIndex >= GetDecisionCount())
        {
            RunPostDecisionContentOrSchedule();
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

        GameManager.Instance.SetEnergy(Mathf.Max(0, GameConstants.BASE_ENERGY_PER_WEEK - spentEnergy));
        GameManager.Instance.ApplyStatChanges(totalEffects);
        ApplyWeekFixedChanges();
        ApplyWeekRiskChanges();
        GameManager.Instance.SaveProgress();

        AdvanceWeek();
    }

    public void AdvanceWeek()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentPlayerData.currentWeek >= GameManager.Instance.GetCurrentProjectTotalWeeks())
        {
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

        UIManager.Instance.HideAllPanels();
        SetFlowStage(StoryFlowStage.Ending);

        EndingResultData result = GameManager.Instance.EvaluateCurrentProjectEnding();
        ProjectEnded?.Invoke(result);

        EndingPanel endingPanel = FindObjectOfType<EndingPanel>(true);
        if (endingPanel != null)
        {
            endingPanel.ShowEnding(result);
            return;
        }

        UIManager.Instance.ShowPanel(EndingPanelName);
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

        if (!GameManager.Instance.AdvanceToNextProject())
        {
            return;
        }

        UIManager.Instance.HidePanel(EndingPanelName);
        SetFlowStage(StoryFlowStage.Transition);

        TransitionPanel transitionPanel = FindObjectOfType<TransitionPanel>(true);
        if (transitionPanel != null)
        {
            transitionPanel.ShowTransition(GameManager.Instance.CurrentProjectStory);
            return;
        }

        UIManager.Instance.ShowPanel(TransitionPanelName);
    }

    public void StartCurrentProjectFromTransition()
    {
        UIManager.Instance.HidePanel(TransitionPanelName);
        StartWeek();
    }

    private void ShowNextDecisionOrSchedule()
    {
        while (true)
        {
            DecisionEventData decision = GetDecisionByIndex(_decisionStepIndex);
            if (decision == null)
            {
                RunPostDecisionContentOrSchedule();
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
            if (!resetData && schedulePanel.HasCachedSchedule)
            {
                schedulePanel.ReopenSchedule();
                return;
            }

            schedulePanel.ShowSchedule(DataManager.Instance.LoadDailyTasks(), GameConstants.BASE_ENERGY_PER_WEEK, OnScheduleComplete);
            return;
        }

        UIManager.Instance.ShowPanel(SchedulePanelName);
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
        GameManager.Instance.ApplyStatChanges(option.effects);
        GameManager.Instance.ApplyRiskChange(option.riskChange);
        GameManager.Instance.RecordAIDecision(decision.eventId, hasViewedAiAdvice, isFollowedAiAdvice, decisionLatencyMs);
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
            ShowDialogue(conditionalEvent.dialogues, RunPostDecisionContentOrSchedule);
            return;
        }

        RunPostDecisionContentOrSchedule();
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

    private void ResolveMiniGamePlaceholder(DecisionEventData decision)
    {
        if (decision == null || string.IsNullOrWhiteSpace(decision.miniGameType))
        {
            return;
        }

        if (decision.miniGameType == "cpm")
        {
            _runtimeFlags[CpmCorrectFlag] = false;
            return;
        }

        if (decision.miniGameType == "risk_dashboard")
        {
            _runtimeFlags[decision.eventId] = false;
        }
    }

    private bool ShouldRunConditionalEvent(ConditionalEventData conditionalEvent)
    {
        if (conditionalEvent == null || string.IsNullOrWhiteSpace(conditionalEvent.conditionFlag))
        {
            return false;
        }

        bool currentValue = false;
        _runtimeFlags.TryGetValue(conditionalEvent.conditionFlag, out currentValue);
        return currentValue == conditionalEvent.conditionValue;
    }

    private int GetDecisionCount()
    {
        int count = 0;
        if (_currentWeekEvent != null && HasSelectableOptions(_currentWeekEvent.decisionEvent))
        {
            count += 1;
        }

        if (_currentWeekEvent != null && HasSelectableOptions(_currentWeekEvent.secondDecisionEvent))
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
        _runtimeFlags.Clear();
        ResetWeekState();
    }

    private void ResetWeekState()
    {
        _currentWeekEvent = null;
        _decisionStepIndex = 0;
        _quizOpenRequestedFromSchedule = false;
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
    Conditional,
    PostDecision,
    Schedule,
    Quiz,
    Settlement,
    Ending,
    Transition
}






