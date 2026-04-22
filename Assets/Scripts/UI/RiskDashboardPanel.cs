using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Presents the Project 2 hidden-risk dashboard mini-game and returns its risk delta.
/// </summary>
public class RiskDashboardPanel : MonoBehaviour
{
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _instructionText;
    [SerializeField] private TMP_Text _aiAdviceText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private HorizontalLayoutGroup _moduleLayout;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private readonly List<Button> _moduleButtons = new List<Button>();

    private RiskDashboardGame _game;
    private Action<RiskDashboardGame.SessionResult> _onCompleted;
    private RiskDashboardGame.SessionResult _pendingResult;

    private void Awake()
    {
        EnsureLayout();
        BindCloseButton();
    }

    private void Update()
    {
        if (_game == null || !_game.IsRunning || _pendingResult != null)
        {
            return;
        }

        _game.Advance(Time.unscaledDeltaTime);
        RefreshModuleButtons();
        UpdateTimerText();

        if (_game.IsRunning)
        {
            return;
        }

        _pendingResult = _game.GetResult();
        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(true);
            _resultText.text = "修复了" + _pendingResult.ResolvedCount + "个问题，遗漏了" + _pendingResult.MissedCount
                + "个问题，误点" + _pendingResult.WrongClicks + "次。\n"
                + "结果已同步到项目风险评估。";
        }

