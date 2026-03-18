using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransitionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _inheritanceText;
    [SerializeField] private Button _startButton;

    private void Awake()
    {
        EnsureLayout();
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnClickStart);
            _startButton.onClick.AddListener(OnClickStart);
        }
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

    private void EnsureLayout()
    {
        if (_titleText != null && _descriptionText != null && _inheritanceText != null && _startButton != null)
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
        contentRect.anchorMin = new Vector2(0.22f, 0.18f);
        contentRect.anchorMax = new Vector2(0.78f, 0.82f);
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
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = EnsureText(contentRoot.transform, "TitleText", sharedFont, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 64f);
        _descriptionText = EnsureText(contentRoot.transform, "DescriptionText", sharedFont, 28f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 120f);
        _inheritanceText = EnsureText(contentRoot.transform, "InheritanceText", sharedFont, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 180f);
        _startButton = EnsureButton(contentRoot.transform, "StartButton", sharedFont, "开始新项目");
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

    private static TMP_FontAsset FindSharedFont()
    {
        TextMeshProUGUI existingText = FindObjectOfType<TextMeshProUGUI>(true);
        return existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
    }
}
