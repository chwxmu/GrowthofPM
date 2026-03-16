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
    public List<OptionData> options;
}

[Serializable]
public class OptionData
{
    public string text;
    public string narrative;
    public StatEffects effects;
    public int riskChange;
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
