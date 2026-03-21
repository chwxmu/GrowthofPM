using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TransitionPanel : MonoBehaviour
{
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _inheritanceText;
    [SerializeField] private LayoutElement _bottomSpacer;
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private void Awake()
    {
        EnsureLayout();
        BindStartButton();
    }

    private void OnDestroy()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnClickStart);
        }
    }

    public void ShowTransition(ProjectStoryData projectStory)
    {
        EnsureLayout();
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_titleText != null)
        {
            _titleText.text = projectStory != null && !string.IsNullOrWhiteSpace(projectStory.projectName)
                ? "新项目：" + projectStory.projectName
                : "新项目开始";
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = BuildDescription(projectStory);
        }

        if (_inheritanceText != null)
        {
            _inheritanceText.text = BuildInheritanceText();
        }

        RefreshTextLayout();
        LogPanelVisibility("ShowTransition restored visibility");
    }

    private void OnClickStart()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartCurrentProjectFromTransition();
        }
    }

    private static string BuildDescription(ProjectStoryData projectStory)
    {
        string projectName = projectStory != null && !string.IsNullOrWhiteSpace(projectStory.projectName)
            ? projectStory.projectName
            : "下一项目";

        int totalWeeks = projectStory != null ? projectStory.totalWeeks : 0;
        if (totalWeeks > 0)
        {
            return "你将进入“" + projectName + "”，共 " + totalWeeks + " 周。准备迎接新的项目挑战。";
        }

        return "你将进入“" + projectName + "”。准备迎接新的项目挑战。";
    }

    private static string BuildInheritanceText()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            return string.Empty;
        }

        PlayerData data = GameManager.Instance.CurrentPlayerData;
        return "继承属性\n"
            + "技术力：" + data.techPower + "\n"
            + "沟通力：" + data.commPower + "\n"
            + "管理力：" + data.managePower + "\n"
            + "抗压力：" + data.stressPower;
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
        Debug.Log($"[TransitionPanel] : {context}. active={gameObject.activeSelf} alpha={(canvasGroup != null ? canvasGroup.alpha : -1f)} interactable={(canvasGroup != null && canvasGroup.interactable)} blocksRaycasts={(canvasGroup != null && canvasGroup.blocksRaycasts)}");
    }

    private void RefreshTextLayout()
    {
        UpdateTextHeight(_titleText, 72f);
        UpdateTextHeight(_descriptionText, 108f);
        UpdateTextHeight(_inheritanceText, 168f);

        if (_contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
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

        float width = text.rectTransform.rect.width > 0f ? text.rectTransform.rect.width : 680f;
        Vector2 preferred = text.GetPreferredValues(text.text, width, 0f);
        float preferredHeight = Mathf.Ceil(preferred.y + text.margin.y + text.margin.w + 20f);
        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = Mathf.Max(minHeight, preferredHeight);
    }

    private void EnsureLayout()
    {
        if (_contentRoot != null && _titleText != null && _descriptionText != null && _inheritanceText != null && _bottomSpacer != null && _startButton != null)
        {
            ApplyAllFonts();
            return;
        }

        TMP_FontAsset sharedFont = ResolveUIFont();
        RectTransform root = EnsureRectTransform(gameObject);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color32(10, 18, 28, 225);

        GameObject contentObject = FindOrCreateChild(gameObject, "PanelContent");
        _contentRoot = EnsureRectTransform(contentObject);
        _contentRoot.anchorMin = new Vector2(0.22f, 0.14f);
        _contentRoot.anchorMax = new Vector2(0.78f, 0.88f);
        _contentRoot.offsetMin = Vector2.zero;
        _contentRoot.offsetMax = Vector2.zero;

        Image contentImage = contentObject.GetComponent<Image>();
        if (contentImage == null)
        {
            contentImage = contentObject.AddComponent<Image>();
        }
        contentImage.color = new Color32(26, 34, 54, 245);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentObject.transform, "TitleText", sharedFont, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 72f);
        _descriptionText = EnsureText(contentObject.transform, "DescriptionText", sharedFont, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 108f);
        _inheritanceText = EnsureText(contentObject.transform, "InheritanceText", sharedFont, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 168f);
        _bottomSpacer = EnsureSpacer(contentObject.transform, "BottomSpacer");
        _startButton = EnsureButton(contentObject.transform, "StartButton", sharedFont, "开始新项目");

        _titleText.transform.SetSiblingIndex(0);
        _descriptionText.transform.SetSiblingIndex(1);
        _inheritanceText.transform.SetSiblingIndex(2);
        _bottomSpacer.transform.SetSiblingIndex(3);
        _startButton.transform.SetSiblingIndex(4);

        ApplyAllFonts();
        BindStartButton();
    }

    private void BindStartButton()
    {
        if (_startButton == null)
        {
            return;
        }

        _startButton.onClick.RemoveListener(OnClickStart);
        _startButton.onClick.AddListener(OnClickStart);
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

    private static LayoutElement EnsureSpacer(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject spacerObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        if (existing == null)
        {
            spacerObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = spacerObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = spacerObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 12f;
        layoutElement.preferredHeight = 12f;
        layoutElement.flexibleHeight = 1f;
        return layoutElement;
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
        layoutElement.minHeight = 60f;
        layoutElement.preferredHeight = 60f;

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

        if (_descriptionText != null)
        {
            _descriptionText.font = sharedFont;
        }

        if (_inheritanceText != null)
        {
            _inheritanceText.font = sharedFont;
        }

        ApplyButtonLabelFont(_startButton, sharedFont);
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

        Debug.LogError("[TransitionPanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");
        return TMP_Settings.defaultFontAsset;
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
