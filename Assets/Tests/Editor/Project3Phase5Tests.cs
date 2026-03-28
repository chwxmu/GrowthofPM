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

public class Project3Phase5Tests
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
        DestroyAllOfType<GameSummaryPanel>();
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
        DestroyAllOfType<GameSummaryPanel>();
    }

    [Test]
    public void StoryManager_StartWeekShouldShowDailyIntroBeforePrologueForProject3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 1);
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        storyManager.StartWeek();

        Assert.AreEqual(StoryFlowStage.DailyIntro, storyManager.CurrentFlowStage);
        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        Assert.AreEqual(StoryFlowStage.DailyIntro, gameManager.CurrentPlayerData.savedFlowStage);

        List<DialogueLine> shownDialogues = GetPrivateField<List<DialogueLine>>(dialoguePanel, "_dialogues");
        Assert.NotNull(shownDialogues);
        Assert.Greater(shownDialogues.Count, 0);
        StringAssert.Contains("周一早上 8:50", shownDialogues[0].text);

        storyManager.OnDailyIntroComplete();
        Assert.AreEqual(StoryFlowStage.Prologue, storyManager.CurrentFlowStage);
        shownDialogues = GetPrivateField<List<DialogueLine>>(dialoguePanel, "_dialogues");
        StringAssert.Contains("周一早上 9:00", shownDialogues[0].text);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreDailyIntroCheckpointForProject3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 1);
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.DailyIntro;
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.DailyIntro, storyManager.CurrentFlowStage);
        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        List<DialogueLine> shownDialogues = GetPrivateField<List<DialogueLine>>(dialoguePanel, "_dialogues");
        StringAssert.Contains("周一早上 8:50", shownDialogues[0].text);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestorePrologueCheckpointAfterDailyIntroForProject3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 1);
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.Prologue;
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.Prologue, storyManager.CurrentFlowStage);
        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        List<DialogueLine> shownDialogues = GetPrivateField<List<DialogueLine>>(dialoguePanel, "_dialogues");
        StringAssert.Contains("周一早上 9:00", shownDialogues[0].text);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestorePostDecisionCheckpointForProject3()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 11);
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.PostDecision;
        gameManager.CurrentPlayerData.savedDecisionStepIndex = 1;
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.PostDecision, storyManager.CurrentFlowStage);
        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        List<DialogueLine> shownDialogues = GetPrivateField<List<DialogueLine>>(dialoguePanel, "_dialogues");
        StringAssert.Contains("机房", shownDialogues[0].text);
    }

    [Test]
    public void StoryManager_HandleGameSceneLoadedShouldRestoreScheduleCheckpointWithSavedSelection()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 9);
        SchedulePanel schedulePanel = CreateComponent<SchedulePanel>("SchedulePanel");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        gameManager.CurrentPlayerData.savedFlowStage = StoryFlowStage.Schedule;
        gameManager.CurrentPlayerData.savedDecisionStepIndex = 1;
        gameManager.CurrentPlayerData.savedScheduleTaskNames = new List<string> { "开会", "资源协调", "开会" };
        SetPrivateField(gameManager, "_currentState", GameState.Playing);

        storyManager.HandleGameSceneLoaded();

        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);
        Assert.IsTrue(schedulePanel.gameObject.activeSelf);

        List<DailyTaskData> selectedTasks = GetPrivateField<List<DailyTaskData>>(schedulePanel, "_selectedTasks");
        Assert.AreEqual(3, selectedTasks.Count);
        Assert.AreEqual("开会", selectedTasks[0].name);
        Assert.AreEqual("资源协调", selectedTasks[1].name);
        Assert.AreEqual("开会", selectedTasks[2].name);
    }

    [Test]
    public void GameManager_ShouldCalculateProject3QualityBreakdown()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 3,
            currentWeek = 12,
            aiTrustRecords = new List<AITrustRecord>
            {
                new AITrustRecord { eventId = "g1", projectNumber = 3, weekNumber = 1, aiQuality = "good", hasViewed = true, isFollowed = true },
                new AITrustRecord { eventId = "g2", projectNumber = 3, weekNumber = 2, aiQuality = "good", hasViewed = true, isFollowed = false },
                new AITrustRecord { eventId = "n1", projectNumber = 3, weekNumber = 3, aiQuality = "neutral", hasViewed = true, isFollowed = true },
                new AITrustRecord { eventId = "b1", projectNumber = 3, weekNumber = 4, aiQuality = "bad", hasViewed = false, isFollowed = false },
                new AITrustRecord { eventId = "b2", projectNumber = 3, weekNumber = 5, aiQuality = "bad", hasViewed = true, isFollowed = true }
            }
        });

        Assert.AreEqual(5, gameManager.GetTotalDecisionCount(3));
        Assert.AreEqual(4, gameManager.GetAIViewedCountByProject(3));
        Assert.AreEqual(3, gameManager.GetAIFollowedCountByProject(3));
        Assert.AreEqual(2, gameManager.GetAIRecordCountByQuality("good"));
        Assert.AreEqual(1, gameManager.GetAIFollowedCountByQuality("good"));
        Assert.AreEqual(2, gameManager.GetAIRecordCountByQuality("bad"));
        Assert.AreEqual(1, gameManager.GetAIFollowedCountByQuality("bad"));
        Assert.AreEqual(0.5f, gameManager.GetAIAdoptionRateByQuality("good"), 0.001f);
        Assert.AreEqual(0.5f, gameManager.GetAIAdoptionRateByQuality("bad"), 0.001f);
    }

    [Test]
    public void GameSummaryPanel_ShowSummaryShouldDisplayJourneyAndQualityBreakdown()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 3,
            currentWeek = 12,
            techPower = 120,
            commPower = 110,
            managePower = 130,
            stressPower = 105,
            totalQuizAnswered = 7,
            totalQuizCorrect = 5,
            aiTrustRecords = new List<AITrustRecord>
            {
                new AITrustRecord { eventId = "p1", projectNumber = 1, weekNumber = 1, hasViewed = true, isFollowed = true },
                new AITrustRecord { eventId = "p2", projectNumber = 2, weekNumber = 1, hasViewed = true, isFollowed = false },
                new AITrustRecord { eventId = "g1", projectNumber = 3, weekNumber = 1, aiQuality = "good", hasViewed = true, isFollowed = true },
                new AITrustRecord { eventId = "n1", projectNumber = 3, weekNumber = 2, aiQuality = "neutral", hasViewed = false, isFollowed = false },
                new AITrustRecord { eventId = "b1", projectNumber = 3, weekNumber = 3, aiQuality = "bad", hasViewed = true, isFollowed = false }
            }
        });

        GameSummaryPanel panel = CreateComponent<GameSummaryPanel>("GameSummaryPanel");
        panel.ShowSummary(new EndingResultData
        {
            title = "优秀项目经理",
            description = "项目成功，年终奖翻倍。",
            grade = "excellent"
        });

        TMP_Text journeyText = GetPrivateField<TMP_Text>(panel, "_journeyText");
        TMP_Text overallRateText = GetPrivateField<TMP_Text>(panel, "_overallRateText");
        Transform qualityRatesRoot = GetPrivateField<Transform>(panel, "_qualityRatesRoot");
        TMP_Text badQualityValue = qualityRatesRoot.Find("BadQualityRow/ValueText").GetComponent<TMP_Text>();

        StringAssert.Contains("已完成周数：33", journeyText.text);
        StringAssert.Contains("总决策数：5", journeyText.text);
        StringAssert.Contains("答题次数：7", journeyText.text);
        StringAssert.Contains("答对题数：5", journeyText.text);
        StringAssert.Contains("查看建议：4/5", overallRateText.text);
        StringAssert.Contains("跟随建议：2/5", overallRateText.text);
        StringAssert.Contains("0/1（0%）", badQualityValue.text);
    }

    [Test]
    public void StoryManager_EndProjectShouldRouteProject3ToGameSummaryPanel()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject3GameManager(dataManager, 12, 150);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        GameSummaryPanel summaryPanel = CreateComponent<GameSummaryPanel>("GameSummaryPanel");

        storyManager.EndProject();

        Assert.AreEqual(StoryFlowStage.Summary, storyManager.CurrentFlowStage);
        Assert.AreEqual(StoryFlowStage.Summary, gameManager.CurrentPlayerData.savedFlowStage);
        Assert.IsTrue(summaryPanel.gameObject.activeSelf);
    }

    [Test]
    public void Project2ToProject3SummaryFlow_ShouldReachGameSummaryPanel()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        CreateComponent<UIManager>("UIManager");
        GameManager gameManager = CreateProject2GameManager(dataManager, 12, 320, 35);
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        CreateComponent<TransitionPanel>("TransitionPanel");
        GameSummaryPanel summaryPanel = CreateComponent<GameSummaryPanel>("GameSummaryPanel");

        storyManager.ContinueToNextProjectFromEnding();
        storyManager.StartCurrentProjectFromTransition();

        gameManager.CurrentPlayerData.currentWeek = 12;
        gameManager.CurrentPlayerData.techPower = 160;
        gameManager.CurrentPlayerData.commPower = 150;
        gameManager.CurrentPlayerData.managePower = 170;
        gameManager.CurrentPlayerData.stressPower = 145;
        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(3));

        storyManager.EndProject();

        Assert.AreEqual(3, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(StoryFlowStage.Summary, storyManager.CurrentFlowStage);
        Assert.IsTrue(summaryPanel.gameObject.activeSelf);
    }

    [Test]
    public void GameScene_ShouldContainGameSummaryPanelShell()
    {
#if UNITY_EDITOR
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
#endif

        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/TitleText"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/DetailsScroll/Viewport/Content/EndingSection/EndingTitleText"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/DetailsScroll/Viewport/Content/StatsSection/StatsRoot/TechRow"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/DetailsScroll/Viewport/Content/JourneySection/JourneyText"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/DetailsScroll/Viewport/Content/AIAnalysisSection/ProjectRatesRoot/Project3Row"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/DetailsScroll/Viewport/Content/AIAnalysisSection/QualityRatesRoot/GoodQualityRow"));
        Assert.NotNull(FindInOpenScene("GameCanvas/PanelsRoot/GameSummaryPanel/PanelContent/ButtonRow/RestartButton"));
    }

    private GameManager CreateProject3GameManager(DataManager dataManager, int weekNumber, int baseStatValue = 120)
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 3,
            currentWeek = weekNumber,
            energy = GameConstants.BASE_ENERGY_PER_WEEK,
            techPower = baseStatValue,
            commPower = baseStatValue,
            managePower = baseStatValue,
            stressPower = baseStatValue,
            aiTrustRecords = new List<AITrustRecord>(),
            eventFlags = new List<EventFlagRecord>(),
            savedScheduleTaskNames = new List<string>()
        });
        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(3));
        SetPrivateField(gameManager, "_currentState", GameState.Playing);
        return gameManager;
    }

    private GameManager CreateProject2GameManager(DataManager dataManager, int weekNumber, int baseStatValue, int initialHiddenRisk)
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 2,
            currentWeek = weekNumber,
            energy = GameConstants.BASE_ENERGY_PER_WEEK,
            techPower = baseStatValue,
            commPower = baseStatValue,
            managePower = baseStatValue,
            stressPower = baseStatValue,
            hiddenRisk = initialHiddenRisk,
            aiTrustRecords = new List<AITrustRecord>(),
            eventFlags = new List<EventFlagRecord>(),
            savedScheduleTaskNames = new List<string>()
        });
        SetPrivateField(gameManager, "_currentProjectStory", dataManager.LoadProjectStory(2));
        SetPrivateField(gameManager, "_currentState", GameState.Playing);
        return gameManager;
    }

    private T CreateComponent<T>(string name) where T : Component
    {
        GameObject gameObject = new GameObject(name);
        _createdObjects.Add(gameObject);
        return gameObject.AddComponent<T>();
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
