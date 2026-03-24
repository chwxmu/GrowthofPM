using UnityEngine;

/// <summary>
/// Provides a single entry point for project-specific AI advice and trust tracking.
/// </summary>
public class AIAdvisor : Singleton<AIAdvisor>
{
    /// <summary>
    /// Gets the current AI advisor name based on the active project.
    /// </summary>
    public string CurrentAIName => GetAIName(GetCurrentProjectNumber());

    /// <summary>
    /// Gets the current AI advisor personality description based on the active project.
    /// </summary>
    public string CurrentAIPersonality => GetAIPersonality(GetCurrentProjectNumber());

    /// <summary>
    /// Returns the authored advice text for a decision event.
    /// </summary>
    /// <param name="eventData">Decision event data.</param>
    /// <returns>Advice text, or an empty string when unavailable.</returns>
    public string GetAdvice(DecisionEventData eventData)
    {
        return eventData == null || string.IsNullOrWhiteSpace(eventData.aiAdvice)
            ? string.Empty
            : eventData.aiAdvice.Trim();
    }

    /// <summary>
    /// Returns the authored recommended option index for a decision event.
    /// </summary>
    /// <param name="eventData">Decision event data.</param>
    /// <returns>Recommended option index, or -1 when unavailable.</returns>
    public int GetRecommendedOption(DecisionEventData eventData)
    {
        return eventData != null ? eventData.aiRecommendedOption : -1;
    }

    /// <summary>
    /// Builds the formatted advice string shown in the decision panel.
    /// </summary>
    /// <param name="eventData">Decision event data.</param>
    /// <returns>Advice text prefixed with the AI name.</returns>
    public string GetAdviceDisplayText(DecisionEventData eventData)
    {
        string advice = GetAdvice(eventData);
        if (string.IsNullOrWhiteSpace(advice))
        {
            return string.Empty;
        }

        return CurrentAIName + "建议：\n" + advice;
    }

    /// <summary>
    /// Records whether the player followed the AI recommendation for a decision.
    /// </summary>
    /// <param name="eventId">Decision event identifier.</param>
    /// <param name="selectedOption">Player-selected option index.</param>
    /// <param name="recommendedOption">AI-recommended option index.</param>
    /// <param name="hasViewed">Whether the player saw the advice.</param>
    /// <param name="decisionLatencyMs">Time spent before choosing.</param>
    /// <param name="aiQuality">Authored AI quality label.</param>
    public void RecordDecision(string eventId, int selectedOption, int recommendedOption, bool hasViewed, int decisionLatencyMs, string aiQuality = "")
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        bool isFollowed = recommendedOption >= 0 && selectedOption == recommendedOption;
        GameManager.Instance.RecordAIDecision(eventId, hasViewed, isFollowed, decisionLatencyMs, aiQuality);
    }

    /// <summary>
    /// Returns the overall AI adoption rate for the current save.
    /// </summary>
    /// <returns>Adoption rate in the range of 0-1.</returns>
    public float GetAdoptionRate()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetAIAdoptionRate() : 0f;
    }

    /// <summary>
    /// Returns the AI adoption rate for a specific project.
    /// </summary>
    /// <param name="projectNumber">Target project number.</param>
    /// <returns>Adoption rate in the range of 0-1.</returns>
    public float GetAdoptionRateByProject(int projectNumber)
    {
        return GameManager.Instance != null ? GameManager.Instance.GetAIAdoptionRateByProject(projectNumber) : 0f;
    }

    private static string GetAIName(int projectNumber)
    {
        switch (projectNumber)
        {
            case 2:
                return GameConstants.PROJECT2_AI_NAME;
            case 3:
                return GameConstants.PROJECT3_AI_NAME;
            default:
                return "顾问";
        }
    }

    private static string GetAIPersonality(int projectNumber)
    {
        switch (projectNumber)
        {
            case 2:
                return GameConstants.PROJECT2_AI_PERSONALITY;
            case 3:
                return GameConstants.PROJECT3_AI_PERSONALITY;
            default:
                return string.Empty;
        }
    }

    private static int GetCurrentProjectNumber()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentPlayerData != null
            ? GameManager.Instance.CurrentPlayerData.currentProject
            : 0;
    }
}
