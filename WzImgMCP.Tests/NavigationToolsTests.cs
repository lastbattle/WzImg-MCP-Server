using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class NavigationToolsTests : IClassFixture<TestFixture>
{
    private readonly NavigationTools _tools;

    public NavigationToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new NavigationTools(fixture.Session);
    }

    [Fact]
    public void NavigationBasics_Work()
    {
        var subdirs = _tools.GetSubdirectories("Map");
        MarkdownTestHelper.AssertSuccess(subdirs);
        Assert.Contains("map0", subdirs, StringComparison.OrdinalIgnoreCase);

        var props = _tools.ListProperties("Character", "Test.img", limit: 5);
        MarkdownTestHelper.AssertSuccess(props);
        Assert.Contains("testString", props, StringComparison.OrdinalIgnoreCase);

        var tree = _tools.GetTreeStructure("Character", "Test.img", depth: 2, maxChildrenPerNode: 3);
        MarkdownTestHelper.AssertSuccess(tree);
        Assert.Contains("tree", tree, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchAndPath_Work()
    {
        var byName = _tools.SearchByName("test*", category: "Character", image: "Test.img", maxResults: 10, compact: true);
        MarkdownTestHelper.AssertSuccess(byName);
        Assert.Contains("path", byName, StringComparison.OrdinalIgnoreCase);

        var byValue = _tools.SearchByValue("Hello", category: "Character", image: "Test.img", maxResults: 10, compact: true);
        MarkdownTestHelper.AssertSuccess(byValue);
        Assert.Contains("matches", byValue, StringComparison.OrdinalIgnoreCase);

        var path = _tools.GetPropertyPath("Character", "Test.img", "info/name");
        MarkdownTestHelper.AssertSuccess(path);
        Assert.Contains("info/name", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidPath_Fails()
    {
        var result = _tools.GetPropertyPath("Character", "Test.img", "nonexistent/path");
        MarkdownTestHelper.AssertFailure(result);
    }
}
