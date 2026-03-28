using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Presents the Project 2 CPM dependency mini-game and reports whether the player solved it.
/// </summary>
public class CPMGamePanel : MonoBehaviour
{
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    private const float ContentAnchorMin = 0.07f;
    private const float ContentAnchorMax = 0.93f;
    private const float ContentPadding = 24f;
    private const float ContentSpacing = 12f;
    private const float InstructionBlockHeight = 96f;
    private const float AdviceBlockHeight = 64f;
    private const float PlayfieldHeight = 320f;
    private const float ConnectionBlockHeight = 72f;
    private const float FeedbackBlockHeight = 64f;
    private const float FooterHeight = 56f;
    private const float FooterReservedPadding = 92f;

    private static readonly Vector2[] NodeAnchoredPositions =
    {
        new Vector2(-250f, 102f),
        new Vector2(230f, 112f),
        new Vector2(-220f, -74f),
        new Vector2(240f, -78f)
    };

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _instructionText;
    [SerializeField] private TMP_Text _aiAdviceText;
    [SerializeField] private TMP_Text _connectionText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _lineLayer;
    [SerializeField] private RectTransform _nodeLayer;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private readonly List<RectTransform> _nodeRects = new List<RectTransform>();
    private readonly List<Image> _connectionLines = new List<Image>();
    private readonly System.Random _layoutRandom = new System.Random();

    private CPMGame _game;
    private Action<bool> _onCompleted;
    private bool _hasPendingResult;
    private bool _isSuccessful;
    private int _dragSourceIndex = -1;
    private Image _previewLine;

