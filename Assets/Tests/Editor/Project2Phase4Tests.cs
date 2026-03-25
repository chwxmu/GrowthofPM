using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

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

    private GameManager CreateProject2GameManager(DataManager dataManager, int weekNumber)
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 2,
            currentWeek = weekNumber,
            techPower = 50,
            commPower = 50,
            managePower = 50,
            stressPower = 50,
            hiddenRisk = 0,
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
}
