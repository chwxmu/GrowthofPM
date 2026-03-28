using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class Project1ClosureTests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
    }

    [TearDown]
    public void TearDown()
    {
        DataManager dataManager = UnityEngine.Object.FindObjectOfType<DataManager>();
        if (dataManager != null)
        {
            dataManager.DeleteSave();
        }

        for (int i = _createdObjects.Count - 1; i >= 0; i -= 1)
        {
            if (_createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }
        }

        _createdObjects.Clear();

        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
    }

    [Test]
    public void DataManager_LoadProject1StoryShouldContainNineSequentialWeeksAndWeek3SecondDecision()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");

        ProjectStoryData story = dataManager.LoadProjectStory(1);

        Assert.NotNull(story);
        Assert.AreEqual(1, story.projectNumber);
        Assert.AreEqual(9, story.totalWeeks);
        Assert.NotNull(story.weeks);
        Assert.AreEqual(9, story.weeks.Count);
        for (int weekNumber = 1; weekNumber <= 9; weekNumber += 1)
        {
            Assert.NotNull(story.weeks.Find(item => item != null && item.weekNumber == weekNumber), $"Missing week {weekNumber}");
        }

        WeekEventData week1 = story.weeks.Find(item => item != null && item.weekNumber == 1);
        WeekEventData week3 = story.weeks.Find(item => item != null && item.weekNumber == 3);
        WeekEventData week9 = story.weeks.Find(item => item != null && item.weekNumber == 9);

        Assert.NotNull(week1);
        Assert.NotNull(week1.decisionEvent);
        Assert.AreEqual(3, week1.decisionEvent.options.Count);
        Assert.NotNull(week3);
        Assert.NotNull(week3.decisionEvent);
        Assert.NotNull(week3.secondDecisionEvent);
        Assert.NotNull(week9);
        Assert.IsTrue(week9.decisionEvent == null || week9.decisionEvent.options == null || week9.decisionEvent.options.Count == 0);
    }

    [Test]
    public void StoryManager_Project1FirstChoicePathShouldReachEndingAcrossNineWeeks()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();

        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(1);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(1));
        Assert.NotNull(gameManager.CurrentProjectStory);
        Assert.NotNull(uiManager);

        for (int weekNumber = 1; weekNumber <= GameConstants.PROJECT1_WEEKS; weekNumber += 1)
        {
            WeekEventData weekEvent = gameManager.GetCurrentWeekEvent();
            Assert.NotNull(weekEvent, $"Current week event should exist for week {weekNumber}");

            SetPrivateField(storyManager, "_currentWeekEvent", weekEvent);
            SetPrivateField(storyManager, "_decisionStepIndex", 0);

            if (weekEvent.decisionEvent != null)
            {
                InvokePrivate(storyManager, "OnDecisionOptionSelected", 0, false, false, 0);
            }

            if (weekEvent.secondDecisionEvent != null)
            {
                InvokePrivate(storyManager, "OnDecisionOptionSelected", 0, false, false, 0);
            }

            storyManager.OnScheduleComplete(new List<DailyTaskData>());
        }

        Assert.AreEqual(GameConstants.PROJECT1_WEEKS, gameManager.CurrentPlayerData.currentWeek);
        Assert.AreEqual(StoryFlowStage.Ending, storyManager.CurrentFlowStage);
        Assert.NotNull(gameManager.EvaluateCurrentProjectEnding());
    }

    [Test]
    public void StoryManager_Project1Week3FirstDecisionShouldCheckpointSecondDecision()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(3);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        SetPrivateField(storyManager, "_currentWeekEvent", gameManager.GetCurrentWeekEvent());
        InvokePrivate(storyManager, "OnDecisionOptionSelected", 0, false, false, 120);

        Assert.AreEqual(StoryFlowStage.Decision, storyManager.CurrentFlowStage);
        Assert.AreEqual(StoryFlowStage.Decision, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.savedDecisionStepIndex);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.aiTrustRecords.Count);
        Assert.AreEqual("p1_w3_d1", gameManager.CurrentPlayerData.aiTrustRecords[0].eventId);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreWeek3SecondDecision()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(3);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        gameManager.UpdateFlowCheckpoint(StoryFlowStage.Decision, 1);
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        DecisionEventData currentDecision = GetPrivateField<DecisionEventData>(decisionPanel, "_currentEventData");
        Assert.AreEqual(StoryFlowStage.Decision, storyManager.CurrentFlowStage);
        Assert.NotNull(currentDecision);
        Assert.AreEqual("p1_w3_d2", currentDecision.eventId);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreEndingCheckpoint()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(GameConstants.PROJECT1_WEEKS);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        EndingPanel endingPanel = CreateComponent<EndingPanel>("EndingPanel");
        endingPanel.gameObject.SetActive(false);

        gameManager.UpdateFlowCheckpoint(StoryFlowStage.Ending);
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        EndingResultData currentResult = GetPrivateField<EndingResultData>(endingPanel, "_currentResult");
        Assert.AreEqual(StoryFlowStage.Ending, storyManager.CurrentFlowStage);
        Assert.NotNull(currentResult);
        Assert.AreEqual(gameManager.EvaluateCurrentProjectEnding().endingId, currentResult.endingId);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreTransitionCheckpoint()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(GameConstants.PROJECT1_WEEKS);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        TransitionPanel transitionPanel = CreateComponent<TransitionPanel>("TransitionPanel");
        transitionPanel.gameObject.SetActive(false);

        gameManager.UpdateFlowCheckpoint(StoryFlowStage.Transition, 0, 2);
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.Transition, storyManager.CurrentFlowStage);
        Assert.IsTrue(transitionPanel.gameObject.activeSelf);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(2, gameManager.CurrentPlayerData.pendingProjectNumber);
    }

    [Test]
    public void StoryManager_ContinueToNextProjectFromEndingShouldBlockFailEnding()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(GameConstants.PROJECT1_WEEKS);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.techPower = 10;
        gameManager.CurrentPlayerData.commPower = 10;
        gameManager.CurrentPlayerData.managePower = 10;
        gameManager.CurrentPlayerData.stressPower = 10;

        storyManager.ContinueToNextProjectFromEnding();

        Assert.AreEqual(1, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(0, gameManager.CurrentPlayerData.pendingProjectNumber);
        Assert.AreEqual(StoryFlowStage.None, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.AreNotEqual(StoryFlowStage.Transition, storyManager.CurrentFlowStage);
    }

    [Test]
    public void StoryManager_StartCurrentProjectFromTransitionShouldAdvanceToProject2AndResetHiddenRisk()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateGameManagerForProject1Week(GameConstants.PROJECT1_WEEKS);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.hiddenRisk = 18;
        gameManager.CurrentPlayerData.pendingProjectNumber = 2;
        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.Transition;

        storyManager.StartCurrentProjectFromTransition();

        Assert.AreEqual(2, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.currentWeek);
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK, gameManager.CurrentPlayerData.energy);
        Assert.AreEqual(0, gameManager.CurrentPlayerData.hiddenRisk);
        Assert.AreEqual(StoryFlowStage.Prologue, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.AreEqual(0, gameManager.CurrentPlayerData.pendingProjectNumber);
    }

    private GameManager CreateGameManagerForProject1Week(int weekNumber)
    {
        DataManager dataManager = UnityEngine.Object.FindObjectOfType<DataManager>();
        Assert.NotNull(dataManager);

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = weekNumber,
            energy = GameConstants.BASE_ENERGY_PER_WEEK,
            techPower = 90,
            commPower = 90,
            managePower = 90,
            stressPower = 90,
            aiTrustRecords = new List<AITrustRecord>(),
            eventFlags = new List<EventFlagRecord>()
        });
        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(1));
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
        for (int i = 0; i < objects.Length; i += 1)
        {
            if (objects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(objects[i].gameObject);
            }
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field not found: {fieldName}");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field not found: {fieldName}");
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method not found: {methodName}");
        method.Invoke(target, args);
    }
}
