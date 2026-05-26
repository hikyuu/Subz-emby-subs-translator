namespace SubZ.Plugin.Tests;

public sealed class LocalizedTextEncodingTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static readonly string[] MojibakeFragments =
    [
        FromCodePoints(0x93C3),
        FromCodePoints(0x6D93),
        FromCodePoints(0x9803),
        FromCodePoints(0x83BD),
        FromCodePoints(0x5E3D),
        FromCodePoints(0x951A),
        FromCodePoints(0x88AA),
        FromCodePoints(0x6CBB),
        FromCodePoints(0x4E15),
        FromCodePoints(0x8CF1),
        FromCodePoints(0x5576),
        FromCodePoints(0x5594),
        FromCodePoints(0x5CB7),
        FromCodePoints(0x5CC4),
        "T" + FromCodePoints(0x7709),
        FromCodePoints(0x6D60),
        FromCodePoints(0x93AF),
        "\uFFFD"
    ];

    [Fact]
    public void SourceLanguageDescriptionsAreReadableUnicode()
    {
        var source = ReadRepositoryFile("src", "SubZ.Plugin", "Configuration", "SourceLanguageOption.cs");

        Assert.Contains("[Description(\"日本語\")]", source);
        Assert.Contains("[Description(\"中文（简体）\")]", source);
        Assert.Contains("[Description(\"中文（繁体）\")]", source);
        Assert.Contains("[Description(\"한국어\")]", source);
        Assert.Contains("[Description(\"Français\")]", source);
        Assert.Contains("[Description(\"Español\")]", source);
        Assert.Contains("[Description(\"Português\")]", source);
        Assert.Contains("[Description(\"Русский\")]", source);
        Assert.Contains("[Description(\"العربية\")]", source);
        Assert.Contains("[Description(\"हिन्दी\")]", source);
        Assert.Contains("[Description(\"ภาษาไทย\")]", source);
        Assert.Contains("[Description(\"Tiếng Việt\")]", source);
        Assert.Contains("[Description(\"Türkçe\")]", source);
    }

    [Fact]
    public void StatusDashboardChineseLanguageOptionIsReadableUnicode()
    {
        var source = ReadRepositoryFile("src", "SubZ.Plugin", "UI", "StatusDashboardV2.html");

        Assert.Contains("<option value=\"zh\">中文</option>", source);
    }

    [Fact]
    public void TextSourceFilesDoNotContainKnownMojibakeFragments()
    {
        var textFiles = EnumerateTextFiles("src", "tests", "docs", "scripts")
            .Concat(new[]
            {
                Path.Combine(RepositoryRoot, "README.md"),
                Path.Combine(RepositoryRoot, "README.zh-CN.md")
            })
            .Where(File.Exists)
            .ToArray();

        var hits = new List<string>();
        foreach (var file in textFiles)
        {
            var text = File.ReadAllText(file);
            foreach (var fragment in MojibakeFragments)
            {
                if (text.Contains(fragment, StringComparison.Ordinal))
                {
                    hits.Add($"{Path.GetRelativePath(RepositoryRoot, file)} contains {fragment}");
                }
            }
        }

        Assert.Empty(hits);
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(new[] { RepositoryRoot }.Concat(pathParts).ToArray()));
    }

    private static string FromCodePoints(params int[] codePoints)
    {
        return string.Concat(codePoints.Select(char.ConvertFromUtf32));
    }

    private static IEnumerable<string> EnumerateTextFiles(params string[] rootNames)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".html",
            ".js",
            ".md",
            ".ps1",
            ".csproj",
            ".json",
            ".txt"
        };

        foreach (var rootName in rootNames)
        {
            var root = Path.Combine(RepositoryRoot, rootName);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(RepositoryRoot, file);
                if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (allowedExtensions.Contains(Path.GetExtension(file)))
                {
                    yield return file;
                }
            }
        }
    }
}
