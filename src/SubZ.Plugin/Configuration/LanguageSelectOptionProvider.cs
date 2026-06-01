using System;
using System.Collections.Generic;
using Emby.Web.GenericEdit.Common;

namespace SubZ.Plugin.Configuration;

public static class LanguageSelectOptionProvider
{
    /// <summary>
    /// ffprobe language tag lookup table:
    /// Key = user-configured Value, Value = set of possible ffprobe output variants for that language.
    /// </summary>
    public static readonly Dictionary<string, HashSet<string>> FfprobeLanguageLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "chi", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "chi", "zho", "zh", "zh-cn", "zh-hans" } },
        { "zh-tw", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zh-tw", "zh-hant", "chi" } },
        { "zh-hk", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zh-hk", "zh-hant", "chi" } },
        { "eng", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "eng", "en" } },
        { "ger", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ger", "deu", "de" } },
        { "jpn", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jpn", "ja" } },
        { "hin", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hin", "hi" } },
        { "fre", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fre", "fra", "fr" } },
        { "ita", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ita", "it" } },
        { "por", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "por", "pt", "pt-br" } },
        { "rus", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rus", "ru" } },
        { "spa", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spa", "es" } },
        { "kor", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kor", "ko" } },
        { "ara", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ara", "ar" } },
        { "tha", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tha", "th" } },
        { "vie", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "vie", "vi" } },
        { "ind", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ind", "id" } },
        { "tur", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tur", "tr" } },
        { "may", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "may", "ms" } },
    };

    /// <summary>
    /// Gets ffprobe language tags (ISO 639-2/B + BCP 47) for common languages, used in multi-select configuration.
    /// ffprobe's tag:language typically outputs three-letter ISO 639-2 codes,
    /// while MP4/MKV containers occasionally use BCP 47 format (e.g., zh-CN).
    /// </summary>
    public static IEnumerable<EditorSelectOption> GetOptions()
    {
        var list = new List<EditorSelectOption>
        {
            new() { Name = "中文（简体）", Value = "chi" },
            new() { Name = "中文（繁體·台灣）", Value = "zh-tw" },
            new() { Name = "中文（繁體·香港）", Value = "zh-hk" },
            new() { Name = "English", Value = "eng" },
            new() { Name = "日本語", Value = "jpn" },
            new() { Name = "한국어", Value = "kor" },
            new() { Name = "Français", Value = "fre" },
            new() { Name = "Deutsch", Value = "ger" },
            new() { Name = "Español", Value = "spa" },
            new() { Name = "Português", Value = "por" },
            new() { Name = "Русский", Value = "rus" },
            new() { Name = "Italiano", Value = "ita" },
            new() { Name = "العربية", Value = "ara" },
            new() { Name = "हिन्दी", Value = "hin" },
            new() { Name = "ภาษาไทย", Value = "tha" },
            new() { Name = "Tiếng Việt", Value = "vie" },
            new() { Name = "Bahasa Indonesia", Value = "ind" },
            new() { Name = "Türkçe", Value = "tur" },
            new() { Name = "Malay", Value = "may" },
        };

        return list;
    }
}
