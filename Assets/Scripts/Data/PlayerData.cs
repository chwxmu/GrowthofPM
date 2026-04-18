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
    public StoryFlowStage savedFlowStage = StoryFlowStage.None;
    public int savedDecisionStepIndex = 0;
    public int pendingProjectNumber = 0;
    public int totalQuizAnswered = 0;
    public int totalQuizCorrect = 0;
    public int quizTechBonusGained = 0;
    public int quizManageBonusGained = 0;
    public int quizCommBonusGained = 0;
    public int quizStressBonusGained = 0;
    public List<AITrustRecord> aiTrustRecords = new List<AITrustRecord>();
    public List<EventFlagRecord> eventFlags = new List<EventFlagRecord>();
    public List<string> savedScheduleTaskNames = new List<string>();
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
