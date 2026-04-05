using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class ModifyToolsTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly ModifyTools _tools;

    public ModifyToolsTests(TestFixture fixture)
    {
        _fixture = fixture;
        fixture.InitializeDataSource();
        _tools = new ModifyTools(fixture.Session);
    }

    [Fact]
    public void ScalarUpdates_Work()
    {
        MarkdownTestHelper.AssertSuccess(_tools.SetString("Character", "Test.img", "testString", "Updated"));
        MarkdownTestHelper.AssertSuccess(_tools.SetInt("Character", "Test.img", "testInt", 99));
        MarkdownTestHelper.AssertSuccess(_tools.SetFloat("Character", "Test.img", "testFloat", 7.5f));
        MarkdownTestHelper.AssertSuccess(_tools.SetVector("Character", "Test.img", "testVector", 1, 2));
    }

    [Fact]
    public void PropertyStructureOps_Work()
    {
        var add = _tools.AddProperty("Character", "Test.img", "info", "newProp", "String", "abc");
        MarkdownTestHelper.AssertSuccess(add);

        var rename = _tools.RenameProperty("Character", "Test.img", "info/newProp", "renamedProp");
        MarkdownTestHelper.AssertSuccess(rename);

        var copy = _tools.CopyProperty("Character", "Test.img", "info/renamedProp", "Character", "Test.img", "");
        MarkdownTestHelper.AssertSuccess(copy);

        var delete = _tools.DeleteProperty("Character", "Test.img", "info/renamedProp");
        MarkdownTestHelper.AssertSuccess(delete);
    }

    [Fact]
    public void CanvasAndPersistenceOps_Work()
    {
        var onePxPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7+J1gAAAAASUVORK5CYII=";

        var setBmp = _tools.SetCanvasBitmap("Character", "Test.img", "testCanvas", onePxPngBase64);
        if (!setBmp.Contains("success: true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("error", setBmp, StringComparison.OrdinalIgnoreCase);
            return;
        }

        var setOrigin = _tools.SetCanvasOrigin("Character", "Test.img", "testCanvas", 5, 6);
        MarkdownTestHelper.AssertSuccess(setOrigin);

        var importPng = _tools.ImportPng("Character", "Test.img", "", "importedCanvas", onePxPngBase64, 0, 0);
        MarkdownTestHelper.AssertSuccess(importPng);

        var save = _tools.SaveImage("Character", "Test.img");
        MarkdownTestHelper.AssertSuccess(save);

        var discard = _tools.DiscardChanges("Character", "Test.img");
        Assert.True(
            discard.Contains("success: true", StringComparison.OrdinalIgnoreCase)
            || discard.Contains("error", StringComparison.OrdinalIgnoreCase));
    }
}
