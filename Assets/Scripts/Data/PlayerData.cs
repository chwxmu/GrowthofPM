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
}

[Serializable]
public class AITrustRecord
{
    public string eventId;
    public bool adoptedAIAdvice;
    public bool hasViewed;
    public bool isFollowed;
    public int decisionLatencyMs;
}
