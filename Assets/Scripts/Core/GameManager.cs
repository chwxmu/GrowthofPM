using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private const string TechPowerStatKey = "techPower";
    private const string CommPowerStatKey = "commPower";
    private const string ManagePowerStatKey = "managePower";
    private const string StressPowerStatKey = "stressPower";

    [SerializeField] private GameState _currentState = GameState.Menu;
    [SerializeField] private PlayerData _currentPlayerData;

    private ProjectStoryData _currentProjectStory;

    public event Action<PlayerData> PlayerDataChanged;

    public GameState CurrentState => _currentState;
    public PlayerData CurrentPlayerData => _currentPlayerData;
    public ProjectStoryData CurrentProjectStory => _currentProjectStory;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        TryBootstrapGameSceneWhenDirectPlay();
    }


    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    #endregion

    #region Public API

    public void StartNewGame()
    {
        _currentPlayerData = new PlayerData();
        _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
        _currentState = GameState.Playing;

        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        PlayerData loadedData = DataManager.Instance.LoadGame();
        if (loadedData == null)
        {
            Debug.LogWarning("[GameManager] 没有可用存档，已忽略继续游戏。\n");
            return;
        }

        _currentPlayerData = loadedData;
        _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
        _currentState = GameState.Playing;

        SceneManager.LoadScene("GameScene");
    }

    public void ApplyStatChanges(StatEffects effects)
    {
        if (_currentPlayerData == null || effects == null)
        {
            return;
        }

        _currentPlayerData.techPower = ClampStat(_currentPlayerData.techPower + effects.techPower);
        _currentPlayerData.commPower = ClampStat(_currentPlayerData.commPower + effects.commPower);
        _currentPlayerData.managePower = ClampStat(_currentPlayerData.managePower + effects.managePower);
        _currentPlayerData.stressPower = ClampStat(_currentPlayerData.stressPower + effects.stressPower);

        NotifyDataChanged();
    }

    public void AddEnergy(int amount)
    {
        if (_currentPlayerData == null || amount <= 0)
        {
            return;
        }

        _currentPlayerData.energy += amount;
        NotifyDataChanged();
    }

    public void ConsumeEnergy(int amount)
    {
        if (_currentPlayerData == null || amount <= 0)
        {
            return;
        }

        _currentPlayerData.energy = Mathf.Max(0, _currentPlayerData.energy - amount);
        NotifyDataChanged();
    }

    public void SetEnergy(int amount)
    {
        if (_currentPlayerData == null)
        {
            return;
        }

        _currentPlayerData.energy = Mathf.Max(0, amount);
        NotifyDataChanged();
    }

    public int GetTotalStats()
    {
        if (_currentPlayerData == null)
        {
            return 0;
        }

        return _currentPlayerData.techPower + _currentPlayerData.commPower + _currentPlayerData.managePower + _currentPlayerData.stressPower;
    }

    public void SaveProgress()
    {
        if (_currentPlayerData == null)
        {
            return;
        }

        DataManager.Instance.SaveGame(_currentPlayerData);
    }

    public void NextWeek()
    {
        if (_currentPlayerData == null)
        {
            return;
        }

        _currentPlayerData.currentWeek += 1;
        int totalWeeks = GetWeeksForProject(_currentPlayerData.currentProject);

        if (_currentPlayerData.currentWeek > totalWeeks)
        {
            if (_currentPlayerData.currentProject < 3)
            {
                _currentPlayerData.currentProject += 1;
                _currentPlayerData.currentWeek = 1;
                _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
            }
            else
            {
                _currentPlayerData.currentWeek = totalWeeks;
                _currentState = GameState.Paused;
            }
        }

        _currentPlayerData.energy = GameConstants.BASE_ENERGY_PER_WEEK;

        SaveProgress();
        NotifyDataChanged();
    }

    public void StartProject(int projectNumber)
    {
        if (_currentPlayerData == null)
        {
            _currentPlayerData = new PlayerData();
        }

        _currentPlayerData.currentProject = Mathf.Clamp(projectNumber, 1, 3);
        _currentPlayerData.currentWeek = 1;
        _currentPlayerData.energy = GameConstants.BASE_ENERGY_PER_WEEK;
        _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
        _currentState = GameState.Playing;
        NotifyDataChanged();
    }

    public void SetCurrentWeek(int weekNumber)
    {
        if (_currentPlayerData == null)
        {
            return;
        }

        _currentPlayerData.currentWeek = Mathf.Max(1, weekNumber);
        NotifyDataChanged();
    }

    public void ReloadCurrentProjectStory()
    {
        if (_currentPlayerData == null)
        {
            return;
        }

        _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
    }

    public void RecordAIAdviceAdoption(string eventId, bool adoptedAIAdvice)
    {
        RecordAIDecision(eventId, false, adoptedAIAdvice, 0);
    }

    public void RecordAIDecision(string eventId, bool hasViewed, bool isFollowed, int decisionLatencyMs, string aiQuality = "")
    {
        if (_currentPlayerData == null || string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        if (_currentPlayerData.aiTrustRecords == null)
        {
            _currentPlayerData.aiTrustRecords = new List<AITrustRecord>();
        }

        int currentProjectNumber = GetCurrentProjectNumber();
        AITrustRecord existingRecord = _currentPlayerData.aiTrustRecords.Find(record => record != null
            && string.Equals(record.eventId, eventId, StringComparison.Ordinal)
            && (record.projectNumber == currentProjectNumber || record.projectNumber <= 0));
        if (existingRecord == null)
        {
            existingRecord = new AITrustRecord
            {
                eventId = eventId
            };
            _currentPlayerData.aiTrustRecords.Add(existingRecord);
        }

        existingRecord.projectNumber = currentProjectNumber;
        existingRecord.weekNumber = _currentPlayerData.currentWeek;
        existingRecord.adoptedAIAdvice = isFollowed;
        existingRecord.aiQuality = NormalizeAIQuality(aiQuality);
        existingRecord.hasViewed = hasViewed;
        existingRecord.isFollowed = isFollowed;
        existingRecord.decisionLatencyMs = Mathf.Max(0, decisionLatencyMs);

        SaveProgress();
        NotifyDataChanged();
    }

    public void SetEventFlag(string flagId, bool value)
    {
        if (_currentPlayerData == null || string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        if (_currentPlayerData.eventFlags == null)
        {
            _currentPlayerData.eventFlags = new List<EventFlagRecord>();
        }

        string normalizedFlagId = NormalizeEventFlagId(flagId);
        int currentProjectNumber = GetCurrentProjectNumber();
        EventFlagRecord existingRecord = FindEventFlagRecord(normalizedFlagId, currentProjectNumber);
        if (existingRecord == null)
        {
            existingRecord = new EventFlagRecord
            {
                flagId = normalizedFlagId,
                projectNumber = currentProjectNumber
            };
            _currentPlayerData.eventFlags.Add(existingRecord);
        }

        existingRecord.value = value;
        SaveProgress();
    }

    public bool TryGetEventFlag(string flagId, out bool value)
    {
        value = false;
        if (_currentPlayerData == null || _currentPlayerData.eventFlags == null || string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        EventFlagRecord existingRecord = FindEventFlagRecord(NormalizeEventFlagId(flagId), GetCurrentProjectNumber());
        if (existingRecord == null)
        {
            return false;
        }

        value = existingRecord.value;
        return true;
    }

    public void ApplyRiskChange(int riskChange)
    {
        if (_currentPlayerData == null || riskChange == 0)
        {
            return;
        }

        _currentPlayerData.hiddenRisk = Mathf.Max(0, _currentPlayerData.hiddenRisk + riskChange);
        NotifyDataChanged();
    }

    public float GetAIAdoptionRate()
    {
        if (_currentPlayerData == null || _currentPlayerData.aiTrustRecords == null || _currentPlayerData.aiTrustRecords.Count == 0)
        {
            return 0f;
        }

        int adoptedCount = 0;
        int totalCount = 0;
        foreach (AITrustRecord record in _currentPlayerData.aiTrustRecords)
        {
            if (record == null)
            {
                continue;
            }

            bool isFollowed = record.isFollowed || record.adoptedAIAdvice;
            if (isFollowed)
            {
                adoptedCount += 1;
            }

            totalCount += 1;
        }

        if (totalCount <= 0)
        {
            return 0f;
        }

        return (float)adoptedCount / totalCount;
    }

    public EndingResultData EvaluateCurrentProjectEnding()
    {
        if (_currentPlayerData == null)
        {
            return null;
        }

        EndingsData endingsData = DataManager.Instance.LoadEndings();
        if (endingsData == null || endingsData.projects == null)
        {
            return null;
        }

        ProjectEndingData projectEnding = endingsData.projects.Find(item => item != null && item.projectNumber == _currentPlayerData.currentProject);
        if (projectEnding == null)
        {
            return null;
        }

        if (projectEnding.riskFailThreshold >= 0 && _currentPlayerData.hiddenRisk >= projectEnding.riskFailThreshold)
        {
            return projectEnding.fail;
        }

        if (MeetsEndingRequirements(projectEnding.excellentThreshold, projectEnding.excellentStatThresholds))
        {
            return projectEnding.excellent;
        }

        if (MeetsEndingRequirements(projectEnding.passThreshold, projectEnding.passStatThresholds))
        {
            return projectEnding.pass;
        }

        return projectEnding.fail;
    }


    private bool MeetsEndingRequirements(int totalThreshold, EndingStatThresholdData statThresholds)
    {
        if (statThresholds == null)
        {
            Debug.LogError($"[GameManager] 结局单项阈值配置缺失: project={_currentPlayerData.currentProject}");
            return false;
        }

        if (GetTotalStats() < totalThreshold)
        {
            return false;
        }

        return GetStatValue(TechPowerStatKey) >= statThresholds.techPower
            && GetStatValue(CommPowerStatKey) >= statThresholds.commPower
            && GetStatValue(ManagePowerStatKey) >= statThresholds.managePower
            && GetStatValue(StressPowerStatKey) >= statThresholds.stressPower;
    }

    public bool HasNextProject()
    {
        return _currentPlayerData != null && _currentPlayerData.currentProject < 3;
    }

    public bool AdvanceToNextProject()
    {
        if (!HasNextProject())
        {
            return false;
        }

        StartProject(_currentPlayerData.currentProject + 1);
        SaveProgress();
        return true;
    }

    public int GetStatValue(string statKey)
    {
        if (_currentPlayerData == null || string.IsNullOrWhiteSpace(statKey))
        {
            return 0;
        }

        switch (statKey.Trim())
        {
            case "techPower":
                return _currentPlayerData.techPower;
            case "commPower":
                return _currentPlayerData.commPower;
            case "managePower":
                return _currentPlayerData.managePower;
            case "stressPower":
                return _currentPlayerData.stressPower;
            default:
                return 0;
        }
    }

    public string GetCurrentProjectName()
    {
        if (_currentProjectStory != null && !string.IsNullOrEmpty(_currentProjectStory.projectName))
        {
            return _currentProjectStory.projectName;
        }

        int projectNumber = _currentPlayerData != null ? _currentPlayerData.currentProject : 1;
        return $"项目{projectNumber}";
    }

    public int GetCurrentProjectTotalWeeks()
    {
        if (_currentProjectStory != null && _currentProjectStory.totalWeeks > 0)
        {
            return _currentProjectStory.totalWeeks;
        }

        int projectNumber = _currentPlayerData != null ? _currentPlayerData.currentProject : 1;
        return GetWeeksForProject(projectNumber);
    }

    public WeekEventData GetCurrentWeekEvent()
    {
        if (_currentProjectStory == null || _currentProjectStory.weeks == null || _currentPlayerData == null)
        {
            return null;
        }

        return _currentProjectStory.weeks.Find(item => item.weekNumber == _currentPlayerData.currentWeek);
    }

    public string GetCurrentPhaseText()
    {
        WeekEventData currentWeek = GetCurrentWeekEvent();
        if (currentWeek == null || string.IsNullOrWhiteSpace(currentWeek.phase))
        {
            return "-";
        }

        switch (currentWeek.phase.Trim().ToLowerInvariant())
        {
            case "startup":
                return "启动";
            case "planning":
                return "计划";
            case "execution":
                return "执行";
            case "monitoring":
                return "监控";
            case "closing":
                return "收尾";
            default:
                return "-";
        }
    }

    #endregion

    #region Internal Helpers

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuScene")
        {
            _currentState = GameState.Menu;
            return;
        }

        if (scene.name != "GameScene")
        {
            return;
        }

        InitializeGameSceneRuntime();
    }

    private void TryBootstrapGameSceneWhenDirectPlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "GameScene" || _currentState == GameState.Playing)
        {
            return;
        }

        Debug.Log("[GameManager] 检测到直接在 GameScene 运行，自动初始化主流程。");
        _currentState = GameState.Playing;
        InitializeGameSceneRuntime();
    }

    private void InitializeGameSceneRuntime()
    {
        if (_currentPlayerData == null)
        {
            _currentPlayerData = new PlayerData();
        }

        if (_currentProjectStory == null)
        {
            _currentProjectStory = DataManager.Instance.LoadProjectStory(_currentPlayerData.currentProject);
        }

        UIManager.Instance.RebuildPanelRegistry();
        UIManager.Instance.HideAllPanels();
        NotifyDataChanged();
        StoryManager.Instance.HandleGameSceneLoaded();
    }



    private void NotifyDataChanged()
    {
        PlayerDataChanged?.Invoke(_currentPlayerData);

        TopStatusBar topStatusBar = FindObjectOfType<TopStatusBar>(true);
        if (topStatusBar != null)
        {
            topStatusBar.UpdateDisplay(_currentPlayerData);
        }
    }

    private static int ClampStat(int value)
    {
        return Mathf.Max(0, value);
    }

    private int GetCurrentProjectNumber()
    {
        return _currentPlayerData != null ? _currentPlayerData.currentProject : 0;
    }

    private EventFlagRecord FindEventFlagRecord(string flagId, int projectNumber)
    {
        if (_currentPlayerData == null || _currentPlayerData.eventFlags == null || string.IsNullOrWhiteSpace(flagId))
        {
            return null;
        }

        return _currentPlayerData.eventFlags.Find(record => record != null
            && string.Equals(record.flagId, flagId, StringComparison.Ordinal)
            && record.projectNumber == projectNumber);
    }

    private static string NormalizeAIQuality(string aiQuality)
    {
        return string.IsNullOrWhiteSpace(aiQuality) ? string.Empty : aiQuality.Trim().ToLowerInvariant();
    }

    private static string NormalizeEventFlagId(string flagId)
    {
        return string.IsNullOrWhiteSpace(flagId) ? string.Empty : flagId.Trim();
    }

    private static int GetWeeksForProject(int projectNumber)
    {
        switch (projectNumber)
        {
            case 1:
                return GameConstants.PROJECT1_WEEKS;
            case 2:
                return GameConstants.PROJECT2_WEEKS;
            case 3:
                return GameConstants.PROJECT3_WEEKS;
            default:
                return GameConstants.PROJECT1_WEEKS;
        }
    }

    #endregion
}


