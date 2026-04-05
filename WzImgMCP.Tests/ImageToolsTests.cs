using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class ImageToolsTests : IClassFixture<TestFixture>
{
    private readonly ImageTools _tools;

    public ImageToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new ImageTools(fixture.Session);
    }

    [Fact]
    public void CanvasMetadata_Work()
    {
        var info = _tools.GetCanvasInfo("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(info);
        Assert.Contains("width", info, StringComparison.OrdinalIgnoreCase);

        var origin = _tools.GetCanvasOrigin("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(origin);
        Assert.Contains("- x: 16", origin, StringComparison.OrdinalIgnoreCase);

        var delay = _tools.GetCanvasDelay("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(delay);
    }

    [Fact]
    public void AnimationAndCanvasList_Work()
    {
        var framesMeta = _tools.GetAnimationFrames("Character", "Test.img", "stand", metadataOnly: true, limit: 2);
        MarkdownTestHelper.AssertSuccess(framesMeta);
        Assert.DoesNotContain("base64_png:", framesMeta, StringComparison.OrdinalIgnoreCase);

        var framesFull = _tools.GetAnimationFrames("Character", "Test.img", "stand", metadataOnly: false, limit: 1);
        MarkdownTestHelper.AssertSuccess(framesFull);
        Assert.Contains("base64_png", framesFull, StringComparison.OrdinalIgnoreCase);

        var list = _tools.ListCanvasInImage("Character", "Test.img", maxDepth: 4);
        MarkdownTestHelper.AssertSuccess(list);
        Assert.Contains("testCanvas", list, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BitmapAndBounds_Work()
    {
        var bmp = _tools.GetCanvasBitmap("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(bmp);
        Assert.Contains("base64_png", bmp, StringComparison.OrdinalIgnoreCase);

        var bounds = _tools.GetCanvasBounds("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(bounds);

        var resolved = _tools.ResolveCanvasLink("Character", "Test.img", "testCanvas");
        MarkdownTestHelper.AssertSuccess(resolved);
    }
}
