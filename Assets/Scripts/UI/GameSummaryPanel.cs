using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameSummaryPanel : MonoBehaviour
{
#if UNITY_EDITOR
    private const string SimsunFontAssetPath = "Assets/Fonts/SIMSUN SDF.asset";
#endif

    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _endingTitleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Transform _statsRoot;
    [SerializeField] private TMP_Text _journeyText;
    [SerializeField] private TMP_Text _overallRateText;
    [SerializeField] private TMP_Text _timelineLegendText;
    [SerializeField] private Transform _decisionTimelineRoot;
    [SerializeField] private Transform _projectRatesRoot;
    [SerializeField] private Transform _qualityRatesRoot;
    [SerializeField] private ScrollRect _detailsScrollRect;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private TMP_FontAsset _preferredChineseFont;

    private void Awake()
    {
        EnsureLayout();
        BindButtons();
    }

    private void OnDestroy()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnClickRestart);
        }

        if (_menuButton != null)
        {
            _menuButton.onClick.RemoveListener(OnClickMenu);
        }
    }

    public void ShowSummary(EndingResultData result)
    {
        EnsureLayout();
        gameObject.SetActive(true);
        RestorePanelVisibility();

        if (_titleText != null)
        {
            _titleText.text = "项目经理成长总结";
        }

        if (_endingTitleText != null)
        {
            _endingTitleText.text = result != null && !string.IsNullOrWhiteSpace(result.title) ? result.title : "最终结局";
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = result != null ? result.description : string.Empty;
        }

        RefreshStatsSection();
        RefreshJourneySection();
        RefreshAnalysisSection();
        RefreshLayout();
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

    private void BindButtons()
    {
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

    private void RefreshStatsSection()
    {
        PlayerData data = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
        int techPower = data != null ? data.techPower : 0;
        int commPower = data != null ? data.commPower : 0;
        int managePower = data != null ? data.managePower : 0;
        int stressPower = data != null ? data.stressPower : 0;
        int maxStat = Mathf.Max(100, techPower, commPower, managePower, stressPower);

        RefreshBarRow(_statsRoot, "TechRow", "技术力", techPower, techPower, maxStat, new Color32(78, 161, 222, 255));
        RefreshBarRow(_statsRoot, "CommRow", "沟通力", commPower, commPower, maxStat, new Color32(115, 192, 126, 255));
        RefreshBarRow(_statsRoot, "ManageRow", "管理力", managePower, managePower, maxStat, new Color32(236, 182, 87, 255));
        RefreshBarRow(_statsRoot, "StressRow", "抗压力", stressPower, stressPower, maxStat, new Color32(215, 114, 91, 255));
    }

    private void RefreshJourneySection()
    {
        if (_journeyText == null)
        {
            return;
        }

        int totalWeeks = GameManager.Instance != null ? GameManager.Instance.GetTotalWeeksPlayed() : 0;
        int totalDecisions = GameManager.Instance != null ? GameManager.Instance.GetTotalDecisionCount() : 0;
        int totalQuizAnswered = GameManager.Instance != null ? GameManager.Instance.GetTotalQuizAnsweredCount() : 0;
        int totalQuizCorrect = GameManager.Instance != null ? GameManager.Instance.GetTotalQuizCorrectCount() : 0;

        _journeyText.text = "已完成周数：" + totalWeeks + "\n"
            + "总决策数：" + totalDecisions + "\n"
            + "答题次数：" + totalQuizAnswered + "\n"
            + "答对题数：" + totalQuizCorrect;
    }

    private void RefreshAnalysisSection()
    {
        if (_overallRateText != null)
        {
            int totalDecisions = GameManager.Instance != null ? GameManager.Instance.GetTotalDecisionCount() : 0;
            int viewedCount = GameManager.Instance != null ? GameManager.Instance.GetAIViewedCountByProject(0) : 0;
            int followedCount = GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByProject(0) : 0;
            int viewRate = Mathf.RoundToInt((GameManager.Instance != null ? GameManager.Instance.GetAIAdviceViewRate() : 0f) * 100f);
            int adoptionRate = Mathf.RoundToInt((GameManager.Instance != null ? GameManager.Instance.GetAIAdoptionRate() : 0f) * 100f);
            _overallRateText.text = "查看建议：" + viewedCount + "/" + totalDecisions + "（" + viewRate + "%）\n"
                + "跟随建议：" + followedCount + "/" + totalDecisions + "（" + adoptionRate + "%）";
        }

        if (_timelineLegendText != null)
        {
            _timelineLegendText.text = "绿色=采纳建议  红色=未采纳  灰色=未查看";
        }

        RefreshDecisionTimeline();
        RefreshProjectRateRows();
        RefreshQualityRateRows();
    }

    private void RefreshDecisionTimeline()
    {
        if (_decisionTimelineRoot == null)
        {
            return;
        }

        GridLayoutGroup layout = _decisionTimelineRoot.GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            layout = _decisionTimelineRoot.gameObject.AddComponent<GridLayoutGroup>();
        }
        layout.cellSize = new Vector2(22f, 46f);
        layout.spacing = new Vector2(8f, 8f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 8;
        layout.childAlignment = TextAnchor.UpperLeft;

        List<AITrustRecord> records = BuildSortedRecords();
        EnsureTimelineEntryCount(records.Count);
        for (int index = 0; index < _decisionTimelineRoot.childCount; index += 1)
        {
            Transform entry = _decisionTimelineRoot.GetChild(index);
            bool hasRecord = index < records.Count;
            entry.gameObject.SetActive(hasRecord);
            if (!hasRecord)
            {
                continue;
            }

            AITrustRecord record = records[index];
            Image image = entry.GetComponent<Image>();
            if (image != null)
            {
                image.color = GetTimelineColor(record);
            }

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = record != null ? "P" + record.projectNumber + "-W" + record.weekNumber : string.Empty;
            }
        }
    }

    private void RefreshProjectRateRows()
    {
        RefreshBarRow(_projectRatesRoot, "Project1Row", "P1 采纳率", GameManager.Instance != null ? GameManager.Instance.GetTotalDecisionCount(1) : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByProject(1) : 0, 1f, new Color32(78, 161, 222, 255));
        RefreshBarRow(_projectRatesRoot, "Project2Row", "P2 采纳率", GameManager.Instance != null ? GameManager.Instance.GetTotalDecisionCount(2) : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByProject(2) : 0, 1f, new Color32(236, 182, 87, 255));
        RefreshBarRow(_projectRatesRoot, "Project3Row", "P3 采纳率", GameManager.Instance != null ? GameManager.Instance.GetTotalDecisionCount(3) : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByProject(3) : 0, 1f, new Color32(115, 192, 126, 255));
    }

    private void RefreshQualityRateRows()
    {
        RefreshBarRow(_qualityRatesRoot, "GoodQualityRow", "好建议采纳率", GameManager.Instance != null ? GameManager.Instance.GetAIRecordCountByQuality("good") : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByQuality("good") : 0, 1f, new Color32(88, 170, 105, 255));
        RefreshBarRow(_qualityRatesRoot, "NeutralQualityRow", "中性建议采纳率", GameManager.Instance != null ? GameManager.Instance.GetAIRecordCountByQuality("neutral") : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByQuality("neutral") : 0, 1f, new Color32(106, 146, 212, 255));
        RefreshBarRow(_qualityRatesRoot, "BadQualityRow", "坏建议采纳率", GameManager.Instance != null ? GameManager.Instance.GetAIRecordCountByQuality("bad") : 0, GameManager.Instance != null ? GameManager.Instance.GetAIFollowedCountByQuality("bad") : 0, 1f, new Color32(204, 96, 96, 255));
    }

    private void RefreshBarRow(Transform parent, string rowName, string labelText, int displayValue, int barValue, int maxValue, Color barColor)
    {
        float normalizedValue = maxValue > 0 ? Mathf.Clamp01((float)barValue / maxValue) : 0f;
        RefreshBarRow(parent, rowName, labelText, displayValue + "", normalizedValue, barColor, displayValue + "");
    }

    private void RefreshBarRow(Transform parent, string rowName, string labelText, int totalCount, int matchedCount, float maxValue, Color barColor)
    {
        float normalizedValue = totalCount > 0 ? Mathf.Clamp01(matchedCount / (float)totalCount) : 0f;
        int percent = totalCount > 0 ? Mathf.RoundToInt((matchedCount / (float)totalCount) * 100f) : 0;
        string valueText = matchedCount + "/" + totalCount + "（" + percent + "%）";
        RefreshBarRow(parent, rowName, labelText, valueText, normalizedValue, barColor, valueText);
    }

    private void RefreshBarRow(Transform parent, string rowName, string labelText, string metricText, float normalizedValue, Color barColor, string valueText)
    {
        if (parent == null)
        {
            return;
        }

        Transform row = EnsureBarRow(parent, rowName, labelText, barColor);
        if (row == null)
        {
            return;
        }

        TMP_Text label = row.Find("LabelText") != null ? row.Find("LabelText").GetComponent<TMP_Text>() : null;
        TMP_Text value = row.Find("ValueText") != null ? row.Find("ValueText").GetComponent<TMP_Text>() : null;
        Transform bar = row.Find("BarBackground");
        Transform fill = bar != null ? bar.Find("Fill") : null;

        if (label != null)
        {
            label.text = labelText;
        }

        if (value != null)
        {
            value.text = valueText;
        }

        if (fill != null)
        {
            RectTransform fillRect = fill as RectTransform;
            if (fillRect != null)
            {
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(normalizedValue, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }

            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = barColor;
            }
        }

        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = row.gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 42f;
        layoutElement.preferredHeight = 42f;
    }

    private List<AITrustRecord> BuildSortedRecords()
    {
        List<AITrustRecord> sortedRecords = new List<AITrustRecord>();
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null || GameManager.Instance.CurrentPlayerData.aiTrustRecords == null)
        {
            return sortedRecords;
        }

        foreach (AITrustRecord record in GameManager.Instance.CurrentPlayerData.aiTrustRecords)
        {
            if (record != null)
            {
                sortedRecords.Add(record);
            }
        }

        sortedRecords.Sort((left, right) =>
        {
            int projectCompare = left.projectNumber.CompareTo(right.projectNumber);
            if (projectCompare != 0)
            {
                return projectCompare;
            }

            int weekCompare = left.weekNumber.CompareTo(right.weekNumber);
            if (weekCompare != 0)
            {
                return weekCompare;
            }

            return string.Compare(left.eventId, right.eventId, StringComparison.Ordinal);
        });

        return sortedRecords;
    }

    private void EnsureTimelineEntryCount(int targetCount)
    {
        if (_decisionTimelineRoot == null)
        {
            return;
        }

        while (_decisionTimelineRoot.childCount < targetCount)
        {
            GameObject entry = new GameObject("DecisionBar" + (_decisionTimelineRoot.childCount + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            entry.transform.SetParent(_decisionTimelineRoot, false);

            Image image = entry.GetComponent<Image>();
            image.color = new Color32(72, 72, 72, 255);

            LayoutElement layoutElement = entry.GetComponent<LayoutElement>();
            layoutElement.minHeight = 46f;
            layoutElement.preferredHeight = 46f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(entry.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.color = Color.white;
        }
    }

    private static Color GetTimelineColor(AITrustRecord record)
    {
        if (record == null)
        {
            return new Color32(72, 72, 72, 255);
        }

        if (!record.hasViewed)
        {
            return new Color32(112, 112, 112, 255);
        }

        if (record.isFollowed || record.adoptedAIAdvice)
        {
            return new Color32(86, 164, 98, 255);
        }

        return new Color32(198, 96, 96, 255);
    }

    private void RefreshLayout()
    {
        ApplyAllFonts();

        if (_detailsScrollRect != null && _detailsScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_detailsScrollRect.content);
            _detailsScrollRect.verticalNormalizedPosition = 1f;
        }

        if (_contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        }
    }

    private void EnsureLayout()
    {
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
        _contentRoot.anchorMin = new Vector2(0.12f, 0.06f);
        _contentRoot.anchorMax = new Vector2(0.88f, 0.94f);
        _contentRoot.offsetMin = Vector2.zero;
        _contentRoot.offsetMax = Vector2.zero;

        Image contentImage = contentObject.GetComponent<Image>();
        if (contentImage == null)
        {
            contentImage = contentObject.AddComponent<Image>();
        }
        contentImage.color = new Color32(26, 34, 54, 245);

        VerticalLayoutGroup rootLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        if (rootLayout == null)
        {
            rootLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        }
        rootLayout.padding = new RectOffset(28, 28, 28, 28);
        rootLayout.spacing = 18f;
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        _titleText = EnsureText(contentObject.transform, "TitleText", 34f, FontStyles.Bold, TextAlignmentOptions.Center, 60f);

        _detailsScrollRect = EnsureScrollRect(contentObject.transform, "DetailsScroll");
        Transform detailsContent = _detailsScrollRect != null ? _detailsScrollRect.content : null;

        Transform endingSection = EnsureSection(detailsContent, "EndingSection", "最终结局");
        _endingTitleText = EnsureText(endingSection, "EndingTitleText", 30f, FontStyles.Bold, TextAlignmentOptions.Left, 54f);
        _descriptionText = EnsureText(endingSection, "DescriptionText", 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 140f);

        Transform statsSection = EnsureSection(detailsContent, "StatsSection", "四维属性总览");
        _statsRoot = EnsureVerticalRoot(statsSection, "StatsRoot", 12f);
        EnsureBarRow(_statsRoot, "TechRow", "技术力", new Color32(78, 161, 222, 255));
        EnsureBarRow(_statsRoot, "CommRow", "沟通力", new Color32(115, 192, 126, 255));
        EnsureBarRow(_statsRoot, "ManageRow", "管理力", new Color32(236, 182, 87, 255));
        EnsureBarRow(_statsRoot, "StressRow", "抗压力", new Color32(215, 114, 91, 255));

        Transform journeySection = EnsureSection(detailsContent, "JourneySection", "Journey 统计");
        _journeyText = EnsureText(journeySection, "JourneyText", 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, 108f);

        Transform aiSection = EnsureSection(detailsContent, "AIAnalysisSection", "AI 采纳分析");
        _overallRateText = EnsureText(aiSection, "OverallRateText", 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, 88f);
        _timelineLegendText = EnsureText(aiSection, "TimelineLegendText", 20f, FontStyles.Normal, TextAlignmentOptions.Left, 36f);
        _decisionTimelineRoot = EnsureGridRoot(aiSection, "DecisionTimelineRoot");
        _projectRatesRoot = EnsureVerticalRoot(aiSection, "ProjectRatesRoot", 10f);
        _qualityRatesRoot = EnsureVerticalRoot(aiSection, "QualityRatesRoot", 10f);
        EnsureBarRow(_projectRatesRoot, "Project1Row", "P1 采纳率", new Color32(78, 161, 222, 255));
        EnsureBarRow(_projectRatesRoot, "Project2Row", "P2 采纳率", new Color32(236, 182, 87, 255));
        EnsureBarRow(_projectRatesRoot, "Project3Row", "P3 采纳率", new Color32(115, 192, 126, 255));
        EnsureBarRow(_qualityRatesRoot, "GoodQualityRow", "好建议采纳率", new Color32(88, 170, 105, 255));
        EnsureBarRow(_qualityRatesRoot, "NeutralQualityRow", "中性建议采纳率", new Color32(106, 146, 212, 255));
        EnsureBarRow(_qualityRatesRoot, "BadQualityRow", "坏建议采纳率", new Color32(204, 96, 96, 255));

        GameObject buttonRow = FindOrCreateChild(contentObject, "ButtonRow");
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

        _restartButton = EnsureButton(buttonRow.transform, "RestartButton", "重新开始");
        _menuButton = EnsureButton(buttonRow.transform, "MenuButton", "返回主菜单");

        ApplyDecorativeTheme(contentObject.transform);
        ApplyAllFonts();
    }

    private void ApplyDecorativeTheme(Transform contentRoot)
    {
        if (contentRoot == null)
        {
            return;
        }

        Image contentImage = contentRoot.GetComponent<Image>();
        if (contentImage != null)
        {
            contentImage.color = new Color32(18, 28, 46, 246);
        }

        Outline contentOutline = contentRoot.GetComponent<Outline>();
        if (contentOutline == null)
        {
            contentOutline = contentRoot.gameObject.AddComponent<Outline>();
        }
        contentOutline.effectColor = new Color32(147, 194, 255, 64);
        contentOutline.effectDistance = new Vector2(1f, -1f);

        GameObject topIconObject = FindOrCreateChild(contentRoot.gameObject, "TopIcon");
        topIconObject.transform.SetSiblingIndex(0);
        LayoutElement topIconLayout = topIconObject.GetComponent<LayoutElement>();
        if (topIconLayout == null)
        {
            topIconLayout = topIconObject.AddComponent<LayoutElement>();
        }
        topIconLayout.minHeight = 46f;
        topIconLayout.preferredHeight = 46f;

        RectTransform topIconRect = EnsureRectTransform(topIconObject);
        topIconRect.sizeDelta = new Vector2(46f, 46f);

        Image topIconImage = topIconObject.GetComponent<Image>();
        if (topIconImage == null)
        {
            topIconImage = topIconObject.AddComponent<Image>();
        }
        topIconImage.sprite = UIVisualResources.LoadIcon("personal_experience_icon");
        topIconImage.preserveAspect = true;
        topIconImage.color = new Color32(216, 232, 255, 255);
        topIconImage.raycastTarget = false;

        GameObject watermarkObject = FindOrCreateChild(contentRoot.gameObject, "Watermark");
        RectTransform watermarkRect = EnsureRectTransform(watermarkObject);
        watermarkRect.anchorMin = new Vector2(1f, 1f);
        watermarkRect.anchorMax = new Vector2(1f, 1f);
        watermarkRect.pivot = new Vector2(1f, 1f);
        watermarkRect.sizeDelta = new Vector2(84f, 84f);
        watermarkRect.anchoredPosition = new Vector2(-18f, -18f);

        LayoutElement watermarkLayout = watermarkObject.GetComponent<LayoutElement>();
        if (watermarkLayout == null)
        {
            watermarkLayout = watermarkObject.AddComponent<LayoutElement>();
        }
        watermarkLayout.ignoreLayout = true;

        Image watermarkImage = watermarkObject.GetComponent<Image>();
        if (watermarkImage == null)
        {
            watermarkImage = watermarkObject.AddComponent<Image>();
        }
        watermarkImage.sprite = UIVisualResources.LoadIcon("progress");
        watermarkImage.preserveAspect = true;
        watermarkImage.color = new Color32(164, 206, 255, 18);
        watermarkImage.raycastTarget = false;

        if (_titleText != null)
        {
            _titleText.transform.SetSiblingIndex(1);
            _titleText.color = new Color32(245, 249, 255, 255);
        }

        EnsureButtonIcon(_restartButton, "protagonist");
        EnsureButtonIcon(_menuButton, "reminder");
    }

    private Transform EnsureSection(Transform parent, string name, string headerText)
    {
        GameObject sectionObject = FindOrCreateChild(parent != null ? parent.gameObject : gameObject, name);
        Image background = sectionObject.GetComponent<Image>();
        if (background == null)
        {
            background = sectionObject.AddComponent<Image>();
        }
        background.color = new Color32(18, 25, 40, 170);

        VerticalLayoutGroup layout = sectionObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = sectionObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = sectionObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = sectionObject.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleHeight = 0f;

        TMP_Text header = EnsureText(sectionObject.transform, "SectionHeaderText", 26f, FontStyles.Bold, TextAlignmentOptions.Left, 40f);
        header.text = headerText;
        return sectionObject.transform;
    }

    private static Transform EnsureVerticalRoot(Transform parent, string name, float spacing)
    {
        GameObject root = FindOrCreateChild(parent != null ? parent.gameObject : null, name);
        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = root.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return root.transform;
    }

    private static Transform EnsureGridRoot(Transform parent, string name)
    {
        GameObject root = FindOrCreateChild(parent != null ? parent.gameObject : null, name);
        LayoutElement layoutElement = root.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = root.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 160f;
        layoutElement.preferredHeight = 160f;
        return root.transform;
    }

    private Transform EnsureBarRow(Transform parent, string rowName, string labelText, Color barColor)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject rowObject = FindOrCreateChild(parent.gameObject, rowName);
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        }
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        EnsureRowIcon(rowObject.transform, rowName);
        TMP_Text label = EnsureText(rowObject.transform, "LabelText", 22f, FontStyles.Normal, TextAlignmentOptions.Left, 36f);
        LayoutElement labelLayout = label.GetComponent<LayoutElement>();
        if (labelLayout == null)
        {
            labelLayout = label.gameObject.AddComponent<LayoutElement>();
        }
        labelLayout.minWidth = 148f;
        labelLayout.preferredWidth = 148f;
        label.text = labelText;

        GameObject barBackground = FindOrCreateChild(rowObject, "BarBackground");
        Image background = barBackground.GetComponent<Image>();
        if (background == null)
        {
            background = barBackground.AddComponent<Image>();
        }
        background.color = new Color32(49, 58, 79, 255);

        LayoutElement barLayout = barBackground.GetComponent<LayoutElement>();
        if (barLayout == null)
        {
            barLayout = barBackground.AddComponent<LayoutElement>();
        }
        barLayout.flexibleWidth = 1f;
        barLayout.minHeight = 24f;
        barLayout.preferredHeight = 24f;

        RectTransform barRect = EnsureRectTransform(barBackground);
        barRect.sizeDelta = new Vector2(0f, 24f);

        GameObject fillObject = FindOrCreateChild(barBackground, "Fill");
        Image fillImage = fillObject.GetComponent<Image>();
        if (fillImage == null)
        {
            fillImage = fillObject.AddComponent<Image>();
        }
        fillImage.color = barColor;

        RectTransform fillRect = EnsureRectTransform(fillObject);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        TMP_Text value = EnsureText(rowObject.transform, "ValueText", 22f, FontStyles.Bold, TextAlignmentOptions.Right, 36f);
        LayoutElement valueLayout = value.GetComponent<LayoutElement>();
        if (valueLayout == null)
        {
            valueLayout = value.gameObject.AddComponent<LayoutElement>();
        }
        valueLayout.minWidth = 138f;
        valueLayout.preferredWidth = 138f;

        return rowObject.transform;
    }

    private ScrollRect EnsureScrollRect(Transform parent, string name)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject scrollObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        if (existing == null && parent != null)
        {
            scrollObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = scrollObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = scrollObject.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 360f;
        layoutElement.preferredHeight = 520f;

        Image background = scrollObject.GetComponent<Image>();
        if (background == null)
        {
            background = scrollObject.AddComponent<Image>();
        }
        background.color = new Color32(15, 22, 35, 160);

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
        contentLayout.spacing = 16f;
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

    private TMP_Text EnsureText(Transform parent, string name, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, float minHeight)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null && parent != null)
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

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.margin = new Vector4(12f, 8f, 12f, 8f);
        return text;
    }

    private Button EnsureButton(Transform parent, string name, string labelText)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (existing == null && parent != null)
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
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = labelText;
        return button;
    }

    private static void EnsureButtonIcon(Button button, string iconResource)
    {
        if (button == null)
        {
            return;
        }

        GameObject iconObject = FindOrCreateChild(button.gameObject, "Icon");
        RectTransform iconRect = EnsureRectTransform(iconObject);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(18f, 18f);
        iconRect.anchoredPosition = new Vector2(14f, 0f);

        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = iconObject.AddComponent<Image>();
        }
        iconImage.sprite = UIVisualResources.LoadIcon(iconResource);
        iconImage.preserveAspect = true;
        iconImage.color = new Color32(236, 243, 255, 255);
        iconImage.raycastTarget = false;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(28f, 0f, 10f, 0f);
        }
    }

    private static void EnsureRowIcon(Transform row, string rowName)
    {
        string iconResource = GetRowIconResource(rowName);
        if (string.IsNullOrWhiteSpace(iconResource) || row == null)
        {
            return;
        }

        GameObject iconObject = FindOrCreateChild(row.gameObject, "Icon");
        iconObject.transform.SetAsFirstSibling();

        LayoutElement layoutElement = iconObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = iconObject.AddComponent<LayoutElement>();
        }
        layoutElement.minWidth = 22f;
        layoutElement.preferredWidth = 22f;
        layoutElement.minHeight = 22f;
        layoutElement.preferredHeight = 22f;

        RectTransform iconRect = EnsureRectTransform(iconObject);
        iconRect.sizeDelta = new Vector2(22f, 22f);

        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = iconObject.AddComponent<Image>();
        }
        iconImage.sprite = UIVisualResources.LoadIcon(iconResource);
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
    }

    private static string GetRowIconResource(string rowName)
    {
        switch (rowName)
        {
            case "TechRow":
                return "technical_skill_icon";
            case "CommRow":
                return "communication_skill_icon";
            case "ManageRow":
                return "management_skill_icon";
            case "StressRow":
                return "stress_resistance_icon";
            case "Project1Row":
            case "Project2Row":
            case "Project3Row":
                return "progress";
            case "GoodQualityRow":
            case "NeutralQualityRow":
            case "BadQualityRow":
                return "dialogue_choice";
            default:
                return null;
        }
    }

    private void ApplyAllFonts()
    {
        TMP_FontAsset sharedFont = ResolveUIFont();
        if (sharedFont == null)
        {
            return;
        }

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < allTexts.Length; index += 1)
        {
            if (allTexts[index] != null)
            {
                allTexts[index].font = sharedFont;
            }
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

        Debug.LogError("[GameSummaryPanel] : Missing required TMP Chinese font reference: Assets/Fonts/SIMSUN SDF.asset");
        return TMP_Settings.defaultFontAsset;
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

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
