using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DecisionPanel : MonoBehaviour
{
    private const float StatFloatMoveY = 46f;
    private const float StatFloatFadeInDuration = 0.15f;
    private const float StatFloatHoldDuration = 0.45f;
    private const float StatFloatFadeOutDuration = 0.25f;
    private const string SimsunFontName = "SIMSUN SDF";
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _aiAdviceText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private VerticalLayoutGroup _optionLayout;
    [SerializeField] private TMP_Text _floatingStatText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private readonly List<Button> _optionButtons = new List<Button>();
    private Action<int, bool, bool, int> _onOptionSelected;
    private DecisionEventData _currentEventData;
    private bool _selectionLocked;
    private bool _hasViewedAiAdvice;
    private bool _hasPendingSelection;
    private int _selectedOptionIndex;
    private bool _selectedFollowedAiAdvice;
    private int _selectedDecisionLatencyMs;
    private float _decisionShownTime;
    private Tween _floatingStatTween;

    private void Awake()
    {
        EnsureLayout();
        BindCloseButton();
    }

    private void OnDestroy()
    {
        _floatingStatTween?.Kill();

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickCloseSelection);
        }
    }

    public void ShowDecision(DecisionEventData eventData, Action<int, bool, bool, int> onOptionSelected)
    {
        EnsureLayout();

        _floatingStatTween?.Kill();
        _currentEventData = eventData;
        _onOptionSelected = onOptionSelected;
        _selectionLocked = false;
        _hasPendingSelection = false;
        _selectedOptionIndex = -1;
        _selectedFollowedAiAdvice = false;
        _selectedDecisionLatencyMs = 0;
        _hasViewedAiAdvice = _currentEventData != null && !string.IsNullOrWhiteSpace(_currentEventData.aiAdvice);
        _decisionShownTime = Time.realtimeSinceStartup;
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_descriptionText != null)
        {
            _descriptionText.text = _currentEventData != null ? _currentEventData.description : string.Empty;
        }

        if (_aiAdviceText != null)
        {
            _aiAdviceText.gameObject.SetActive(_hasViewedAiAdvice);
            _aiAdviceText.text = _hasViewedAiAdvice ? _currentEventData.aiAdvice : string.Empty;
        }

        if (_feedbackText != null)
        {
            _feedbackText.text = string.Empty;
            _feedbackText.gameObject.SetActive(false);
        }

        if (_floatingStatText != null)
        {
            _floatingStatText.text = string.Empty;
            _floatingStatText.gameObject.SetActive(false);
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(false);
            _closeButton.interactable = false;
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
    private void BuildOptions()
    {
        EnsureOptionCount(_currentEventData != null && _currentEventData.options != null ? _currentEventData.options.Count : 0);

        for (int i = 0; i < _optionButtons.Count; i += 1)
        {
            Button button = _optionButtons[i];
            if (button == null)
            {
                continue;
            }

            bool hasOption = _currentEventData != null
                && _currentEventData.options != null
                && i < _currentEventData.options.Count
                && _currentEventData.options[i] != null;

            button.gameObject.SetActive(hasOption);
            button.onClick.RemoveAllListeners();

            if (!hasOption)
            {
                continue;
            }

            OptionData option = _currentEventData.options[i];
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            bool isLocked = IsOptionLocked(option);

            if (label != null)
            {
                TMP_FontAsset optionFont = ResolveUIFont();
                if (optionFont != null)
                {
                    label.font = optionFont;
                }

                label.text = BuildOptionLabel(option, isLocked);
            }

            button.interactable = !_selectionLocked && !isLocked;

            if (!isLocked)
            {
                int capturedIndex = i;
                button.onClick.AddListener(() => OnClickOption(capturedIndex));
            }
        }
    }

    private void OnClickOption(int selectedIndex)
    {
        if (_selectionLocked || _currentEventData == null || _currentEventData.options == null || selectedIndex < 0 || selectedIndex >= _currentEventData.options.Count)
        {
            return;
        }

        OptionData selectedOption = _currentEventData.options[selectedIndex];
        if (selectedOption == null)
        {
            return;
        }

        _selectionLocked = true;
        _hasPendingSelection = true;
        _selectedOptionIndex = selectedIndex;
        _selectedFollowedAiAdvice = _currentEventData.aiRecommendedOption >= 0 && selectedIndex == _currentEventData.aiRecommendedOption;
        _selectedDecisionLatencyMs = Mathf.Max(0, Mathf.RoundToInt((Time.realtimeSinceStartup - _decisionShownTime) * 1000f));

        SetAllButtonsInteractable(false);
        ShowFeedback(selectedOption);
        PlayStatChangeFloatAnimation(selectedOption.effects);

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(true);
            _closeButton.interactable = true;
        }
    }

    private void OnClickCloseSelection()
    {
        if (!_hasPendingSelection || _selectedOptionIndex < 0)
        {
            return;
        }

        if (_closeButton != null)
        {
            _closeButton.interactable = false;
            _closeButton.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
        _hasPendingSelection = false;
        _onOptionSelected?.Invoke(_selectedOptionIndex, _hasViewedAiAdvice, _selectedFollowedAiAdvice, _selectedDecisionLatencyMs);
    }

    private void ShowFeedback(OptionData option)
    {
        if (_feedbackText == null)
        {
            return;
        }

        _feedbackText.gameObject.SetActive(true);
        _feedbackText.text = BuildFeedbackText(option);
    }

    private void PlayStatChangeFloatAnimation(StatEffects effects)
    {
        string statChangeText = BuildStatChangeText(effects);
        if (string.IsNullOrWhiteSpace(statChangeText))
        {
            return;
        }

        _floatingStatTween?.Kill();

        TMP_Text floatingText = EnsureFloatingStatText();
        if (floatingText == null)
        {
            return;
        }

        floatingText.gameObject.SetActive(true);
        floatingText.text = statChangeText;

        RectTransform rect = floatingText.transform as RectTransform;
        if (rect == null)
        {
            rect = floatingText.gameObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(0f, 52f);

        CanvasGroup canvasGroup = floatingText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = floatingText.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, StatFloatFadeInDuration));
        sequence.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + StatFloatMoveY, StatFloatFadeInDuration + StatFloatHoldDuration + StatFloatFadeOutDuration));
        sequence.AppendInterval(StatFloatHoldDuration);
        sequence.Append(canvasGroup.DOFade(0f, StatFloatFadeOutDuration));
        sequence.OnComplete(() =>
        {
            if (floatingText != null)
            {
                floatingText.gameObject.SetActive(false);
                floatingText.text = string.Empty;
            }
        });

        _floatingStatTween = sequence;
    }

    private TMP_Text EnsureFloatingStatText()
    {
        if (_floatingStatText != null)
        {
            return _floatingStatText;
        }

        if (_feedbackText == null)
        {
            return null;
        }

        Transform existing = _feedbackText.transform.Find("FloatingStatText");
        if (existing != null)
        {
            _floatingStatText = existing.GetComponent<TMP_Text>();
            return _floatingStatText;
        }

        GameObject textObject = new GameObject("FloatingStatText", typeof(RectTransform));
        textObject.transform.SetParent(_feedbackText.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveUIFont();
        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.richText = true;
        text.color = Color.white;

        _floatingStatText = text;
        return _floatingStatText;
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (Button button in _optionButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private static bool IsConditionConfigured(string statKey, int threshold)
    {
        return !string.IsNullOrWhiteSpace(statKey) && threshold > 0;
    }

    private bool IsOptionLocked(OptionData option)
    {
        if (option == null || !IsConditionConfigured(option.conditionStat, option.conditionThreshold))
        {
            return false;
        }

        int currentValue = GameManager.Instance != null ? GameManager.Instance.GetStatValue(option.conditionStat) : 0;
        return currentValue < option.conditionThreshold;
    }

    private string BuildOptionLabel(OptionData option, bool isLocked)
    {
        if (option == null)
        {
            return string.Empty;
        }

        string optionText = string.IsNullOrWhiteSpace(option.text) ? "未命名选项" : option.text;
        string effectPreviewText = BuildOptionEffectPreview(option);

        if (!isLocked)
        {
            return optionText + "\n<size=24><color=#9ED0FF>" + effectPreviewText + "</color></size>";
        }

        return optionText
            + "\n<size=24><color=#9ED0FF>" + effectPreviewText + "</color></size>"
            + "\n<size=24><color=#B0B0B0>锁定：" + GetLockReason(option) + "</color></size>";
    }

    private static string BuildOptionEffectPreview(OptionData option)
    {
        string statChangeText = BuildStatChangeText(option != null ? option.effects : null);
        if (string.IsNullOrWhiteSpace(statChangeText))
        {
            return "效果：无属性变化";
        }

        return "效果：" + statChangeText;
    }

    private static string GetLockReason(OptionData option)
    {
        if (option != null && !string.IsNullOrWhiteSpace(option.conditionDescription))
        {
            return option.conditionDescription.Trim();
        }

        string statName = GetStatDisplayName(option != null ? option.conditionStat : string.Empty);
        int threshold = option != null ? option.conditionThreshold : 0;
        return statName + "不足，需要≥" + threshold;
    }

    private string BuildFeedbackText(OptionData option)
    {
        if (option == null)
        {
            return string.Empty;
        }

        string narrative = string.IsNullOrWhiteSpace(option.narrative) ? string.Empty : option.narrative;
        string statChangeText = BuildStatChangeText(option.effects);

        if (string.IsNullOrWhiteSpace(narrative))
        {
            return statChangeText;
        }

        if (string.IsNullOrWhiteSpace(statChangeText))
        {
            return narrative;
        }

        return narrative + "\n\n" + statChangeText;
    }

    private static string BuildStatChangeText(StatEffects effects)
    {
        if (effects == null)
        {
            return string.Empty;
        }

        List<string> changes = new List<string>();
        AppendStatChange(changes, "技术力", effects.techPower);
        AppendStatChange(changes, "沟通力", effects.commPower);
        AppendStatChange(changes, "管理力", effects.managePower);
        AppendStatChange(changes, "抗压力", effects.stressPower);
        return string.Join("  ", changes);
    }

    private static void AppendStatChange(List<string> changes, string statName, int value)
    {
        if (value == 0)
        {
            return;
        }

        string color = value > 0 ? "#7CFF8A" : "#FF8A8A";
        string sign = value > 0 ? "+" : string.Empty;
        changes.Add("<color=" + color + ">" + statName + " " + sign + value + "</color>");
    }

    private static string GetStatDisplayName(string statKey)
    {
        switch (statKey)
        {
            case "techPower":
                return "技术力";
            case "commPower":
                return "沟通力";
            case "managePower":
                return "管理力";
            case "stressPower":
                return "抗压力";
            default:
                return "属性";
        }
    }

    private void EnsureOptionCount(int targetCount)
    {
        if (_optionLayout == null)
        {
            return;
        }

        while (_optionButtons.Count < targetCount)
        {
            _optionButtons.Add(CreateOptionButton(_optionButtons.Count));
        }
    }

    private Button CreateOptionButton(int index)
    {
        GameObject buttonObject = new GameObject("OptionButton" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(_optionLayout.transform, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(42, 56, 84, 220);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(230, 240, 255, 255);
        colors.pressedColor = new Color32(210, 225, 245, 255);
        colors.selectedColor = new Color32(230, 240, 255, 255);
        colors.disabledColor = new Color32(150, 150, 150, 180);
        button.colors = colors;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = 96f;
        layoutElement.preferredHeight = 120f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 18f);
        textRect.offsetMax = new Vector2(-24f, -18f);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = ResolveUIFont();
        label.fontSize = 28f;
        label.enableWordWrapping = true;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;

        return button;
    }

    private void EnsureLayout()
    {
        if (_descriptionText != null && _aiAdviceText != null && _feedbackText != null && _optionLayout != null && _closeButton != null)
        {
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
        background.color = new Color32(12, 18, 32, 220);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.15f, 0.12f);
        contentRect.anchorMax = new Vector2(0.85f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(26, 34, 54, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _descriptionText = EnsureText(contentRoot.transform, "DescriptionText", sharedFont, 30f, FontStyles.Bold, TextAlignmentOptions.TopLeft, 90f);
        _aiAdviceText = EnsureText(contentRoot.transform, "AIAdviceText", sharedFont, 28f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 120f);
        _feedbackText = EnsureText(contentRoot.transform, "FeedbackText", sharedFont, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 120f);

        GameObject optionsRoot = FindOrCreateChild(contentRoot, "OptionsRoot");
        LayoutElement optionsLayoutElement = optionsRoot.GetComponent<LayoutElement>();
        if (optionsLayoutElement == null)
        {
            optionsLayoutElement = optionsRoot.AddComponent<LayoutElement>();
        }
        optionsLayoutElement.minHeight = 240f;
        optionsLayoutElement.preferredHeight = 420f;

        _optionLayout = optionsRoot.GetComponent<VerticalLayoutGroup>();
        if (_optionLayout == null)
        {
            _optionLayout = optionsRoot.AddComponent<VerticalLayoutGroup>();
        }
        _optionLayout.spacing = 16f;
        _optionLayout.childAlignment = TextAnchor.UpperLeft;
        _optionLayout.childControlWidth = true;
        _optionLayout.childControlHeight = false;
        _optionLayout.childForceExpandWidth = true;
        _optionLayout.childForceExpandHeight = false;

        _closeButton = EnsureCornerButton(contentRoot.transform, "CloseButton", sharedFont, "关闭");
        _closeButton.gameObject.SetActive(false);
        _closeButton.interactable = false;
        BindCloseButton();
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, float minHeight)
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
        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = minHeight;

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
        text.margin = new Vector4(20f, 16f, 20f, 16f);
        return text;
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

    private void BindCloseButton()
    {
        if (_closeButton == null)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(OnClickCloseSelection);
        _closeButton.onClick.AddListener(OnClickCloseSelection);
    }

    private static Button EnsureCornerButton(Transform parent, string name, TMP_FontAsset font, string buttonText)
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

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(114, 62, 62, 255);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

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
        label.text = buttonText;
        return button;
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

        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFonts.Length; i += 1)
        {
            TMP_FontAsset font = loadedFonts[i];
            if (font != null && font.name == SimsunFontName)
            {
                _preferredChineseFont = font;
                return _preferredChineseFont;
            }
        }

        TextMeshProUGUI existingText = FindObjectOfType<TextMeshProUGUI>(true);
        if (existingText != null && existingText.font != null)
        {
            return existingText.font;
        }

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
