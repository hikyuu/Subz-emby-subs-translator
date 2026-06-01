using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SubZ.Plugin.Configuration;

namespace SubZ.Plugin.Services;

public sealed class SubtitleSourceResolution
{
    public string? SourceSubtitlePath { get; set; }
    public string? TempExtractedSubtitlePath { get; set; }
    public bool IsEmbedded { get; set; }
}

public static class SubtitleSourceResolver
{
    private static readonly SubtitleTrackSelector TrackSelector = new SubtitleTrackSelector();

    private static readonly HashSet<string> SubtitleExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".srt", ".ass", ".ssa", ".vtt"
    };

    private static readonly HashSet<string> TextSubtitleCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "subrip", "srt", "ass", "ssa", "webvtt", "mov_text"
    };

    // ffprobe language tag lookup table:
    // Key = user-configured Value, Value = set of possible ffprobe output variants for that language
    private static readonly Dictionary<string, HashSet<string>> FfprobeLanguageLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "chi", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "chi", "zho", "zh", "zh-cn", "zh-hans" } },
        { "zh-tw", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zh-tw", "zh-hant", "chi" } }, // Many MKV files also tag Traditional Chinese as chi
        { "zh-hk", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zh-hk", "zh-hant", "chi" } }, // Many MKV files also tag Traditional Chinese as chi
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

    // Checks whether the video's embedded subtitles contain any of the skipped languages.
    // Returns true if an embedded subtitle track's language code matches the configured skip list.
    // The matching logic covers various ffprobe output formats: ISO 639-2/B, ISO 639-2/T, ISO 639-1, BCP 47.
    public static bool ShouldSkipEmbeddedByLanguage(string videoFile, PluginOptions options, bool debugEnabled)
    {
        var skipConfigValues = ParseSkipConfigValues(options.SkipEmbeddedLanguageCodes);
        if (skipConfigValues.Count == 0)
        {
            return false;
        }

        // Expand configured Values (ISO 639-2/B) into all possible ffprobe variants
        var skipVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cfg in skipConfigValues)
        {
            // Exact match: the configured value itself
            skipVariants.Add(cfg);

            // Look up all variants from the lookup table
            if (FfprobeLanguageLookup.TryGetValue(cfg, out var variants))
            {
                skipVariants.UnionWith(variants);
            }
        }

        var toolPaths = global::SubZ.Plugin.Plugin.ResolveFfmpegToolPaths(options);
        var streams = ProbeSubtitleStreams(videoFile, options, debugEnabled, toolPaths);
        foreach (var track in streams)
        {
            if (!track.IsTextTrack)
            {
                continue;
            }

            var lang = (track.Language ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(lang))
            {
                continue;
            }

            if (skipVariants.Contains(lang) ||
                skipVariants.Contains(lang.Split('-')[0]) ||
                skipVariants.Contains(GetIso6392B(lang)))
            {
                if (debugEnabled)
                {
                    InMemoryTranslationJobDispatcher.AppendRuntimeLog(
                        "Info", $"Skip by embedded language: {videoFile} has track #{track.Id} lang={lang}");
                }
                return true;
            }
        }

        return false;
    }

    // Attempt to map a language code in any format back to ISO 639-2/B three-letter code (reverse lookup).
    private static string GetIso6392B(string lang)
    {
        foreach (var kvp in FfprobeLanguageLookup)
        {
            if (kvp.Value.Contains(lang))
            {
                return kvp.Key;
            }
        }
        return lang;
    }

    private static HashSet<string> ParseSkipConfigValues(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in raw!.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                codes.Add(trimmed);
            }
        }
        return codes;
    }

    public static SubtitleSourceResolution Resolve(string videoFile, PluginOptions options, bool debugEnabled)
    {
        var external = FindSourceSubtitle(videoFile, options);
        if (!string.IsNullOrWhiteSpace(external))
        {
            return new SubtitleSourceResolution
            {
                SourceSubtitlePath = external,
                IsEmbedded = false
            };
        }

        var extracted = TryExtractEmbeddedTextSubtitle(videoFile, options, debugEnabled);
        return new SubtitleSourceResolution
        {
            SourceSubtitlePath = extracted,
            TempExtractedSubtitlePath = extracted,
            IsEmbedded = !string.IsNullOrWhiteSpace(extracted)
        };
    }

    public static void CleanupTemp(SubtitleSourceResolution source)
    {
        var temp = source.TempExtractedSubtitlePath;
        if (!string.IsNullOrWhiteSpace(temp) && File.Exists(temp))
        {
            File.Delete(temp);
        }
    }

    private static string? FindSourceSubtitle(string videoFile, PluginOptions options)
    {
        var dir = Path.GetDirectoryName(videoFile) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(videoFile);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var candidates = Directory.EnumerateFiles(dir, stem + ".*", SearchOption.TopDirectoryOnly)
            .Where(f => SubtitleExts.Contains(Path.GetExtension(f)))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var preferredSource = NormalizeLanguageCode(options.PreferredSourceLanguage, "en");
        var hit = candidates.FirstOrDefault(f => HasLanguageToken(f, stem, preferredSource));
        if (!string.IsNullOrWhiteSpace(hit))
        {
            return hit;
        }

        var shortCode = preferredSource.Split('-')[0];
        hit = candidates.FirstOrDefault(f => HasLanguageToken(f, stem, shortCode));
        if (!string.IsNullOrWhiteSpace(hit))
        {
            return hit;
        }

        return candidates[0];
    }

    private static bool HasLanguageToken(string subtitlePath, string videoStem, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(subtitlePath)
            || string.IsNullOrWhiteSpace(videoStem)
            || string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        var fileStem = Path.GetFileNameWithoutExtension(subtitlePath) ?? string.Empty;
        if (!fileStem.StartsWith(videoStem, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = fileStem.Substring(videoStem.Length);
        if (suffix.Length == 0)
        {
            return false;
        }

        var tokens = suffix.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(t => string.Equals(t, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static string? TryExtractEmbeddedTextSubtitle(string videoFile, PluginOptions options, bool debugEnabled)
    {
        var toolPaths = global::SubZ.Plugin.Plugin.ResolveFfmpegToolPaths(options);
        var streams = ProbeSubtitleStreams(videoFile, options, debugEnabled, toolPaths);
        var chosen = TrackSelector.SelectBest(streams, options);
        if (chosen == null)
        {
            if (debugEnabled)
            {
                InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"No text subtitle stream found in: {videoFile}");
            }
            return null;
        }

        var temp = Path.Combine(Path.GetTempPath(), "subz_" + Guid.NewGuid().ToString("N") + ".srt");
        var ffmpegPath = toolPaths.FfmpegPath;
        var args = string.Join(
            " ",
            "-y",
            "-i", QuoteProcessArg(videoFile),
            "-map", "0:" + chosen.Id.ToString(CultureInfo.InvariantCulture),
            "-c:s", "srt",
            QuoteProcessArg(temp));

        if (debugEnabled)
        {
            InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"Run ffmpeg ({toolPaths.FfmpegSource}): {ffmpegPath} {args}");
        }

        var code = RunProcess(ffmpegPath, args, out var err);
        if (code != 0 || !File.Exists(temp))
        {
            if (debugEnabled)
            {
                InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"ffmpeg exit={code}, output={TrimForDebug(err)}");
            }
            return null;
        }

        if (debugEnabled)
        {
            InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"Embedded subtitle extracted to: {temp}");
        }

        return temp;
    }

    public static bool HasEmbeddedTargetSubtitle(string videoFile, PluginOptions options, bool debugEnabled)
    {
        var toolPaths = global::SubZ.Plugin.Plugin.ResolveFfmpegToolPaths(options);
        var streams = ProbeSubtitleStreams(videoFile, options, debugEnabled, toolPaths);
        return SubtitleTrackSelector.HasTargetTrack(streams, options);
    }

    private static List<SubtitleTrackInfo> ProbeSubtitleStreams(string videoFile, PluginOptions options, bool debugEnabled, FfmpegToolPaths toolPaths)
    {
        var ffprobePath = toolPaths.FfprobePath;
        var args = string.Join(
            " ",
            "-v", "error",
            "-select_streams", "s",
            "-show_entries", "stream=index,codec_name:stream_disposition=default,forced,hearing_impaired:stream_tags=language",
            "-of", "compact=p=0:nk=0",
            QuoteProcessArg(videoFile));

        if (debugEnabled)
        {
            InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"Run ffprobe ({toolPaths.FfprobeSource}): {ffprobePath} {args}");
        }

        var code = RunProcess(ffprobePath, args, out var output);
        if (code != 0 || string.IsNullOrWhiteSpace(output))
        {
            if (debugEnabled)
            {
                InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"ffprobe exit={code}, output={TrimForDebug(output)}");
            }
            return new List<SubtitleTrackInfo>();
        }

        var result = new List<SubtitleTrackInfo>();
        var lines = output.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var tokens = trimmed.Split('|');
            var indexText = string.Empty;
            var codec = string.Empty;
            var language = string.Empty;
            var isDefault = false;
            var isForced = false;
            var isHearingImpaired = false;

            foreach (var token in tokens)
            {
                var eq = token.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var key = token.Substring(0, eq).Trim().ToLowerInvariant();
                var value = token.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "index":
                        indexText = value;
                        break;
                    case "codec_name":
                        codec = value;
                        break;
                    case "tag:language":
                    case "tags:language":
                        language = value;
                        break;
                    case "disposition:default":
                        isDefault = ParseIntFlag(value);
                        break;
                    case "disposition:forced":
                        isForced = ParseIntFlag(value);
                        break;
                    case "disposition:hearing_impaired":
                        isHearingImpaired = ParseIntFlag(value);
                        break;
                }
            }

            if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var streamIndex))
            {
                continue;
            }
            result.Add(new SubtitleTrackInfo
            {
                Id = streamIndex,
                Codec = codec ?? string.Empty,
                Language = language,
                IsDefault = isDefault,
                IsForced = isForced,
                IsHearingImpaired = isHearingImpaired,
                IsTextTrack = TextSubtitleCodecs.Contains(codec ?? string.Empty)
            });
        }

        if (debugEnabled)
        {
            InMemoryTranslationJobDispatcher.AppendRuntimeLog("Debug", $"ffprobe subtitle stream count={result.Count}");
        }

        return result;
    }

    private static int RunProcess(string fileName, string args, out string output)
    {
        const int timeoutMs = 180000;

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var proc = new Process { StartInfo = psi })
        {
            proc.Start();

            var stdOutTask = proc.StandardOutput.ReadToEndAsync();
            var stdErrTask = proc.StandardError.ReadToEndAsync();

            if (!proc.WaitForExit(timeoutMs))
            {
                try
                {
                    proc.Kill();
                }
                catch
                {
                    // best effort
                }

                Task.WaitAll(new Task[] { stdOutTask, stdErrTask }, 5000);
                output = "Process timeout after " + timeoutMs.ToString(CultureInfo.InvariantCulture) + " ms.";
                return -1;
            }

            Task.WaitAll(new Task[] { stdOutTask, stdErrTask });
            var stdOut = stdOutTask.Result;
            var stdErr = stdErrTask.Result;
            output = (stdOut + "\n" + stdErr).Trim();
            return proc.ExitCode;
        }
    }

    private static string NormalizeLanguageCode(string? configured, string fallback)
    {
        var raw = (configured ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    private static string QuoteProcessArg(string value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\\\"");
        return "\"" + safe + "\"";
    }

    private static string TrimForDebug(string value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 4000)
        {
            text = text.Substring(0, 4000) + "...(truncated)";
        }

        return text.Replace("\r", " ").Replace("\n", " ");
    }

    private static bool ParseIntFlag(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i != 0;
    }
}
