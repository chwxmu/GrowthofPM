using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Phase2Flow7To12Tests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<EndingPanel>();
        DestroyAllOfType<TransitionPanel>();
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
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<EndingPanel>();
        DestroyAllOfType<TransitionPanel>();
    }

    [Test]
    public void SchedulePanel_ShouldHandleAddRemoveAndEnergyGuard()
    {
        SchedulePanel panel = CreateComponent<SchedulePanel>("SchedulePanel");
        List<DailyTaskData> tasks = new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "编码",
                energyCost = 60,
                effects = new StatEffects { techPower = 2 }
            },
            new DailyTaskData
            {
                name = "评审",
                energyCost = 80,
                effects = new StatEffects { managePower = 1 }
            }
        };

        panel.ShowSchedule(tasks, 100, _ => { });

        InvokePrivate(panel, "OnClickAddTask", 0);
        List<DailyTaskData> selectedAfterAdd = GetPrivateField<List<DailyTaskData>>(panel, "_selectedTasks");
        TMP_Text energyText = GetPrivateField<TMP_Text>(panel, "_energyText");

        Assert.AreEqual(1, selectedAfterAdd.Count);
        StringAssert.Contains("40 / 100", energyText.text);

        InvokePrivate(panel, "OnClickAddTask", 1);
        List<DailyTaskData> selectedAfterBlockedAdd = GetPrivateField<List<DailyTaskData>>(panel, "_selectedTasks");

        Assert.AreEqual(1, selectedAfterBlockedAdd.Count);
        StringAssert.Contains("40 / 100", energyText.text);

        InvokePrivate(panel, "OnClickRemoveTask", 0);
        List<DailyTaskData> selectedAfterRemove = GetPrivateField<List<DailyTaskData>>(panel, "_selectedTasks");

        Assert.AreEqual(0, selectedAfterRemove.Count);
        StringAssert.Contains("100 / 100", energyText.text);
    }

    [Test]
    public void SchedulePanel_ConfirmShouldReturnSelectedTasksCopy()
    {
        SchedulePanel panel = CreateComponent<SchedulePanel>("SchedulePanel");
        List<DailyTaskData> tasks = new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "需求梳理",
                energyCost = 30,
                effects = new StatEffects { commPower = 1 }
            }
        };

        List<DailyTaskData> confirmedTasks = null;
        panel.ShowSchedule(tasks, 100, selected => confirmedTasks = selected);

        InvokePrivate(panel, "OnClickAddTask", 0);
        InvokePrivate(panel, "OnClickConfirm");

        List<DailyTaskData> internalSelected = GetPrivateField<List<DailyTaskData>>(panel, "_selectedTasks");
        Assert.NotNull(confirmedTasks);
        Assert.AreEqual(1, confirmedTasks.Count);
        Assert.AreNotSame(internalSelected, confirmedTasks);
    }

    [Test]
    public void SchedulePanel_ShowScheduleShouldReuseExistingTaskWidgetsAfterCacheLoss()
    {
        SchedulePanel panel = CreateComponent<SchedulePanel>("SchedulePanel");
        List<DailyTaskData> tasks = new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "开会",
                energyCost = 40,
                effects = new StatEffects { managePower = 1 }
            },
            new DailyTaskData
            {
                name = "写周报",
                energyCost = 30,
                effects = new StatEffects { commPower = 1 }
            }
        };

        panel.ShowSchedule(tasks, 100, _ => { });

        VerticalLayoutGroup availableLayout = GetPrivateField<VerticalLayoutGroup>(panel, "_availableTaskLayout");
        Assert.AreEqual(tasks.Count, availableLayout.transform.childCount);

        GetPrivateField<List<Button>>(panel, "_availableButtons").Clear();
        panel.ShowSchedule(tasks, 100, _ => { });

        GetPrivateField<List<Button>>(panel, "_availableButtons").Clear();
        panel.ShowSchedule(tasks, 100, _ => { });

        Assert.AreEqual(tasks.Count, availableLayout.transform.childCount);
        Assert.AreEqual(tasks.Count, CountActiveChildren(availableLayout.transform));
    }

    [Test]
    public void StoryManager_CanOpenQuizShouldMatchFlowStage()
    {
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");

        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Schedule);
        Assert.IsTrue(storyManager.CanOpenQuiz());

        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Decision);
        Assert.IsFalse(storyManager.CanOpenQuiz());

        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Quiz);
        Assert.IsTrue(storyManager.CanOpenQuiz());
    }

    [Test]
    public void StoryManager_OpenScheduleFromTopBarShouldResetSelectionAfterWeekAdvance()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = 1,
            energy = GameConstants.BASE_ENERGY_PER_WEEK,
            aiTrustRecords = new List<AITrustRecord>()
        });

        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SchedulePanel schedulePanel = CreateComponent<SchedulePanel>("SchedulePanel");
        uiManager.RegisterPanel("SchedulePanel", schedulePanel.gameObject);

        schedulePanel.ShowSchedule(new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "周例会",
                energyCost = 60,
                effects = new StatEffects { managePower = 1 }
            }
        }, GameConstants.BASE_ENERGY_PER_WEEK, _ => { });

        InvokePrivate(schedulePanel, "OnClickAddTask", 0);
        Assert.AreEqual(1, GetPrivateField<List<DailyTaskData>>(schedulePanel, "_selectedTasks").Count);

        gameManager.SetCurrentWeek(2);
        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Schedule);

        storyManager.OpenScheduleFromTopBar();

        Assert.AreEqual(0, GetPrivateField<List<DailyTaskData>>(schedulePanel, "_selectedTasks").Count);
    }

    [Test]
    public void StoryManager_SkipMainStoryShortcutShouldJumpToDecisionWhenAvailable()
    {
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SetPrivateField(storyManager, "_currentWeekEvent", new WeekEventData
        {
            decisionEvent = new DecisionEventData
            {
                eventId = "skip_main_story_test",
                description = "测试决策",
                options = new List<OptionData>
                {
                    new OptionData
                    {
                        text = "按计划推进",
                        effects = new StatEffects()
                    }
                }
            }
        });
        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Prologue);
        SetPrivateField(storyManager, "_decisionStepIndex", 0);

        storyManager.TrySkipWeekMainStoryToDecision();

        Assert.AreEqual(StoryFlowStage.Decision, storyManager.CurrentFlowStage);
        Assert.AreEqual(0, GetPrivateField<int>(storyManager, "_decisionStepIndex"));
    }

    [Test]
    public void StoryManager_SkipMainStoryShortcutShouldIgnoreNonMainStoryStage()
    {
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SetPrivateField(storyManager, "_currentWeekEvent", new WeekEventData
        {
            decisionEvent = new DecisionEventData
            {
                options = new List<OptionData>
                {
                    new OptionData
                    {
                        text = "测试选项",
                        effects = new StatEffects()
                    }
                }
            }
        });
        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Schedule);
        SetPrivateField(storyManager, "_decisionStepIndex", 1);

        storyManager.TrySkipWeekMainStoryToDecision();

        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);
        Assert.AreEqual(1, GetPrivateField<int>(storyManager, "_decisionStepIndex"));
    }

    [Test]
    public void Singleton_InstanceShouldRecoverAfterApplicationQuitFlagInEditMode()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");

        Assert.NotNull(GameManager.Instance);
        Assert.AreSame(gameManager, GameManager.Instance);

        FieldInfo shuttingDownFlag = typeof(Singleton<GameManager>).GetField("_isShuttingDown", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(shuttingDownFlag, "Field not found: _isShuttingDown");
        shuttingDownFlag.SetValue(null, true);

        GameManager recoveredInstance = GameManager.Instance;
        Assert.NotNull(recoveredInstance);
        Assert.AreSame(gameManager, recoveredInstance);
    }

    [Test]
    public void QuizPanel_CorrectAnswerShouldAddEnergy()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            energy = 100,
            aiTrustRecords = new List<AITrustRecord>()
        });

        QuizPanel panel = CreateComponent<QuizPanel>("QuizPanel");
        InvokePrivate(panel, "EnsureLayout");
        SetPrivateField(panel, "_currentQuestion", new QuizQuestionData
        {
            question = "敏捷回顾会通常用于什么？",
            options = new List<string> { "估算工期", "总结改进" },
            correctIndex = 1
        });
        SetPrivateField(panel, "_answered", false);

        InvokePrivate(panel, "BuildOptions");
        InvokePrivate(panel, "OnClickOption", 1);

        TMP_Text feedbackText = GetPrivateField<TMP_Text>(panel, "_feedbackText");
        Assert.AreEqual(110, gameManager.CurrentPlayerData.energy);
        Assert.IsTrue(feedbackText.gameObject.activeSelf);
        StringAssert.Contains("回答正确", feedbackText.text);
    }

    [Test]
    public void QuizPanel_WrongAnswerShouldNotAddEnergy()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            energy = 100,
            aiTrustRecords = new List<AITrustRecord>()
        });

        QuizPanel panel = CreateComponent<QuizPanel>("QuizPanel");
        InvokePrivate(panel, "EnsureLayout");
        SetPrivateField(panel, "_currentQuestion", new QuizQuestionData
        {
            question = "哪项是风险应对策略？",
            options = new List<string> { "祈祷", "规避" },
            correctIndex = 1
        });
        SetPrivateField(panel, "_answered", false);

        InvokePrivate(panel, "BuildOptions");
        InvokePrivate(panel, "OnClickOption", 0);

        TMP_Text feedbackText = GetPrivateField<TMP_Text>(panel, "_feedbackText");
        Assert.AreEqual(100, gameManager.CurrentPlayerData.energy);
        StringAssert.Contains("正确答案：规避", feedbackText.text);
    }

    [Test]
    public void StoryManager_OnScheduleCompleteShouldApplySettlementAndSave()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            techPower = 10,
            commPower = 10,
            managePower = 10,
            stressPower = 10,
            hiddenRisk = 0,
            energy = GameConstants.BASE_ENERGY_PER_WEEK,
            currentProject = 1,
            currentWeek = GameConstants.PROJECT1_WEEKS,
            aiTrustRecords = new List<AITrustRecord>()
        });

        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SetPrivateField(storyManager, "_currentWeekEvent", new WeekEventData
        {
            fixedStatChanges = new StatEffects
            {
                techPower = 2,
                commPower = 1
            },
            riskAutoChange = 4
        });

        List<DailyTaskData> selectedTasks = new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "任务A",
                energyCost = 120,
                effects = new StatEffects
                {
                    techPower = 3,
                    stressPower = 5
                }
            }
        };

        storyManager.OnScheduleComplete(selectedTasks);

        PlayerData current = gameManager.CurrentPlayerData;
        Assert.AreEqual(15, current.techPower);
        Assert.AreEqual(11, current.commPower);
        Assert.AreEqual(10, current.managePower);
        Assert.AreEqual(15, current.stressPower);
        Assert.AreEqual(4, current.hiddenRisk);
        Assert.AreEqual(180, current.energy);

        PlayerData saved = dataManager.LoadGame();
        Assert.NotNull(saved);
        Assert.AreEqual(15, saved.techPower);
        Assert.AreEqual(180, saved.energy);
    }

    [Test]
    public void StoryManager_AdvanceWeekShouldIncrementAndResetEnergyBeforeFinalWeek()
    {
        CreateComponent<DataManager>("DataManager");

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = 1,
            energy = 25,
            aiTrustRecords = new List<AITrustRecord>()
        });

        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        storyManager.AdvanceWeek();

        Assert.AreEqual(2, gameManager.CurrentPlayerData.currentWeek);
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK, gameManager.CurrentPlayerData.energy);
    }

    [Test]
    public void StoryManager_AdvanceWeekShouldEnterEndingAtFinalWeek()
    {
        CreateComponent<DataManager>("DataManager");

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = GameConstants.PROJECT1_WEEKS,
            aiTrustRecords = new List<AITrustRecord>()
        });

        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        storyManager.AdvanceWeek();

        Assert.AreEqual(StoryFlowStage.Ending, storyManager.CurrentFlowStage);
    }

    [Test]
    public void StoryManager_AdvanceWeekShouldRestoreEndingPanelVisibilityAtFinalWeek()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = GameConstants.PROJECT1_WEEKS,
            techPower = 100,
            commPower = 100,
            managePower = 100,
            stressPower = 100,
            aiTrustRecords = new List<AITrustRecord>()
        });

        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        EndingPanel endingPanel = CreateComponent<EndingPanel>("EndingPanel");
        uiManager.RegisterPanel("EndingPanel", endingPanel.gameObject);

        CanvasGroup group = endingPanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        endingPanel.gameObject.SetActive(true);

        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        storyManager.AdvanceWeek();

        Assert.AreEqual(StoryFlowStage.Ending, storyManager.CurrentFlowStage);
        Assert.IsTrue(endingPanel.gameObject.activeSelf);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
    }

    [Test]
    public void GameManager_EvaluateEndingShouldUseRiskFailThresholdForProject2()
    {
        CreateComponent<DataManager>("DataManager");

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 2,
            hiddenRisk = 999,
            techPower = 120,
            commPower = 120,
            managePower = 120,
            stressPower = 120,
            aiTrustRecords = new List<AITrustRecord>()
        });

        EndingResultData result = gameManager.EvaluateCurrentProjectEnding();
        Assert.NotNull(result);
        Assert.AreEqual("fail", result.grade);
    }

    [Test]
    public void EndingPanel_NextProjectButtonVisibilityShouldFollowRules()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        PlayerData playerData = new PlayerData
        {
            currentProject = 1,
            aiTrustRecords = new List<AITrustRecord>()
        };
        SetPrivateField(gameManager, "_currentPlayerData", playerData);

        EndingPanel panel = CreateComponent<EndingPanel>("EndingPanel");

        panel.ShowEnding(new EndingResultData
        {
            title = "失败",
            description = "未达目标",
            grade = "fail"
        });
        Button nextProjectButton = GetPrivateField<Button>(panel, "_nextProjectButton");
        Assert.IsFalse(nextProjectButton.gameObject.activeSelf);

        panel.ShowEnding(new EndingResultData
        {
            title = "通过",
            description = "达成目标",
            grade = "pass"
        });
        Assert.IsTrue(nextProjectButton.gameObject.activeSelf);

        playerData.currentProject = 3;
        panel.ShowEnding(new EndingResultData
        {
            title = "通过",
            description = "达成目标",
            grade = "pass"
        });
        Assert.IsFalse(nextProjectButton.gameObject.activeSelf);
    }

    [Test]
    public void EndingPanel_ShowEndingShouldPlaceLongDescriptionInsideScrollableArea()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            techPower = 120,
            commPower = 118,
            managePower = 115,
            stressPower = 110,
            aiTrustRecords = new List<AITrustRecord>()
        });

        EndingPanel panel = CreateComponent<EndingPanel>("EndingPanel");
        string longDescription = string.Join(string.Empty, new string[10].Select(_ => "这是一个很长的结局描述，用于验证结局界面的文本不会再和其他内容重叠。"));

        panel.ShowEnding(new EndingResultData
        {
            title = "结局标题",
            description = longDescription,
            grade = "pass"
        });

        ScrollRect detailsScrollRect = GetPrivateField<ScrollRect>(panel, "_detailsScrollRect");
        TMP_Text descriptionText = GetPrivateField<TMP_Text>(panel, "_descriptionText");
        LayoutElement descriptionLayout = descriptionText.GetComponent<LayoutElement>();

        Assert.NotNull(detailsScrollRect);
        Assert.AreSame(detailsScrollRect.content, descriptionText.transform.parent);
        Assert.Greater(descriptionLayout.preferredHeight, 220f);
    }

    [Test]
    public void GameManager_AdvanceToNextProjectShouldKeepStatsAndResetWeekState()
    {
        CreateComponent<DataManager>("DataManager");

        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            techPower = 77,
            commPower = 66,
            managePower = 55,
            stressPower = 44,
            currentProject = 1,
            currentWeek = 5,
            energy = 20,
            aiTrustRecords = new List<AITrustRecord>()
        });

        bool moved = gameManager.AdvanceToNextProject();
        PlayerData data = gameManager.CurrentPlayerData;

        Assert.IsTrue(moved);
        Assert.AreEqual(2, data.currentProject);
        Assert.AreEqual(1, data.currentWeek);
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK, data.energy);
        Assert.AreEqual(77, data.techPower);
        Assert.AreEqual(66, data.commPower);
        Assert.AreEqual(55, data.managePower);
        Assert.AreEqual(44, data.stressPower);
    }

    [Test]
    public void StoryManager_ContinueToNextProjectShouldRestoreTransitionPanelVisibility()
    {
        CreateComponent<DataManager>("DataManager");
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = 1,
            currentWeek = GameConstants.PROJECT1_WEEKS,
            techPower = 90,
            commPower = 92,
            managePower = 95,
            stressPower = 88,
            aiTrustRecords = new List<AITrustRecord>()
        });

        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        TransitionPanel transitionPanel = CreateComponent<TransitionPanel>("TransitionPanel");
        uiManager.RegisterPanel("TransitionPanel", transitionPanel.gameObject);

        CanvasGroup group = transitionPanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        transitionPanel.gameObject.SetActive(true);

        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        storyManager.ContinueToNextProjectFromEnding();

        Assert.AreEqual(StoryFlowStage.Transition, storyManager.CurrentFlowStage);
        Assert.AreEqual(2, gameManager.CurrentPlayerData.currentProject);
        Assert.AreEqual(1, gameManager.CurrentPlayerData.currentWeek);
        Assert.IsTrue(transitionPanel.gameObject.activeSelf);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
    }

    [Test]
    public void TransitionPanel_ShouldDisplayProjectInfoAndInheritedStats()
    {
        GameManager gameManager = CreateComponent<GameManager>("GameManager");
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            techPower = 66,
            commPower = 58,
            managePower = 62,
            stressPower = 49,
            aiTrustRecords = new List<AITrustRecord>()
        });

        TransitionPanel panel = CreateComponent<TransitionPanel>("TransitionPanel");
        panel.ShowTransition(new ProjectStoryData
        {
            projectName = "凤凰重构",
            totalWeeks = 12
        });

        TMP_Text titleText = GetPrivateField<TMP_Text>(panel, "_titleText");
        TMP_Text inheritanceText = GetPrivateField<TMP_Text>(panel, "_inheritanceText");

        StringAssert.Contains("凤凰重构", titleText.text);
        StringAssert.Contains("技术力：66", inheritanceText.text);
        StringAssert.Contains("沟通力：58", inheritanceText.text);
        StringAssert.Contains("管理力：62", inheritanceText.text);
        StringAssert.Contains("抗压力：49", inheritanceText.text);
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

    private static int CountActiveChildren(Transform parent)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i += 1)
        {
            if (parent.GetChild(i).gameObject.activeSelf)
            {
                count += 1;
            }
        }

        return count;
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

        Assert.Fail($"Method not found: {methodName}");
        return null;
    }
}


