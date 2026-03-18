using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopStatusBar : MonoBehaviour
{
    [Header("项目信息")]
    [SerializeField] private TMP_Text _projectInfoText;
    [SerializeField] private TMP_Text _phaseText;

    [Header("四维属性")]
    [SerializeField] private Slider _techSlider;
    [SerializeField] private TMP_Text _techValueText;
    [SerializeField] private Slider _commSlider;
    [SerializeField] private TMP_Text _commValueText;
    [SerializeField] private Slider _manageSlider;
    [SerializeField] private TMP_Text _manageValueText;
    [SerializeField] private Slider _stressSlider;
    [SerializeField] private TMP_Text _stressValueText;

    [Header("精力")]
    [SerializeField] private Slider _energySlider;
    [SerializeField] private TMP_Text _energyValueText;
    [SerializeField] private Button _quizEntryButton;
    [SerializeField] private Button _scheduleEntryButton;

    #region Unity Lifecycle

    private void Awake()
    {
        AutoBindIfNeeded();

        if (_quizEntryButton != null)
        {
            _quizEntryButton.onClick.AddListener(OnClickQuizEntry);
        }

        if (_scheduleEntryButton != null)
        {
            _scheduleEntryButton.onClick.AddListener(OnClickScheduleEntry);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDataChanged += UpdateDisplay;
            UpdateDisplay(GameManager.Instance.CurrentPlayerData);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDataChanged -= UpdateDisplay;
        }
    }

    private void OnDestroy()
    {
        if (_quizEntryButton != null)
        {
            _quizEntryButton.onClick.RemoveListener(OnClickQuizEntry);
        }

        if (_scheduleEntryButton != null)
        {
            _scheduleEntryButton.onClick.RemoveListener(OnClickScheduleEntry);
        }
    }

    #endregion

    #region Public API

    public void UpdateDisplay(PlayerData data)
    {
        if (data == null)
        {
            return;
        }

        string projectName = GameManager.Instance != null ? GameManager.Instance.GetCurrentProjectName() : $"项目{data.currentProject}";
        int totalWeeks = GameManager.Instance != null ? GameManager.Instance.GetCurrentProjectTotalWeeks() : GameConstants.PROJECT1_WEEKS;
        string phaseText = GameManager.Instance != null ? GameManager.Instance.GetCurrentPhaseText() : "-";

        if (_projectInfoText != null)
        {
            _projectInfoText.text = $"{projectName} 第{data.currentWeek}周/共{totalWeeks}周";
        }

        if (_phaseText != null)
        {
            _phaseText.text = phaseText;
        }

        UpdateSliderAndText(_techSlider, _techValueText, data.techPower, 100);
        UpdateSliderAndText(_commSlider, _commValueText, data.commPower, 100);
        UpdateSliderAndText(_manageSlider, _manageValueText, data.managePower, 100);
        UpdateSliderAndText(_stressSlider, _stressValueText, data.stressPower, 100);
        UpdateSliderAndText(_energySlider, _energyValueText, data.energy, GameConstants.BASE_ENERGY_PER_WEEK);
    }

    public void SetQuizEntryInteractable(bool interactable)
    {
        ApplyEntryButtonVisualState(_quizEntryButton, interactable);
    }

    public void SetScheduleEntryInteractable(bool interactable)
    {
        ApplyEntryButtonVisualState(_scheduleEntryButton, interactable);
    }

    #endregion

    #region Internal Helpers

    private void OnClickQuizEntry()
    {
        if (StoryManager.Instance != null && !StoryManager.Instance.CanOpenQuiz())
        {
            Debug.Log("[TopStatusBar] 当前阶段暂不可进入答题。\n");
            return;
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OpenQuizFromSchedule();
            return;
        }

        UIManager.Instance.ShowPanel("QuizPanel");
    }

    private void OnClickScheduleEntry()
    {
        if (StoryManager.Instance != null && !StoryManager.Instance.CanOpenSchedule())
        {
            Debug.Log("[TopStatusBar] 当前阶段暂不可进入日程安排。\n");
            return;
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OpenScheduleFromTopBar();
            return;
        }

        UIManager.Instance.ShowPanel("SchedulePanel");
    }

    private void AutoBindIfNeeded()
    {
        _projectInfoText = _projectInfoText != null ? _projectInfoText : FindText("ProjectInfoText");
        _phaseText = _phaseText != null ? _phaseText : FindText("PhaseText");

        _techSlider = _techSlider != null ? _techSlider : FindSlider("TechSlider");
        _techValueText = _techValueText != null ? _techValueText : FindText("TechValueText");

        _commSlider = _commSlider != null ? _commSlider : FindSlider("CommSlider");
        _commValueText = _commValueText != null ? _commValueText : FindText("CommValueText");

        _manageSlider = _manageSlider != null ? _manageSlider : FindSlider("ManageSlider");
        _manageValueText = _manageValueText != null ? _manageValueText : FindText("ManageValueText");

        _stressSlider = _stressSlider != null ? _stressSlider : FindSlider("StressSlider");
        _stressValueText = _stressValueText != null ? _stressValueText : FindText("StressValueText");

        _energySlider = _energySlider != null ? _energySlider : FindSlider("EnergySlider");
        _energyValueText = _energyValueText != null ? _energyValueText : FindText("EnergyValueText");

        if (_quizEntryButton == null)
        {
            Transform quizTarget = transform.Find("QuizButton");
            if (quizTarget != null)
            {
                _quizEntryButton = quizTarget.GetComponent<Button>();
            }
        }

        if (_scheduleEntryButton == null)
        {
            Transform scheduleTarget = transform.Find("ScheduleButton");
            if (scheduleTarget != null)
            {
                _scheduleEntryButton = scheduleTarget.GetComponent<Button>();
            }
        }

        if (_scheduleEntryButton == null)
        {
            _scheduleEntryButton = CreateScheduleButton();
        }

        EnsureEntryButtonVisuals();
        AdjustEnergyRowLayout();
    }

    private Button CreateScheduleButton()
    {
        GameObject buttonObject = new GameObject("ScheduleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = new Vector2(132f, 44f);

        Vector2 anchoredPosition = new Vector2(-128f, 12f);
        if (_quizEntryButton != null)
        {
            RectTransform quizRect = _quizEntryButton.GetComponent<RectTransform>();
            if (quizRect != null)
            {
                anchoredPosition = new Vector2(quizRect.anchoredPosition.x - quizRect.sizeDelta.x - 12f, quizRect.anchoredPosition.y);
            }
        }

        buttonRect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(63, 107, 186, 255);

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        if (_projectInfoText != null && _projectInfoText.font != null)
        {
            label.font = _projectInfoText.font;
        }

        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableWordWrapping = false;
        label.text = "日程安排";

        return button;
    }

    private void EnsureEntryButtonVisuals()
    {
        SetQuizEntryLabel();
        AlignScheduleButtonLeftOfQuiz();
    }

    private void SetQuizEntryLabel()
    {
        if (_quizEntryButton == null)
        {
            return;
        }

        RectTransform quizRect = _quizEntryButton.GetComponent<RectTransform>();
        if (quizRect != null && quizRect.sizeDelta.x < 96f)
        {
            quizRect.sizeDelta = new Vector2(96f, quizRect.sizeDelta.y);
        }

        TMP_Text quizLabel = _quizEntryButton.GetComponentInChildren<TMP_Text>(true);
        if (quizLabel == null)
        {
            return;
        }

        if (_projectInfoText != null && _projectInfoText.font != null)
        {
            quizLabel.font = _projectInfoText.font;
        }

        quizLabel.enableWordWrapping = false;
        quizLabel.alignment = TextAlignmentOptions.Center;
        quizLabel.fontSize = 24f;
        quizLabel.text = "答题";
    }

    private void AlignScheduleButtonLeftOfQuiz()
    {
        if (_scheduleEntryButton == null || _quizEntryButton == null)
        {
            return;
        }

        RectTransform scheduleRect = _scheduleEntryButton.GetComponent<RectTransform>();
        RectTransform quizRect = _quizEntryButton.GetComponent<RectTransform>();
        if (scheduleRect == null || quizRect == null)
        {
            return;
        }

        scheduleRect.anchorMin = quizRect.anchorMin;
        scheduleRect.anchorMax = quizRect.anchorMax;
        scheduleRect.pivot = quizRect.pivot;

        float spacing = 12f;
        scheduleRect.anchoredPosition = new Vector2(quizRect.anchoredPosition.x - quizRect.sizeDelta.x - spacing, quizRect.anchoredPosition.y);
    }

    private void AdjustEnergyRowLayout()
    {
        TMP_Text energyLabel = FindText("EnergyLabel");
        if (energyLabel != null)
        {
            SetLocalY(energyLabel.rectTransform, -144f);
        }

        if (_energyValueText != null)
        {
            SetLocalY(_energyValueText.rectTransform, -144f);
        }

        if (_energySlider != null)
        {
            RectTransform sliderRect = _energySlider.GetComponent<RectTransform>();
            SetLocalY(sliderRect, -140f);
        }
    }

    private static void SetLocalY(RectTransform rectTransform, float y)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector3 local = rectTransform.localPosition;
        if (!Mathf.Approximately(local.y, y))
        {
            rectTransform.localPosition = new Vector3(local.x, y, local.z);
        }
    }

    private static void ApplyEntryButtonVisualState(Button button, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        // Keep button clickable so users always get feedback from click handlers.
        button.interactable = true;

        Graphic target = button.targetGraphic;
        if (target != null)
        {
            Color graphicColor = target.color;
            graphicColor.a = interactable ? 1f : 0.6f;
            target.color = graphicColor;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Color labelColor = label.color;
            labelColor.a = interactable ? 1f : 0.7f;
            label.color = labelColor;
        }
    }

    private TMP_Text FindText(string childName)
    {
        Transform target = transform.Find(childName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Slider FindSlider(string childName)
    {
        Transform target = transform.Find(childName);
        return target != null ? target.GetComponent<Slider>() : null;
    }

    private static void UpdateSliderAndText(Slider slider, TMP_Text valueText, int value, int defaultMax)
    {
        if (valueText != null)
        {
            valueText.text = value.ToString();
        }

        if (slider == null)
        {
            return;
        }

        slider.minValue = 0;
        int targetMax = Mathf.Max(defaultMax, value);
        if (slider.maxValue < targetMax)
        {
            slider.maxValue = targetMax;
        }

        slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
    }

    #endregion
}