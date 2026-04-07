using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSceneController : MonoBehaviour
{
    private const string ContinueGameDefaultLabel = "继续游戏";
    private const string ContinueGameNoSaveLabel = "继续游戏（无存档）";
    private const string MenuBackgroundResource = "OfficeNight";
    private const string MenuHeroResource = "zhu_jue";

    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueGameButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private TMP_Text _continueGameButtonLabel;
    [SerializeField] private Text _continueGameButtonLegacyLabel;

    private Image _backgroundPanelImage;
    private TMP_Text _titleText;

    #region Unity Lifecycle

    private void Awake()
    {
        AutoBindIfNeeded();
        ApplyVisualTheme();
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void Start()
    {
        RefreshContinueButton();
        ApplyVisualTheme();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    #endregion

    #region Internal Helpers

    private void BindEvents()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.AddListener(OnClickNewGame);
        }

        if (_continueGameButton != null)
        {
            _continueGameButton.onClick.AddListener(OnClickContinueGame);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnClickQuit);
        }
    }

    private void UnbindEvents()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.RemoveListener(OnClickNewGame);
        }

        if (_continueGameButton != null)
        {
            _continueGameButton.onClick.RemoveListener(OnClickContinueGame);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnClickQuit);
        }
    }

    private void AutoBindIfNeeded()
    {
        if (_newGameButton == null)
        {
            GameObject target = GameObject.Find("NewGameButton");
            if (target != null)
            {
                _newGameButton = target.GetComponent<Button>();
            }
        }

        if (_continueGameButton == null)
        {
            GameObject target = GameObject.Find("ContinueGameButton");
            if (target != null)
            {
                _continueGameButton = target.GetComponent<Button>();
            }
        }

        if (_quitButton == null)
        {
            GameObject target = GameObject.Find("QuitButton");
            if (target != null)
            {
                _quitButton = target.GetComponent<Button>();
            }
        }

        BindContinueButtonLabelIfNeeded();
        BindVisualReferencesIfNeeded();
    }

    private void BindVisualReferencesIfNeeded()
    {
        if (_backgroundPanelImage == null)
        {
            GameObject target = GameObject.Find("BackgroundPanel");
            if (target != null)
            {
                _backgroundPanelImage = target.GetComponent<Image>();
            }
        }

        if (_titleText == null)
        {
            GameObject target = GameObject.Find("TitleText");
            if (target != null)
            {
                _titleText = target.GetComponent<TMP_Text>();
            }
        }
    }

    private void RefreshContinueButton()
    {
        if (_continueGameButton == null)
        {
            return;
        }

        bool hasSave = DataManager.Instance != null && DataManager.Instance.HasSaveFile();
        _continueGameButton.interactable = hasSave;
        SetContinueButtonLabel(hasSave ? ContinueGameDefaultLabel : ContinueGameNoSaveLabel);
    }

    private void BindContinueButtonLabelIfNeeded()
    {
        if (_continueGameButton == null)
        {
            return;
        }

        if (_continueGameButtonLabel == null)
        {
            _continueGameButtonLabel = _continueGameButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_continueGameButtonLegacyLabel == null)
        {
            _continueGameButtonLegacyLabel = _continueGameButton.GetComponentInChildren<Text>(true);
        }
    }

    private void SetContinueButtonLabel(string labelText)
    {
        BindContinueButtonLabelIfNeeded();

        if (_continueGameButtonLabel != null)
        {
            _continueGameButtonLabel.text = labelText;
        }

        if (_continueGameButtonLegacyLabel != null)
        {
            _continueGameButtonLegacyLabel.text = labelText;
        }
    }

    private void ApplyVisualTheme()
    {
        BindVisualReferencesIfNeeded();
        ApplyBackgroundTheme();
        ApplyTitleTheme();
        ApplyButtonTheme(_newGameButton, new Color32(46, 91, 148, 240), "progress");
        ApplyButtonTheme(_continueGameButton, new Color32(29, 63, 110, 236), "protagonist");
        ApplyButtonTheme(_quitButton, new Color32(94, 74, 74, 228));
    }

    private void ApplyBackgroundTheme()
    {
        if (_backgroundPanelImage == null)
        {
            return;
        }

        Sprite backgroundSprite = UIVisualResources.LoadDialogueBackground(MenuBackgroundResource);
        if (backgroundSprite != null)
        {
            _backgroundPanelImage.sprite = backgroundSprite;
            _backgroundPanelImage.preserveAspect = false;
            _backgroundPanelImage.color = Color.white;
        }

        GameObject overlayObject = FindOrCreateChild(_backgroundPanelImage.gameObject, "BackgroundOverlay");
        overlayObject.transform.SetAsLastSibling();

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        if (overlayRect == null)
        {
            overlayRect = overlayObject.AddComponent<RectTransform>();
        }
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayObject.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = overlayObject.AddComponent<Image>();
        }
        overlayImage.color = new Color32(10, 18, 30, 125);
        overlayImage.raycastTarget = false;

        GameObject menuCanvas = GameObject.Find("MenuCanvas");
        if (menuCanvas == null)
        {
            return;
        }

        GameObject heroObject = FindOrCreateChild(menuCanvas, "HeroIllustration");
        heroObject.transform.SetSiblingIndex(Mathf.Min(2, menuCanvas.transform.childCount - 1));

        RectTransform heroRect = heroObject.GetComponent<RectTransform>();
        if (heroRect == null)
        {
            heroRect = heroObject.AddComponent<RectTransform>();
        }
        heroRect.anchorMin = new Vector2(1f, 0f);
        heroRect.anchorMax = new Vector2(1f, 0f);
        heroRect.pivot = new Vector2(1f, 0f);
        heroRect.sizeDelta = new Vector2(430f, 620f);
        heroRect.anchoredPosition = new Vector2(-36f, 0f);

        Image heroImage = heroObject.GetComponent<Image>();
        if (heroImage == null)
        {
            heroImage = heroObject.AddComponent<Image>();
        }
        heroImage.sprite = UIVisualResources.LoadCharacter(MenuHeroResource);
        heroImage.preserveAspect = true;
        heroImage.color = new Color32(255, 255, 255, 245);
        heroImage.raycastTarget = false;
    }

    private void ApplyTitleTheme()
    {
        if (_titleText == null)
        {
            return;
        }

        _titleText.color = new Color32(244, 248, 255, 255);
    }

    private void ApplyButtonTheme(Button button, Color32 baseColor, string iconResource = null)
    {
        if (button == null)
        {
            return;
        }

        Image background = button.GetComponent<Image>();
        if (background != null)
        {
            background.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = new Color32(
            (byte)Mathf.Clamp(baseColor.r + 20, 0, 255),
            (byte)Mathf.Clamp(baseColor.g + 20, 0, 255),
            (byte)Mathf.Clamp(baseColor.b + 20, 0, 255),
            255);
        colors.pressedColor = new Color32(
            (byte)Mathf.Clamp(baseColor.r - 18, 0, 255),
            (byte)Mathf.Clamp(baseColor.g - 18, 0, 255),
            (byte)Mathf.Clamp(baseColor.b - 18, 0, 255),
            255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(95, 111, 131, 160);
        button.colors = colors;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.alignment = string.IsNullOrWhiteSpace(iconResource) ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            label.margin = string.IsNullOrWhiteSpace(iconResource)
                ? new Vector4(0f, 0f, 0f, 0f)
                : new Vector4(48f, 0f, 16f, 0f);
        }

        if (string.IsNullOrWhiteSpace(iconResource))
        {
            return;
        }

        GameObject iconObject = FindOrCreateChild(button.gameObject, "Icon");
        iconObject.transform.SetAsFirstSibling();

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        if (iconRect == null)
        {
            iconRect = iconObject.AddComponent<RectTransform>();
        }
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(24f, 24f);
        iconRect.anchoredPosition = new Vector2(22f, 0f);

        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = iconObject.AddComponent<Image>();
        }
        iconImage.sprite = UIVisualResources.LoadIcon(iconResource);
        iconImage.preserveAspect = true;
        iconImage.color = new Color32(232, 241, 255, 255);
        iconImage.raycastTarget = false;
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent != null ? parent.transform.Find(childName) : null;
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void OnClickNewGame()
    {
        GameManager.Instance.StartNewGame();
    }

    private static void OnClickContinueGame()
    {
        GameManager.Instance.ContinueGame();
    }

    private static void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
