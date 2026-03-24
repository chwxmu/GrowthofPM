using System;

/// <summary>
/// Runs the Project 2 risk dashboard reaction mini-game.
/// </summary>
public class RiskDashboardGame
{
    private const float DefaultDurationSeconds = 30f;
    private const float DefaultAlertLifetimeSeconds = 3f;
    private const float DefaultSpawnIntervalSeconds = 1.1f;

    private readonly Random _random;
    private readonly ModuleState[] _modules;

    private float _timeRemaining;
    private float _spawnTimer;
    private bool _isRunning;
    private int _resolvedCount;
    private int _missedCount;
    private int _wrongClicks;
    private int _totalRiskChange;

    /// <summary>
    /// Captures the final result of a dashboard session.
    /// </summary>
    public sealed class SessionResult
    {
        public SessionResult(int resolvedCount, int missedCount, int wrongClicks, int totalRiskChange)
        {
            ResolvedCount = resolvedCount;
            MissedCount = missedCount;
            WrongClicks = wrongClicks;
            TotalRiskChange = totalRiskChange;
        }

        public int ResolvedCount { get; private set; }
        public int MissedCount { get; private set; }
        public int WrongClicks { get; private set; }
        public int TotalRiskChange { get; private set; }
    }

    private sealed class ModuleState
    {
        public ModuleState(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public bool IsAlert { get; set; }
        public float AlertElapsedSeconds { get; set; }
    }

    public RiskDashboardGame(int seed = 0)
    {
        _random = seed == 0 ? new Random(Environment.TickCount) : new Random(seed);
        _modules = new[]
        {
            new ModuleState("CPU"),
            new ModuleState("内存"),
            new ModuleState("网络")
        };
    }

    /// <summary>
    /// Gets the remaining session time in seconds.
    /// </summary>
    public float TimeRemaining => _timeRemaining;

    /// <summary>
    /// Gets whether the session is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the number of dashboard modules.
    /// </summary>
    public int ModuleCount => _modules.Length;

    /// <summary>
    /// Starts a new timed dashboard session.
    /// </summary>
    public void StartSession()
    {
        _timeRemaining = DefaultDurationSeconds;
        _spawnTimer = 0f;
        _isRunning = true;
        _resolvedCount = 0;
        _missedCount = 0;
        _wrongClicks = 0;
        _totalRiskChange = 0;

        for (int index = 0; index < _modules.Length; index += 1)
        {
            _modules[index].IsAlert = false;
            _modules[index].AlertElapsedSeconds = 0f;
        }
    }

    /// <summary>
    /// Advances the mini-game clock and alert lifecycle.
    /// </summary>
    /// <param name="deltaTime">Elapsed real time in seconds.</param>
    public void Advance(float deltaTime)
    {
        if (!_isRunning)
        {
            return;
        }

        float safeDeltaTime = Math.Max(0f, deltaTime);
        _timeRemaining = Math.Max(0f, _timeRemaining - safeDeltaTime);
        _spawnTimer -= safeDeltaTime;

        if (_timeRemaining <= 0f)
        {
            _isRunning = false;
        }

        for (int index = 0; index < _modules.Length; index += 1)
        {
            if (!_modules[index].IsAlert)
            {
                continue;
            }

            _modules[index].AlertElapsedSeconds += safeDeltaTime;
            if (_modules[index].AlertElapsedSeconds < DefaultAlertLifetimeSeconds)
            {
                continue;
            }

            _modules[index].IsAlert = false;
            _modules[index].AlertElapsedSeconds = 0f;
            _missedCount += 1;
            _totalRiskChange += 3;
        }

        while (_spawnTimer <= 0f && _isRunning)
        {
            SpawnAlert();
            _spawnTimer += DefaultSpawnIntervalSeconds;
        }
    }

    /// <summary>
    /// Attempts to fix a module selected by the player.
    /// </summary>
    /// <param name="moduleIndex">Module index selected by the player.</param>
    /// <returns>True when the click fixed an active alert.</returns>
    public bool TryFixModule(int moduleIndex)
    {
        if (!_isRunning || moduleIndex < 0 || moduleIndex >= _modules.Length)
        {
            return false;
        }

        ModuleState module = _modules[moduleIndex];
        if (!module.IsAlert)
        {
            _wrongClicks += 1;
            _totalRiskChange += 1;
            return false;
        }

        module.IsAlert = false;
        module.AlertElapsedSeconds = 0f;
        _resolvedCount += 1;
        _totalRiskChange -= 2;
        return true;
    }

    /// <summary>
    /// Returns the module label for UI rendering.
    /// </summary>
    /// <param name="moduleIndex">Module index.</param>
    /// <returns>Module name.</returns>
    public string GetModuleName(int moduleIndex)
    {
        return moduleIndex >= 0 && moduleIndex < _modules.Length ? _modules[moduleIndex].Name : string.Empty;
    }

    /// <summary>
    /// Returns whether a module is currently in an alert state.
    /// </summary>
    /// <param name="moduleIndex">Module index.</param>
    /// <returns>True when the module is red and clickable.</returns>
    public bool IsModuleAlert(int moduleIndex)
    {
        return moduleIndex >= 0 && moduleIndex < _modules.Length && _modules[moduleIndex].IsAlert;
    }

    /// <summary>
    /// Returns the final result snapshot for the current session.
    /// </summary>
    /// <returns>Result summary.</returns>
    public SessionResult GetResult()
    {
        return new SessionResult(_resolvedCount, _missedCount, _wrongClicks, _totalRiskChange);
    }

    private void SpawnAlert()
    {
        int[] availableIndices = new int[_modules.Length];
        int availableCount = 0;
        for (int index = 0; index < _modules.Length; index += 1)
        {
            if (_modules[index].IsAlert)
            {
                continue;
            }

            availableIndices[availableCount] = index;
            availableCount += 1;
        }

        if (availableCount <= 0)
        {
            return;
        }

        int targetIndex = availableIndices[_random.Next(0, availableCount)];
        _modules[targetIndex].IsAlert = true;
        _modules[targetIndex].AlertElapsedSeconds = 0f;
    }
}