        SetModuleButtonsInteractable(false);
        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(true);
            _closeButton.interactable = true;
        }
    }

    /// <summary>
    /// Shows the risk dashboard mini-game for the supplied decision event.
    /// </summary>
    /// <param name="eventData">Mini-game decision data.</param>
    /// <param name="onCompleted">Callback invoked after the player closes the result state.</param>
    public void ShowGame(DecisionEventData eventData, Action<RiskDashboardGame.SessionResult> onCompleted)
    {
        EnsureLayout();
        BindCloseButton();

        _game = new RiskDashboardGame();
        _game.StartSession();
        _onCompleted = onCompleted;
        _pendingResult = null;
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_titleText != null)
        {
            _titleText.text = "风险仪表盘纠偏";
        }

        if (_instructionText != null)
        {
            _instructionText.text = eventData != null && !string.IsNullOrWhiteSpace(eventData.description)
                ? eventData.description
                : "在限时内点击红色模块完成修复。";
        }

        if (_aiAdviceText != null)
        {
            string adviceText = AIAdvisor.Instance != null ? AIAdvisor.Instance.GetAdviceDisplayText(eventData) : string.Empty;
            _aiAdviceText.gameObject.SetActive(!string.IsNullOrWhiteSpace(adviceText));
            _aiAdviceText.text = adviceText;
        }

        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(true);
            _resultText.text = "点击红色模块即可修复问题，漏掉报警会增加隐藏风险。";
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(false);
            _closeButton.interactable = false;
        }

        EnsureModuleButtons();
        SetModuleButtonsInteractable(true);
        RefreshModuleButtons();
        UpdateTimerText();
    }

    private void EnsureModuleButtons()
    {
        RefreshStaticModuleButtons();

        if (_moduleLayout == null || _game == null)
        {
            return;
        }

        while (_moduleButtons.Count < _game.ModuleCount)
        {
            _moduleButtons.Add(CreateModuleButton(_moduleButtons.Count));
        }

        for (int index = 0; index < _moduleButtons.Count; index += 1)
        {
            bool isActive = index < _game.ModuleCount;
            _moduleButtons[index].gameObject.SetActive(isActive);
            if (!isActive)
            {
                continue;
            }

            TMP_Text label = _moduleButtons[index].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = _game.GetModuleName(index);
            }

            _moduleButtons[index].onClick.RemoveAllListeners();
            int capturedIndex = index;
            _moduleButtons[index].onClick.AddListener(() => OnClickModule(capturedIndex));
        }
    }

    private Button CreateModuleButton(int index)
    {
        GameObject buttonObject = new GameObject("ModuleButton" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(_moduleLayout.transform, false);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 180f;
        layoutElement.minHeight = 180f;
        layoutElement.preferredWidth = 220f;

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color32(54, 104, 82, 255);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 16f);
        labelRect.offsetMax = new Vector2(-16f, -16f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = ResolveUIFont();
        label.fontSize = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.color = Color.white;

        return buttonObject.GetComponent<Button>();
    }

    private void OnClickModule(int moduleIndex)
    {
        if (_game == null || !_game.IsRunning)
        {
            return;
        }

        GameAudioManager.Instance.PlayButtonClick();
        bool isCorrect = _game.TryFixModule(moduleIndex);
        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(true);
            _resultText.text = isCorrect
                ? "修复成功，继续关注新的报警。"
                : "误点了绿色模块，本次操作增加了隐藏风险。";
        }

        RefreshModuleButtons();
    }

    private void RefreshModuleButtons()
    {
        if (_game == null)
        {
            return;
        }

        for (int index = 0; index < _moduleButtons.Count; index += 1)
        {
            if (_moduleButtons[index] == null || index >= _game.ModuleCount)
            {
                continue;
            }

            bool isAlert = _game.IsModuleAlert(index);
            Image background = _moduleButtons[index].GetComponent<Image>();
            if (background != null)
            {
                background.color = isAlert
                    ? new Color32(194, 74, 74, 255)
                    : new Color32(54, 104, 82, 255);
            }

            TMP_Text label = _moduleButtons[index].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = _game.GetModuleName(index) + (isAlert ? "\n<size=22><color=#FFE9A3>异常</color></size>" : "\n<size=22><color=#D2FFE0>稳定</color></size>");
            }
        }
    }

    private void UpdateTimerText()
    {
        if (_timerText == null || _game == null)
        {
            return;
        }

        _timerText.text = "剩余时间：" + Mathf.CeilToInt(_game.TimeRemaining) + " 秒";
    }

    private void SetModuleButtonsInteractable(bool isInteractable)
    {
        for (int index = 0; index < _moduleButtons.Count; index += 1)
        {
            if (_moduleButtons[index] != null)
            {
                _moduleButtons[index].interactable = isInteractable;
            }
        }
    }

    private void OnClickClose()
    {
        if (_pendingResult == null)
        {
            return;
        }

        GameAudioManager.Instance.PlayButtonClick();
        gameObject.SetActive(false);
        RiskDashboardGame.SessionResult result = _pendingResult;
        _pendingResult = null;
        _onCompleted?.Invoke(result);
    }

    private void RestorePanelVisibility()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void BindCloseButton()
    {
        if (_closeButton == null)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(OnClickClose);
        _closeButton.onClick.AddListener(OnClickClose);
    }

    private void EnsureLayout()
    {
        TryBindSceneReferences();

        if (_titleText != null && _timerText != null && _moduleLayout != null && _closeButton != null)
        {
            ApplyBoundLayoutDefaults();
            BindCloseButton();
            return;
        }

        TMP_FontAsset font = ResolveUIFont();
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
        background.color = new Color32(9, 17, 30, 224);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.1f, 0.12f);
        contentRect.anchorMax = new Vector2(0.9f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(22, 33, 54, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", font, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 54f);
        _instructionText = EnsureText(contentRoot.transform, "InstructionText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 110f);
        _aiAdviceText = EnsureText(contentRoot.transform, "AIAdviceText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 76f);
        _timerText = EnsureText(contentRoot.transform, "TimerText", font, 28f, FontStyles.Bold, TextAlignmentOptions.Center, 50f);

        GameObject modulesObject = FindOrCreateChild(contentRoot, "ModulesRoot");
        LayoutElement modulesLayoutElement = modulesObject.GetComponent<LayoutElement>();
        if (modulesLayoutElement == null)
        {
            modulesLayoutElement = modulesObject.AddComponent<LayoutElement>();
        }
        modulesLayoutElement.minHeight = 200f;
        modulesLayoutElement.preferredHeight = 200f;

        _moduleLayout = modulesObject.GetComponent<HorizontalLayoutGroup>();
        if (_moduleLayout == null)
        {
            _moduleLayout = modulesObject.AddComponent<HorizontalLayoutGroup>();
        }
        _moduleLayout.spacing = 20f;
        _moduleLayout.childControlWidth = true;
        _moduleLayout.childControlHeight = true;
        _moduleLayout.childForceExpandWidth = true;
        _moduleLayout.childForceExpandHeight = false;

        _resultText = EnsureText(contentRoot.transform, "ResultText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 90f);
        _closeButton = EnsureButton(contentRoot.transform, "CloseButton", font, "继续");
    }

    private void ApplyBoundLayoutDefaults()
    {
        TMP_FontAsset font = ResolveUIFont();
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
        background.color = new Color32(9, 17, 30, 224);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.1f, 0.12f);
        contentRect.anchorMax = new Vector2(0.9f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(22, 33, 54, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", font, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 54f);
        _instructionText = EnsureText(contentRoot.transform, "InstructionText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 110f);
        _aiAdviceText = EnsureText(contentRoot.transform, "AIAdviceText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 76f);
        _timerText = EnsureText(contentRoot.transform, "TimerText", font, 28f, FontStyles.Bold, TextAlignmentOptions.Center, 50f);

        GameObject modulesObject = FindOrCreateChild(contentRoot, "ModulesRoot");
        LayoutElement modulesLayoutElement = modulesObject.GetComponent<LayoutElement>();
        if (modulesLayoutElement == null)
        {
            modulesLayoutElement = modulesObject.AddComponent<LayoutElement>();
        }
        modulesLayoutElement.minHeight = 200f;
        modulesLayoutElement.preferredHeight = 200f;

        _moduleLayout = modulesObject.GetComponent<HorizontalLayoutGroup>();
        if (_moduleLayout == null)
        {
            _moduleLayout = modulesObject.AddComponent<HorizontalLayoutGroup>();
        }
        _moduleLayout.spacing = 20f;
        _moduleLayout.childControlWidth = true;
        _moduleLayout.childControlHeight = true;
        _moduleLayout.childForceExpandWidth = true;
        _moduleLayout.childForceExpandHeight = false;

        _resultText = EnsureText(contentRoot.transform, "ResultText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 90f);
        _closeButton = EnsureButton(contentRoot.transform, "CloseButton", font, "继续");
    }

    private void TryBindSceneReferences()
    {
        _titleText = _titleText != null ? _titleText : FindChildComponent<TMP_Text>("PanelContent/TitleText");
        _instructionText = _instructionText != null ? _instructionText : FindChildComponent<TMP_Text>("PanelContent/InstructionText");
        _aiAdviceText = _aiAdviceText != null ? _aiAdviceText : FindChildComponent<TMP_Text>("PanelContent/AIAdviceText");
        _timerText = _timerText != null ? _timerText : FindChildComponent<TMP_Text>("PanelContent/TimerText");
        _resultText = _resultText != null ? _resultText : FindChildComponent<TMP_Text>("PanelContent/ResultText");
        _moduleLayout = _moduleLayout != null ? _moduleLayout : FindChildComponent<HorizontalLayoutGroup>("PanelContent/ModulesRoot");
        _closeButton = _closeButton != null ? _closeButton : FindChildComponent<Button>("PanelContent/CloseButton");
        RefreshStaticModuleButtons();
    }

    private void RefreshStaticModuleButtons()
    {
        _moduleButtons.Clear();
        if (_moduleLayout == null)
        {
            return;
        }

        for (int index = 0; index < 3; index += 1)
        {
            Transform module = _moduleLayout.transform.Find("ModuleButton" + (index + 1));
            if (module != null)
            {
                Button button = module.GetComponent<Button>();
                if (button != null)
                {
                    _moduleButtons.Add(button);
                }
            }
        }
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
        text.margin = new Vector4(16f, 12f, 16f, 12f);
        return text;
    }

    private static Button EnsureButton(Transform parent, string name, TMP_FontAsset font, string labelText)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 58f;
        layoutElement.preferredHeight = 58f;

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color32(68, 98, 134, 255);

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

        return buttonObject.GetComponent<Button>();
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

        Debug.LogError("[RiskDashboardPanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");
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

    private T FindChildComponent<T>(string relativePath) where T : Component
    {
        Transform child = transform.Find(relativePath);
        return child != null ? child.GetComponent<T>() : null;
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
}
