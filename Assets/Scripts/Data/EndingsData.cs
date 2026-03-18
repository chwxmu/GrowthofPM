using System;
using System.Collections.Generic;

[Serializable]
public class EndingsData
{
    public List<ProjectEndingData> projects;
}

[Serializable]
public class ProjectEndingData
{
    public int projectNumber;
    public int excellentThreshold;
    public EndingStatThresholdData excellentStatThresholds;
    public int passThreshold;
    public EndingStatThresholdData passStatThresholds;
    public int riskFailThreshold;
    public EndingResultData excellent;
    public EndingResultData pass;
    public EndingResultData fail;
}

[Serializable]
public class EndingStatThresholdData
{
    public int techPower;
    public int commPower;
    public int managePower;
    public int stressPower;
}

[Serializable]
public class EndingResultData
{
    public string endingId;
    public string title;
    public string description;
    public string grade;
}
