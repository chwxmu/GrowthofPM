using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndingPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _aiRateText;
    [SerializeField] private ScrollRect _detailsScrollRect;
    [SerializeField] private Button _nextProjectButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;

    private EndingResultData _currentResult;

    private void Awake()
    {
        EnsureLayout();
        BindButtons();
    }

    private void OnDestroy()
    {
        if (_nextProjectButton != null)
        {
            _nextProjectButton.onClick.RemoveListener(OnClickNextProject);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnClickRestart);
        }

        if (_menuButton != null)
        {
            _menuButton.onClick.RemoveListener(OnClickMenu);
        }
    }

    public void ShowEnding(EndingResultData result)
    {
        EnsureLayout();
        _currentResult = result;
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_titleText != null)
        {
            _titleText.text = result != null && !string.IsNullOrWhiteSpace(result.title) ? result.title : "项目结局";
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = result != null ? result.description : string.Empty;
        }

        if (_statsText != null)
        {
            _statsText.text = BuildStatsText();
        }

        if (_aiRateText != null)
        {
            _aiRateText.text = BuildAIAdoptionText();
        }

        if (_nextProjectButton != null)
        {
            _nextProjectButton.gameObject.SetActive(ShouldShowNextProjectButton());
        }

        RefreshTextLayout();

        LogPanelVisibility("ShowEnding restored visibility");
    }

    private void BindButtons()
    {
        if (_nextProjectButton != null)
        {
            _nextProjectButton.onClick.RemoveListener(OnClickNextProject);
            _nextProjectButton.onClick.AddListener(OnClickNextProject);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnClickRestart);
            _restartButton.onClick.AddListener(OnClickRestart);
        }

        if (_menuButton != null)
        {
            _menuButton.onClick.RemoveListener(OnClickMenu);
            _menuButton.onClick.AddListener(OnClickMenu);
        }
    }

    private void OnClickNextProject()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.ContinueToNextProjectFromEnding();
        }
    }

    private void OnClickRestart()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteSave();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    private void OnClickMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }

    private bool ShouldShowNextProjectButton()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasNextProject())
        {
            return false;
        }

        return _currentResult == null || !string.Equals(_currentResult.grade, "fail", System.StringComparison.OrdinalIgnoreCase);
    }

    private void RestorePanelVisibility()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void LogPanelVisibility(string context)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        Debug.Log($"[EndingPanel] : {context}. active={gameObject.activeSelf} alpha={(canvasGroup != null ? canvasGroup.alpha : -1f)} interactable={(canvasGroup != null && canvasGroup.interactable)} blocksRaycasts={(canvasGroup != null && canvasGroup.blocksRaycasts)}");
    }

    private void RefreshTextLayout()
    {
        UpdateTextHeight(_titleText, 72f);
        UpdateTextHeight(_descriptionText, 220f);
        UpdateTextHeight(_statsText, 132f);
        UpdateTextHeight(_aiRateText, 56f);

        if (_detailsScrollRect != null && _detailsScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_detailsScrollRect.content);
            _detailsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private static void UpdateTextHeight(TMP_Text text, float minHeight)
    {
        if (text == null)
        {
            return;
        }

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = text.gameObject.AddComponent<LayoutElement>();
        }

        float width = text.rectTransform.rect.width > 0f ? text.rectTransform.rect.width : 720f;
        Vector2 preferred = text.GetPreferredValues(text.text, width, 0f);
        float preferredHeight = Mathf.Ceil(preferred.y + text.margin.y + text.margin.w + 20f);
        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = Mathf.Max(minHeight, preferredHeight);
    }

    private static string BuildStatsText()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return string.Empty;
        }

        PlayerData data = GameManager.Instance.CurrentPlayerData;
        return "技术力：" + data.techPower + "\n"
            + "沟通力：" + data.commPower + "\n"
            + "管理力：" + data.managePower + "\n"
            + "抗压力：" + data.stressPower;
    }

    private static string BuildAIAdoptionText()
    {
        float rate = GameManager.Instance != null ? GameManager.Instance.GetAIAdoptionRate() : 0f;
        return "AI 采纳率：" + Mathf.RoundToInt(rate * 100f) + "%";
    }

    private void EnsureLayout()
    {
        if (_titleText != null && _descriptionText != null && _statsText != null && _aiRateText != null && _detailsScrollRect != null && _nextProjectButton != null && _restartButton != null && _menuButton != null)
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
        background.color = new Color32(10, 18, 28, 225);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.2f, 0.12f);
        contentRect.anchorMax = new Vector2(0.8f, 0.88f);
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
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", sharedFont, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 72f);

        _detailsScrollRect = EnsureDetailsScrollRect(contentRoot.transform, "DetailsScroll");
        Transform detailsContent = _detailsScrollRect.content;
        _descriptionText = EnsureText(detailsContent, "DescriptionText", sharedFont, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 220f);
        _statsText = EnsureText(detailsContent, "StatsText", sharedFont, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 132f);
        _aiRateText = EnsureText(detailsContent, "AIRateText", sharedFont, 24f, FontStyles.Bold, TextAlignmentOptions.Left, 56f);

        GameObject buttonRow = FindOrCreateChild(contentRoot, "ButtonRow");
        HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        if (buttonLayout == null)
        {
            buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        }
        buttonLayout.spacing = 16f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = buttonRow.GetComponent<LayoutElement>();
        if (rowLayout == null)
        {
            rowLayout = buttonRow.AddComponent<LayoutElement>();
        }
        rowLayout.preferredHeight = 60f;
        rowLayout.minHeight = 60f;

        _nextProjectButton = EnsureButton(buttonRow.transform, "NextProjectButton", sharedFont, "继续下一个项目");
        _restartButton = EnsureButton(buttonRow.transform, "RestartButton", sharedFont, "重新开始");
        _menuButton = EnsureButton(buttonRow.transform, "MenuButton", sharedFont, "返回主菜单");
        BindButtons();
    }

    private static ScrollRect EnsureDetailsScrollRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject scrollObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        if (existing == null)
        {
            scrollObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = scrollObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = scrollObject.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 280f;
        layoutElement.preferredHeight = 360f;

        Image background = scrollObject.GetComponent<Image>();
        if (background == null)
        {
            background = scrollObject.AddComponent<Image>();
        }
        background.color = new Color32(18, 25, 40, 160);

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollObject.AddComponent<ScrollRect>();
        }

        GameObject viewport = FindOrCreateChild(scrollObject, "Viewport");
        RectTransform viewportRect = EnsureRectTransform(viewport);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage == null)
        {
            viewportImage = viewport.AddComponent<Image>();
        }
        viewportImage.color = new Color(0f, 0f, 0f, 0.02f);

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.AddComponent<RectMask2D>();
        }

        GameObject content = FindOrCreateChild(viewport, "Content");
        RectTransform contentRect = EnsureRectTransform(content);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(8f, 0f);
        contentRect.offsetMax = new Vector2(-8f, 0f);

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        if (contentLayout == null)
        {
            contentLayout = content.AddComponent<VerticalLayoutGroup>();
        }
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        return scrollRect;
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
        text.overflowMode = TextOverflowModes.Overflow;
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
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = labelText;
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
