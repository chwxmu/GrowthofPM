using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int techPower = GameConstants.INITIAL_STAT_VALUE;
    public int commPower = GameConstants.INITIAL_STAT_VALUE;
    public int managePower = GameConstants.INITIAL_STAT_VALUE;
    public int stressPower = GameConstants.INITIAL_STAT_VALUE;
    public int energy = GameConstants.BASE_ENERGY_PER_WEEK;
    public int currentProject = 1;
    public int currentWeek = 1;
    public int hiddenRisk = 0;
    public List<AITrustRecord> aiTrustRecords = new List<AITrustRecord>();
    public List<EventFlagRecord> eventFlags = new List<EventFlagRecord>();
}

[Serializable]
public class AITrustRecord
{
    public string eventId;
    public int projectNumber;
    public int weekNumber;
    public bool adoptedAIAdvice;
    public string aiQuality;
    public bool hasViewed;
    public bool isFollowed;
    public int decisionLatencyMs;
}

[Serializable]
public class EventFlagRecord
{
    public string flagId;
    public int projectNumber;
    public bool value;
}
