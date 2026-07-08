using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class ExportToolsTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly ExportTools _tools;

    public ExportToolsTests(TestFixture fixture)
    {
        _fixture = fixture;
        fixture.InitializeDataSource();
        _tools = new ExportTools(fixture.Session);
    }

    [Fact]
    public void ExportToMd_Inline_ReturnsMarkdown()
    {
        var result = _tools.ExportToMd("Character", "Test.img", maxDepth: 3);
        MarkdownTestHelper.AssertSuccess(result);
        Assert.Contains("node_count", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("markdown_data", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToMd_OutputPath_WritesMarkdownFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wzimgmcp_exporttests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var requestedPath = Path.Combine(dir, "out.txt");
            var result = _tools.ExportToMd("Character", "Test.img", outputPath: requestedPath);
            var actualPath = Path.Combine(dir, "out.md");

            MarkdownTestHelper.AssertSuccess(result);
            Assert.Contains("out.md", result, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(requestedPath));
            Assert.True(File.Exists(actualPath));
            Assert.Contains("- _name: Test.img", File.ReadAllText(actualPath), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ExportMediaFiles_Work()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wzimgmcp_exporttests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var pngPath = Path.Combine(dir, "out.png");
            var png = _tools.ExportPng("Character", "Test.img", "testCanvas", pngPath);
            MarkdownTestHelper.AssertSuccess(png);
            Assert.True(File.Exists(pngPath));

            var mp3Path = Path.Combine(dir, "out.mp3");
            var mp3 = _tools.ExportMp3("Sound", "TestSound.img", "soundInfo", mp3Path);
            MarkdownTestHelper.AssertFailure(mp3);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
