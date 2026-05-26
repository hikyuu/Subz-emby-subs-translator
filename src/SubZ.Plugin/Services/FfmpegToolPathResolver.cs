using System;
using System.IO;
using System.Reflection;
using SubZ.Plugin.Configuration;

namespace SubZ.Plugin.Services;

public interface IFfmpegToolPathProvider
{
    object? ResolveFfmpegManager();

    object? ResolveFfmpegConfiguration();
}

public sealed class FfmpegToolPaths
{
    public string FfmpegPath { get; set; } = "/bin/ffmpeg";
    public string FfprobePath { get; set; } = "/bin/ffprobe";
    public string FfmpegSource { get; set; } = "fallback";
    public string FfprobeSource { get; set; } = "fallback";
}

public static class FfmpegToolPathResolver
{
    private const string FallbackFfmpegPath = "/bin/ffmpeg";
    private const string FallbackFfprobePath = "/bin/ffprobe";

    public static FfmpegToolPaths Resolve(PluginOptions options, IFfmpegToolPathProvider? provider)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var paths = new FfmpegToolPaths();

        var managerConfig = ExtractConfiguration(provider?.ResolveFfmpegManager());
        var directConfig = provider?.ResolveFfmpegConfiguration();

        paths.FfmpegPath = ResolveOne(
            options.FfmpegPath,
            managerConfig,
            directConfig,
            "EncoderPath",
            FallbackFfmpegPath,
            out var ffmpegSource);
        paths.FfmpegSource = ffmpegSource;

        paths.FfprobePath = ResolveOne(
            options.FfprobePath,
            managerConfig,
            directConfig,
            "ProbePath",
            FallbackFfprobePath,
            out var ffprobeSource);
        paths.FfprobeSource = ffprobeSource;

        return paths;
    }

    private static string ResolveOne(
        string? manualPath,
        object? managerConfig,
        object? directConfig,
        string propertyName,
        string fallbackPath,
        out string source)
    {
        var manual = Normalize(manualPath);
        if (IsUsablePath(manual))
        {
            source = "manual";
            return manual;
        }

        var fromManager = Normalize(ReadStringProperty(managerConfig, propertyName));
        if (IsUsablePath(fromManager))
        {
            source = "emby-manager";
            return fromManager;
        }

        var fromDirectConfig = Normalize(ReadStringProperty(directConfig, propertyName));
        if (IsUsablePath(fromDirectConfig))
        {
            source = "emby-config";
            return fromDirectConfig;
        }

        source = "fallback";
        return fallbackPath;
    }

    private static object? ExtractConfiguration(object? manager)
    {
        if (manager is null)
        {
            return null;
        }

        return ReadObjectProperty(manager, "IFfmpegConfiguration")
            ?? ReadObjectProperty(manager, "FfmpegConfiguration");
    }

    private static object? ReadObjectProperty(object target, string propertyName)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(target);
    }

    private static string? ReadStringProperty(object? target, string propertyName)
    {
        if (target is null)
        {
            return null;
        }

        var value = ReadObjectProperty(target, propertyName);
        return value as string;
    }

    private static string Normalize(string? path)
    {
        return (path ?? string.Empty).Trim();
    }

    private static bool IsUsablePath(string path)
    {
        return path.Length > 0 && File.Exists(path);
    }
}
