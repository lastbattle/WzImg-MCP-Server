using WzImgMCP.Server;
using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class FileToolsTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly FileTools _tools;

    public FileToolsTests(TestFixture fixture)
    {
        _fixture = fixture;
        _tools = new FileTools(_fixture.Session);
    }

    [Fact]
    public void InitDataSource_WithValidPath_Succeeds()
    {
        var result = _tools.InitDataSource(_fixture.TestDataPath);
        MarkdownTestHelper.AssertSuccess(result);
        Assert.Contains(_fixture.TestDataPath, result);
        Assert.Contains("categories", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitDataSource_WithInvalidPath_Fails()
    {
        var result = _tools.InitDataSource("C:\\NonExistent\\Path\\12345");
        MarkdownTestHelper.AssertFailure(result);
        Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListAndCacheApis_ReturnExpectedKeys()
    {
        _fixture.InitializeDataSource();

        var categories = _tools.ListCategories();
        MarkdownTestHelper.AssertSuccess(categories);
        Assert.Contains("character", categories, StringComparison.OrdinalIgnoreCase);

        var images = _tools.ListImagesInCategory("Character");
        MarkdownTestHelper.AssertSuccess(images);
        Assert.Contains("test.img", images, StringComparison.OrdinalIgnoreCase);

        var cache = _tools.GetCacheStats();
        MarkdownTestHelper.AssertSuccess(cache);
        MarkdownTestHelper.AssertHasKey(cache, "cache_hit_ratio");

        var clear = _tools.ClearCache();
        MarkdownTestHelper.AssertSuccess(clear);
    }
}
