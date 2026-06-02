using SubZ.Plugin.Configuration;
using SubZ.Plugin.Services;

namespace SubZ.Plugin.Tests;

public sealed class SubtitleTrackSelectorTests
{
    [Theory]
    [InlineData("chi")]
    [InlineData("zho")]
    [InlineData("zh")]
    [InlineData("zh-CN")]
    [InlineData("cmn")]
    public void HasTargetTrackRecognizesChineseAliasesForZhCn(string language)
    {
        var tracks = new[]
        {
            new SubtitleTrackInfo
            {
                Id = 6,
                Codec = "subrip",
                Language = language,
                IsTextTrack = true
            }
        };

        var hasTarget = SubtitleTrackSelector.HasTargetTrack(tracks, new PluginOptions());

        Assert.True(hasTarget);
    }

    [Fact]
    public void HasTargetTrackRecognizesChineseImageSubtitleTracks()
    {
        var tracks = new[]
        {
            new SubtitleTrackInfo
            {
                Id = 2,
                Codec = "hdmv_pgs_subtitle",
                Language = "chi",
                IsTextTrack = false
            }
        };

        var hasTarget = SubtitleTrackSelector.HasTargetTrack(tracks, new PluginOptions());

        Assert.True(hasTarget);
    }

    [Fact]
    public void HasTargetTrackIgnoresNonTargetImageSubtitleTracks()
    {
        var tracks = new[]
        {
            new SubtitleTrackInfo
            {
                Id = 2,
                Codec = "hdmv_pgs_subtitle",
                Language = "eng",
                IsTextTrack = false
            }
        };

        var hasTarget = SubtitleTrackSelector.HasTargetTrack(tracks, new PluginOptions());

        Assert.False(hasTarget);
    }

    [Theory]
    [InlineData("zh-TW")]
    [InlineData("zh-Hant")]
    [InlineData("zh-HK")]
    public void HasTargetTrackDoesNotTreatExplicitTraditionalChineseAsZhCn(string language)
    {
        var tracks = new[]
        {
            new SubtitleTrackInfo
            {
                Id = 7,
                Codec = "subrip",
                Language = language,
                IsTextTrack = true
            }
        };

        var hasTarget = SubtitleTrackSelector.HasTargetTrack(tracks, new PluginOptions());

        Assert.False(hasTarget);
    }
}
