using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EndingEvaluationTests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
    }

    [TearDown]
    public void TearDown()
    {
        DataManager dataManager = UnityEngine.Object.FindObjectOfType<DataManager>();
        if (dataManager != null)
        {
            dataManager.DeleteSave();
        }

        for (int i = _createdObjects.Count - 1; i >= 0; i -= 1)
        {
            if (_createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }
        }

        _createdObjects.Clear();

        DestroyAllOfType<GameManager>();
        DestroyAllOfType<DataManager>();
    }

    [Test]
    public void PassTotalMet_ButPassSingleThresholdMissing_ShouldFail()
    {
        ProjectEndingData projectEnding = LoadProjectEndingData(1);
        int[] stats = BuildStatsMeetingRequirements(projectEnding.passThreshold, projectEnding.passStatThresholds);
        stats[2] = Mathf.Max(0, projectEnding.passStatThresholds.managePower - 1);
        stats[0] += 1;

        EndingResultData result = EvaluateEnding(1, stats[0], stats[1], stats[2], stats[3], 0);

        Assert.NotNull(result);
        Assert.AreEqual("fail", result.grade);
    }

    [Test]
    public void ExcellentTotalMet_ButOnlyPassSingleThresholdsMet_ShouldReturnPass()
    {
        ProjectEndingData projectEnding = LoadProjectEndingData(1);
        int[] stats = BuildStatsMeetingRequirements(projectEnding.excellentThreshold, projectEnding.passStatThresholds);

        EndingResultData result = EvaluateEnding(1, stats[0], stats[1], stats[2], stats[3], 0);

        Assert.NotNull(result);
        Assert.AreEqual("pass", result.grade);
    }

    [Test]
    public void ExcellentTotalAndSingleThresholdsMet_ShouldReturnExcellent()
    {
        ProjectEndingData projectEnding = LoadProjectEndingData(1);
        int[] stats = BuildStatsMeetingRequirements(projectEnding.excellentThreshold, projectEnding.excellentStatThresholds);

        EndingResultData result = EvaluateEnding(1, stats[0], stats[1], stats[2], stats[3], 0);

        Assert.NotNull(result);
        Assert.AreEqual("excellent", result.grade);
    }

    [Test]
    public void PassTotalAndSingleThresholdsMet_ButExcellentSingleThresholdsMissing_ShouldReturnPass()
    {
        ProjectEndingData projectEnding = LoadProjectEndingData(1);
        int[] stats = BuildStatsMeetingRequirements(projectEnding.passThreshold, projectEnding.passStatThresholds);

        EndingResultData result = EvaluateEnding(1, stats[0], stats[1], stats[2], stats[3], 0);

        Assert.NotNull(result);
        Assert.AreEqual("pass", result.grade);
    }

    [Test]
    public void RiskThresholdExceeded_ShouldReturnFailBeforeStatChecks()
    {
        ProjectEndingData projectEnding = LoadProjectEndingData(2);
        int[] stats = BuildStatsMeetingRequirements(projectEnding.excellentThreshold, projectEnding.excellentStatThresholds);

        EndingResultData result = EvaluateEnding(2, stats[0], stats[1], stats[2], stats[3], projectEnding.riskFailThreshold + 1);

        Assert.NotNull(result);
        Assert.AreEqual("fail", result.grade);
    }

    private EndingResultData EvaluateEnding(int projectNumber, int techPower, int commPower, int managePower, int stressPower, int hiddenRisk)
    {
        EnsureDataManager();
        GameManager gameManager = EnsureGameManager();
        SetPrivateField(gameManager, "_currentPlayerData", new PlayerData
        {
            currentProject = projectNumber,
            techPower = techPower,
            commPower = commPower,
            managePower = managePower,
            stressPower = stressPower,
            hiddenRisk = hiddenRisk,
            aiTrustRecords = new List<AITrustRecord>()
        });

        return gameManager.EvaluateCurrentProjectEnding();
    }

    private ProjectEndingData LoadProjectEndingData(int projectNumber)
    {
        DataManager dataManager = EnsureDataManager();
        EndingsData endingsData = dataManager.LoadEndings();
        Assert.NotNull(endingsData);
        Assert.NotNull(endingsData.projects);

        ProjectEndingData projectEnding = endingsData.projects.Find(item => item != null && item.projectNumber == projectNumber);
        Assert.NotNull(projectEnding);
        Assert.NotNull(projectEnding.excellentStatThresholds);
        Assert.NotNull(projectEnding.passStatThresholds);
        return projectEnding;
    }

    private static int[] BuildStatsMeetingRequirements(int totalThreshold, EndingStatThresholdData statThresholds)
    {
        int techPower = statThresholds.techPower;
        int commPower = statThresholds.commPower;
        int managePower = statThresholds.managePower;
        int stressPower = statThresholds.stressPower;
        int remaining = totalThreshold - (techPower + commPower + managePower + stressPower);
        if (remaining > 0)
        {
            techPower += remaining;
        }

        return new[] { techPower, commPower, managePower, stressPower };
    }

    private DataManager EnsureDataManager()
    {
        DataManager dataManager = UnityEngine.Object.FindObjectOfType<DataManager>();
        return dataManager != null ? dataManager : CreateComponent<DataManager>("DataManager");
    }

    private GameManager EnsureGameManager()
    {
        GameManager gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
        return gameManager != null ? gameManager : CreateComponent<GameManager>("GameManager");
    }

    private T CreateComponent<T>(string name) where T : Component
    {
        GameObject gameObject = new GameObject(name);
        _createdObjects.Add(gameObject);
        return gameObject.AddComponent<T>();
    }

    private static void DestroyAllOfType<T>() where T : Component
    {
        T[] objects = UnityEngine.Object.FindObjectsOfType<T>(true);
        for (int i = 0; i < objects.Length; i += 1)
        {
            if (objects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(objects[i].gameObject);
            }
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field not found: {fieldName}");
        field.SetValue(target, value);
    }
}
