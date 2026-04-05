using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class PropertyToolsTests : IClassFixture<TestFixture>
{
    private readonly PropertyTools _tools;

    public PropertyToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new PropertyTools(fixture.Session);
    }

    [Fact]
    public void PropertyReads_Work()
    {
        var p = _tools.GetProperty("Character", "Test.img", "testString");
        MarkdownTestHelper.AssertSuccess(p);
        Assert.Contains("hello world", p, StringComparison.OrdinalIgnoreCase);

        var v = _tools.GetPropertyValue("Character", "Test.img", "testInt");
        MarkdownTestHelper.AssertSuccess(v);
        Assert.Contains("42", v, StringComparison.OrdinalIgnoreCase);

        var s = _tools.GetString("Character", "Test.img", "testString");
        MarkdownTestHelper.AssertSuccess(s);
        Assert.Contains("found: true", s, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TypedAndChildren_Work()
    {
        var i = _tools.GetInt("Character", "Test.img", "testInt");
        MarkdownTestHelper.AssertSuccess(i);

        var f = _tools.GetFloat("Character", "Test.img", "testFloat");
        MarkdownTestHelper.AssertSuccess(f);

        var vec = _tools.GetVector("Character", "Test.img", "testVector");
        MarkdownTestHelper.AssertSuccess(vec);
        Assert.Contains("- x: 100", vec, StringComparison.OrdinalIgnoreCase);

        var children = _tools.GetChildren("Character", "Test.img", "info", compact: true, limit: 10);
        MarkdownTestHelper.AssertSuccess(children);
        Assert.Contains("name", children, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UolAndBatch_Work()
    {
        var uol = _tools.ResolveUol("Character", "Test.img", "testUol");
        MarkdownTestHelper.AssertSuccess(uol);
        Assert.Contains("target_path", uol, StringComparison.OrdinalIgnoreCase);

        var batch = _tools.GetPropertiesBatch(new List<PropertyRequest>
        {
            new() { Category = "Character", Image = "Test.img", Path = "testString" },
            new() { Category = "Character", Image = "Test.img", Path = "testInt" }
        });
        MarkdownTestHelper.AssertSuccess(batch);
        Assert.Contains("success_count", batch, StringComparison.OrdinalIgnoreCase);
    }
}
