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

    #region Unity Lifecycle

    private void Awake()
    {
        AutoBindIfNeeded();
        if (_quizEntryButton != null)
        {
            _quizEntryButton.onClick.AddListener(OnClickQuizEntry);
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

    public void SetQuizEntryInteractable(bool interactable)
    {
        if (_quizEntryButton != null)
        {
            _quizEntryButton.interactable = interactable;
        }
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
            Transform target = transform.Find("QuizButton");
            if (target != null)
            {
                _quizEntryButton = target.GetComponent<Button>();
            }
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
