using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class LifecycleToolsTests : IClassFixture<TestFixture>
{
    private readonly LifecycleTools _tools;

    public LifecycleToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new LifecycleTools(fixture.Session);
    }

    [Fact]
    public void ParseAndIsParsed_Work()
    {
        var parse = _tools.ParseImage("Character", "Test.img");
        MarkdownTestHelper.AssertSuccess(parse);
        Assert.Contains("property_count", parse, StringComparison.OrdinalIgnoreCase);

        var state = _tools.IsImageParsed("Character", "Test.img");
        MarkdownTestHelper.AssertSuccess(state);
        Assert.Contains("is_parsed: true", state, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParsedListAndUnparse_Work()
    {
        _tools.ParseImage("Character", "Test.img");

        var parsed = _tools.GetParsedImages();
        MarkdownTestHelper.AssertSuccess(parsed);
        Assert.Contains("test.img", parsed, StringComparison.OrdinalIgnoreCase);

        var unparse = _tools.UnparseImage("Character", "Test.img");
        MarkdownTestHelper.AssertSuccess(unparse);
    }

    [Fact]
    public void CategoryPreloadUnload_Work()
    {
        var preload = _tools.PreloadCategory("Character");
        MarkdownTestHelper.AssertSuccess(preload);

        var unload = _tools.UnloadCategory("Character");
        MarkdownTestHelper.AssertSuccess(unload);
    }
}
