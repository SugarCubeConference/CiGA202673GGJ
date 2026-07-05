using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class LevelProgressSave
{
    public const string FileName = "level_progress.yaml";
    public const string LevelSceneFolder = "Assets/Scenes/DeathAnchor";

    private const int FirstUnlockedLevel = 0;

    private static readonly string[] LevelSceneNames =
    {
        "第零关（0705重置）",
        "第一关（0705重置）",
        "第二关（0705重置）",
        "第三关（0705重置）",
        "第四关（0705重置）",
        "第五关（0705重置）",
        "第六关",
        "第七关",
        "第八关",
        "第九关（0705重置）",
        "第十关（0705）"
    };

    public static int LevelCount => LevelSceneNames.Length;

    public static string SavePath => Path.Combine(GameRootPath, FileName);

    public static string GameRootPath
    {
        get
        {
            DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
            return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
        }
    }

    public static HashSet<int> LoadUnlockedLevels()
    {
        HashSet<int> unlockedLevels = CreateDefaultProgress();
        string path = SavePath;

        try
        {
            if (!File.Exists(path))
            {
                SaveUnlockedLevels(unlockedLevels);
                return unlockedLevels;
            }

            HashSet<int> loadedLevels = ReadUnlockedLevels(File.ReadAllLines(path));
            if (loadedLevels.Count > 0)
            {
                unlockedLevels = loadedLevels;
            }

            bool changed = NormalizeUnlockedLevels(unlockedLevels);
            if (changed)
            {
                SaveUnlockedLevels(unlockedLevels);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LevelProgressSave] Failed to load progress. Falling back to level 0 only.\n" + ex);
            SaveUnlockedLevels(unlockedLevels);
        }

        return unlockedLevels;
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        return LoadUnlockedLevels().Contains(levelIndex);
    }

    public static bool UnlockLevel(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
        {
            return false;
        }

        HashSet<int> unlockedLevels = LoadUnlockedLevels();
        if (!unlockedLevels.Add(levelIndex))
        {
            return false;
        }

        SaveUnlockedLevels(unlockedLevels);
        return true;
    }

    public static void UnlockLevelAndNext(string sceneName)
    {
        int currentLevelIndex = GetLevelIndexForScene(sceneName);
        if (!IsValidLevelIndex(currentLevelIndex))
        {
            Debug.LogWarning("[LevelProgressSave] Scene is not registered as a selectable level: " + sceneName);
            return;
        }

        HashSet<int> unlockedLevels = LoadUnlockedLevels();
        unlockedLevels.Add(currentLevelIndex);

        int nextLevelIndex = currentLevelIndex + 1;
        if (IsValidLevelIndex(nextLevelIndex))
        {
            unlockedLevels.Add(nextLevelIndex);
        }

        SaveUnlockedLevels(unlockedLevels);
    }

    public static bool TryGetLevelScene(int levelIndex, out string sceneName, out string scenePath)
    {
        if (!IsValidLevelIndex(levelIndex))
        {
            sceneName = string.Empty;
            scenePath = string.Empty;
            return false;
        }

        sceneName = LevelSceneNames[levelIndex];
        scenePath = LevelSceneFolder + "/" + sceneName + ".unity";
        return true;
    }

    public static int GetLevelIndexForScene(string sceneName)
    {
        for (int i = 0; i < LevelSceneNames.Length; i++)
        {
            if (string.Equals(LevelSceneNames[i], sceneName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static HashSet<int> CreateDefaultProgress()
    {
        HashSet<int> unlockedLevels = new HashSet<int>();
        unlockedLevels.Add(FirstUnlockedLevel);
        return unlockedLevels;
    }

    private static HashSet<int> ReadUnlockedLevels(string[] lines)
    {
        HashSet<int> unlockedLevels = new HashSet<int>();
        bool readingUnlockedLevels = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = StripComment(lines[i]).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("unlockedLevels:", StringComparison.OrdinalIgnoreCase))
            {
                readingUnlockedLevels = true;
                ReadInlineList(line, unlockedLevels);
                continue;
            }

            if (!readingUnlockedLevels)
            {
                continue;
            }

            if (line.EndsWith(":", StringComparison.Ordinal))
            {
                readingUnlockedLevels = false;
                continue;
            }

            if (line.StartsWith("-", StringComparison.Ordinal))
            {
                TryAddLevelIndex(line.Substring(1), unlockedLevels);
            }
        }

        return unlockedLevels;
    }

    private static void ReadInlineList(string line, HashSet<int> unlockedLevels)
    {
        int openBracket = line.IndexOf('[');
        int closeBracket = line.IndexOf(']');
        if (openBracket < 0 || closeBracket <= openBracket)
        {
            return;
        }

        string list = line.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] parts = list.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            TryAddLevelIndex(parts[i], unlockedLevels);
        }
    }

    private static bool TryAddLevelIndex(string value, HashSet<int> unlockedLevels)
    {
        int levelIndex;
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out levelIndex))
        {
            return false;
        }

        if (!IsValidLevelIndex(levelIndex))
        {
            return false;
        }

        unlockedLevels.Add(levelIndex);
        return true;
    }

    private static bool NormalizeUnlockedLevels(HashSet<int> unlockedLevels)
    {
        bool changed = false;

        if (!unlockedLevels.Contains(FirstUnlockedLevel))
        {
            unlockedLevels.Add(FirstUnlockedLevel);
            changed = true;
        }

        List<int> invalidLevels = new List<int>();
        foreach (int levelIndex in unlockedLevels)
        {
            if (!IsValidLevelIndex(levelIndex))
            {
                invalidLevels.Add(levelIndex);
            }
        }

        for (int i = 0; i < invalidLevels.Count; i++)
        {
            unlockedLevels.Remove(invalidLevels[i]);
            changed = true;
        }

        return changed;
    }

    private static void SaveUnlockedLevels(HashSet<int> unlockedLevels)
    {
        try
        {
            NormalizeUnlockedLevels(unlockedLevels);

            List<int> sortedLevels = new List<int>(unlockedLevels);
            sortedLevels.Sort();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# 2026GGJ level select progress");
            builder.AppendLine("unlockedLevels:");
            for (int i = 0; i < sortedLevels.Count; i++)
            {
                builder.Append("  - ");
                builder.AppendLine(sortedLevels[i].ToString(CultureInfo.InvariantCulture));
            }

            Directory.CreateDirectory(GameRootPath);
            File.WriteAllText(SavePath, builder.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LevelProgressSave] Failed to save progress to " + SavePath + "\n" + ex);
        }
    }

    private static string StripComment(string line)
    {
        int commentIndex = line.IndexOf('#');
        return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
    }

    private static bool IsValidLevelIndex(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex < LevelSceneNames.Length;
    }
}
