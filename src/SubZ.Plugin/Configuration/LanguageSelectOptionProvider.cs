using System.Collections.Generic;
using Emby.Web.GenericEdit.Common;

namespace SubZ.Plugin.Configuration;

public static class LanguageSelectOptionProvider
{
    /// <summary>
    /// Gets ffprobe language tags (ISO 639-2/B + BCP 47) for common languages, used in multi-select configuration.
    /// ffprobe's tag:language typically outputs three-letter ISO 639-2 codes,
    /// while MP4/MKV containers occasionally use BCP 47 format (e.g., zh-CN).
    /// </summary>
    public static IEnumerable<EditorSelectOption> GetOptions()
    {
        var list = new List<EditorSelectOption>
        {
            new() { Name = "English", Value = "eng" },
            new() { Name = "中文（简体）", Value = "chi" },
            new() { Name = "中文（繁體·台灣）", Value = "zh-tw" },
            new() { Name = "中文（繁體·香港）", Value = "zh-hk" },
            new() { Name = "Deutsch", Value = "ger" },
            new() { Name = "日本語", Value = "jpn" },
            new() { Name = "हिन्दी", Value = "hin" },
            new() { Name = "Français", Value = "fre" },
            new() { Name = "Italiano", Value = "ita" },
            new() { Name = "Português", Value = "por" },
            new() { Name = "Русский", Value = "rus" },
            new() { Name = "Español", Value = "spa" },
        };

        return list;
    }
}
