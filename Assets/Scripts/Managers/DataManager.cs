using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    private const string SaveFileName = "save.json";

    #region Public API

    public ProjectStoryData LoadProjectStory(int projectNumber)
    {
        string resourcePath = $"Data/project{projectNumber}/story";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] 无法加载剧情文件: {resourcePath}");
            return null;
        }

        return DeserializeFromJson<ProjectStoryData>(textAsset.text, resourcePath);
    }

    public List<DailyTaskData> LoadDailyTasks()
    {
        const string resourcePath = "Data/daily_tasks";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] 无法加载日常任务文件: {resourcePath}");
            return new List<DailyTaskData>();
        }

        DailyTaskList wrapper = DeserializeFromJson<DailyTaskList>(textAsset.text, resourcePath);
        return wrapper != null && wrapper.tasks != null ? wrapper.tasks : new List<DailyTaskData>();
    }

    public List<QuizQuestionData> LoadQuizQuestions()
    {
        const string resourcePath = "Data/quiz_questions";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] 无法加载答题文件: {resourcePath}");
            return new List<QuizQuestionData>();
        }

        QuizQuestionList wrapper = DeserializeFromJson<QuizQuestionList>(textAsset.text, resourcePath);
        return wrapper != null && wrapper.questions != null ? wrapper.questions : new List<QuizQuestionData>();
    }

    public EndingsData LoadEndings()
    {
        const string resourcePath = "Data/endings";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] 无法加载结局文件: {resourcePath}");
            return null;
        }

        return DeserializeFromJson<EndingsData>(textAsset.text, resourcePath);
    }

    public void SaveGame(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DataManager] SaveGame 传入空数据，已忽略。");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(), json);
    }

    public PlayerData LoadGame()
    {
        string savePath = GetSavePath();
        if (!File.Exists(savePath))
        {
            return null;
        }

        string json = File.ReadAllText(savePath);
        PlayerData loaded = DeserializeFromJson<PlayerData>(json, savePath);
        if (loaded == null)
        {
            return null;
        }

        if (loaded.aiTrustRecords == null)
        {
            loaded.aiTrustRecords = new List<AITrustRecord>();
        }

        return loaded;
    }

    public bool HasSaveFile()
    {
        return File.Exists(GetSavePath());
    }

    public void DeleteSave()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    #endregion

    #region Internal Helpers

    private static T DeserializeFromJson<T>(string json, string context) where T : class
    {
        try
        {
            T data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                Debug.LogError($"[DataManager] 反序列化结果为空: {context}");
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DataManager] JSON 解析失败: {context}\n{exception.Message}");
            return null;
        }
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    #endregion
}
