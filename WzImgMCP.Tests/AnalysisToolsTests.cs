using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class AnalysisToolsTests : IClassFixture<TestFixture>
{
    private readonly AnalysisTools _tools;

    public AnalysisToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new AnalysisTools(fixture.Session);
    }

    [Fact]
    public void StatisticsAndSummary_Work()
    {
        var stats = _tools.GetStatistics();
        MarkdownTestHelper.AssertSuccess(stats);
        Assert.Contains("category_count", stats, StringComparison.OrdinalIgnoreCase);

        var summary = _tools.GetCategorySummary("Character");
        MarkdownTestHelper.AssertSuccess(summary);
        Assert.Contains("image_count", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VersionAndValidation_Work()
    {
        var version = _tools.GetVersionInfo();
        MarkdownTestHelper.AssertSuccess(version);
        Assert.Contains("version", version, StringComparison.OrdinalIgnoreCase);

        var validation = _tools.ValidateImage("Character", "Test.img");
        MarkdownTestHelper.AssertSuccess(validation);
        Assert.Contains("issue_count", validation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompareAndBrokenUol_Work()
    {
        var compare = _tools.CompareProperties("Character", "Test.img", "info", "Character", "Test.img", "info", 3);
        MarkdownTestHelper.AssertSuccess(compare);

        var broken = _tools.FindBrokenUols("Character", "Test.img", 20);
        MarkdownTestHelper.AssertSuccess(broken);
    }
}
