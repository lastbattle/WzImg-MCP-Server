using System.Text.RegularExpressions;
using WzImgMCP.Server;
using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class IntegrationTests : IDisposable
{
    private static readonly string? RealDataPath = Environment.GetEnvironmentVariable("WZIMGMCP_TEST_DATA_PATH");
    private readonly WzSessionManager _session;
    private readonly bool _dataExists;
    private string? _testMobImage;
    private string? _testMobAnimation;

    public IntegrationTests()
    {
        _dataExists = !string.IsNullOrEmpty(RealDataPath)
            && Directory.Exists(RealDataPath)
            && File.Exists(Path.Combine(RealDataPath, "manifest.json"));
        _session = new WzSessionManager();

        if (_dataExists)
        {
            _session.InitDataSource(RealDataPath!);
            DiscoverMobAnimation();
        }
    }

    private void DiscoverMobAnimation()
    {
        var nav = new NavigationTools(_session);
        var result = nav.SearchByName("stand", category: "Mob", maxResults: 1, compact: false);
        if (!result.Contains("- success: true", StringComparison.OrdinalIgnoreCase)) return;

        var image = MarkdownTestHelper.GetFirstValue(result, "image");
        var path = MarkdownTestHelper.GetFirstValue(result, "path");
        if (!string.IsNullOrWhiteSpace(image) && !string.IsNullOrWhiteSpace(path))
        {
            _testMobImage = image;
            _testMobAnimation = path;
        }
    }

    public void Dispose() => _session.Dispose();

    [SkippableFact]
    public void RealData_BasicOperations_Work()
    {
        Skip.IfNot(_dataExists, "Real data path not available");

        var file = new FileTools(_session);
        var info = file.GetDataSourceInfo();
        MarkdownTestHelper.AssertSuccess(info);
        Assert.Contains("category_count", info, StringComparison.OrdinalIgnoreCase);

        var cats = file.ListCategories();
        MarkdownTestHelper.AssertSuccess(cats);
        Assert.Contains("mob", cats, StringComparison.OrdinalIgnoreCase);

        var mobList = file.ListImagesInCategory("Mob");
        MarkdownTestHelper.AssertSuccess(mobList);
        Assert.Contains(".img", mobList, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public void RealData_AnimationAndSearch_Work()
    {
        Skip.IfNot(_dataExists, "Real data path not available");
        Skip.If(string.IsNullOrEmpty(_testMobImage) || string.IsNullOrEmpty(_testMobAnimation), "No mob animation found");

        var image = new ImageTools(_session);
        var nav = new NavigationTools(_session);

        var framesMeta = image.GetAnimationFrames("Mob", _testMobImage!, _testMobAnimation!, metadataOnly: true, limit: 3);
        MarkdownTestHelper.AssertSuccess(framesMeta);

        var framesFull = image.GetAnimationFrames("Mob", _testMobImage!, _testMobAnimation!, metadataOnly: false, limit: 3);
        MarkdownTestHelper.AssertSuccess(framesFull);

        Assert.True(framesMeta.Length < framesFull.Length);

        var search = nav.SearchByName("*", category: "Mob", maxResults: 5, compact: true);
        MarkdownTestHelper.AssertSuccess(search);
        Assert.Contains("matches", search, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public void RealData_ExportInlineLimit_Behaves()
    {
        Skip.IfNot(_dataExists, "Real data path not available");
        Skip.If(string.IsNullOrEmpty(_testMobImage), "No mob found");

        var export = new ExportTools(_session);
        var result = export.ExportToJson("Mob", _testMobImage!, maxDepth: 10);

        if (result.Contains("- success: false", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("100KB", result, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            MarkdownTestHelper.AssertSuccess(result);
            Assert.Contains("node_count", result, StringComparison.OrdinalIgnoreCase);
        }
    }
}
