using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuizPanel : MonoBehaviour
{
    private const float ContentSpacing = 10f;
    private const float QuestionBlockHeight = 98f;
    private const float FeedbackBlockHeight = 52f;
    private const float ActionRowHeight = 56f;
    private const float ContinueButtonWidth = 196f;
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private VerticalLayoutGroup _optionLayout;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private readonly List<Button> _optionButtons = new List<Button>();
    private readonly List<Image> _optionButtonImages = new List<Image>();
    private readonly List<TMP_Text> _optionLabels = new List<TMP_Text>();

    private QuizQuestionData _currentQuestion;
    private bool _answered;

    private void Awake()
    {
        EnsureLayout();
        BindButtons();
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnClickContinue);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickClose);
        }
    }

    public void ShowQuiz()
    {
        EnsureLayout();
        ApplyLayoutTuning();
        ApplyAllFonts();
        gameObject.SetActive(true);
        RestorePanelVisibility();

        List<QuizQuestionData> questions = DataManager.Instance != null ? DataManager.Instance.LoadQuizQuestions() : null;
        _currentQuestion = PickRandomQuestion(questions);
        _answered = false;

        if (_titleText != null)
        {
            _titleText.text = "答题挑战";
        }

        if (_questionText != null)
        {
            _questionText.text = _currentQuestion != null ? _currentQuestion.question : "暂无题目数据";
        }

        if (_feedbackText != null)
        {
            _feedbackText.text = string.Empty;
            _feedbackText.gameObject.SetActive(false);
        }

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(false);
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(true);
            _closeButton.interactable = true;
        }

        BuildOptions();
    }

    private void RestorePanelVisibility()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void BindButtons()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnClickContinue);
            _continueButton.onClick.AddListener(OnClickContinue);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickClose);
            _closeButton.onClick.AddListener(OnClickClose);
        }
    }

    private void BuildOptions()
    {
        int optionCount = _currentQuestion != null && _currentQuestion.options != null ? _currentQuestion.options.Count : 0;
        EnsureOptionCount(optionCount);
        TMP_FontAsset taskFont = ResolveUIFont();

        for (int i = 0; i < _optionButtons.Count; i += 1)
        {
            bool hasOption = i < optionCount && _currentQuestion.options[i] != null;
            Button button = _optionButtons[i];
            TMP_Text label = _optionLabels[i];
            Image image = _optionButtonImages[i];

            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(hasOption);
            button.onClick.RemoveAllListeners();

            if (!hasOption)
            {
                continue;
            }

            if (label != null)
            {
                if (taskFont != null)
                {
                    label.font = taskFont;
                }

                label.text = _currentQuestion.options[i];
            }

            if (image != null)
            {
                image.color = new Color32(42, 56, 84, 220);
            }

            button.interactable = !_answered;
            int capturedIndex = i;
            button.onClick.AddListener(() => OnClickOption(capturedIndex));
        }
    }

    private void OnClickOption(int selectedIndex)
    {
        if (_answered || _currentQuestion == null || _currentQuestion.options == null)
        {
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= _currentQuestion.options.Count)
        {
            return;
        }

        _answered = true;
        bool isCorrect = selectedIndex == _currentQuestion.correctIndex;

        for (int i = 0; i < _optionButtons.Count; i += 1)
        {
            Button button = _optionButtons[i];
            Image image = i < _optionButtonImages.Count ? _optionButtonImages[i] : null;
            if (button == null || !button.gameObject.activeSelf)
            {
                continue;
            }

            button.interactable = false;
            if (image == null)
            {
                continue;
            }

            if (i == _currentQuestion.correctIndex)
            {
                image.color = new Color32(64, 148, 88, 255);
            }
            else if (i == selectedIndex)
            {
                image.color = isCorrect ? new Color32(64, 148, 88, 255) : new Color32(168, 72, 72, 255);
            }
            else
            {
                image.color = new Color32(60, 68, 82, 220);
            }
        }

        if (isCorrect && GameManager.Instance != null)
        {
            GameManager.Instance.AddEnergy(GameConstants.QUIZ_ENERGY_REWARD);
        }

        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.text = BuildFeedbackText(isCorrect);
        }

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(true);
        }
    }

    private void OnClickContinue()
    {
        ShowQuiz();
    }

    private void OnClickClose()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CloseQuizAndReturn();
            return;
        }

        gameObject.SetActive(false);
    }

    private string BuildFeedbackText(bool isCorrect)
    {
        if (isCorrect)
        {
            return "回答正确，精力 +" + GameConstants.QUIZ_ENERGY_REWARD;
        }

        string correctAnswer = string.Empty;
        if (_currentQuestion != null && _currentQuestion.options != null && _currentQuestion.correctIndex >= 0 && _currentQuestion.correctIndex < _currentQuestion.options.Count)
        {
            correctAnswer = _currentQuestion.options[_currentQuestion.correctIndex];
        }

        return "回答错误，正确答案：" + correctAnswer;
    }

    private void EnsureOptionCount(int targetCount)
    {
        if (_optionLayout == null)
        {
            return;
        }

        while (_optionButtons.Count < targetCount)
        {
            CreateOptionButton(_optionButtons.Count);
        }
    }

    private void CreateOptionButton(int index)
    {
        GameObject buttonObject = new GameObject("OptionButton" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(_optionLayout.transform, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(42, 56, 84, 220);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = 88f;
        layoutElement.preferredHeight = 96f;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(230, 240, 255, 255);
        colors.pressedColor = new Color32(210, 225, 245, 255);
        colors.selectedColor = new Color32(230, 240, 255, 255);
        colors.disabledColor = Color.white;
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 12f);
        labelRect.offsetMax = new Vector2(-20f, -12f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = ResolveUIFont();
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = true;
        label.color = Color.white;

        _optionButtons.Add(button);
        _optionButtonImages.Add(image);
        _optionLabels.Add(label);
    }

    private bool IsLayoutReady()
    {
        return _titleText != null && _questionText != null && _feedbackText != null && _optionLayout != null && _continueButton != null && _closeButton != null;
    }

    private void EnsureLayout()
    {
        if (IsLayoutReady())
        {
            HideLegacyBackButton();
            ApplyLayoutTuning();
            ApplyAllFonts();
            return;
        }

        TMP_FontAsset sharedFont = ResolveUIFont();
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color32(10, 18, 28, 215);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.18f, 0.14f);
        contentRect.anchorMax = new Vector2(0.82f, 0.86f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentImage = contentRoot.GetComponent<Image>();
        if (contentImage == null)
        {
            contentImage = contentRoot.AddComponent<Image>();
        }
        contentImage.color = new Color32(26, 34, 54, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = ContentSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", sharedFont, 32f, FontStyles.Bold, TextAlignmentOptions.Center, 56f);
        _questionText = EnsureText(contentRoot.transform, "QuestionText", sharedFont, 28f, FontStyles.Bold, TextAlignmentOptions.TopLeft, QuestionBlockHeight);
        _feedbackText = EnsureText(contentRoot.transform, "FeedbackText", sharedFont, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, FeedbackBlockHeight);

        if (_feedbackText != null)
        {
            _feedbackText.color = new Color32(210, 240, 220, 255);
        }

        _optionLayout = EnsureList(contentRoot.transform, "OptionList");

        GameObject actionRoot = FindOrCreateChild(contentRoot, "ActionRow");
        HorizontalLayoutGroup actionLayout = actionRoot.GetComponent<HorizontalLayoutGroup>();
        if (actionLayout == null)
        {
            actionLayout = actionRoot.AddComponent<HorizontalLayoutGroup>();
        }
        actionLayout.spacing = 0f;
        actionLayout.childAlignment = TextAnchor.MiddleRight;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;

        LayoutElement actionLayoutElement = actionRoot.GetComponent<LayoutElement>();
        if (actionLayoutElement == null)
        {
            actionLayoutElement = actionRoot.AddComponent<LayoutElement>();
        }
        actionLayoutElement.preferredHeight = ActionRowHeight;
        actionLayoutElement.minHeight = ActionRowHeight;

        _continueButton = EnsureButton(actionRoot.transform, "ContinueButton", sharedFont, "继续答题");
        LayoutElement continueLayout = _continueButton.GetComponent<LayoutElement>();
        if (continueLayout != null)
        {
            continueLayout.flexibleWidth = 0f;
            continueLayout.minWidth = ContinueButtonWidth;
            continueLayout.preferredWidth = ContinueButtonWidth;
        }

        _closeButton = EnsureCornerButton(contentRoot.transform, "CloseButton", sharedFont, "关闭");

        HideLegacyBackButton();
        ApplyLayoutTuning();
        ApplyAllFonts();
        BindButtons();
    }

    private void ApplyLayoutTuning()
    {
        Transform contentRoot = transform.Find("PanelContent");
        if (contentRoot == null)
        {
            return;
        }

        VerticalLayoutGroup contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (contentLayout != null)
        {
            contentLayout.spacing = ContentSpacing;
        }

        ApplyTextBlockHeight(_questionText, QuestionBlockHeight);
        ApplyTextBlockHeight(_feedbackText, FeedbackBlockHeight);

        if (_questionText != null)
        {
            _questionText.margin = new Vector4(16f, 8f, 16f, 8f);
        }

        if (_feedbackText != null)
        {
            _feedbackText.margin = new Vector4(16f, 6f, 16f, 6f);
        }

        if (_optionLayout != null)
        {
            _optionLayout.spacing = 10f;
        }

        Transform actionRoot = contentRoot.Find("ActionRow");
        if (actionRoot != null)
        {
            HorizontalLayoutGroup actionLayout = actionRoot.GetComponent<HorizontalLayoutGroup>();
            if (actionLayout != null)
            {
                actionLayout.spacing = 0f;
                actionLayout.childAlignment = TextAnchor.MiddleRight;
                actionLayout.childControlWidth = true;
                actionLayout.childForceExpandWidth = false;
            }

            LayoutElement actionLayoutElement = actionRoot.GetComponent<LayoutElement>();
            if (actionLayoutElement != null)
            {
                actionLayoutElement.preferredHeight = ActionRowHeight;
                actionLayoutElement.minHeight = ActionRowHeight;
            }
        }

        if (_continueButton != null)
        {
            LayoutElement continueLayout = _continueButton.GetComponent<LayoutElement>();
            if (continueLayout != null)
            {
                continueLayout.flexibleWidth = 0f;
                continueLayout.minWidth = ContinueButtonWidth;
                continueLayout.preferredWidth = ContinueButtonWidth;
            }
        }
    }

    private static void ApplyTextBlockHeight(TMP_Text text, float blockHeight)
    {
        if (text == null)
        {
            return;
        }

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            return;
        }

        layoutElement.preferredHeight = blockHeight;
        layoutElement.minHeight = blockHeight;
    }

    private void HideLegacyBackButton()
    {
        Transform actionRoot = transform.Find("PanelContent/ActionRow");
        if (actionRoot == null)
        {
            return;
        }

        Transform legacyBackButton = actionRoot.Find("BackButton");
        if (legacyBackButton != null)
        {
            legacyBackButton.gameObject.SetActive(false);
        }
    }

    private void ApplyAllFonts()
    {
        TMP_FontAsset sharedFont = ResolveUIFont();
        if (sharedFont == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.font = sharedFont;
        }

        if (_questionText != null)
        {
            _questionText.font = sharedFont;
        }

        if (_feedbackText != null)
        {
            _feedbackText.font = sharedFont;
        }

        for (int i = 0; i < _optionLabels.Count; i += 1)
        {
            TMP_Text optionLabel = _optionLabels[i];
            if (optionLabel != null)
            {
                optionLabel.font = sharedFont;
            }
        }

        ApplyButtonLabelFont(_continueButton, sharedFont);
        ApplyButtonLabelFont(_closeButton, sharedFont);
    }

    private static QuizQuestionData PickRandomQuestion(List<QuizQuestionData> questions)
    {
        if (questions == null || questions.Count == 0)
        {
            return null;
        }

        List<QuizQuestionData> validQuestions = new List<QuizQuestionData>();
        foreach (QuizQuestionData question in questions)
        {
            if (question == null || string.IsNullOrWhiteSpace(question.question) || question.options == null || question.options.Count == 0)
            {
                continue;
            }

            if (question.correctIndex < 0 || question.correctIndex >= question.options.Count)
            {
                continue;
            }

            validQuestions.Add(question);
        }

        if (validQuestions.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, validQuestions.Count);
        return validQuestions[randomIndex];
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, float preferredHeight)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
        {
            textObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = textObject.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.color = Color.white;
        text.margin = new Vector4(16f, 12f, 16f, 12f);
        return text;
    }

    private static VerticalLayoutGroup EnsureList(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject listObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        if (existing == null)
        {
            listObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = listObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = listObject.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 220f;

        VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = listObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static Button EnsureButton(Transform parent, string name, TMP_FontAsset font, string buttonText)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
        }

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(69, 114, 206, 255);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 56f;
        layoutElement.preferredHeight = 56f;
        layoutElement.flexibleWidth = 1f;

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = FindOrCreateChild(buttonObject, "Label");
        RectTransform labelRect = EnsureRectTransform(labelObject);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }
        if (font != null)
        {
            label.font = font;
        }

        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = buttonText;
        return button;
    }

    private static Button EnsureCornerButton(Transform parent, string name, TMP_FontAsset font, string labelText)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
        }

        RectTransform rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(96f, 44f);
        rect.anchoredPosition = new Vector2(-10f, -10f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(114, 62, 62, 255);

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = FindOrCreateChild(buttonObject, "Label");
        RectTransform labelRect = EnsureRectTransform(labelObject);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }
        if (font != null)
        {
            label.font = font;
        }

        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = labelText;
        return button;
    }

    private static void ApplyButtonLabelFont(Button button, TMP_FontAsset font)
    {
        if (button == null || font == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.font = font;
        }
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent.transform.Find(childName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = target.AddComponent<RectTransform>();
        }

        return rectTransform;
    }

    private TMP_FontAsset ResolveUIFont()
    {
        if (_preferredChineseFont != null)
        {
            return _preferredChineseFont;
        }

#if UNITY_EDITOR
        _preferredChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SimsunFontAssetPath);
        if (_preferredChineseFont != null)
        {
            return _preferredChineseFont;
        }
#endif

        Debug.LogError("[QuizPanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");

        return TMP_Settings.defaultFontAsset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_preferredChineseFont == null)
        {
            _preferredChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SimsunFontAssetPath);
        }
    }
#endif
}
