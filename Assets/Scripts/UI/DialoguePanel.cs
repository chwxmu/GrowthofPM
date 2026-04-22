using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DialoguePanel : MonoBehaviour
{
    private const string AdvanceHintText = "点击 / 空格 继续";
    private const float ContentBottomAnchor = 0.03f;
    private const float ContentTopAnchor = 0.35f;
    private const float ContentTopAnchorWithoutPortrait = 0.35f;
    private const float PortraitRightInset = 36f;
    private const float PortraitWidth = 430f;
    private const float PortraitHeight = 620f;

    [Serializable]
    private class LocationBackgroundEntry
    {
        public string locationKeyword;
        public Sprite background;
    }

    private const float CharactersPerSecond = 30f;
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    private static readonly string[] BackgroundResourceRoots =
    {
        "Backgrounds/Dialogue/",
        "Backgrounds/Locations/",
        "Backgrounds/"
    };

    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Sprite _defaultBackground;
    [SerializeField] private List<LocationBackgroundEntry> _locationBackgrounds = new List<LocationBackgroundEntry>();
    [SerializeField] private TMP_Text _locationText;
    [SerializeField] private TMP_Text _speakerText;
    [SerializeField] private TMP_Text _contentText;
    [SerializeField] private TMP_Text _hintText;
    [SerializeField] private Button _clickButton;
    [SerializeField] private Image _portraitFrame;
    [SerializeField] private Image _portraitImage;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private readonly List<DialogueLine> _dialogues = new List<DialogueLine>();
    private readonly Dictionary<string, Sprite> _runtimeBackgroundCache = new Dictionary<string, Sprite>();
    private readonly HashSet<string> _missingBackgroundLocations = new HashSet<string>();

    private Action _onComplete;
    private Coroutine _typingCoroutine;
    private int _currentIndex;
    private bool _isTyping;
    private string _fullText = string.Empty;
    private string _lastResolvedLocation = string.Empty;
    private RectTransform _contentRect;

    private void Awake()
    {
        EnsureLayout();
        if (_clickButton != null)
        {
            _clickButton.onClick.AddListener(OnClickNextByButton);
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || !IsAdvanceShortcutPressed())
        {
            return;
        }

        AdvanceDialogueByShortcut();
    }

    private void OnDestroy()
    {
        if (_clickButton != null)
        {
            _clickButton.onClick.RemoveListener(OnClickNextByButton);
        }
    }

    private void OnClickNextByButton()
    {
        if (_dialogues.Count == 0)
        {
            return;
        }

        GameAudioManager.Instance.PlayButtonClick();
        OnClickNext();
    }

    public void ShowDialogues(List<DialogueLine> dialogues, Action onComplete)
    {
        EnsureLayout();

        _dialogues.Clear();
        if (dialogues != null)
        {
            _dialogues.AddRange(dialogues);
        }

        _onComplete = onComplete;
        _currentIndex = 0;
        _lastResolvedLocation = string.Empty;
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_dialogues.Count == 0)
        {
            CompleteDialogue();
            return;
        }

        ShowCurrentDialogue();
    }

    public void ForceCloseWithoutCallback()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        _fullText = string.Empty;
        _currentIndex = 0;
        _dialogues.Clear();
        _onComplete = null;
        gameObject.SetActive(false);
    }

    private void OnClickNext()
    {
        if (_dialogues.Count == 0)
        {
            return;
        }

        if (_isTyping)
        {
            CompleteCurrentLineInstantly();
            return;
        }

        _currentIndex += 1;
        if (_currentIndex >= _dialogues.Count)
        {
            CompleteDialogue();
            return;
        }

        ShowCurrentDialogue();
    }

    private void ShowCurrentDialogue()
    {
        if (_currentIndex < 0 || _currentIndex >= _dialogues.Count)
        {
            CompleteDialogue();
            return;
        }

        DialogueLine line = _dialogues[_currentIndex] ?? new DialogueLine();
        _fullText = string.IsNullOrEmpty(line.text) ? string.Empty : line.text;

        if (_locationText != null)
        {
            _locationText.text = string.IsNullOrWhiteSpace(line.location) ? string.Empty : line.location;
        }

        UpdateBackgroundByLocation(line.location);

        if (_speakerText != null)
        {
            _speakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? "旁白" : line.speaker;
        }

        UpdateSpeakerPortrait(line.speaker);

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        _typingCoroutine = StartCoroutine(TypeLine(_fullText));
    }

    private void UpdateBackgroundByLocation(string location)
    {
        if (_backgroundImage == null)
        {
            return;
        }

        string normalizedLocation = NormalizeLocation(location);
        if (_lastResolvedLocation == normalizedLocation)
        {
            return;
        }

        Sprite resolvedBackground = ResolveBackgroundSprite(normalizedLocation);
        ApplyBackgroundSprite(resolvedBackground);
        _lastResolvedLocation = normalizedLocation;
    }

    private Sprite ResolveBackgroundSprite(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return _defaultBackground;
        }

        if (_runtimeBackgroundCache.TryGetValue(location, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        for (int i = 0; i < _locationBackgrounds.Count; i += 1)
        {
            LocationBackgroundEntry entry = _locationBackgrounds[i];
            if (entry == null || entry.background == null || string.IsNullOrWhiteSpace(entry.locationKeyword))
            {
                continue;
            }

            if (location.IndexOf(entry.locationKeyword.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _runtimeBackgroundCache[location] = entry.background;
                return entry.background;
            }
        }

        List<string> candidates = BuildLocationCandidates(location);
        for (int i = 0; i < BackgroundResourceRoots.Length; i += 1)
        {
            string root = BackgroundResourceRoots[i];
            for (int j = 0; j < candidates.Count; j += 1)
            {
                string resourcePath = root + candidates[j];
                Sprite sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite == null)
                {
                    continue;
                }

                _runtimeBackgroundCache[location] = sprite;
                return sprite;
            }
        }

        Sprite semanticBackground = UIVisualResources.ResolveSemanticDialogueBackground(location);
        if (semanticBackground != null)
        {
            _runtimeBackgroundCache[location] = semanticBackground;
            return semanticBackground;
        }

        if (!_missingBackgroundLocations.Contains(location))
        {
            _missingBackgroundLocations.Add(location);
            Debug.Log($"[DialoguePanel] 未找到地点背景资源: {location}");
        }

        return _defaultBackground;
    }

    private void ApplyBackgroundSprite(Sprite sprite)
    {
        if (_backgroundImage == null)
        {
            return;
        }

        _backgroundImage.sprite = sprite;
        _backgroundImage.type = Image.Type.Simple;
        _backgroundImage.preserveAspect = false;
        _backgroundImage.color = sprite != null ? Color.white : new Color32(20, 28, 44, 255);
    }

    private void UpdateSpeakerPortrait(string speaker)
    {
        Sprite portraitSprite = UIVisualResources.ResolveSpeakerPortrait(speaker);
        bool hasPortrait = portraitSprite != null;

        if (_portraitFrame != null)
        {
            _portraitFrame.gameObject.SetActive(hasPortrait);
        }

        if (_portraitImage != null)
        {
            _portraitImage.sprite = portraitSprite;
            _portraitImage.preserveAspect = false;
            _portraitImage.color = hasPortrait ? Color.white : Color.clear;
        }

        RefreshPortraitFrameLayout();
        RefreshPortraitImageLayout(portraitSprite, hasPortrait);
        RefreshPortraitLayout(hasPortrait);
    }

    private void RefreshPortraitLayout(bool hasPortrait)
    {
        if (_contentRect == null)
        {
            Transform contentTransform = transform.Find("PanelContent");
            _contentRect = contentTransform as RectTransform;
        }

        if (_contentRect != null)
        {
            _contentRect.anchorMin = new Vector2(0.05f, ContentBottomAnchor);
            _contentRect.anchorMax = hasPortrait ? new Vector2(0.68f, ContentTopAnchor) : new Vector2(0.92f, ContentTopAnchorWithoutPortrait);
            _contentRect.offsetMin = Vector2.zero;
            _contentRect.offsetMax = Vector2.zero;
        }
    }

    private static List<string> BuildLocationCandidates(string location)
    {
        List<string> candidates = new List<string>();
        AddCandidate(candidates, location);
        AddCandidate(candidates, location.Replace(" ", string.Empty));
        AddCandidate(candidates, SanitizeLocationName(location));

        int cnBracketIndex = location.IndexOf('（');
        if (cnBracketIndex > 0)
        {
            AddCandidate(candidates, location.Substring(0, cnBracketIndex));
        }

        int bracketIndex = location.IndexOf('(');
        if (bracketIndex > 0)
        {
            AddCandidate(candidates, location.Substring(0, bracketIndex));
        }

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string normalized = candidate.Trim();
        if (candidates.Contains(normalized))
        {
            return;
        }

        candidates.Add(normalized);
    }

    private static string SanitizeLocationName(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return string.Empty;
        }

        char[] invalidChars =
        {
            ' ',
            '\t',
            '\r',
            '\n',
            '【',
            '】',
            '（',
            '）',
            '(',
            ')',
            ':',
            '：',
            '、',
            '。'
        };

        string sanitized = location;
        for (int i = 0; i < invalidChars.Length; i += 1)
        {
            sanitized = sanitized.Replace(invalidChars[i].ToString(), string.Empty);
        }

        return sanitized.Trim();
    }

    private static string NormalizeLocation(string location)
    {
        return string.IsNullOrWhiteSpace(location) ? string.Empty : location.Trim();
    }

    private IEnumerator TypeLine(string lineText)
    {
        _isTyping = true;

        if (_contentText != null)
        {
            _contentText.text = string.Empty;
        }

        if (string.IsNullOrEmpty(lineText))
        {
            _isTyping = false;
            yield break;
        }

        float delay = 1f / CharactersPerSecond;
        for (int i = 1; i <= lineText.Length; i += 1)
        {
            if (_contentText != null)
            {
                _contentText.text = lineText.Substring(0, i);
            }

            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
    }

    private void CompleteCurrentLineInstantly()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        if (_contentText != null)
        {
            _contentText.text = _fullText;
        }
    }

    private void CompleteDialogue()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        gameObject.SetActive(false);
        Action callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
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
    private void EnsureLayout()
    {
        if (_backgroundImage != null && _locationText != null && _speakerText != null && _contentText != null && _hintText != null && _clickButton != null && _portraitFrame != null && _portraitImage != null)
        {
            ApplyAllFonts();
            RefreshHintText();
            ApplyPortraitContainerStyle();
            RefreshPortraitFrameLayout();
            RefreshPortraitLayout(_portraitFrame.gameObject.activeSelf);
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

        GameObject backgroundRoot = FindOrCreateChild(gameObject, "BackgroundImage");
        backgroundRoot.transform.SetAsFirstSibling();
        RectTransform backgroundRect = EnsureRectTransform(backgroundRoot);
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        _backgroundImage = backgroundRoot.GetComponent<Image>();
        if (_backgroundImage == null)
        {
            _backgroundImage = backgroundRoot.AddComponent<Image>();
        }
        _backgroundImage.raycastTarget = false;
        ApplyBackgroundSprite(_defaultBackground);

        GameObject dimmerRoot = FindOrCreateChild(gameObject, "BackgroundDimmer");
        dimmerRoot.transform.SetSiblingIndex(1);
        RectTransform dimmerRect = EnsureRectTransform(dimmerRoot);
        dimmerRect.anchorMin = Vector2.zero;
        dimmerRect.anchorMax = Vector2.one;
        dimmerRect.offsetMin = Vector2.zero;
        dimmerRect.offsetMax = Vector2.zero;

        Image dimmerImage = dimmerRoot.GetComponent<Image>();
        if (dimmerImage == null)
        {
            dimmerImage = dimmerRoot.AddComponent<Image>();
        }
        dimmerImage.color = new Color32(6, 12, 22, 126);
        dimmerImage.raycastTarget = false;

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        _contentRect = contentRect;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(8, 14, 24, 224);

        Outline contentOutline = contentRoot.GetComponent<Outline>();
        if (contentOutline == null)
        {
            contentOutline = contentRoot.AddComponent<Outline>();
        }
        contentOutline.effectColor = new Color32(140, 191, 255, 72);
        contentOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentRoot.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.padding = new RectOffset(28, 28, 24, 24);
        layoutGroup.spacing = 12f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject portraitFrameObject = FindOrCreateChild(gameObject, "PortraitFrame");
        portraitFrameObject.transform.SetSiblingIndex(Mathf.Max(2, gameObject.transform.childCount - 1));

        _portraitFrame = portraitFrameObject.GetComponent<Image>();
        if (_portraitFrame == null)
        {
            _portraitFrame = portraitFrameObject.AddComponent<Image>();
        }

        GameObject portraitImageObject = FindOrCreateChild(portraitFrameObject, "PortraitImage");
        RectTransform portraitImageRect = EnsureRectTransform(portraitImageObject);

        _portraitImage = portraitImageObject.GetComponent<Image>();
        if (_portraitImage == null)
        {
            _portraitImage = portraitImageObject.AddComponent<Image>();
        }
        _portraitImage.raycastTarget = false;
        _portraitImage.preserveAspect = true;
        ApplyPortraitContainerStyle();
        RefreshPortraitFrameLayout();

        _locationText = EnsureText(contentRoot.transform, "LocationText", sharedFont, 28, FontStyles.Bold, TextAlignmentOptions.Left);
        _speakerText = EnsureText(contentRoot.transform, "SpeakerText", sharedFont, 34, FontStyles.Bold, TextAlignmentOptions.Left);
        _contentText = EnsureText(contentRoot.transform, "ContentText", sharedFont, 32, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        _hintText = EnsureText(contentRoot.transform, "HintText", sharedFont, 24, FontStyles.Italic, TextAlignmentOptions.BottomRight);
        RefreshHintText();

        _locationText.color = new Color32(174, 205, 233, 255);
        _speakerText.color = new Color32(246, 249, 255, 255);
        _contentText.color = new Color32(246, 249, 255, 255);
        _hintText.color = new Color32(170, 188, 214, 255);

        if (_clickButton == null)
        {
            _clickButton = GetComponent<Button>();
            if (_clickButton == null)
            {
                _clickButton = gameObject.AddComponent<Button>();
            }

            ColorBlock colors = _clickButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            _clickButton.colors = colors;
        }

        ApplyAllFonts();
        UpdateSpeakerPortrait(string.Empty);
    }

    private void RefreshHintText()
    {
        if (_hintText != null)
        {
            _hintText.text = AdvanceHintText;
        }
    }

    private void ApplyPortraitContainerStyle()
    {
        if (_portraitFrame != null)
        {
            _portraitFrame.color = Color.clear;
            _portraitFrame.raycastTarget = false;
            RemoveComponentIfExists<Outline>(_portraitFrame.gameObject);
        }

        if (_portraitImage == null)
        {
            return;
        }

        _portraitImage.raycastTarget = false;
        _portraitImage.preserveAspect = false;
    }

    private void RefreshPortraitFrameLayout()
    {
        if (_portraitFrame == null)
        {
            return;
        }

        RectTransform portraitFrameRect = _portraitFrame.rectTransform;
        portraitFrameRect.anchorMin = new Vector2(1f, 0f);
        portraitFrameRect.anchorMax = new Vector2(1f, 0f);
        portraitFrameRect.pivot = new Vector2(1f, 0f);
        portraitFrameRect.anchoredPosition = new Vector2(-PortraitRightInset, 0f);
        portraitFrameRect.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
    }

    private void RefreshPortraitImageLayout(Sprite portraitSprite, bool hasPortrait)
    {
        if (_portraitImage == null)
        {
            return;
        }

        RectTransform portraitImageRect = _portraitImage.rectTransform;
        if (portraitImageRect == null)
        {
            return;
        }

        portraitImageRect.anchorMin = new Vector2(0.5f, 0f);
        portraitImageRect.anchorMax = new Vector2(0.5f, 0f);
        portraitImageRect.pivot = new Vector2(0.5f, 0f);
        portraitImageRect.anchoredPosition = Vector2.zero;

        if (!hasPortrait || portraitSprite == null || _portraitFrame == null)
        {
            portraitImageRect.sizeDelta = Vector2.zero;
            return;
        }

        RectTransform portraitFrameRect = _portraitFrame.rectTransform;
        float frameWidth = portraitFrameRect.rect.width > 0f ? portraitFrameRect.rect.width : PortraitWidth;
        float frameHeight = portraitFrameRect.rect.height > 0f ? portraitFrameRect.rect.height : PortraitHeight;
        float spriteWidth = Mathf.Max(1f, portraitSprite.rect.width);
        float spriteHeight = Mathf.Max(1f, portraitSprite.rect.height);
        float spriteAspect = spriteWidth / spriteHeight;

        float targetWidth = frameWidth;
        float targetHeight = spriteAspect > 0f ? targetWidth / spriteAspect : frameHeight;

        if (frameHeight > 0f && targetHeight > frameHeight)
        {
            targetHeight = frameHeight;
            targetWidth = targetHeight * spriteAspect;
        }

        portraitImageRect.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private static bool IsAdvanceShortcutPressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private void AdvanceDialogueByShortcut()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        OnClickNext();
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

    private static void RemoveComponentIfExists<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return;
        }

        T component = target.GetComponent<T>();
        if (component == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(component);
        }
        else
        {
            DestroyImmediate(component);
        }
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                textObject.transform.SetParent(parent, false);
            }

            text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }
        }

        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.margin = new Vector4(8f, 4f, 8f, 4f);
        text.color = Color.white;
        return text;
    }

    private void ApplyAllFonts()
    {
        TMP_FontAsset sharedFont = ResolveUIFont();
        if (sharedFont == null)
        {
            return;
        }

        if (_locationText != null)
        {
            _locationText.font = sharedFont;
        }

        if (_speakerText != null)
        {
            _speakerText.font = sharedFont;
        }

        if (_contentText != null)
        {
            _contentText.font = sharedFont;
        }

        if (_hintText != null)
        {
            _hintText.font = sharedFont;
        }
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

        Debug.LogError("[DialoguePanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");
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
