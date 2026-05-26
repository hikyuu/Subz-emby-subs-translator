using SubZ.Plugin.Configuration;
using SubZ.Plugin.Services;

namespace SubZ.Plugin.Tests;

public sealed class FfmpegToolPathResolverTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "subz_ffmpeg_tests_" + Guid.NewGuid().ToString("N"));

    public FfmpegToolPathResolverTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ResolveUsesExistingManualPathsBeforeEmbyPaths()
    {
        var manualFfmpeg = Touch("manual-ffmpeg.exe");
        var manualFfprobe = Touch("manual-ffprobe.exe");
        var embyFfmpeg = Touch("emby-ffmpeg.exe");
        var embyFfprobe = Touch("emby-ffprobe.exe");

        var paths = FfmpegToolPathResolver.Resolve(
            new PluginOptions
            {
                FfmpegPath = manualFfmpeg,
                FfprobePath = manualFfprobe
            },
            new FakeProvider(
                manager: new FakeFfmpegManager(new FakeFfmpegConfiguration(embyFfmpeg, embyFfprobe)),
                configuration: null));

        Assert.Equal(manualFfmpeg, paths.FfmpegPath);
        Assert.Equal(manualFfprobe, paths.FfprobePath);
        Assert.Equal("manual", paths.FfmpegSource);
        Assert.Equal("manual", paths.FfprobeSource);
    }

    [Fact]
    public void ResolveUsesManagerConfigurationBeforeDirectConfiguration()
    {
        var managerFfmpeg = Touch("manager-ffmpeg.exe");
        var managerFfprobe = Touch("manager-ffprobe.exe");
        var directFfmpeg = Touch("direct-ffmpeg.exe");
        var directFfprobe = Touch("direct-ffprobe.exe");

        var paths = FfmpegToolPathResolver.Resolve(
            new PluginOptions(),
            new FakeProvider(
                manager: new FakeFfmpegManager(new FakeFfmpegConfiguration(managerFfmpeg, managerFfprobe)),
                configuration: new FakeFfmpegConfiguration(directFfmpeg, directFfprobe)));

        Assert.Equal(managerFfmpeg, paths.FfmpegPath);
        Assert.Equal(managerFfprobe, paths.FfprobePath);
        Assert.Equal("emby-manager", paths.FfmpegSource);
        Assert.Equal("emby-manager", paths.FfprobeSource);
    }

    [Fact]
    public void ResolveUsesDirectConfigurationWhenManagerIsUnavailable()
    {
        var directFfmpeg = Touch("direct-ffmpeg.exe");
        var directFfprobe = Touch("direct-ffprobe.exe");

        var paths = FfmpegToolPathResolver.Resolve(
            new PluginOptions(),
            new FakeProvider(
                manager: null,
                configuration: new FakeFfmpegConfiguration(directFfmpeg, directFfprobe)));

        Assert.Equal(directFfmpeg, paths.FfmpegPath);
        Assert.Equal(directFfprobe, paths.FfprobePath);
        Assert.Equal("emby-config", paths.FfmpegSource);
        Assert.Equal("emby-config", paths.FfprobeSource);
    }

    [Fact]
    public void ResolveFallsBackToBinPathsWhenNothingUsableIsAvailable()
    {
        var paths = FfmpegToolPathResolver.Resolve(
            new PluginOptions
            {
                FfmpegPath = Path.Combine(_tempDir, "missing-ffmpeg.exe"),
                FfprobePath = Path.Combine(_tempDir, "missing-ffprobe.exe")
            },
            new FakeProvider(
                manager: new FakeFfmpegManager(new FakeFfmpegConfiguration("missing-manager-ffmpeg", "missing-manager-ffprobe")),
                configuration: new FakeFfmpegConfiguration("missing-direct-ffmpeg", "missing-direct-ffprobe")));

        Assert.Equal("/bin/ffmpeg", paths.FfmpegPath);
        Assert.Equal("/bin/ffprobe", paths.FfprobePath);
        Assert.Equal("fallback", paths.FfmpegSource);
        Assert.Equal("fallback", paths.FfprobeSource);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string Touch(string fileName)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class FakeProvider : IFfmpegToolPathProvider
    {
        private readonly object? _manager;
        private readonly object? _configuration;

        public FakeProvider(object? manager, object? configuration)
        {
            _manager = manager;
            _configuration = configuration;
        }

        public object? ResolveFfmpegManager()
        {
            return _manager;
        }

        public object? ResolveFfmpegConfiguration()
        {
            return _configuration;
        }
    }

    private sealed class FakeFfmpegManager
    {
        public FakeFfmpegManager(object? configuration)
        {
            IFfmpegConfiguration = configuration;
        }

        public object? IFfmpegConfiguration { get; }
    }

    private sealed class FakeFfmpegConfiguration
    {
        public FakeFfmpegConfiguration(string encoderPath, string probePath)
        {
            EncoderPath = encoderPath;
            ProbePath = probePath;
        }

        public string EncoderPath { get; }
        public string ProbePath { get; }
    }
}
