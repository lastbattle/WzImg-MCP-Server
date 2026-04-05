using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class BatchToolsTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly BatchTools _tools;

    public BatchToolsTests(TestFixture fixture)
    {
        _fixture = fixture;
        fixture.InitializeDataSource();
        _tools = new BatchTools(fixture.Session);
    }

    [Fact]
    public void ExtractAndPack_CurrentlyNotImplemented_ReturnFailures()
    {
        var extract = _tools.ExtractToImg(_fixture.TestDataPath, Path.Combine(Path.GetTempPath(), "wz_extract_" + Guid.NewGuid().ToString("N")));
        MarkdownTestHelper.AssertFailure(extract);

        var pack = _tools.PackToWz(_fixture.TestDataPath, Path.Combine(Path.GetTempPath(), "wz_pack_" + Guid.NewGuid().ToString("N")));
        MarkdownTestHelper.AssertFailure(pack);
    }

    [Fact]
    public void BatchSearchAndExport_Work()
    {
        var search = _tools.BatchSearch("test*", categories: "Character", searchType: "name", maxResults: 20);
        MarkdownTestHelper.AssertSuccess(search);
        Assert.Contains("result_count", search, StringComparison.OrdinalIgnoreCase);

        var outDir = Path.Combine(Path.GetTempPath(), "wz_batch_export_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var export = _tools.BatchExportImages("Character", outDir, format: "png", maxImages: 5);
            MarkdownTestHelper.AssertSuccess(export);
            Assert.Contains("exported_count", export, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(outDir, true);
        }
    }
}
