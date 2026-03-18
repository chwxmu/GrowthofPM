using System;
using System.Collections.Generic;

[Serializable]
public class ProjectStoryData
{
    public int projectNumber;
    public string projectName;
    public int totalWeeks;
    public List<WeekEventData> weeks;
}

[Serializable]
public class WeekEventData
{
    public int weekNumber;
    public string phase;
    public List<DialogueLine> prologueDialogues;
    public List<DialogueLine> dailyIntroDialogues;
    public DecisionEventData decisionEvent;
    public DecisionEventData secondDecisionEvent;
    public ConditionalEventData conditionalEvent;
    public List<DialogueLine> postDecisionDialogues;
    public StatEffects postDecisionStatChanges;
    public RiskBasedDialogueData riskBasedDialogue;
    public int riskAutoChange;
    public StatEffects fixedStatChanges;
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    public string location;
    public string text;
}

[Serializable]
public class DecisionEventData
{
    public string eventId;
    public string description;
    public string aiAdvice;
    public string aiQuality;
    public int aiRecommendedOption;
    public string conditionStat;
    public int conditionThreshold;
    public bool isMiniGame;
    public string miniGameType;
    public List<OptionData> options;
}

[Serializable]
public class ConditionalEventData
{
    public string conditionFlag;
    public bool conditionValue;
    public List<DialogueLine> dialogues;
    public StatEffects statPenalty;
    public int riskPenalty;
}

[Serializable]
public class RiskBasedDialogueData
{
    public List<DialogueLine> low;
    public List<DialogueLine> medium;
    public List<DialogueLine> high;
}

[Serializable]
public class OptionData
{
    public string text;
    public string narrative;
    public StatEffects effects;
    public int riskChange;
    public string conditionStat;
    public int conditionThreshold;
    public string conditionDescription;
}

[Serializable]
public class StatEffects
{
    public int techPower;
    public int commPower;
    public int managePower;
    public int stressPower;
}

[Serializable]
public class DailyTaskData
{
    public string name;
    public int energyCost;
    public StatEffects effects;
}

[Serializable]
public class QuizQuestionData
{
    public string question;
    public List<string> options;
    public int correctIndex;
}

[Serializable]
public class DailyTaskList
{
    public List<DailyTaskData> tasks;
}

[Serializable]
public class QuizQuestionList
{
    public List<QuizQuestionData> questions;
}
