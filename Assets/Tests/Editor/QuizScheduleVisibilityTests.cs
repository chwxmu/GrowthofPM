using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizScheduleVisibilityTests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<AIAdvisor>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
        DestroyAllOfType<MenuSceneController>();
        EnsureTmpFontHost();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i -= 1)
        {
            if (_createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }
        }

        _createdObjects.Clear();

        DataManager[] existingDataManagers = UnityEngine.Object.FindObjectsOfType<DataManager>(true);
        if (existingDataManagers.Length > 0 && existingDataManagers[0] != null)
        {
            existingDataManagers[0].DeleteSave();
        }

        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<AIAdvisor>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
        DestroyAllOfType<MenuSceneController>();
    }

    [Test]
    public void OpenQuizFromSchedule_ShouldRestoreQuizPanelVisibility()
    {
        CreateComponent<DataManager>("DataManager");
        CreateComponent<GameManager>("GameManager");
        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        QuizPanel quizPanel = CreateComponent<QuizPanel>("QuizPanel");

        uiManager.RegisterPanel("QuizPanel", quizPanel.gameObject);

        CanvasGroup group = quizPanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        quizPanel.gameObject.SetActive(false);

        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Schedule);

        storyManager.OpenQuizFromSchedule();

        Assert.IsTrue(quizPanel.gameObject.activeSelf);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
        Assert.AreEqual(StoryFlowStage.Quiz, storyManager.CurrentFlowStage);
    }

    [Test]
    public void CloseQuizAndReturn_ShouldReopenScheduleAndReleaseQuizRaycasts()
    {
        UIManager uiManager = CreateComponent<UIManager>("UIManager");
        StoryManager storyManager = CreateComponent<StoryManager>("StoryManager");
        SchedulePanel schedulePanel = CreateComponent<SchedulePanel>("SchedulePanel");
        QuizPanel quizPanel = CreateComponent<QuizPanel>("QuizPanel");

        uiManager.RegisterPanel("SchedulePanel", schedulePanel.gameObject);
        uiManager.RegisterPanel("QuizPanel", quizPanel.gameObject);

        schedulePanel.ShowSchedule(new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "开会",
                energyCost = 40,
                effects = new StatEffects { managePower = 1 }
            }
        }, 100, _ => { });

        CanvasGroup scheduleGroup = schedulePanel.gameObject.AddComponent<CanvasGroup>();
        scheduleGroup.alpha = 0f;
        scheduleGroup.interactable = false;
        scheduleGroup.blocksRaycasts = false;
        schedulePanel.gameObject.SetActive(false);

        CanvasGroup quizGroup = quizPanel.gameObject.AddComponent<CanvasGroup>();
        quizGroup.alpha = 1f;
        quizGroup.interactable = true;
        quizGroup.blocksRaycasts = true;
        quizPanel.gameObject.SetActive(true);

        SetPrivateField(storyManager, "_quizOpenRequestedFromSchedule", true);
        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Quiz);

        storyManager.CloseQuizAndReturn();

        Assert.IsTrue(schedulePanel.gameObject.activeSelf);
        Assert.AreEqual(1f, scheduleGroup.alpha);
        Assert.IsTrue(scheduleGroup.interactable);
        Assert.IsTrue(scheduleGroup.blocksRaycasts);
        Assert.IsFalse(quizGroup.interactable);
        Assert.IsFalse(quizGroup.blocksRaycasts);
        Assert.AreEqual(StoryFlowStage.Schedule, storyManager.CurrentFlowStage);
    }

    [Test]
    public void CloseQuizAndReturn_ShouldSyncScheduleEnergyWithQuizReward()
    {
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
        QuizPanel quizPanel = CreateComponent<QuizPanel>("QuizPanel");

        uiManager.RegisterPanel("SchedulePanel", schedulePanel.gameObject);
        uiManager.RegisterPanel("QuizPanel", quizPanel.gameObject);

        schedulePanel.ShowSchedule(new List<DailyTaskData>
        {
            new DailyTaskData
            {
                name = "开会",
                energyCost = 40,
                effects = new StatEffects { managePower = 1 }
            }
        }, GameConstants.BASE_ENERGY_PER_WEEK, _ => { });

        InvokePrivate(schedulePanel, "OnClickAddTask", 0);
        gameManager.AddEnergy(GameConstants.QUIZ_ENERGY_REWARD);

        CanvasGroup scheduleGroup = schedulePanel.gameObject.AddComponent<CanvasGroup>();
        scheduleGroup.alpha = 0f;
        scheduleGroup.interactable = false;
        scheduleGroup.blocksRaycasts = false;
        schedulePanel.gameObject.SetActive(false);

        CanvasGroup quizGroup = quizPanel.gameObject.AddComponent<CanvasGroup>();
        quizGroup.alpha = 1f;
        quizGroup.interactable = true;
        quizGroup.blocksRaycasts = true;
        quizPanel.gameObject.SetActive(true);

        SetPrivateField(storyManager, "_quizOpenRequestedFromSchedule", true);
        SetPrivateField(storyManager, "_currentFlowStage", StoryFlowStage.Quiz);

        storyManager.CloseQuizAndReturn();

        Assert.IsTrue(schedulePanel.gameObject.activeSelf);
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK + GameConstants.QUIZ_ENERGY_REWARD, GetPrivateField<int>(schedulePanel, "_maxEnergy"));
        Assert.AreEqual(GameConstants.BASE_ENERGY_PER_WEEK + GameConstants.QUIZ_ENERGY_REWARD - 40, GetPrivateField<int>(schedulePanel, "_remainingEnergy"));
    }

    [Test]
    public void ShowDialogues_ShouldRestoreDialoguePanelVisibility()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        CanvasGroup group = dialoguePanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        dialoguePanel.gameObject.SetActive(false);

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "旁白",
                location = "会议室",
                text = "测试对话"
            }
        }, null);

        Assert.IsTrue(dialoguePanel.gameObject.activeSelf);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
    }

    [Test]
    public void ShowDialogues_ShouldExposeKeyboardAdvanceHint()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "旁白",
                location = "会议室",
                text = "测试对话"
            }
        }, null);

        TMP_Text hintText = GetPrivateField<TMP_Text>(dialoguePanel, "_hintText");

        Assert.AreEqual("点击 / 空格 继续", hintText.text);
    }

    [Test]
    public void AdvanceDialogueByShortcut_ShouldMoveToNextDialogue()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "旁白",
                location = "会议室",
                text = "第一句"
            },
            new DialogueLine
            {
                speaker = "小李",
                location = "办公室",
                text = "第二句"
            }
        }, null);

        SetPrivateField(dialoguePanel, "_isTyping", false);

        InvokePrivate(dialoguePanel, "AdvanceDialogueByShortcut");

        TMP_Text speakerText = GetPrivateField<TMP_Text>(dialoguePanel, "_speakerText");
        TMP_Text locationText = GetPrivateField<TMP_Text>(dialoguePanel, "_locationText");

        Assert.AreEqual("小李", speakerText.text);
        Assert.AreEqual("办公室", locationText.text);
    }

    [Test]
    public void ShowDialogues_ShouldRemovePortraitFrameDecoration()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "旁白",
                location = "会议室",
                text = "测试对话"
            }
        }, null);

        Image portraitFrame = GetPrivateField<Image>(dialoguePanel, "_portraitFrame");
        Image portraitImage = GetPrivateField<Image>(dialoguePanel, "_portraitImage");

        Assert.IsNotNull(portraitFrame);
        Assert.IsNotNull(portraitImage);
        Assert.AreEqual(0f, portraitFrame.color.a, 0.001f);
        Assert.IsNull(portraitFrame.GetComponent<Outline>());
        Assert.AreEqual(new Vector2(0.5f, 0f), portraitImage.rectTransform.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), portraitImage.rectTransform.anchorMax);
        Assert.AreEqual(new Vector2(0.5f, 0f), portraitImage.rectTransform.pivot);
        Assert.AreEqual(Vector2.zero, portraitImage.rectTransform.anchoredPosition);
    }

    [Test]
    public void ShowDialogues_ShouldShiftDialogueContentAndPortraitDown()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "朱诀",
                location = "会议室",
                text = "测试对话"
            }
        }, null);

        RectTransform contentRect = dialoguePanel.transform.Find("PanelContent") as RectTransform;
        Image portraitFrame = GetPrivateField<Image>(dialoguePanel, "_portraitFrame");

        Assert.IsNotNull(contentRect);
        Assert.IsNotNull(portraitFrame);
        Assert.AreEqual(0.03f, contentRect.anchorMin.y, 0.0001f);
        Assert.AreEqual(0.35f, contentRect.anchorMax.y, 0.0001f);
        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.anchorMin);
        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.anchorMax);
        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.pivot);
        Assert.AreEqual(new Vector2(-36f, 0f), portraitFrame.rectTransform.anchoredPosition);
        Assert.AreEqual(new Vector2(430f, 620f), portraitFrame.rectTransform.sizeDelta);
    }

    [Test]
    public void ShowDialogues_ShouldBottomAlignPortraitImageInsideFrame()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "朱诀",
                location = "会议室",
                text = "测试对话"
            }
        }, null);

        Image portraitFrame = GetPrivateField<Image>(dialoguePanel, "_portraitFrame");
        Image portraitImage = GetPrivateField<Image>(dialoguePanel, "_portraitImage");

        Assert.IsNotNull(portraitFrame);
        Assert.IsNotNull(portraitImage);
        Assert.AreEqual(0f, portraitFrame.rectTransform.anchorMin.y, 0.0001f);
        Assert.AreEqual(0f, portraitImage.rectTransform.anchorMin.y, 0.0001f);
        Assert.AreEqual(0f, portraitImage.rectTransform.anchorMax.y, 0.0001f);
        Assert.AreEqual(0f, portraitImage.rectTransform.pivot.y, 0.0001f);
        Assert.AreEqual(0f, portraitImage.rectTransform.anchoredPosition.y, 0.0001f);
        Assert.Greater(portraitImage.rectTransform.sizeDelta.x, 0f);
        Assert.Greater(portraitImage.rectTransform.sizeDelta.y, 0f);
    }

    [Test]
    public void ShowDialogues_ShouldRefreshPortraitLayoutWhenLayoutAlreadyExists()
    {
        DialoguePanel dialoguePanel = CreateComponent<DialoguePanel>("DialoguePanel");

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "朱诀",
                location = "会议室",
                text = "第一次显示"
            }
        }, null);

        Image portraitFrame = GetPrivateField<Image>(dialoguePanel, "_portraitFrame");
        Assert.IsNotNull(portraitFrame);

        portraitFrame.rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
        portraitFrame.rectTransform.anchorMax = new Vector2(0.96f, 0.86f);

        dialoguePanel.ShowDialogues(new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "朱诀",
                location = "深夜办公室",
                text = "第二次显示"
            }
        }, null);

        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.anchorMin);
        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.anchorMax);
        Assert.AreEqual(new Vector2(1f, 0f), portraitFrame.rectTransform.pivot);
        Assert.AreEqual(new Vector2(-36f, 0f), portraitFrame.rectTransform.anchoredPosition);
        Assert.AreEqual(new Vector2(430f, 620f), portraitFrame.rectTransform.sizeDelta);
    }

    [Test]
    public void RefreshContinueButton_ShouldDisableContinueAndShowNoSaveHint()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.DeleteSave();

        MenuSceneController controller = CreateComponent<MenuSceneController>("MenuSceneController");
        Button continueButton = CreateButtonWithLabel("ContinueGameButton", "继续游戏");
        SetPrivateField(controller, "_continueGameButton", continueButton);

        InvokePrivate(controller, "RefreshContinueButton");

        Assert.IsFalse(continueButton.interactable);
        Assert.AreEqual("继续游戏（无存档）", continueButton.GetComponentInChildren<TMP_Text>(true).text);
    }

    [Test]
    public void RefreshContinueButton_ShouldEnableContinueAndRestoreDefaultLabelWhenSaveExists()
    {
        DataManager dataManager = CreateComponent<DataManager>("DataManager");
        dataManager.SaveGame(new PlayerData
        {
            currentProject = 2,
            currentWeek = 5,
            aiTrustRecords = new List<AITrustRecord>()
        });

        MenuSceneController controller = CreateComponent<MenuSceneController>("MenuSceneController");
        Button continueButton = CreateButtonWithLabel("ContinueGameButton", "继续游戏（无存档）");
        continueButton.interactable = false;
        SetPrivateField(controller, "_continueGameButton", continueButton);

        InvokePrivate(controller, "RefreshContinueButton");

        Assert.IsTrue(continueButton.interactable);
        Assert.AreEqual("继续游戏", continueButton.GetComponentInChildren<TMP_Text>(true).text);
    }

    [Test]
    public void MenuSceneController_ShouldNotCreateRuntimeMenuVisuals()
    {
        GameObject menuCanvas = new GameObject("MenuCanvas", typeof(RectTransform));
        _createdObjects.Add(menuCanvas);

        MenuSceneController controller = menuCanvas.AddComponent<MenuSceneController>();

        GameObject backgroundPanel = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(Image));
        _createdObjects.Add(backgroundPanel);
        backgroundPanel.transform.SetParent(menuCanvas.transform, false);

        InvokePrivate(controller, "Awake");
        InvokePrivate(controller, "Start");

        Assert.IsNull(menuCanvas.transform.Find("HeroIllustration"));
        Assert.IsNull(backgroundPanel.transform.Find("BackgroundOverlay"));
        Assert.IsNull(backgroundPanel.GetComponent<Image>().sprite);
    }

    [Test]
    public void ShowDecision_ShouldRestoreDecisionPanelVisibility()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        CanvasGroup group = decisionPanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        decisionPanel.gameObject.SetActive(false);

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = "测试决策",
            aiAdvice = "建议优先保障质量",
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    effects = new StatEffects { techPower = 1 }
                }
            }
        }, (_, _, _, _) => { });

        Assert.IsTrue(decisionPanel.gameObject.activeSelf);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
    }

    [Test]
    public void ShowDecision_ShouldHideAIAdviceUntilPlayerClicksAdviceButton()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = "测试决策",
            aiAdvice = "建议优先保障质量",
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    effects = new StatEffects { techPower = 1 }
                }
            }
        }, (_, _, _, _) => { });

        Button aiAdviceButton = GetPrivateField<Button>(decisionPanel, "_aiAdviceButton");
        HorizontalLayoutGroup aiAdviceRow = GetPrivateField<HorizontalLayoutGroup>(decisionPanel, "_aiAdviceRow");
        TMP_Text aiAdviceText = GetPrivateField<TMP_Text>(decisionPanel, "_aiAdviceText");
        TMP_Text aiAdviceButtonLabel = aiAdviceButton.GetComponentInChildren<TMP_Text>(true);

        Assert.IsTrue(aiAdviceButton.gameObject.activeSelf);
        Assert.IsTrue(aiAdviceRow.gameObject.activeSelf);
        Assert.IsTrue(aiAdviceButton.interactable);
        Assert.IsFalse(aiAdviceText.gameObject.activeSelf);
        Assert.IsFalse(GetPrivateField<bool>(decisionPanel, "_hasViewedAiAdvice"));
        Assert.AreEqual("查看AI建议", aiAdviceButtonLabel.text);

        aiAdviceButton.onClick.Invoke();

        Assert.IsFalse(aiAdviceRow.gameObject.activeSelf);
        Assert.IsTrue(aiAdviceText.gameObject.activeSelf);
        Assert.IsTrue(GetPrivateField<bool>(decisionPanel, "_hasViewedAiAdvice"));
        StringAssert.Contains("建议优先保障质量", aiAdviceText.text);
    }

    [Test]
    public void ShowDecision_ShouldClampDescriptionAndAdviceHeights()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        string longDescription = string.Empty;
        string longAdvice = string.Empty;
        for (int i = 0; i < 30; i += 1)
        {
            longDescription += "这是一段很长的决策背景描述，用于验证描述区域不会占满整个决策面板。";
            longAdvice += "这是一段很长的AI建议内容，用于验证建议展开后的文本区域高度被合理限制。";
        }

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = longDescription,
            aiAdvice = longAdvice,
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    narrative = "测试结果"
                }
            }
        }, (_, _, _, _) => { });

        TMP_Text descriptionText = GetPrivateField<TMP_Text>(decisionPanel, "_descriptionText");
        LayoutElement descriptionLayout = descriptionText.GetComponent<LayoutElement>();
        Assert.LessOrEqual(descriptionLayout.preferredHeight, 96f);
        Assert.LessOrEqual(descriptionText.rectTransform.sizeDelta.y, 96.1f);

        Button aiAdviceButton = GetPrivateField<Button>(decisionPanel, "_aiAdviceButton");
        aiAdviceButton.onClick.Invoke();

        TMP_Text aiAdviceText = GetPrivateField<TMP_Text>(decisionPanel, "_aiAdviceText");
        LayoutElement aiAdviceLayout = aiAdviceText.GetComponent<LayoutElement>();
        Assert.LessOrEqual(aiAdviceLayout.preferredHeight, 112f);
        Assert.LessOrEqual(aiAdviceText.rectTransform.sizeDelta.y, 112.1f);
        Assert.AreEqual(TextAlignmentOptions.MidlineLeft, aiAdviceText.alignment);
    }

    [Test]
    public void OnClickOption_ShouldExpandFeedbackLayoutForLongNarrative()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = "测试决策",
            aiAdvice = "建议优先保障质量",
            aiRecommendedOption = 0,
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    narrative = "你选择了一条需要更多说明的方案。这段说明文本用于验证提示区域高度会随着内容增加而扩展，避免底部的AI采纳提示被后续选项区域遮挡。",
                    effects = new StatEffects { techPower = 3, managePower = 2 }
                }
            }
        }, (_, _, _, _) => { });

        Button aiAdviceButton = GetPrivateField<Button>(decisionPanel, "_aiAdviceButton");
        aiAdviceButton.onClick.Invoke();

        InvokePrivate(decisionPanel, "OnClickOption", 0);

        TMP_Text feedbackText = GetPrivateField<TMP_Text>(decisionPanel, "_feedbackText");
        LayoutElement feedbackLayout = feedbackText.GetComponent<LayoutElement>();

        Assert.IsTrue(feedbackText.gameObject.activeSelf);
        StringAssert.Contains("你选择了一条需要更多说明的方案", feedbackText.text);
        StringAssert.DoesNotContain("你采纳了", feedbackText.text);
        StringAssert.DoesNotContain("你没有采纳", feedbackText.text);
        StringAssert.DoesNotContain("技术力", feedbackText.text);
        StringAssert.DoesNotContain("管理力", feedbackText.text);
        Assert.AreEqual(100f, feedbackLayout.preferredHeight, 0.1f);
        Assert.AreEqual(100f, feedbackText.rectTransform.sizeDelta.y, 0.1f);
        Assert.AreEqual(TextAlignmentOptions.MidlineLeft, feedbackText.alignment);
    }

    [Test]
    public void OnClickOption_ShouldNotShowAdoptionPromptBeforeAdviceIsViewed()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = "测试决策",
            aiAdvice = "建议优先保障质量",
            aiRecommendedOption = 0,
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    narrative = "测试结果",
                    effects = new StatEffects { techPower = 1 }
                }
            }
        }, (_, _, _, _) => { });

        InvokePrivate(decisionPanel, "OnClickOption", 0);

        TMP_Text feedbackText = GetPrivateField<TMP_Text>(decisionPanel, "_feedbackText");
        Assert.AreEqual("测试结果", feedbackText.text);
        StringAssert.DoesNotContain("采纳了", feedbackText.text);
        StringAssert.DoesNotContain("没有采纳", feedbackText.text);
    }

    [Test]
    public void ShowDecision_ShouldRemoveOptionsHeaderRow()
    {
        DecisionPanel decisionPanel = CreateComponent<DecisionPanel>("DecisionPanel");

        decisionPanel.ShowDecision(new DecisionEventData
        {
            description = "测试决策",
            options = new List<OptionData>
            {
                new OptionData
                {
                    text = "按计划推进",
                    narrative = "测试结果"
                }
            }
        }, (_, _, _, _) => { });

        Transform contentRoot = decisionPanel.transform.Find("PanelContent");
        Assert.IsNotNull(contentRoot);
        Assert.IsNull(contentRoot.Find("Watermark"));
        Assert.IsNull(contentRoot.Find("OptionsHeaderRow"));
    }

    [Test]
    public void TopStatusBar_AwakeShouldHideSlidersAndCreateStatIcons()
    {
        TopStatusBar topStatusBar = CreateTopStatusBar();
        InvokePrivate(topStatusBar, "AutoBindIfNeeded");

        Assert.IsFalse(topStatusBar.transform.Find("TechSlider").gameObject.activeSelf);
        Assert.IsFalse(topStatusBar.transform.Find("CommSlider").gameObject.activeSelf);
        Assert.IsFalse(topStatusBar.transform.Find("ManageSlider").gameObject.activeSelf);
        Assert.IsFalse(topStatusBar.transform.Find("StressSlider").gameObject.activeSelf);
        Assert.IsFalse(topStatusBar.transform.Find("EnergySlider").gameObject.activeSelf);

        foreach (string iconName in new[] { "TechIcon", "CommIcon", "ManageIcon", "StressIcon", "EnergyIcon" })
        {
            Transform icon = topStatusBar.transform.Find(iconName);
            Assert.NotNull(icon, $"Missing icon: {iconName}");

            Image image = icon.GetComponent<Image>();
            Assert.NotNull(image);
            Assert.NotNull(image.sprite, $"Missing sprite on icon: {iconName}");
        }
    }

    [Test]
    public void TopStatusBar_UpdateDisplayShouldKeepValueOnlyStatTexts()
    {
        TopStatusBar topStatusBar = CreateTopStatusBar();
        InvokePrivate(topStatusBar, "AutoBindIfNeeded");

        topStatusBar.UpdateDisplay(new PlayerData
        {
            currentProject = 2,
            currentWeek = 7,
            techPower = 88,
            commPower = 77,
            managePower = 66,
            stressPower = 55,
            energy = 123,
            aiTrustRecords = new List<AITrustRecord>()
        });

        Assert.AreEqual("88", topStatusBar.transform.Find("TechValueText").GetComponent<TMP_Text>().text);
        Assert.AreEqual("77", topStatusBar.transform.Find("CommValueText").GetComponent<TMP_Text>().text);
        Assert.AreEqual("66", topStatusBar.transform.Find("ManageValueText").GetComponent<TMP_Text>().text);
        Assert.AreEqual("55", topStatusBar.transform.Find("StressValueText").GetComponent<TMP_Text>().text);
        Assert.AreEqual("123", topStatusBar.transform.Find("EnergyValueText").GetComponent<TMP_Text>().text);
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

    private T CreateComponent<T>(string name) where T : Component
    {
        GameObject gameObject = new GameObject(name);
        _createdObjects.Add(gameObject);
        return gameObject.AddComponent<T>();
    }

    private Button CreateButtonWithLabel(string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        _createdObjects.Add(buttonObject);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback != null)
        {
            labelText.font = fallback;
        }
        labelText.text = label;

        return buttonObject.GetComponent<Button>();
    }

    private TopStatusBar CreateTopStatusBar()
    {
        GameObject root = new GameObject("TopStatusBar", typeof(RectTransform), typeof(Image));
        _createdObjects.Add(root);

        CreateTextChild(root.transform, "ProjectInfoText", "项目信息");
        CreateTextChild(root.transform, "PhaseText", "阶段");
        CreateTextChild(root.transform, "TechLabel", "技术力");
        CreateSliderChild(root.transform, "TechSlider");
        CreateTextChild(root.transform, "TechValueText", "0");
        CreateTextChild(root.transform, "CommLabel", "沟通力");
        CreateSliderChild(root.transform, "CommSlider");
        CreateTextChild(root.transform, "CommValueText", "0");
        CreateTextChild(root.transform, "ManageLabel", "管理力");
        CreateSliderChild(root.transform, "ManageSlider");
        CreateTextChild(root.transform, "ManageValueText", "0");
        CreateTextChild(root.transform, "StressLabel", "抗压力");
        CreateSliderChild(root.transform, "StressSlider");
        CreateTextChild(root.transform, "StressValueText", "0");
        CreateTextChild(root.transform, "EnergyLabel", "精力值");
        CreateSliderChild(root.transform, "EnergySlider");
        CreateTextChild(root.transform, "EnergyValueText", "0");
        CreateButtonChild(root.transform, "QuizButton", "答题");
        CreateButtonChild(root.transform, "ScheduleButton", "日程安排");

        return root.AddComponent<TopStatusBar>();
    }

    private TextMeshProUGUI CreateTextChild(Transform parent, string name, string text)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback != null)
        {
            textComponent.font = fallback;
        }

        textComponent.text = text;
        return textComponent;
    }

    private Slider CreateSliderChild(Transform parent, string name)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        return sliderObject.GetComponent<Slider>();
    }

    private Button CreateButtonChild(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback != null)
        {
            labelText.font = fallback;
        }

        labelText.text = label;
        return buttonObject.GetComponent<Button>();
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field not found: {fieldName}");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field not found: {fieldName}");
        return (T)field.GetValue(target);
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method not found: {methodName}");
        return method.Invoke(target, args);
    }
}