    private void Awake()
    {
        EnsureLayout();
        BindButtons();
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(OnClickConfirm);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(OnClickReset);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickClose);
        }
    }

    /// <summary>
    /// Shows the CPM mini-game for the supplied decision event.
    /// </summary>
    /// <param name="eventData">Mini-game decision data.</param>
    /// <param name="onCompleted">Callback invoked when the player closes the result state.</param>
    public void ShowGame(DecisionEventData eventData, Action<bool> onCompleted)
    {
        EnsureLayout();
        BindButtons();

        _game = new CPMGame();
        _onCompleted = onCompleted;
        _hasPendingResult = false;
        _isSuccessful = false;
        _dragSourceIndex = -1;
        RemovePreviewLine();
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_titleText != null)
        {
            _titleText.text = "关键路径连线";
        }

        if (_instructionText != null)
        {
            _instructionText.text = eventData != null && !string.IsNullOrWhiteSpace(eventData.description)
                ? eventData.description
                : "拖拽节点到另一个节点，建立任务依赖关系。";
        }

        if (_aiAdviceText != null)
        {
            string adviceText = AIAdvisor.Instance != null ? AIAdvisor.Instance.GetAdviceDisplayText(eventData) : string.Empty;
            _aiAdviceText.gameObject.SetActive(!string.IsNullOrWhiteSpace(adviceText));
            _aiAdviceText.text = adviceText;
        }

        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(false);
            _feedbackText.text = string.Empty;
        }

        if (_confirmButton != null)
        {
            _confirmButton.gameObject.SetActive(true);
            _confirmButton.interactable = true;
        }

        if (_resetButton != null)
        {
            _resetButton.gameObject.SetActive(true);
            _resetButton.interactable = true;
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(false);
            _closeButton.interactable = false;
        }

        EnsureNodeViews();
        ShuffleNodePositions();
        RedrawConnections();
        UpdateConnectionText();
        SetNodeInteractionEnabled(true);
    }

    private void OnClickConfirm()
    {
        if (_game == null || _hasPendingResult)
        {
            return;
        }

        _isSuccessful = _game.IsSolved();
        _hasPendingResult = true;
        SetNodeInteractionEnabled(false);

        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.text = _isSuccessful
                ? "<color=#7CFF8A>关键路径正确，Week 5 不会触发逻辑阻塞。</color>"
                : "<color=#FF8A8A>关键路径错误，Week 5 将触发逻辑阻塞惩罚。</color>";
        }

        if (_confirmButton != null)
        {
            _confirmButton.gameObject.SetActive(false);
        }

        if (_resetButton != null)
        {
            _resetButton.gameObject.SetActive(false);
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(true);
            _closeButton.interactable = true;
        }
    }

    private void OnClickReset()
    {
        if (_game == null || _hasPendingResult)
        {
            return;
        }

        _game.Reset();
        _dragSourceIndex = -1;
        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.text = "已清空当前依赖关系，请重新连线。";
        }

        RedrawConnections();
        UpdateConnectionText();
    }

    private void OnClickClose()
    {
        if (!_hasPendingResult)
        {
            return;
        }

        gameObject.SetActive(false);
        _hasPendingResult = false;
        _onCompleted?.Invoke(_isSuccessful);
    }

    private void EnsureNodeViews()
    {
        RefreshStaticNodeReferences();

        if (_nodeLayer == null || _game == null)
        {
            return;
        }

        while (_nodeRects.Count < _game.NodeCount)
        {
            _nodeRects.Add(CreateNodeView(_nodeRects.Count));
        }

        for (int index = 0; index < _nodeRects.Count; index += 1)
        {
            bool isActive = index < _game.NodeCount;
            _nodeRects[index].gameObject.SetActive(isActive);
            if (!isActive)
            {
                continue;
            }

            TMP_Text label = _nodeRects[index].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = _game.GetNodeName(index);
            }

            BindNodeEvents(_nodeRects[index].gameObject, index);
        }
    }

    private RectTransform CreateNodeView(int index)
    {
        GameObject nodeObject = new GameObject("Node" + (index + 1), typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(EventTrigger));
        nodeObject.transform.SetParent(_nodeLayer, false);

        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 92f);

        Image background = nodeObject.GetComponent<Image>();
        background.color = new Color32(44, 68, 110, 235);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(nodeObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 10f);
        labelRect.offsetMax = new Vector2(-14f, -10f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = ResolveUIFont();
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.color = Color.white;

        return rect;
    }

    private void BindNodeEvents(GameObject target, int nodeIndex)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();
        AddEventTrigger(trigger, EventTriggerType.BeginDrag, eventData => OnBeginNodeDrag(nodeIndex, eventData));
        AddEventTrigger(trigger, EventTriggerType.Drag, eventData => OnNodeDrag(nodeIndex, eventData));
        AddEventTrigger(trigger, EventTriggerType.EndDrag, eventData => OnEndNodeDrag(nodeIndex, eventData));
    }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(eventData => callback(eventData));
        trigger.triggers.Add(entry);
    }

    private void OnBeginNodeDrag(int nodeIndex, BaseEventData eventData)
    {
        if (_hasPendingResult || _game == null)
        {
            return;
        }

        PointerEventData pointerEventData = eventData as PointerEventData;
        if (pointerEventData == null)
        {
            return;
        }

        _dragSourceIndex = nodeIndex;
        UpdatePreviewLine(GetNodeCenterLocalPosition(nodeIndex), GetPointerLocalPosition(pointerEventData));
    }

    private void OnNodeDrag(int nodeIndex, BaseEventData eventData)
    {
        if (_dragSourceIndex != nodeIndex)
        {
            return;
        }

        PointerEventData pointerEventData = eventData as PointerEventData;
        if (pointerEventData == null)
        {
            return;
        }

        UpdatePreviewLine(GetNodeCenterLocalPosition(nodeIndex), GetPointerLocalPosition(pointerEventData));
    }

    private void OnEndNodeDrag(int nodeIndex, BaseEventData eventData)
    {
        if (_dragSourceIndex != nodeIndex)
        {
            return;
        }

        PointerEventData pointerEventData = eventData as PointerEventData;
        int targetIndex = pointerEventData != null ? FindNodeIndexAtPointer(pointerEventData) : -1;
        RemovePreviewLine();

        if (_game != null && targetIndex >= 0 && targetIndex != _dragSourceIndex)
        {
            bool isConnected = _game.TrySetConnection(_dragSourceIndex, targetIndex);
            if (_feedbackText != null)
            {
                _feedbackText.gameObject.SetActive(true);
                _feedbackText.text = isConnected
                    ? "已建立依赖：" + _game.GetNodeName(_dragSourceIndex) + " -> " + _game.GetNodeName(targetIndex)
                    : "无法建立该依赖，请检查是否形成了循环。";
            }

            if (isConnected)
            {
                RedrawConnections();
                UpdateConnectionText();
            }
        }

        _dragSourceIndex = -1;
    }

    private int FindNodeIndexAtPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            return -1;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        for (int resultIndex = 0; resultIndex < results.Count; resultIndex += 1)
        {
            for (int nodeIndex = 0; nodeIndex < _nodeRects.Count; nodeIndex += 1)
            {
                if (_nodeRects[nodeIndex] != null && results[resultIndex].gameObject.transform.IsChildOf(_nodeRects[nodeIndex]))
                {
                    return nodeIndex;
                }
            }
        }

        return -1;
    }

    private void ShuffleNodePositions()
    {
        List<Vector2> positions = new List<Vector2>(NodeAnchoredPositions);
        for (int index = 0; index < _nodeRects.Count; index += 1)
        {
            int swapIndex = _layoutRandom.Next(index, positions.Count);
            Vector2 temp = positions[index];
            positions[index] = positions[swapIndex];
            positions[swapIndex] = temp;

            _nodeRects[index].anchoredPosition = positions[index];
        }
    }

    private void RedrawConnections()
    {
        if (_lineLayer == null || _game == null)
        {
            return;
        }

        while (_connectionLines.Count < _game.Connections.Count)
        {
            _connectionLines.Add(CreateLineImage("ConnectionLine" + (_connectionLines.Count + 1), new Color32(255, 210, 110, 255)));
        }

        for (int index = 0; index < _connectionLines.Count; index += 1)
        {
            bool hasConnection = index < _game.Connections.Count;
            _connectionLines[index].gameObject.SetActive(hasConnection);
            if (!hasConnection)
            {
                continue;
            }

            Vector2Int connection = _game.Connections[index];
            SetLineRect(_connectionLines[index].rectTransform, GetNodeCenterLocalPosition(connection.x), GetNodeCenterLocalPosition(connection.y), 8f);
        }
    }

    private void UpdateConnectionText()
    {
        if (_connectionText == null || _game == null)
        {
            return;
        }

        if (_game.Connections.Count == 0)
        {
            _connectionText.text = "当前依赖：尚未建立。";
            return;
        }

        List<string> lines = new List<string>();
        for (int index = 0; index < _game.Connections.Count; index += 1)
        {
            Vector2Int connection = _game.Connections[index];
            lines.Add("- " + _game.GetNodeName(connection.x) + " -> " + _game.GetNodeName(connection.y));
        }

        _connectionText.text = "当前依赖：\n" + string.Join("\n", lines.ToArray());
    }

    private void SetNodeInteractionEnabled(bool isEnabled)
    {
        for (int index = 0; index < _nodeRects.Count; index += 1)
        {
            if (_nodeRects[index] == null)
            {
                continue;
            }

            CanvasGroup canvasGroup = _nodeRects[index].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = _nodeRects[index].gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = isEnabled;
            canvasGroup.blocksRaycasts = isEnabled;

            Image image = _nodeRects[index].GetComponent<Image>();
            if (image != null)
            {
                image.color = isEnabled ? new Color32(44, 68, 110, 235) : new Color32(72, 72, 72, 210);
            }
        }
    }

    private void UpdatePreviewLine(Vector2 startPosition, Vector2 endPosition)
    {
        if (_previewLine == null)
        {
            _previewLine = CreateLineImage("PreviewLine", new Color32(140, 220, 255, 255));
        }

        _previewLine.gameObject.SetActive(true);
        SetLineRect(_previewLine.rectTransform, startPosition, endPosition, 5f);
    }

    private void RemovePreviewLine()
    {
        if (_previewLine != null)
        {
            _previewLine.gameObject.SetActive(false);
        }
    }

    private Image CreateLineImage(string objectName, Color color)
    {
        GameObject lineObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(_lineLayer, false);
        Image image = lineObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void SetLineRect(RectTransform rectTransform, Vector2 startPosition, Vector2 endPosition, float thickness)
    {
        Vector2 direction = endPosition - startPosition;
        float length = direction.magnitude;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = (startPosition + endPosition) * 0.5f;
        rectTransform.sizeDelta = new Vector2(length, thickness);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private Vector2 GetNodeCenterLocalPosition(int nodeIndex)
    {
        if (_lineLayer == null || nodeIndex < 0 || nodeIndex >= _nodeRects.Count || _nodeRects[nodeIndex] == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter = _nodeRects[nodeIndex].TransformPoint(_nodeRects[nodeIndex].rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_lineLayer, screenPoint, null, out localPoint);
        return localPoint;
    }

    private Vector2 GetPointerLocalPosition(PointerEventData eventData)
    {
        if (_lineLayer == null || eventData == null)
        {
            return Vector2.zero;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_lineLayer, eventData.position, eventData.pressEventCamera, out localPoint);
        return localPoint;
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

    private void BindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(OnClickConfirm);
            _confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(OnClickReset);
            _resetButton.onClick.AddListener(OnClickReset);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickClose);
            _closeButton.onClick.AddListener(OnClickClose);
        }
    }

    private void EnsureLayout()
    {
        TryBindSceneReferences();

        if (_titleText != null && _instructionText != null && _playfield != null && _confirmButton != null && _closeButton != null)
        {
            ApplyBoundLayoutDefaults();
            BindButtons();
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
        background.color = new Color32(10, 16, 30, 224);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(ContentAnchorMin, ContentAnchorMin);
        contentRect.anchorMax = new Vector2(ContentAnchorMax, ContentAnchorMax);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(24, 34, 55, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)FooterReservedPadding);
        layout.spacing = ContentSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        _titleText = EnsureText(contentRoot.transform, "TitleText", font, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 54f);
        _instructionText = EnsureText(contentRoot.transform, "InstructionText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, InstructionBlockHeight);
        _aiAdviceText = EnsureText(contentRoot.transform, "AIAdviceText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, AdviceBlockHeight);

        GameObject playfieldObject = FindOrCreateChild(contentRoot, "Playfield");
        _playfield = EnsureRectTransform(playfieldObject);
        LayoutElement playfieldLayout = playfieldObject.GetComponent<LayoutElement>();
        if (playfieldLayout == null)
        {
            playfieldLayout = playfieldObject.AddComponent<LayoutElement>();
        }
        playfieldLayout.preferredHeight = PlayfieldHeight;
        playfieldLayout.minHeight = PlayfieldHeight;

        Image playfieldImage = playfieldObject.GetComponent<Image>();
        if (playfieldImage == null)
        {
            playfieldImage = playfieldObject.AddComponent<Image>();
        }
        playfieldImage.color = new Color32(17, 25, 42, 255);

        GameObject lineLayerObject = FindOrCreateChild(playfieldObject, "LineLayer");
        _lineLayer = EnsureRectTransform(lineLayerObject);
        StretchToParent(_lineLayer);

        GameObject nodeLayerObject = FindOrCreateChild(playfieldObject, "NodeLayer");
        _nodeLayer = EnsureRectTransform(nodeLayerObject);
        StretchToParent(_nodeLayer);

        _connectionText = EnsureText(contentRoot.transform, "ConnectionText", font, 22f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ConnectionBlockHeight);
        _feedbackText = EnsureText(contentRoot.transform, "FeedbackText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, FeedbackBlockHeight);

        GameObject footerObject = FindOrCreateChild(contentRoot, "FooterButtons");
        HorizontalLayoutGroup footerLayout = footerObject.GetComponent<HorizontalLayoutGroup>();
        if (footerLayout == null)
        {
            footerLayout = footerObject.AddComponent<HorizontalLayoutGroup>();
        }
        footerLayout.spacing = 18f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        LayoutElement footerElement = footerObject.GetComponent<LayoutElement>();
        if (footerElement == null)
        {
            footerElement = footerObject.AddComponent<LayoutElement>();
        }
        footerElement.ignoreLayout = true;
        footerElement.minHeight = FooterHeight;
        footerElement.preferredHeight = FooterHeight;

        RectTransform footerRect = EnsureRectTransform(footerObject);
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = new Vector2(ContentPadding, ContentPadding);
        footerRect.offsetMax = new Vector2(-ContentPadding, ContentPadding + FooterHeight);

        _confirmButton = EnsureButton(footerObject.transform, "ConfirmButton", font, "确认");
        _resetButton = EnsureButton(footerObject.transform, "ResetButton", font, "重置");
        _closeButton = EnsureButton(footerObject.transform, "CloseButton", font, "继续");
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
        background.color = new Color32(10, 16, 30, 224);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(ContentAnchorMin, ContentAnchorMin);
        contentRect.anchorMax = new Vector2(ContentAnchorMax, ContentAnchorMax);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        Image contentBackground = contentRoot.GetComponent<Image>();
        if (contentBackground == null)
        {
            contentBackground = contentRoot.AddComponent<Image>();
        }
        contentBackground.color = new Color32(24, 34, 55, 245);

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)FooterReservedPadding);
        layout.spacing = ContentSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        _titleText = EnsureText(contentRoot.transform, "TitleText", font, 34f, FontStyles.Bold, TextAlignmentOptions.Center, 54f);
        _instructionText = EnsureText(contentRoot.transform, "InstructionText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, InstructionBlockHeight);
        _aiAdviceText = EnsureText(contentRoot.transform, "AIAdviceText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, AdviceBlockHeight);

        GameObject playfieldObject = FindOrCreateChild(contentRoot, "Playfield");
        _playfield = EnsureRectTransform(playfieldObject);
        LayoutElement playfieldLayout = playfieldObject.GetComponent<LayoutElement>();
        if (playfieldLayout == null)
        {
            playfieldLayout = playfieldObject.AddComponent<LayoutElement>();
        }
        playfieldLayout.preferredHeight = PlayfieldHeight;
        playfieldLayout.minHeight = PlayfieldHeight;

        Image playfieldImage = playfieldObject.GetComponent<Image>();
        if (playfieldImage == null)
        {
            playfieldImage = playfieldObject.AddComponent<Image>();
        }
        playfieldImage.color = new Color32(17, 25, 42, 255);

        GameObject lineLayerObject = FindOrCreateChild(playfieldObject, "LineLayer");
        _lineLayer = EnsureRectTransform(lineLayerObject);
        StretchToParent(_lineLayer);

        GameObject nodeLayerObject = FindOrCreateChild(playfieldObject, "NodeLayer");
        _nodeLayer = EnsureRectTransform(nodeLayerObject);
        StretchToParent(_nodeLayer);

        _connectionText = EnsureText(contentRoot.transform, "ConnectionText", font, 22f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ConnectionBlockHeight);
        _feedbackText = EnsureText(contentRoot.transform, "FeedbackText", font, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, FeedbackBlockHeight);

        GameObject footerObject = FindOrCreateChild(contentRoot, "FooterButtons");
        HorizontalLayoutGroup footerLayout = footerObject.GetComponent<HorizontalLayoutGroup>();
        if (footerLayout == null)
        {
            footerLayout = footerObject.AddComponent<HorizontalLayoutGroup>();
        }
        footerLayout.spacing = 18f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        LayoutElement footerElement = footerObject.GetComponent<LayoutElement>();
        if (footerElement == null)
        {
            footerElement = footerObject.AddComponent<LayoutElement>();
        }
        footerElement.ignoreLayout = true;
        footerElement.minHeight = FooterHeight;
        footerElement.preferredHeight = FooterHeight;

        RectTransform footerRect = EnsureRectTransform(footerObject);
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = new Vector2(ContentPadding, ContentPadding);
        footerRect.offsetMax = new Vector2(-ContentPadding, ContentPadding + FooterHeight);

        _confirmButton = EnsureButton(footerObject.transform, "ConfirmButton", font, "确认");
        _resetButton = EnsureButton(footerObject.transform, "ResetButton", font, "重置");
        _closeButton = EnsureButton(footerObject.transform, "CloseButton", font, "继续");
    }

    private void TryBindSceneReferences()
    {
        _titleText = _titleText != null ? _titleText : FindChildComponent<TMP_Text>("PanelContent/TitleText");
        _instructionText = _instructionText != null ? _instructionText : FindChildComponent<TMP_Text>("PanelContent/InstructionText");
        _aiAdviceText = _aiAdviceText != null ? _aiAdviceText : FindChildComponent<TMP_Text>("PanelContent/AIAdviceText");
        _connectionText = _connectionText != null ? _connectionText : FindChildComponent<TMP_Text>("PanelContent/ConnectionText");
        _feedbackText = _feedbackText != null ? _feedbackText : FindChildComponent<TMP_Text>("PanelContent/FeedbackText");
        _playfield = _playfield != null ? _playfield : FindChildComponent<RectTransform>("PanelContent/Playfield");
        _lineLayer = _lineLayer != null ? _lineLayer : FindChildComponent<RectTransform>("PanelContent/Playfield/LineLayer");
        _nodeLayer = _nodeLayer != null ? _nodeLayer : FindChildComponent<RectTransform>("PanelContent/Playfield/NodeLayer");
        _confirmButton = _confirmButton != null ? _confirmButton : FindChildComponent<Button>("PanelContent/FooterButtons/ConfirmButton");
        _resetButton = _resetButton != null ? _resetButton : FindChildComponent<Button>("PanelContent/FooterButtons/ResetButton");
        _closeButton = _closeButton != null ? _closeButton : FindChildComponent<Button>("PanelContent/FooterButtons/CloseButton");
        RefreshStaticNodeReferences();
    }

    private void RefreshStaticNodeReferences()
    {
        _nodeRects.Clear();
        if (_nodeLayer == null)
        {
            return;
        }

        for (int index = 0; index < NodeAnchoredPositions.Length; index += 1)
        {
            Transform node = _nodeLayer.Find("Node" + (index + 1));
            if (node is RectTransform rectTransform)
            {
                _nodeRects.Add(rectTransform);
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

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color32(54, 89, 124, 255);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 58f;
        layoutElement.preferredHeight = 58f;

        GameObject labelObject = FindOrCreateChild(buttonObject, "Label");
        RectTransform labelRect = EnsureRectTransform(labelObject);
        StretchToParent(labelRect);

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

        Debug.LogError("[CPMGamePanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");
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

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
