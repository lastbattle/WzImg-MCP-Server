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
    public void ExportToJson_Inline_Works()
    {
        var result = _tools.ExportToJson("Character", "Test.img", maxDepth: 3);
        MarkdownTestHelper.AssertSuccess(result);
        Assert.Contains("node_count", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToXmlAndMediaFiles_Work()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wzimgmcp_exporttests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var xmlPath = Path.Combine(dir, "out.xml");
            var xml = _tools.ExportToXml("Character", "Test.img", xmlPath);
            MarkdownTestHelper.AssertSuccess(xml);
            Assert.True(File.Exists(xmlPath));

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
