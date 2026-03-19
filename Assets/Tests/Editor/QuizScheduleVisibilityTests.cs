using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

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
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
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

        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
        DestroyAllOfType<StoryManager>();
        DestroyAllOfType<UIManager>();
        DestroyAllOfType<QuizPanel>();
        DestroyAllOfType<SchedulePanel>();
        DestroyAllOfType<DialoguePanel>();
        DestroyAllOfType<DecisionPanel>();
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
}
