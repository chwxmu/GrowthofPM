using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private VerticalLayoutGroup _optionLayout;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _backButton;

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

        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(OnClickBack);
        }
    }

    public void ShowQuiz()
    {
        EnsureLayout();
        gameObject.SetActive(true);

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

        if (_backButton != null)
        {
            _backButton.gameObject.SetActive(true);
        }

        BuildOptions();
    }

    private void BindButtons()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnClickContinue);
            _continueButton.onClick.AddListener(OnClickContinue);
        }

        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(OnClickBack);
            _backButton.onClick.AddListener(OnClickBack);
        }
    }

    private void BuildOptions()
    {
        int optionCount = _currentQuestion != null && _currentQuestion.options != null ? _currentQuestion.options.Count : 0;
        EnsureOptionCount(optionCount);

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

    private void OnClickBack()
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
        label.font = FindSharedFont();
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = true;
        label.color = Color.white;

        _optionButtons.Add(button);
        _optionButtonImages.Add(image);
        _optionLabels.Add(label);
    }

    private void EnsureLayout()
    {
        if (_titleText != null && _questionText != null && _feedbackText != null && _optionLayout != null && _continueButton != null && _backButton != null)
        {
            return;
        }

        TMP_FontAsset sharedFont = FindSharedFont();
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
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", sharedFont, 32f, FontStyles.Bold, TextAlignmentOptions.Center, 56f);
        _questionText = EnsureText(contentRoot.transform, "QuestionText", sharedFont, 28f, FontStyles.Bold, TextAlignmentOptions.TopLeft, 120f);
        _feedbackText = EnsureText(contentRoot.transform, "FeedbackText", sharedFont, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 72f);

        Image feedbackImage = _feedbackText.GetComponent<Image>();
        if (feedbackImage == null)
        {
            feedbackImage = _feedbackText.gameObject.AddComponent<Image>();
        }
        feedbackImage.color = new Color32(36, 64, 48, 220);

        _optionLayout = EnsureList(contentRoot.transform, "OptionList");

        GameObject actionRoot = FindOrCreateChild(contentRoot, "ActionRow");
        HorizontalLayoutGroup actionLayout = actionRoot.GetComponent<HorizontalLayoutGroup>();
        if (actionLayout == null)
        {
            actionLayout = actionRoot.AddComponent<HorizontalLayoutGroup>();
        }
        actionLayout.spacing = 16f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = true;
        actionLayout.childForceExpandHeight = false;

        LayoutElement actionLayoutElement = actionRoot.GetComponent<LayoutElement>();
        if (actionLayoutElement == null)
        {
            actionLayoutElement = actionRoot.AddComponent<LayoutElement>();
        }
        actionLayoutElement.preferredHeight = 64f;
        actionLayoutElement.minHeight = 64f;

        _continueButton = EnsureButton(actionRoot.transform, "ContinueButton", sharedFont, "继续答题");
        _backButton = EnsureButton(actionRoot.transform, "BackButton", sharedFont, "返回日程");
        BindButtons();
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
        layoutElement.minHeight = 240f;

        VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = listObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 12f;
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

    private static TMP_FontAsset FindSharedFont()
    {
        TextMeshProUGUI existingText = FindObjectOfType<TextMeshProUGUI>(true);
        return existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
    }
}
