using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.UI;

public class Project2Phase4Tests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
#if UNITY_EDITOR
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
#endif
        DestroyAllOfType<AIAdvisor>();
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<EndingPanel>();
        DestroyAllOfType<TransitionPanel>();
        DestroyAllOfType<CPMGamePanel>();
        DestroyAllOfType<RiskDashboardPanel>();
        EnsureTmpFontHost();
    }

    [TearDown]
    public void TearDown()
    {
        DataManager dataManager = UnityEngine.Object.FindObjectOfType<DataManager>();
        if (dataManager != null)
        {
            dataManager.DeleteSave();
        }

        for (int index = _createdObjects.Count - 1; index >= 0; index -= 1)
        {
            if (_createdObjects[index] != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
            }
        }

        _createdObjects.Clear();

        DestroyAllOfType<AIAdvisor>();
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<EndingPanel>();
        DestroyAllOfType<TransitionPanel>();
        DestroyAllOfType<CPMGamePanel>();
        DestroyAllOfType<RiskDashboardPanel>();
    }

    [Test]
    public void AIAdvisor_GetAdviceDisplayTextShouldUseProject2Identity()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 4);

        WeekEventData week4 = gameManager.GetCurrentWeekEvent();
        string adviceText = AIAdvisor.Instance.GetAdviceDisplayText(week4.decisionEvent);

        StringAssert.Contains(GameConstants.PROJECT2_AI_NAME + "建议", adviceText);
        StringAssert.Contains("现有架构已满足上线需求", adviceText);
        Assert.AreEqual(GameConstants.PROJECT2_AI_NAME, AIAdvisor.Instance.CurrentAIName);
    }

    [Test]
    public void AIAdvisor_RecordDecisionShouldPersistFollowState()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateProject2GameManager(dataManager, 4);

        AIAdvisor.Instance.RecordDecision("p2_w4_d1", 1, 1, true, 890);

        PlayerData saved = dataManager.LoadGame();
        Assert.NotNull(saved);
        Assert.AreEqual(1, saved.aiTrustRecords.Count);
        Assert.AreEqual(2, saved.aiTrustRecords[0].projectNumber);
        Assert.AreEqual(4, saved.aiTrustRecords[0].weekNumber);
        Assert.IsTrue(saved.aiTrustRecords[0].isFollowed);
        Assert.AreEqual(890, saved.aiTrustRecords[0].decisionLatencyMs);
    }

    [Test]
    public void CPMGame_IsSolvedShouldMatchAuthoredCriticalPath()
    {
        CPMGame game = new CPMGame();
        Assert.IsTrue(game.TrySetConnection(0, 1));
        Assert.IsTrue(game.TrySetConnection(1, 2));
        Assert.IsTrue(game.TrySetConnection(2, 3));
        Assert.IsTrue(game.IsSolved());

        game.Reset();
        Assert.IsTrue(game.TrySetConnection(0, 2));
        Assert.IsTrue(game.TrySetConnection(2, 1));
        Assert.IsFalse(game.IsSolved());
    }

    [Test]
    public void CPMGame_TrySetConnectionShouldPreservePreviousConnectionWhenReplacementCreatesCycle()
    {
        CPMGame game = new CPMGame();
        Assert.IsTrue(game.TrySetConnection(0, 1));
        Assert.IsTrue(game.TrySetConnection(1, 2));
        Assert.IsTrue(game.TrySetConnection(2, 3));

        Assert.IsFalse(game.TrySetConnection(2, 0));
        Assert.IsTrue(HasConnection(game, 2, 3));
        Assert.IsTrue(game.IsSolved());
    }

    [Test]
    public void StoryManager_ShowNextDecisionOrScheduleShouldLaunchCpmMiniGameAtWeek3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 3);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        CPMGamePanel panel = CreateComponent<CPMGamePanel>("CPMGamePanel");

        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "ShowNextDecisionOrSchedule");

        Assert.AreEqual(StoryFlowStage.MiniGame, storyManager.CurrentFlowStage);
        Assert.IsTrue(panel.gameObject.activeSelf);
        Assert.AreEqual(StoryFlowStage.MiniGame, gameManager.CurrentPlayerData.savedFlowStage);
    }

    [Test]
    public void StoryManager_ShowNextDecisionOrScheduleShouldPersistDecisionCheckpoint()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 4);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        DecisionPanel panel = CreateComponent<DecisionPanel>("DecisionPanel");

        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "ShowNextDecisionOrSchedule");

        Assert.AreEqual(StoryFlowStage.Decision, storyManager.CurrentFlowStage);
        Assert.IsTrue(panel.gameObject.activeSelf);
        Assert.AreEqual(StoryFlowStage.Decision, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.AreEqual(0, gameManager.CurrentPlayerData.savedDecisionStepIndex);
    }

    [Test]
    public void StoryManager_OnCPMGameCompletedShouldPersistFlagAndMoveToSchedule()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 3);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "OnCPMGameCompleted", true);

        bool cpmCorrect;
        Assert.IsTrue(gameManager.TryGetEventFlag(GameConstants.EVENT_FLAG_CPM_CORRECT, out cpmCorrect));
        Assert.IsTrue(cpmCorrect);
        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);

        PlayerData saved = dataManager.LoadGame();
        Assert.NotNull(saved);
        Assert.AreEqual(1, saved.eventFlags.Count);
        Assert.IsTrue(saved.eventFlags[0].value);
    }

    [Test]
    public void StoryManager_ShowNextDecisionOrScheduleShouldTriggerWeek5ConditionalWithoutDecision()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 5);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.SetEventFlag(GameConstants.EVENT_FLAG_CPM_CORRECT, false);
        bool cpmCorrect;
        Assert.IsTrue(gameManager.TryGetEventFlag(GameConstants.EVENT_FLAG_CPM_CORRECT, out cpmCorrect));
        Assert.IsFalse(cpmCorrect);

        WeekEventData week5 = gameManager.GetCurrentWeekEvent();
        Assert.NotNull(week5);
        Assert.NotNull(week5.conditionalEvent);
        Assert.NotNull(week5.conditionalEvent.dialogues);
        Assert.Greater(week5.conditionalEvent.dialogues.Count, 0);
        Assert.IsTrue((bool)InvokePrivate(storyManager, "ShouldRunConditionalEvent", week5.conditionalEvent));

        SetPrivateField(storyManager, "_currentWeekEvent", week5);
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "ShowNextDecisionOrSchedule");

        Assert.AreEqual(StoryFlowStage.Conditional, storyManager.CurrentFlowStage);
        Assert.AreEqual(45, gameManager.CurrentPlayerData.managePower);
        Assert.AreEqual(45, gameManager.CurrentPlayerData.stressPower);
        Assert.AreEqual(10, gameManager.CurrentPlayerData.hiddenRisk);
    }

    [Test]
    public void StoryManager_OnRiskDashboardCompletedShouldApplyHiddenRiskAndPersistCompletion()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 9);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.hiddenRisk = 12;
        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "OnRiskDashboardCompleted", new RiskDashboardGame.SessionResult(6, 2, 1, -7));

        Assert.AreEqual(5, gameManager.CurrentPlayerData.hiddenRisk);
        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);

        bool completed;
        Assert.IsTrue(gameManager.TryGetEventFlag("p2_w9_minigame", out completed));
        Assert.IsTrue(completed);

        PlayerData saved = dataManager.LoadGame();
        Assert.NotNull(saved);
        Assert.AreEqual(5, saved.hiddenRisk);
    }

    [Test]
    public void StoryManager_GetRiskBasedDialoguesForCurrentWeekShouldSelectBucketByHiddenRisk()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 11);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());

        gameManager.CurrentPlayerData.hiddenRisk = 10;
        List<DialogueLine> lowDialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        StringAssert.Contains("全员信心满满", lowDialogues[0].text);

        gameManager.CurrentPlayerData.hiddenRisk = 35;
        List<DialogueLine> mediumDialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        StringAssert.Contains("担忧的眼神", mediumDialogues[0].text);

        gameManager.CurrentPlayerData.hiddenRisk = 65;
        List<DialogueLine> highDialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        StringAssert.Contains("沉默不语", highDialogues[0].text);
    }

    [Test]
    public void GameManager_EvaluateCurrentProjectEndingShouldDowngradeExcellentWhenRiskIsNotLow()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 2,
            currentWeek = 12,
            hiddenRisk = 35,
            techPower = 320,
            commPower = 320,
            managePower = 320,
            stressPower = 320,
            aiTrustRecords = new List<AITrustRecord>(),
            eventFlags = new List<EventFlagRecord>()
        });

        EndingResultData result = gameManager.EvaluateCurrentProjectEnding();
        Assert.NotNull(result);
        Assert.AreEqual("pass", result.grade);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreMiniGameCheckpoint()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 3);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        CPMGamePanel panel = CreateComponent<CPMGamePanel>("CPMGamePanel");

        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.MiniGame;
        gameManager.CurrentPlayerData.savedDecisionStepIndex = 0;
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.MiniGame, storyManager.CurrentFlowStage);
        Assert.IsTrue(panel.gameObject.activeSelf);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreConditionalCheckpointWithoutReapplyingPenalty()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 5);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        gameManager.SetEventFlag(GameConstants.EVENT_FLAG_CPM_CORRECT, false);
        gameManager.CurrentPlayerData.managePower = 45;
        gameManager.CurrentPlayerData.stressPower = 45;
        gameManager.CurrentPlayerData.hiddenRisk = 10;
        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.Conditional;
        gameManager.CurrentPlayerData.savedDecisionStepIndex = 0;
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.Conditional, storyManager.CurrentFlowStage);
        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        Assert.AreEqual(45, gameManager.CurrentPlayerData.managePower);
        Assert.AreEqual(45, gameManager.CurrentPlayerData.stressPower);
        Assert.AreEqual(10, gameManager.CurrentPlayerData.hiddenRisk);
    }

    [Test]
    public void StoryManager_ShowSchedulePanelShouldPersistScheduleCheckpoint()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 11);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SchedulePanel schedulePanel = CreateComponent<SchedulePanel>("SchedulePanel");

        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        InvokePrivate(storyManager, "ShowSchedulePanel", true);

        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);
        Assert.IsTrue(schedulePanel.gameObject.activeSelf);
        Assert.AreEqual(StoryFlowStage.Schedule, gameManager.CurrentPlayerData.savedFlowStage);
    }

    [Test]
    public void GameScene_ShouldContainStaticP2PanelShells()
    {
#if UNITY_EDITOR
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
#endif

        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/DecisionPanel/PanelContent/DescriptionText"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/DecisionPanel/PanelContent/AIAdviceRow/AIAdviceButton"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/CPMGamePanel/PanelContent/Playfield"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/CPMGamePanel/PanelContent/FooterButtons/ConfirmButton"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/RiskDashboardPanel/PanelContent/TimerText"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/RiskDashboardPanel/PanelContent/ModulesRoot"));
    }

    [Test]
    public void CPMGamePanel_ShowGameShouldResetRuntimeStateWhenReopened()
    {
        DecisionEventData eventData = GetProject2WeekEvent(3).decisionEvent;
        CPMGamePanel panel = CreateComponent<CPMGamePanel>("CPMGamePanel");

        panel.ShowGame(eventData, _ => { });

        SetPrivateField(panel, "_hasPendingResult", true);
        TMP_Text feedbackText = GetPrivateField<TMP_Text>(panel, "_feedbackText");
        Button confirmButton = GetPrivateField<Button>(panel, "_confirmButton");
        Button resetButton = GetPrivateField<Button>(panel, "_resetButton");
        Button closeButton = GetPrivateField<Button>(panel, "_closeButton");

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = "旧反馈";
        confirmButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(true);
        closeButton.interactable = true;

        panel.ShowGame(eventData, _ => { });

        Assert.IsFalse(GetPrivateField<bool>(panel, "_hasPendingResult"));
        Assert.IsTrue(confirmButton.gameObject.activeSelf);
        Assert.IsTrue(confirmButton.interactable);
        Assert.IsTrue(resetButton.gameObject.activeSelf);
        Assert.IsTrue(resetButton.interactable);
        Assert.IsFalse(closeButton.gameObject.activeSelf);
        Assert.IsFalse(closeButton.interactable);
        Assert.IsFalse(feedbackText.gameObject.activeSelf);
        Assert.AreEqual(string.Empty, feedbackText.text);
    }

    [Test]
    public void CPMGamePanel_ShowGameShouldPinFooterButtonsToPanelBottom()
    {
        DecisionEventData eventData = GetProject2WeekEvent(3).decisionEvent;
        CPMGamePanel panel = CreateComponent<CPMGamePanel>("CPMGamePanel");

        panel.ShowGame(eventData, _ => { });

        Transform footer = panel.transform.Find("PanelContent/FooterButtons");
        Assert.NotNull(footer);

        RectTransform footerRect = footer.GetComponent<RectTransform>();
        LayoutElement footerLayout = footer.GetComponent<LayoutElement>();

        Assert.NotNull(footerRect);
        Assert.NotNull(footerLayout);
        Assert.IsTrue(footerLayout.ignoreLayout);
        Assert.AreEqual(0f, footerRect.anchorMin.y, 0.001f);
        Assert.AreEqual(0f, footerRect.anchorMax.y, 0.001f);
        Assert.AreEqual(0f, footerRect.pivot.y, 0.001f);
        Assert.Greater(footerRect.offsetMin.y, 0f);
    }

    [Test]
    public void RiskDashboardPanel_ShowGameShouldResetRuntimeStateWhenReopened()
    {
        DecisionEventData eventData = GetProject2WeekEvent(9).decisionEvent;
        RiskDashboardPanel panel = CreateComponent<RiskDashboardPanel>("RiskDashboardPanel");

        panel.ShowGame(eventData, _ => { });

        SetPrivateField(panel, "_pendingResult", new RiskDashboardGame.SessionResult(3, 5, 2, 11));
        TMP_Text resultText = GetPrivateField<TMP_Text>(panel, "_resultText");
        Button closeButton = GetPrivateField<Button>(panel, "_closeButton");

        resultText.gameObject.SetActive(true);
        resultText.text = "旧结果";
        closeButton.gameObject.SetActive(true);
        closeButton.interactable = true;

        panel.ShowGame(eventData, _ => { });

        Assert.IsNull(GetPrivateField<RiskDashboardGame.SessionResult>(panel, "_pendingResult"));
        Assert.AreEqual("点击红色模块即可修复问题，漏掉报警会增加隐藏风险。", resultText.text);
        Assert.IsTrue(resultText.gameObject.activeSelf);
        Assert.IsFalse(closeButton.gameObject.activeSelf);
        Assert.IsFalse(closeButton.interactable);
    }

    [Test]
    public void EndingPanel_ShowEndingShouldHideNextProjectButtonForFailResult()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateProject2GameManager(dataManager, 12, 320, 65);
        EndingPanel panel = CreateComponent<EndingPanel>("EndingPanel");

        ProjectEndingData project2Ending = GetProject2Ending(dataManager);
        panel.ShowEnding(project2Ending.fail);

        Button nextProjectButton = GetPrivateField<Button>(panel, "_nextProjectButton");
        Assert.IsFalse(nextProjectButton.gameObject.activeSelf);
    }

    [Test]
    public void StoryManager_ContinueToNextProjectFromEndingShouldShowTransitionForPassResult()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 35);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        EndingPanel endingPanel = CreateComponent<EndingPanel>("EndingPanel");
        TransitionPanel transitionPanel = CreateComponent<TransitionPanel>("TransitionPanel");

        endingPanel.ShowEnding(gameManager.EvaluateCurrentProjectEnding());
        storyManager.ContinueToNextProjectFromEnding();

        Assert.AreEqual(StoryFlowStage.Transition, storyManager.CurrentFlowStage);
        Assert.IsTrue(transitionPanel.gameObject.activeSelf);
        Assert.AreEqual(StoryFlowStage.Transition, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.AreEqual(3, gameManager.CurrentPlayerData.pendingProjectNumber);
    }

    [Test]
    public void StoryManager_StartCurrentProjectFromTransitionShouldResetHiddenRiskForProject3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 42);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        CreateComponent<TransitionPanel>("TransitionPanel");

        gameManager.CurrentPlayerData.pendingProjectNumber = 3;
        storyManager.StartCurrentProjectFromTransition();

        Assert.AreEqual(3, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.currentWeek);
        Assert.AreEqual(0, gameManager.CurrentPlayerData.hiddenRisk);
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK, gameManager.CurrentPlayerData.energy);
    }

    [Test]
    public void Project2DemoRoute_ExcellentShouldStayLowRiskAndReachExcellentEnding()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 0);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        ApplyProject2Choice(gameManager, 1, 1);
        ApplyProject2Choice(gameManager, 2, 1);
        ApplyProject2Choice(gameManager, 4, 0);
        ApplyProject2Choice(gameManager, 6, 2);
        gameManager.ApplyRiskChange(GetProject2WeekEvent(7).riskAutoChange);
        ApplyProject2Choice(gameManager, 8, 2);
        gameManager.ApplyRiskChange(new RiskDashboardGame.SessionResult(7, 1, 0, -11).TotalRiskChange);
        ApplyProject2Choice(gameManager, 10, 1);

        gameManager.SetCurrentWeek(11);
        SetPrivateField(storyManager, "_currentWeekEvent", GetProject2WeekEvent(11));
        List<DialogueLine> week11Dialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        EndingResultData ending = gameManager.EvaluateCurrentProjectEnding();

        Assert.Less(gameManager.CurrentPlayerData.hiddenRisk, GameConstants.PROJECT2_EXCELLENT_RISK_THRESHOLD);
        StringAssert.Contains("全员信心满满", week11Dialogues[0].text);
        Assert.NotNull(ending);
        Assert.AreEqual("excellent", ending.grade);
        Assert.AreEqual("电商教父", ending.title);
    }

    [Test]
    public void Project2DemoRoute_PassShouldStayMediumRiskAndReachPassEnding()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 0);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        ApplyProject2Choice(gameManager, 1, 0);
        ApplyProject2Choice(gameManager, 2, 2);
        ApplyProject2Choice(gameManager, 4, 2);
        ApplyProject2Choice(gameManager, 6, 0);
        gameManager.ApplyRiskChange(GetProject2WeekEvent(7).riskAutoChange);
        ApplyProject2Choice(gameManager, 8, 1);
        gameManager.ApplyRiskChange(new RiskDashboardGame.SessionResult(4, 5, 3, 10).TotalRiskChange);
        ApplyProject2Choice(gameManager, 10, 2);

        gameManager.SetCurrentWeek(11);
        SetPrivateField(storyManager, "_currentWeekEvent", GetProject2WeekEvent(11));
        List<DialogueLine> week11Dialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        EndingResultData ending = gameManager.EvaluateCurrentProjectEnding();

        Assert.GreaterOrEqual(gameManager.CurrentPlayerData.hiddenRisk, GameConstants.PROJECT2_RISK_DIALOGUE_MEDIUM_THRESHOLD);
        Assert.Less(gameManager.CurrentPlayerData.hiddenRisk, GetProject2Ending(dataManager).riskFailThreshold);
        StringAssert.Contains("担忧的眼神", week11Dialogues[0].text);
        Assert.NotNull(ending);
        Assert.AreEqual("pass", ending.grade);
        Assert.AreEqual("修补匠", ending.title);
    }

    [Test]
    public void Project2DemoRoute_FailShouldReachHighRiskAndFailEnding()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 0);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        ProjectEndingData project2Ending = GetProject2Ending(dataManager);

        ApplyProject2Choice(gameManager, 1, 0);
        ApplyProject2Choice(gameManager, 2, 0);
        ApplyProject2Choice(gameManager, 4, 1);
        ApplyProject2ConditionalPenalty(gameManager, 5);
        ApplyProject2Choice(gameManager, 6, 1);
        gameManager.ApplyRiskChange(GetProject2WeekEvent(7).riskAutoChange);
        ApplyProject2Choice(gameManager, 8, 1);
        gameManager.ApplyRiskChange(new RiskDashboardGame.SessionResult(3, 6, 1, 13).TotalRiskChange);
        ApplyProject2Choice(gameManager, 10, 0);

        gameManager.SetCurrentWeek(11);
        SetPrivateField(storyManager, "_currentWeekEvent", GetProject2WeekEvent(11));
        List<DialogueLine> week11Dialogues = (List<DialogueLine>)InvokePrivate(storyManager, "GetRiskBasedDialoguesForCurrentWeek");
        EndingResultData ending = gameManager.EvaluateCurrentProjectEnding();

        Assert.GreaterOrEqual(gameManager.CurrentPlayerData.hiddenRisk, project2Ending.riskFailThreshold);
        StringAssert.Contains("沉默不语", week11Dialogues[0].text);
        Assert.NotNull(ending);
        Assert.AreEqual("fail", ending.grade);
        Assert.AreEqual("背锅侠", ending.title);
    }

    private GameManager CreateProject2GameManager(DataManager dataManager, int weekNumber, int baseStatValue = 50, int initialHiddenRisk = 0)
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 2,
            currentWeek = weekNumber,
            techPower = baseStatValue,
            commPower = baseStatValue,
            managePower = baseStatValue,
            stressPower = baseStatValue,
            hiddenRisk = initialHiddenRisk,
            aiTrustRecords = new List<AITrustRecord>(),
            eventFlags = new List<EventFlagRecord>()
        });
        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(2));
        Assert.AreSame(gameManager, GameManager.Instance);
        return gameManager;
    }

    private T CreateComponent<T>(string name) where T : Component
    {
        GameObject gameObject = new GameObject(name);
        _createdObjects.Add(gameObject);
        return gameObject.AddComponent<T>();
    }

    private static void DestroyAllOfType<T>() where T : Component
    {
        T[] objects = UnityEngine.Object.FindObjectsOfType<T>(true);
        for (int index = 0; index < objects.Length; index += 1)
        {
            if (objects[index] != null)
            {
                UnityEngine.Object.DestroyImmediate(objects[index].gameObject);
            }
        }
    }

    private void EnsureTmpFontHost()
    {
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback == null)
        {
            return;
        }

        GameObject hostObject = new GameObject("TmpFontHost");
        _createdObjects.Add(hostObject);
        TextMeshProUGUI text = hostObject.AddComponent<TextMeshProUGUI>();
        text.font = fallback;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field not found: " + fieldName);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field not found: " + fieldName);
        return (T)field.GetValue(target);
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        Type currentType = target.GetType();
        while (currentType != null)
        {
            MethodInfo method = currentType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null)
            {
                return method.Invoke(target, args);
            }

            currentType = currentType.BaseType;
        }

        Assert.Fail("Method not found: " + methodName);
        return null;
    }

    private static bool HasConnection(CPMGame game, int fromIndex, int toIndex)
    {
        foreach (Vector2Int connection in game.Connections)
        {
            if (connection.x == fromIndex && connection.y == toIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindInOpenScene(string relativePath)
    {
        string[] segments = relativePath.Split('/');
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            if (!string.Equals(rootObject.name, segments[0], StringComparison.Ordinal))
            {
                continue;
            }

            Transform current = rootObject.transform;
            for (int index = 1; index < segments.Length && current != null; index += 1)
            {
                current = current.Find(segments[index]);
            }

            if (current != null)
            {
                return current;
            }
        }

        return null;
    }

    private static WeekEventData GetProject2WeekEvent(int weekNumber)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(GameConstants.DATA_PROJECT2_STORY_RESOURCE_PATH);
        Assert.NotNull(textAsset);

        ProjectStoryData storyData = JsonUtility.FromJson<ProjectStoryData>(textAsset.text);
        Assert.NotNull(storyData);
        Assert.NotNull(storyData.weeks);

        WeekEventData weekEvent = storyData.weeks.Find(week => week != null && week.weekNumber == weekNumber);
        Assert.NotNull(weekEvent, "Week data not found: " + weekNumber);
        return weekEvent;
    }

    private static ProjectEndingData GetProject2Ending(DataManager dataManager)
    {
        EndingsData endingsData = dataManager.LoadEndings();
        Assert.NotNull(endingsData);
        Assert.NotNull(endingsData.projects);

        ProjectEndingData projectEnding = endingsData.projects.Find(item => item != null && item.projectNumber == 2);
        Assert.NotNull(projectEnding);
        return projectEnding;
    }

    private static void ApplyProject2Choice(GameManager gameManager, int weekNumber, int optionIndex)
    {
        WeekEventData weekEvent = GetProject2WeekEvent(weekNumber);
        Assert.NotNull(weekEvent.decisionEvent);
        Assert.NotNull(weekEvent.decisionEvent.options);
        Assert.Greater(weekEvent.decisionEvent.options.Count, optionIndex);

        OptionData option = weekEvent.decisionEvent.options[optionIndex];
        Assert.NotNull(option);

        gameManager.ApplyStatChanges(option.effects);
        gameManager.ModifyHiddenRisk(option.riskChange);
    }

    private static void ApplyProject2ConditionalPenalty(GameManager gameManager, int weekNumber)
    {
        WeekEventData weekEvent = GetProject2WeekEvent(weekNumber);
        Assert.NotNull(weekEvent.conditionalEvent);

        gameManager.ApplyStatChanges(weekEvent.conditionalEvent.statPenalty);
        gameManager.ApplyRiskChange(weekEvent.conditionalEvent.riskPenalty);
    }
}
