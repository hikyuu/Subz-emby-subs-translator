using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using SubZ.Plugin.Api;
using SubZ.Plugin.Configuration;
using SubZ.Plugin.Services;

namespace SubZ.Plugin;

public sealed class SubZEntryPoint : IServerEntryPoint
{
    private readonly ILibraryManager _libraryManager;
    private readonly ConcurrentDictionary<string, DateTime> _recentlyProcessedFolders = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupGracePeriod = TimeSpan.FromSeconds(30);
    private readonly DateTime _startupBlockedUntil;
    private bool _disposed;

    public SubZEntryPoint(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
        _startupBlockedUntil = DateTime.UtcNow + StartupGracePeriod;
    }

    public void Run()
    {
        _libraryManager.ItemAdded += OnItemAdded;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _libraryManager.ItemAdded -= OnItemAdded;
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        if (_disposed) return;
        if (DateTime.UtcNow < _startupBlockedUntil) return;

        var plugin = Plugin.Instance;
        if (plugin is null) return;

        var options = plugin.CurrentOptions;
        if (!options.Enabled || options.ManualTargetOnlyMode) return;

        // 仅处理影片和剧集；Season 通过 Episode 的 Series 路径间接覆盖
        if (e.Item is not Movie && e.Item is not Episode) return;

        var folderPath = ResolveFolderPath(e.Item);
        if (string.IsNullOrWhiteSpace(folderPath)) return;

        if (!TryAcquireFolderLock(folderPath!)) return;

        var orchestrator = new TranslationOrchestrator(
            options,
            new TranslationExecutionPolicy(options),
            ManualTranslationService.Dispatcher,
            new TranslationProfileResolver());

        orchestrator.TryRunForLibraryIngestAsync(
            new[] { folderPath! },
            CancellationToken.None).ContinueWith(
            static t =>
            {
                if (!t.IsFaulted || t.Exception is null) return;
                var ex = t.Exception.GetBaseException() ?? t.Exception;
                Plugin.LogErrorException("Library ingest hook failed.", ex);
                InMemoryTranslationJobDispatcher.AppendRuntimeLog("Error",
                    $"Library ingest hook failed: {ex.Message}");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static string? ResolveFolderPath(BaseItem item)
    {
        if (item is Episode)
            return item.ContainingFolderPath;

        if (item is Movie)
            return item.ContainingFolderPath;

        return null;
    }

    private bool TryAcquireFolderLock(string folderPath)
    {
        var now = DateTime.UtcNow;
        var accepted = false;

        _recentlyProcessedFolders.AddOrUpdate(
            folderPath,
            _ => { accepted = true; return now; },
            (_, existing) =>
            {
                if (now - existing >= DebounceWindow)
                {
                    accepted = true;
                    return now;
                }

                accepted = false;
                return existing;
            });

        if (!accepted) return false;

        // 惰性清理过期条目
        var cutoff = now - TimeSpan.FromHours(1);
        foreach (var key in _recentlyProcessedFolders.Keys)
        {
            if (_recentlyProcessedFolders.TryGetValue(key, out var ts) && ts < cutoff)
                _recentlyProcessedFolders.TryRemove(key, out _);
        }

        return true;
    }
}
