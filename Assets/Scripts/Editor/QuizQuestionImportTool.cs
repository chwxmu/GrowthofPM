#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports quiz questions from Assets/Resources/Quizs/*.txt into Assets/Resources/Data/quiz_questions.json.
/// </summary>
public static class QuizQuestionImportTool
{
    private const string MenuPath = "Tools/Growth of PM/Quiz/Import Quizs Txt To Quiz JSON";
    private const string SourceFolderPath = "Assets/Resources/Quizs";
    private const string TargetQuizJsonPath = "Assets/Resources/Data/quiz_questions.json";

    private static readonly Dictionary<string, string> SourceFileQuestionTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "技术力模块题目.txt", "techPower" },
        { "管理力模块题目.txt", "managePower" },
        { "抗压力模块题目.txt", "stressPower" },
        { "PMBOK管理知识题目.txt", "random" },
        { "沟通力模块题目.txt", "commPower" }
    };

    private static readonly Regex LeadingNumberRegex = new Regex(@"^\s*\d+\s*[\.、]\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex MultiWhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex QuestionPatternRegex = new Regex(
        @"^(?<question>.+?):\s*A\s*[\.、]?\s*(?<a>.+?):\s*B\s*[\.、]?\s*(?<b>.+?):\s*C\s*[\.、]?\s*(?<c>.+?):\s*D\s*[\.、]?\s*(?<d>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Imports questions from Quizs text files, merges into the existing JSON question bank, and removes duplicates.
    /// </summary>
    [MenuItem(MenuPath)]
    public static void ImportQuizsToQuizJson()
    {
        if (!Directory.Exists(SourceFolderPath))
        {
            Debug.LogError("[QuizQuestionImportTool] : Source folder not found: " + SourceFolderPath);
            return;
        }

        List<QuizQuestionData> mergedQuestions = LoadExistingQuestions();
        HashSet<string> dedupeKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < mergedQuestions.Count; i += 1)
        {
            QuizQuestionData existingQuestion = mergedQuestions[i];
            if (!IsValidQuestion(existingQuestion))
            {
                continue;
            }

            dedupeKeys.Add(BuildDedupeKey(existingQuestion));
        }

        List<FileImportStats> importStats = new List<FileImportStats>();
        int totalAdded = 0;
        int totalDuplicate = 0;
        int totalInvalid = 0;

        foreach (KeyValuePair<string, string> mapping in SourceFileQuestionTypeMap)
        {
            string sourceFilePath = Path.Combine(SourceFolderPath, mapping.Key);
            FileImportStats stats = new FileImportStats(mapping.Key, mapping.Value);
            importStats.Add(stats);

            if (!File.Exists(sourceFilePath))
            {
                stats.MissingFile = true;
                continue;
            }

            string textContent = ReadTextWithFallback(sourceFilePath);
            if (string.IsNullOrWhiteSpace(textContent))
            {
                continue;
            }

            StringReader reader = new StringReader(textContent);
            int lineNumber = 0;
            while (true)
            {
                string rawLine = reader.ReadLine();
                if (rawLine == null)
                {
                    break;
                }

                lineNumber += 1;
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                stats.TotalLines += 1;

                QuizQuestionData parsedQuestion;
                if (!TryParseLine(rawLine, out parsedQuestion))
                {
                    stats.InvalidLines += 1;
                    continue;
                }

                parsedQuestion.questionType = mapping.Value;
                string dedupeKey = BuildDedupeKey(parsedQuestion);
                if (!dedupeKeys.Add(dedupeKey))
                {
                    stats.DuplicateLines += 1;
                    continue;
                }

                mergedQuestions.Add(parsedQuestion);
                stats.AddedLines += 1;
            }

            totalAdded += stats.AddedLines;
            totalDuplicate += stats.DuplicateLines;
            totalInvalid += stats.InvalidLines;
        }

        QuizQuestionList outputWrapper = new QuizQuestionList
        {
            questions = mergedQuestions
        };

        string outputJson = JsonUtility.ToJson(outputWrapper, true);
        string targetDirectory = Path.GetDirectoryName(TargetQuizJsonPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.WriteAllText(TargetQuizJsonPath, outputJson + Environment.NewLine, new UTF8Encoding(true));
        AssetDatabase.Refresh();

        StringBuilder summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine("[QuizQuestionImportTool] Import completed.");
        summaryBuilder.AppendLine("Target: " + TargetQuizJsonPath);
        summaryBuilder.AppendLine("Merged question count: " + mergedQuestions.Count);
        summaryBuilder.AppendLine("Added: " + totalAdded + ", Duplicates: " + totalDuplicate + ", Invalid: " + totalInvalid);
        for (int i = 0; i < importStats.Count; i += 1)
        {
            FileImportStats stats = importStats[i];
            if (stats.MissingFile)
            {
                summaryBuilder.AppendLine("- " + stats.FileName + " [" + stats.QuestionType + "]: missing file");
                continue;
            }

            summaryBuilder.AppendLine(
                "- " + stats.FileName
                + " [" + stats.QuestionType + "]"
                + " total=" + stats.TotalLines
                + ", added=" + stats.AddedLines
                + ", duplicate=" + stats.DuplicateLines
                + ", invalid=" + stats.InvalidLines);
        }

        Debug.Log(summaryBuilder.ToString());
    }

    private static List<QuizQuestionData> LoadExistingQuestions()
    {
        if (!File.Exists(TargetQuizJsonPath))
        {
            return new List<QuizQuestionData>();
        }

        string existingJson = ReadTextWithFallback(TargetQuizJsonPath);
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            return new List<QuizQuestionData>();
        }

        existingJson = existingJson.TrimStart('\uFEFF');

        QuizQuestionList existingWrapper;
        try
        {
            existingWrapper = JsonUtility.FromJson<QuizQuestionList>(existingJson);
        }
        catch (Exception exception)
        {
            Debug.LogError("[QuizQuestionImportTool] : Failed to parse existing quiz_questions.json. " + exception.Message);
            return new List<QuizQuestionData>();
        }

        if (existingWrapper == null || existingWrapper.questions == null)
        {
            return new List<QuizQuestionData>();
        }

        return new List<QuizQuestionData>(existingWrapper.questions);
    }

    private static bool TryParseLine(string rawLine, out QuizQuestionData question)
    {
        question = null;
        string normalized = NormalizeBasic(rawLine);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        normalized = LeadingNumberRegex.Replace(normalized.Trim(), string.Empty);
        int answerSeparatorIndex = normalized.LastIndexOf(':');
        if (answerSeparatorIndex <= 0 || answerSeparatorIndex >= normalized.Length - 1)
        {
            return false;
        }

        string answerToken = normalized.Substring(answerSeparatorIndex + 1).Trim();
        int answerNumber;
        if (!int.TryParse(answerToken, out answerNumber) || answerNumber < 1 || answerNumber > 4)
        {
            return false;
        }

        string body = normalized.Substring(0, answerSeparatorIndex).Trim();
        Match match = QuestionPatternRegex.Match(body);
        if (!match.Success)
        {
            return false;
        }

        string questionText = CleanupSegment(match.Groups["question"].Value);
        string optionA = CleanupSegment(match.Groups["a"].Value);
        string optionB = CleanupSegment(match.Groups["b"].Value);
        string optionC = CleanupSegment(match.Groups["c"].Value);
        string optionD = CleanupSegment(match.Groups["d"].Value);
        if (string.IsNullOrWhiteSpace(questionText)
            || string.IsNullOrWhiteSpace(optionA)
            || string.IsNullOrWhiteSpace(optionB)
            || string.IsNullOrWhiteSpace(optionC)
            || string.IsNullOrWhiteSpace(optionD))
        {
            return false;
        }

        question = new QuizQuestionData
        {
            question = questionText,
            options = new List<string> { optionA, optionB, optionC, optionD },
            correctIndex = answerNumber - 1
        };
        return true;
    }

    private static bool IsValidQuestion(QuizQuestionData question)
    {
        return question != null
            && !string.IsNullOrWhiteSpace(question.question)
            && question.options != null
            && question.options.Count == 4
            && question.correctIndex >= 0
            && question.correctIndex < 4;
    }

    private static string BuildDedupeKey(QuizQuestionData question)
    {
        return NormalizeForKey(question.question)
            + "||" + NormalizeForKey(question.options[0])
            + "||" + NormalizeForKey(question.options[1])
            + "||" + NormalizeForKey(question.options[2])
            + "||" + NormalizeForKey(question.options[3]);
    }

    private static string NormalizeForKey(string text)
    {
        string normalized = NormalizeBasic(text).Trim().ToLowerInvariant();
        return MultiWhitespaceRegex.Replace(normalized, " ");
    }

    private static string CleanupSegment(string text)
    {
        string normalized = NormalizeBasic(text).Trim();
        return MultiWhitespaceRegex.Replace(normalized, " ");
    }

    private static string NormalizeBasic(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i += 1)
        {
            char currentChar = text[i];
            if (currentChar == '\uFEFF' || currentChar == '\u200B' || currentChar == '\u200C' || currentChar == '\u200D')
            {
                continue;
            }

            if (currentChar == '\u3000')
            {
                builder.Append(' ');
                continue;
            }

            if (currentChar >= '\uFF01' && currentChar <= '\uFF5E')
            {
                builder.Append((char)(currentChar - 65248));
                continue;
            }

            builder.Append(currentChar);
        }

        return builder.ToString();
    }

    private static string ReadTextWithFallback(string filePath)
    {
        byte[] rawBytes = File.ReadAllBytes(filePath);
        UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);
        try
        {
            return strictUtf8.GetString(rawBytes);
        }
        catch (DecoderFallbackException)
        {
            // Fallback to common simplified-Chinese codepage when UTF-8 decode fails.
        }

        try
        {
            return Encoding.GetEncoding(936).GetString(rawBytes);
        }
        catch (Exception)
        {
            return Encoding.Default.GetString(rawBytes);
        }
    }

    private sealed class FileImportStats
    {
        public FileImportStats(string fileName, string questionType)
        {
            FileName = fileName;
            QuestionType = questionType;
        }

        public string FileName { get; private set; }
        public string QuestionType { get; private set; }
        public int TotalLines { get; set; }
        public int AddedLines { get; set; }
        public int DuplicateLines { get; set; }
        public int InvalidLines { get; set; }
        public bool MissingFile { get; set; }
    }
}
#endif
