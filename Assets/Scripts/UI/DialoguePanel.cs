using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : MonoBehaviour
{
    [Serializable]
    private class LocationBackgroundEntry
    {
        public string locationKeyword;
        public Sprite background;
    }

    private const float CharactersPerSecond = 30f;

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

    private readonly List<DialogueLine> _dialogues = new List<DialogueLine>();
    private readonly Dictionary<string, Sprite> _runtimeBackgroundCache = new Dictionary<string, Sprite>();
    private readonly HashSet<string> _missingBackgroundLocations = new HashSet<string>();

    private Action _onComplete;
    private Coroutine _typingCoroutine;
    private int _currentIndex;
    private bool _isTyping;
    private string _fullText = string.Empty;
    private string _lastResolvedLocation = string.Empty;

    private void Awake()
    {
        EnsureLayout();
        if (_clickButton != null)
        {
            _clickButton.onClick.AddListener(OnClickNext);
        }
    }

    private void OnDestroy()
    {
        if (_clickButton != null)
        {
            _clickButton.onClick.RemoveListener(OnClickNext);
        }
    }

    public void ShowDialogues(List<DialogueLine> dialogues, Action onComplete)
    {
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
        _backgroundImage.preserveAspect = true;
        _backgroundImage.color = sprite != null ? Color.white : new Color32(20, 28, 44, 255);
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
        if (_locationText != null && _speakerText != null && _contentText != null && _clickButton != null)
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

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.15f, 0.18f);
        contentRect.anchorMax = new Vector2(0.85f, 0.82f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(10, 16, 28, 185);

        VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentRoot.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.padding = new RectOffset(32, 32, 32, 32);
        layoutGroup.spacing = 16f;
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

        _locationText = EnsureText(contentRoot.transform, "LocationText", sharedFont, 28, FontStyles.Bold, TextAlignmentOptions.Left);
        _speakerText = EnsureText(contentRoot.transform, "SpeakerText", sharedFont, 34, FontStyles.Bold, TextAlignmentOptions.Left);
        _contentText = EnsureText(contentRoot.transform, "ContentText", sharedFont, 32, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        _hintText = EnsureText(contentRoot.transform, "HintText", sharedFont, 24, FontStyles.Italic, TextAlignmentOptions.BottomRight);
        _hintText.text = "点击继续";

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
        text.color = Color.white;
        return text;
    }

    private static TMP_FontAsset FindSharedFont()
    {
        TMP_FontAsset simsun = Resources.Load<TMP_FontAsset>("Fonts/SIMSUN SDF");
        if (simsun != null)
        {
            return simsun;
        }

        TextMeshProUGUI existingText = FindObjectOfType<TextMeshProUGUI>(true);
        return existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
    }
}
