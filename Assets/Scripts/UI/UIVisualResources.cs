using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIVisualResources
{
    private const string BackgroundRoot = "Backgrounds/Dialogue/";
    private const string CharacterRoot = "UI/Characters/";
    private const string IconRoot = "UI/Icons/";

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, string> SpeakerPortraitMap = new Dictionary<string, string>
    {
        { "朱诀", "zhu_jue" },
        { "王总", "wang_zong" },
        { "老陈", "lao_chen" },
        { "老周", "lao_zhou" },
        { "李华", "li_hua" },
        { "韩梅梅", "han_meimei" },
        { "徐姐", "xu_jie" },
        { "王小明", "wang_xiaoming" },
        { "张总", "zhang_zong" },
        { "张小红", "zhang_xiaohong" },
        { "小赵", "xiao_zhao" },
        { "小张", "xiao_zhang" },
        { "小李", "xiao_li" },
        { "小许", "xiao_xu" },
        { "刘小张", "liu_xiaozhang" },
        { "钱多多", "qian_duoduo" },
        { "李总", "li_zong" }
    };

    public static Sprite LoadIcon(string resourceName)
    {
        return LoadSprite(IconRoot + resourceName);
    }

    public static Sprite LoadCharacter(string resourceName)
    {
        return LoadSprite(CharacterRoot + resourceName);
    }

    public static Sprite LoadDialogueBackground(string resourceName)
    {
        return LoadSprite(BackgroundRoot + resourceName);
    }

    public static Sprite ResolveSpeakerPortrait(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return null;
        }

        if (SpeakerPortraitMap.TryGetValue(speaker.Trim(), out string resourceName))
        {
            return LoadCharacter(resourceName);
        }

        return null;
    }

    public static Sprite ResolveSemanticDialogueBackground(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return LoadDialogueBackground("OfficeDay");
        }

        string normalizedLocation = location.Trim();
        if (ContainsAny(normalizedLocation, "深夜", "夜"))
        {
            return LoadDialogueBackground("OfficeNight");
        }

        if (ContainsAny(normalizedLocation, "办公室", "工位", "项目办公室", "项目组", "会议室", "公司", "技术部", "测试部", "指挥室"))
        {
            return LoadDialogueBackground("OfficeDay");
        }

        return LoadDialogueBackground("OfficeDay");
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        SpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(source) || values == null)
        {
            return false;
        }

        for (int index = 0; index < values.Length; index += 1)
        {
            string value = values[index];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
